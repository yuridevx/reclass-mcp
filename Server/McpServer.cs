using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ReClassNET.Plugins;

namespace McpPlugin.Server
{
    public class McpServer : IDisposable
    {
        private readonly IPluginHost _host;
        private readonly McpToolRegistry _registry;
        private readonly SynchronizationContext _uiContext;

        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _listenerTask;

        public string Host { get; }
        public int Port { get; }
        public bool IsRunning { get; private set; }

        private const string ProtocolVersion = "2024-11-05";
        private const string ServerName = "reclass-net";
        private const string ServerVersion = "1.0.0";

        public McpServer(IPluginHost host, int port = 13338, string hostAddress = "127.0.0.1")
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _registry = new McpToolRegistry();
            _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
            Host = hostAddress;
            Port = port;
        }

        public void RegisterApi(object api) => _registry.RegisterApi(api);

        public void Start()
        {
            if (IsRunning) return;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{Host}:{Port}/");
            _listener.Start();

            _cts = new CancellationTokenSource();
            _listenerTask = Task.Run(() => ListenAsync(_cts.Token));

            IsRunning = true;
            Log($"MCP Server started on http://{Host}:{Port}/");
        }

        public void Stop()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            _listener?.Stop();
            _listenerTask?.Wait(3000);

            IsRunning = false;
            Log("MCP Server stopped.");
        }

        public void Dispose()
        {
            Stop();
            _listener?.Close();
            _cts?.Dispose();
        }

        private async Task ListenAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context, ct), ct);
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { Log($"Listener error: {ex.Message}"); }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
        {
            var request = context.Request;
            var response = context.Response;

            SetCorsHeaders(response);

            try
            {
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }

                var path = request.Url.AbsolutePath.TrimEnd('/');

                switch (path)
                {
                    case "/sse":
                        await HandleSseAsync(response, ct);
                        break;

                    case "/mcp":
                    case "/message":
                    case "":
                        await HandleMessageAsync(request, response);
                        break;

                    default:
                        SendError(response, 404, "Not found");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"Request error: {ex.Message}");
                SendError(response, 500, ex.Message);
            }
        }

        private async Task HandleSseAsync(HttpListenerResponse response, CancellationToken ct)
        {
            Log("SSE connection received");
            response.ContentType = "text/event-stream; charset=utf-8";
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["Connection"] = "keep-alive";
            response.Headers["X-Accel-Buffering"] = "no";

            using (var writer = new StreamWriter(response.OutputStream, new UTF8Encoding(false)) { AutoFlush = false })
            {
                // Send endpoint event as single write for compatibility
                var endpointUrl = $"http://{Host}:{Port}/message";
                Log($"Sending endpoint event: {endpointUrl}");
                var eventData = $"event: endpoint\r\ndata: {endpointUrl}\r\n\r\n";
                await writer.WriteAsync(eventData);
                await writer.FlushAsync();
                Log("Endpoint event sent");

                // Keep alive
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(30000, ct);
                    await writer.WriteAsync(": ping\r\n");
                    await writer.WriteAsync("\r\n");
                    await writer.FlushAsync();
                }
            }
        }

        private async Task HandleMessageAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            Log($"Message request received: {request.HttpMethod}");
            if (request.HttpMethod != "POST")
            {
                SendError(response, 405, "Method not allowed");
                return;
            }

            string body;
            using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            var result = ProcessRequest(body);
            SendJson(response, 200, result);
        }

        private string ProcessRequest(string body)
        {
            try
            {
                var request = JsonRpc.Parse(body);
                var result = ExecuteMethod(request.Method, request.Params);
                return JsonRpc.Success(request.Id, result);
            }
            catch (McpException ex)
            {
                return JsonRpc.Error(null, ex.Code, ex.Message);
            }
            catch (Exception ex)
            {
                return JsonRpc.Error(null, -32603, ex.Message);
            }
        }

        private object ExecuteMethod(string method, JsonObject args)
        {
            switch (method)
            {
                case "initialize":
                    return new
                    {
                        protocolVersion = ProtocolVersion,
                        capabilities = new { tools = new { listChanged = false } },
                        serverInfo = new { name = ServerName, version = ServerVersion }
                    };

                case "initialized":
                    return new { };

                case "ping":
                    return new { };

                case "tools/list":
                    return _registry.ListTools();

                case "tools/call":
                    return ExecuteToolCall(args);

                default:
                    throw new McpException(-32601, $"Unknown method: {method}");
            }
        }

        private object ExecuteToolCall(JsonObject args)
        {
            var toolName = args?.GetString("name");
            if (string.IsNullOrEmpty(toolName))
                throw new McpException(-32602, "Missing tool name");

            var tool = _registry.GetTool(toolName);
            if (tool == null)
                throw new McpException(-32601, $"Unknown tool: {toolName}");

            var toolArgs = args.GetObject("arguments");

            // Execute on UI thread
            object result = null;
            Exception error = null;

            _uiContext.Send(_ =>
            {
                try { result = tool.Invoke(toolArgs); }
                catch (Exception ex) { error = ex; }
            }, null);

            if (error != null)
                throw new McpException(-32603, error.Message);

            return new
            {
                content = new[] { new { type = "text", text = JsonRpc.Serialize(result) } },
                isError = false
            };
        }

        private void SetCorsHeaders(HttpListenerResponse response)
        {
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        }

        private void SendJson(HttpListenerResponse response, int status, string json)
        {
            response.StatusCode = status;
            response.ContentType = "application/json";
            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private void SendError(HttpListenerResponse response, int status, string message)
        {
            SendJson(response, status, JsonRpc.Error(null, -32600, message));
        }

        private void Log(string message)
        {
            _host.Logger.Log(ReClassNET.Logger.LogLevel.Information, $"[MCP] {message}");
        }
    }

    public class McpException : Exception
    {
        public int Code { get; }
        public McpException(int code, string message) : base(message) => Code = code;
    }
}
