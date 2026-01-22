using System;
using System.Collections.Generic;
using ReClassNET.Nodes;

namespace McpPlugin.Utils
{
    /// <summary>
    /// Utility for converting between node types and string representations.
    /// </summary>
    public static class TypeConverter
    {
        private static readonly Dictionary<string, Type> StringToNodeType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            // Integer types
            ["int8"] = typeof(Int8Node),
            ["int16"] = typeof(Int16Node),
            ["int32"] = typeof(Int32Node),
            ["int64"] = typeof(Int64Node),
            ["uint8"] = typeof(UInt8Node),
            ["uint16"] = typeof(UInt16Node),
            ["uint32"] = typeof(UInt32Node),
            ["uint64"] = typeof(UInt64Node),
            ["nint"] = typeof(NIntNode),
            ["nuint"] = typeof(NUIntNode),

            // Integer type aliases (C-style)
            ["sbyte"] = typeof(Int8Node),
            ["byte"] = typeof(UInt8Node),
            ["char"] = typeof(Int8Node),
            ["short"] = typeof(Int16Node),
            ["ushort"] = typeof(UInt16Node),
            ["int"] = typeof(Int32Node),
            ["uint"] = typeof(UInt32Node),
            ["long"] = typeof(Int64Node),
            ["ulong"] = typeof(UInt64Node),

            // Floating point
            ["float"] = typeof(FloatNode),
            ["double"] = typeof(DoubleNode),

            // Boolean
            ["bool"] = typeof(BoolNode),
            ["boolean"] = typeof(BoolNode),

            // Hex display types
            ["hex8"] = typeof(Hex8Node),
            ["hex16"] = typeof(Hex16Node),
            ["hex32"] = typeof(Hex32Node),
            ["hex64"] = typeof(Hex64Node),

            // Text types
            ["utf8"] = typeof(Utf8TextNode),
            ["utf16"] = typeof(Utf16TextNode),
            ["utf32"] = typeof(Utf32TextNode),
            ["utf8ptr"] = typeof(Utf8TextPtrNode),
            ["utf16ptr"] = typeof(Utf16TextPtrNode),
            ["utf32ptr"] = typeof(Utf32TextPtrNode),
            ["string"] = typeof(Utf8TextNode),
            ["wstring"] = typeof(Utf16TextNode),
            ["stringptr"] = typeof(Utf8TextPtrNode),
            ["wstringptr"] = typeof(Utf16TextPtrNode),

            // Vector/Matrix types
            ["vector2"] = typeof(Vector2Node),
            ["vector3"] = typeof(Vector3Node),
            ["vector4"] = typeof(Vector4Node),
            ["matrix3x3"] = typeof(Matrix3x3Node),
            ["matrix3x4"] = typeof(Matrix3x4Node),
            ["matrix4x4"] = typeof(Matrix4x4Node),
            ["vec2"] = typeof(Vector2Node),
            ["vec3"] = typeof(Vector3Node),
            ["vec4"] = typeof(Vector4Node),
            ["mat3"] = typeof(Matrix3x3Node),
            ["mat3x4"] = typeof(Matrix3x4Node),
            ["mat4"] = typeof(Matrix4x4Node),

            // Pointer types
            ["pointer"] = typeof(PointerNode),
            ["ptr"] = typeof(PointerNode),
            ["array"] = typeof(ArrayNode),

            // Special types
            ["bitfield"] = typeof(BitFieldNode),
            ["bits"] = typeof(BitFieldNode),
            ["enum"] = typeof(EnumNode),
            ["function"] = typeof(FunctionNode),
            ["func"] = typeof(FunctionNode),
            ["functionptr"] = typeof(FunctionPtrNode),
            ["funcptr"] = typeof(FunctionPtrNode),
            ["vtable"] = typeof(VirtualMethodTableNode),
            ["vmt"] = typeof(VirtualMethodTableNode),
            ["class"] = typeof(ClassInstanceNode),
            ["classinstance"] = typeof(ClassInstanceNode),
            ["struct"] = typeof(ClassInstanceNode),
            ["union"] = typeof(UnionNode),
        };

        private static readonly Dictionary<Type, string> NodeTypeToString;

        static TypeConverter()
        {
            NodeTypeToString = new Dictionary<Type, string>();
            foreach (var kvp in StringToNodeType)
            {
                if (!NodeTypeToString.ContainsKey(kvp.Value))
                {
                    NodeTypeToString[kvp.Value] = kvp.Key;
                }
            }
        }

        /// <summary>
        /// Gets the node type for a string type name.
        /// </summary>
        public static Type GetNodeType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            if (StringToNodeType.TryGetValue(typeName.Trim(), out var type))
                return type;

            return null;
        }

        /// <summary>
        /// Gets the string type name for a node type.
        /// </summary>
        public static string GetTypeName(Type nodeType)
        {
            if (nodeType == null)
                return "unknown";

            if (NodeTypeToString.TryGetValue(nodeType, out var name))
                return name;

            return nodeType.Name.Replace("Node", "").ToLowerInvariant();
        }

        /// <summary>
        /// Gets the string type name for a node instance.
        /// </summary>
        public static string GetTypeName(BaseNode node)
        {
            return node == null ? "unknown" : GetTypeName(node.GetType());
        }

        /// <summary>
        /// Creates a node of the specified type.
        /// </summary>
        public static BaseNode CreateNode(string typeName)
        {
            var type = GetNodeType(typeName);
            if (type == null)
                throw new ArgumentException($"Unknown node type: {typeName}");

            return (BaseNode)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Gets all available node type names.
        /// </summary>
        public static IEnumerable<string> GetAllTypeNames()
        {
            return StringToNodeType.Keys;
        }
    }
}
