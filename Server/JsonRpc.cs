using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace McpPlugin.Server
{
    public static class JsonRpc
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public static RpcRequest Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new McpException(-32700, "Empty request");

            try
            {
                var obj = Serializer.Deserialize<Dictionary<string, object>>(json);

                if (obj == null || !obj.TryGetValue("method", out var methodObj))
                    throw new McpException(-32600, "Invalid request: missing method");

                var request = new RpcRequest
                {
                    Method = methodObj?.ToString(),
                    Id = obj.TryGetValue("id", out var id) ? id : null
                };

                if (obj.TryGetValue("params", out var paramsObj) && paramsObj is Dictionary<string, object> dict)
                    request.Params = new JsonObject(dict);

                return request;
            }
            catch (McpException) { throw; }
            catch (Exception ex)
            {
                throw new McpException(-32700, $"Parse error: {ex.Message}");
            }
        }

        public static string Success(object id, object result)
        {
            return Serializer.Serialize(new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = result
            });
        }

        public static string Error(object id, int code, string message)
        {
            return Serializer.Serialize(new Dictionary<string, object>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["error"] = new Dictionary<string, object>
                {
                    ["code"] = code,
                    ["message"] = message
                }
            });
        }

        public static string Serialize(object obj) => Serializer.Serialize(obj);

        public static T Deserialize<T>(string json) => Serializer.Deserialize<T>(json);
    }

    public class RpcRequest
    {
        public object Id { get; set; }
        public string Method { get; set; }
        public JsonObject Params { get; set; }
    }

    public class JsonObject
    {
        private readonly Dictionary<string, object> _data;

        public JsonObject() => _data = new Dictionary<string, object>();
        public JsonObject(Dictionary<string, object> data) => _data = data ?? new Dictionary<string, object>();

        public string GetString(string key)
        {
            return _data.TryGetValue(key, out var val) ? val?.ToString() : null;
        }

        public int? GetInt(string key)
        {
            if (!_data.TryGetValue(key, out var val)) return null;
            if (val is int i) return i;
            if (int.TryParse(val?.ToString(), out var parsed)) return parsed;
            return null;
        }

        public long? GetLong(string key)
        {
            if (!_data.TryGetValue(key, out var val)) return null;
            if (val is long l) return l;
            if (val is int i) return i;
            if (long.TryParse(val?.ToString(), out var parsed)) return parsed;
            return null;
        }

        public bool? GetBool(string key)
        {
            if (!_data.TryGetValue(key, out var val)) return null;
            if (val is bool b) return b;
            if (bool.TryParse(val?.ToString(), out var parsed)) return parsed;
            return null;
        }

        public double? GetDouble(string key)
        {
            if (!_data.TryGetValue(key, out var val)) return null;
            if (val is double d) return d;
            if (val is int i) return i;
            if (val is long l) return l;
            if (double.TryParse(val?.ToString(), out var parsed)) return parsed;
            return null;
        }

        public JsonObject GetObject(string key)
        {
            if (!_data.TryGetValue(key, out var val)) return null;
            if (val is Dictionary<string, object> dict) return new JsonObject(dict);
            return null;
        }

        public object Get(string key) => _data.TryGetValue(key, out var val) ? val : null;

        public Dictionary<string, object> ToDictionary() => new Dictionary<string, object>(_data);
    }
}
