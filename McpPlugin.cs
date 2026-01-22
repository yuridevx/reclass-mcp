using System;
using System.Drawing;
using McpPlugin.Api;
using McpPlugin.Server;
using ReClassNET.Plugins;

namespace McpPlugin
{
    /// <summary>
    /// ReClass.NET MCP Server Plugin
    /// Exposes ReClass.NET functionality through the Model Context Protocol (MCP).
    /// </summary>
    public class McpPluginExt : Plugin
    {
        private McpServer _server;
        private IPluginHost _host;

        public const string PluginName = "MCP Server";
        public const string PluginVersion = "1.0.0";
        public const string PluginAuthor = "ReClass.NET Community";
        public const int DefaultPort = 13338;

        public override Image Icon => null;

        /// <summary>
        /// Initializes the MCP Server plugin.
        /// </summary>
        public override bool Initialize(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));

            try
            {
                // Create and configure the MCP server
                _server = new McpServer(host, DefaultPort);

                // Register all API modules
                RegisterApis();

                // Start the server
                _server.Start();

                _host.Logger.Log(ReClassNET.Logger.LogLevel.Information,
                    $"{PluginName} v{PluginVersion} initialized. Server running on port {DefaultPort}.");

                return true;
            }
            catch (Exception ex)
            {
                _host.Logger.Log(ReClassNET.Logger.LogLevel.Error,
                    $"Failed to initialize {PluginName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Terminates the MCP Server plugin.
        /// </summary>
        public override void Terminate()
        {
            try
            {
                _server?.Stop();
                _server?.Dispose();
                _server = null;

                _host?.Logger.Log(ReClassNET.Logger.LogLevel.Information,
                    $"{PluginName} terminated.");
            }
            catch (Exception ex)
            {
                _host?.Logger.Log(ReClassNET.Logger.LogLevel.Warning,
                    $"Error terminating {PluginName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Registers all API modules with the MCP server.
        /// </summary>
        private void RegisterApis()
        {
            // Process management
            _server.RegisterApi(new ProcessApi(_host));

            // Memory operations
            _server.RegisterApi(new MemoryApi(_host));

            // Project management
            _server.RegisterApi(new ProjectApi(_host));

            // Class/structure management
            _server.RegisterApi(new ClassApi(_host));

            // Node operations
            _server.RegisterApi(new NodeApi(_host));

            // Code generation
            _server.RegisterApi(new CodeGenApi(_host));

            // Memory scanner
            _server.RegisterApi(new ScannerApi(_host));

            // Disassembler
            _server.RegisterApi(new DisassemblerApi(_host));

            // Enum management
            _server.RegisterApi(new EnumApi(_host));
        }
    }
}
