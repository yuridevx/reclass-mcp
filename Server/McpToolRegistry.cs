using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace McpPlugin.Server
{
    public class McpToolRegistry
    {
        private readonly Dictionary<string, McpTool> _tools = new Dictionary<string, McpTool>(StringComparer.OrdinalIgnoreCase);

        public void RegisterApi(object api)
        {
            if (api == null) return;

            foreach (var method in api.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = method.GetCustomAttribute<McpToolAttribute>();
                if (attr == null) continue;

                var tool = new McpTool(attr.Name ?? method.Name.ToLowerInvariant(), attr.Description ?? "", method, api);
                _tools[tool.Name] = tool;
            }
        }

        public McpTool GetTool(string name) => _tools.TryGetValue(name, out var tool) ? tool : null;

        public object ListTools()
        {
            return new
            {
                tools = _tools.Values.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    inputSchema = new
                    {
                        type = "object",
                        properties = t.GetProperties(),
                        required = t.GetRequired()
                    }
                }).ToArray()
            };
        }
    }

    public class McpTool
    {
        public string Name { get; }
        public string Description { get; }

        private readonly MethodInfo _method;
        private readonly object _instance;
        private readonly ParameterInfo[] _params;

        public McpTool(string name, string description, MethodInfo method, object instance)
        {
            Name = name;
            Description = description;
            _method = method;
            _instance = instance;
            _params = method.GetParameters();
        }

        public object Invoke(JsonObject args)
        {
            var values = new object[_params.Length];

            for (int i = 0; i < _params.Length; i++)
            {
                var p = _params[i];
                var value = args?.Get(p.Name);

                if (value != null)
                    values[i] = Convert(value, p.ParameterType);
                else if (p.HasDefaultValue)
                    values[i] = p.DefaultValue;
                else if (!p.ParameterType.IsValueType || Nullable.GetUnderlyingType(p.ParameterType) != null)
                    values[i] = null;
                else
                    throw new McpException(-32602, $"Missing required parameter: {p.Name}");
            }

            return _method.Invoke(_instance, values);
        }

        public Dictionary<string, object> GetProperties()
        {
            return _params.ToDictionary(
                p => p.Name,
                p => (object)new { type = JsonType(p.ParameterType), description = p.Name }
            );
        }

        public string[] GetRequired()
        {
            return _params.Where(p => !p.HasDefaultValue).Select(p => p.Name).ToArray();
        }

        private object Convert(object value, Type target)
        {
            if (value == null) return null;

            var underlying = Nullable.GetUnderlyingType(target) ?? target;

            if (underlying.IsInstanceOfType(value)) return value;

            // Dictionary conversions
            if (value is Dictionary<string, object> dict)
            {
                if (target == typeof(Dictionary<string, object>)) return dict;
                if (target == typeof(Dictionary<string, long>))
                    return dict.ToDictionary(kv => kv.Key, kv => System.Convert.ToInt64(kv.Value));
            }

            // Array conversions
            if (value is object[] arr && target.IsArray)
            {
                var elemType = target.GetElementType();
                var result = Array.CreateInstance(elemType, arr.Length);
                for (int i = 0; i < arr.Length; i++)
                    result.SetValue(Convert(arr[i], elemType), i);
                return result;
            }

            // Basic conversions
            try
            {
                if (underlying == typeof(IntPtr))
                    return new IntPtr(System.Convert.ToInt64(value));
                return System.Convert.ChangeType(value, underlying);
            }
            catch
            {
                return target.IsValueType ? Activator.CreateInstance(target) : null;
            }
        }

        private string JsonType(Type type)
        {
            var t = Nullable.GetUnderlyingType(type) ?? type;
            if (t == typeof(string)) return "string";
            if (t == typeof(bool)) return "boolean";
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return "integer";
            if (t == typeof(float) || t == typeof(double) || t == typeof(decimal)) return "number";
            if (t.IsArray) return "array";
            return "object";
        }
    }
}
