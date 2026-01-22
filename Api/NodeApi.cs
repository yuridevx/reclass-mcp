using System;
using System.Collections.Generic;
using System.Linq;
using McpPlugin.Models.Dtos;
using McpPlugin.Server;
using McpPlugin.Utils;
using ReClassNET.Memory;
using ReClassNET.Nodes;
using ReClassNET.Plugins;
using ReClassNET.Project;

namespace McpPlugin.Api
{
    /// <summary>
    /// Node operations API for MCP.
    /// </summary>
    public class NodeApi
    {
        private readonly IPluginHost _host;

        public NodeApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        private ReClassNetProject CurrentProject => _host.MainWindow.CurrentProject;

        /// <summary>
        /// Lists available node types.
        /// </summary>
        [McpTool("list_node_types", Description = "List available node types")]
        public object ListNodeTypes()
        {
            try
            {
                var types = TypeConverter.GetAllTypeNames().Select(name =>
                {
                    var nodeType = TypeConverter.GetNodeType(name);
                    var node = (BaseNode)Activator.CreateInstance(nodeType);
                    return new
                    {
                        name,
                        size = node.MemorySize,
                        category = GetNodeCategory(nodeType)
                    };
                }).OrderBy(t => t.category).ThenBy(t => t.name);

                return new { types };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Lists nodes in a class.
        /// </summary>
        [McpTool("list_nodes", Description = "List nodes in a class")]
        public object ListNodes(string @class)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { error = $"Class not found: {@class}" };
                }

                var nodes = new List<NodeInfoDto>();
                int offset = 0;

                foreach (var node in classNode.Nodes)
                {
                    nodes.Add(NodeToDto(node, offset));
                    offset += node.MemorySize;
                }

                return new { @class = classNode.Name, nodes };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Adds a node to a class.
        /// </summary>
        [McpTool("add_node", Description = "Add a node to a class")]
        public object AddNode(string @class, string type, int offset = -1)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var nodeType = TypeConverter.GetNodeType(type);
                if (nodeType == null)
                {
                    return new { ok = false, error = $"Unknown node type: {type}" };
                }

                var newNode = (BaseNode)Activator.CreateInstance(nodeType);

                if (offset < 0)
                {
                    // Append to end
                    classNode.AddBytes(newNode.MemorySize);
                    var lastNode = classNode.Nodes.LastOrDefault();
                    if (lastNode != null)
                    {
                        classNode.ReplaceChildNode(lastNode, newNode);
                    }
                }
                else
                {
                    // Find node at offset and replace
                    var (targetNode, actualOffset) = FindNodeAtOffset(classNode, offset);
                    if (targetNode == null)
                    {
                        return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                    }

                    classNode.ReplaceChildNode(targetNode, newNode);
                }

                return new
                {
                    ok = true,
                    node = NodeToDto(newNode, offset >= 0 ? offset : classNode.MemorySize - newNode.MemorySize)
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Removes a node from a class.
        /// </summary>
        [McpTool("remove_node", Description = "Remove a node from a class")]
        public object RemoveNode(string @class, int offset)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var (targetNode, _) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                classNode.RemoveNode(targetNode);

                return new { ok = true, offset = $"0x{offset:X}" };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Changes the type of a node.
        /// </summary>
        [McpTool("change_node_type", Description = "Change the type of a node")]
        public object ChangeNodeType(string @class, int offset, string newType)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var (targetNode, actualOffset) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                var nodeType = TypeConverter.GetNodeType(newType);
                if (nodeType == null)
                {
                    return new { ok = false, error = $"Unknown node type: {newType}" };
                }

                var newNode = (BaseNode)Activator.CreateInstance(nodeType);
                newNode.Name = targetNode.Name;
                newNode.Comment = targetNode.Comment;

                classNode.ReplaceChildNode(targetNode, newNode);

                return new
                {
                    ok = true,
                    node = NodeToDto(newNode, actualOffset)
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Renames a node.
        /// </summary>
        [McpTool("rename_node", Description = "Rename a node")]
        public object RenameNode(string @class, int offset, string name)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var (targetNode, _) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                var oldName = targetNode.Name;
                targetNode.Name = name;

                return new { ok = true, oldName, newName = name };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Sets a node's comment.
        /// </summary>
        [McpTool("set_node_comment", Description = "Set a node's comment")]
        public object SetNodeComment(string @class, int offset, string comment)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var (targetNode, _) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                targetNode.Comment = comment;

                return new { ok = true, offset = $"0x{offset:X}", comment };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Reads the current value at a node.
        /// </summary>
        [McpTool("read_node_value", Description = "Read the current value at a node")]
        public object ReadNodeValue(string @class, int offset)
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { error = $"Class not found: {@class}" };
                }

                // Try to parse the address formula
                if (string.IsNullOrWhiteSpace(classNode.AddressFormula))
                {
                    return new { error = "Class has no address formula" };
                }

                if (!AddressHelper.TryParse(classNode.AddressFormula, out var baseAddress))
                {
                    return new { error = "Could not parse class address formula" };
                }

                if (baseAddress == IntPtr.Zero)
                {
                    return new { error = "Class has no base address" };
                }

                var (targetNode, actualOffset) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { error = $"No node at offset 0x{offset:X}" };
                }

                var address = baseAddress + actualOffset;
                var data = _host.Process.ReadRemoteMemory(address, targetNode.MemorySize);
                var valueStr = FormatNodeValue(targetNode, data);

                return new
                {
                    offset = $"0x{actualOffset:X}",
                    address = AddressHelper.ToHexString(address),
                    type = TypeConverter.GetTypeName(targetNode),
                    value = valueStr,
                    hex = BitConverter.ToString(data).Replace("-", " ")
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Inserts padding bytes at offset.
        /// </summary>
        [McpTool("insert_bytes", Description = "Insert padding bytes at offset")]
        public object InsertBytes(string @class, int offset, int count)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                if (count <= 0 || count > 0x10000)
                {
                    return new { ok = false, error = "Invalid count (1-65536)" };
                }

                // Find the node at offset and insert before it
                var (targetNode, _) = FindNodeAtOffset(classNode, offset);

                if (targetNode != null)
                {
                    // InsertBytes takes a node position, not an index
                    classNode.InsertBytes(targetNode, count);
                }
                else
                {
                    // Append at end
                    classNode.AddBytes(count);
                }

                return new { ok = true, offset = $"0x{offset:X}", count, newSize = classNode.MemorySize };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Auto-detects node types based on memory contents.
        /// Maps to: NodeDissector.DissectNodes / GuessNode
        /// </summary>
        [McpTool("dissect_nodes", Description = "Auto-detect node types based on memory contents")]
        public object DissectNodes(string @class, int startOffset = 0, int endOffset = -1)
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { ok = false, error = "No process attached" };
                }

                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                // Try to parse the address formula
                if (string.IsNullOrWhiteSpace(classNode.AddressFormula))
                {
                    return new { ok = false, error = "Class has no address formula" };
                }

                if (!AddressHelper.TryParse(classNode.AddressFormula, out var baseAddress))
                {
                    return new { ok = false, error = "Could not parse class address formula" };
                }

                if (baseAddress == IntPtr.Zero)
                {
                    return new { ok = false, error = "Class has no base address" };
                }

                if (endOffset < 0)
                    endOffset = classNode.MemorySize;

                // Read memory for the entire class
                var memorySize = classNode.MemorySize;
                var data = _host.Process.ReadRemoteMemory(baseAddress, memorySize);
                var memory = new MemoryBuffer { Size = memorySize };
                memory.UpdateFrom(_host.Process, baseAddress);

                var dissectedNodes = new List<object>();
                int currentOffset = 0;

                foreach (var node in classNode.Nodes.ToList())
                {
                    if (currentOffset < startOffset || currentOffset >= endOffset)
                    {
                        currentOffset += node.MemorySize;
                        continue;
                    }

                    if (node is BaseHexNode hexNode)
                    {
                        if (NodeDissector.GuessNode(hexNode, _host.Process, memory, out var guessedNode))
                        {
                            classNode.ReplaceChildNode(hexNode, guessedNode);
                            dissectedNodes.Add(new
                            {
                                offset = $"0x{currentOffset:X}",
                                oldType = TypeConverter.GetTypeName(hexNode),
                                newType = TypeConverter.GetTypeName(guessedNode)
                            });
                        }
                    }

                    currentOffset += node.MemorySize;
                }

                return new
                {
                    ok = true,
                    @class = classNode.Name,
                    dissectedCount = dissectedNodes.Count,
                    dissectedNodes
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Sets the inner class for a pointer node.
        /// </summary>
        [McpTool("set_pointer_class", Description = "Set the inner class for a pointer node")]
        public object SetPointerClass(string @class, int offset, string innerClass)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var (targetNode, actualOffset) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                if (!(targetNode is PointerNode pointerNode))
                {
                    return new { ok = false, error = $"Node at offset 0x{offset:X} is not a pointer" };
                }

                var innerClassNode = FindClass(innerClass);
                if (innerClassNode == null)
                {
                    return new { ok = false, error = $"Inner class not found: {innerClass}" };
                }

                pointerNode.ChangeInnerNode(new ClassInstanceNode());
                if (pointerNode.InnerNode is ClassInstanceNode classInstance)
                {
                    classInstance.ChangeInnerNode(innerClassNode);
                }

                return new
                {
                    ok = true,
                    offset = $"0x{actualOffset:X}",
                    innerClass = innerClassNode.Name
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Sets the count for an array node.
        /// </summary>
        [McpTool("set_array_count", Description = "Set the count for an array node")]
        public object SetArrayCount(string @class, int offset, int count)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var (targetNode, actualOffset) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                if (!(targetNode is BaseWrapperArrayNode arrayNode))
                {
                    return new { ok = false, error = $"Node at offset 0x{offset:X} is not an array" };
                }

                if (count <= 0 || count > 0x10000)
                {
                    return new { ok = false, error = "Invalid count (1-65536)" };
                }

                arrayNode.Count = count;

                return new
                {
                    ok = true,
                    offset = $"0x{actualOffset:X}",
                    count,
                    newSize = arrayNode.MemorySize
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Sets the bit count for a bitfield node.
        /// </summary>
        [McpTool("set_bitfield_bits", Description = "Set the bit count for a bitfield node")]
        public object SetBitfieldBits(string @class, int offset, int bits)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var (targetNode, actualOffset) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                if (!(targetNode is BitFieldNode bitfieldNode))
                {
                    return new { ok = false, error = $"Node at offset 0x{offset:X} is not a bitfield" };
                }

                if (bits < 1 || bits > 64)
                {
                    return new { ok = false, error = "Invalid bit count (1-64)" };
                }

                bitfieldNode.Bits = bits;

                return new
                {
                    ok = true,
                    offset = $"0x{actualOffset:X}",
                    bits,
                    newSize = bitfieldNode.MemorySize
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Sets the inner class for a class instance node.
        /// </summary>
        [McpTool("set_instance_class", Description = "Set the inner class for a class instance node")]
        public object SetInstanceClass(string @class, int offset, string innerClass)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var (targetNode, actualOffset) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                if (!(targetNode is ClassInstanceNode instanceNode))
                {
                    return new { ok = false, error = $"Node at offset 0x{offset:X} is not a class instance" };
                }

                var innerClassNode = FindClass(innerClass);
                if (innerClassNode == null)
                {
                    return new { ok = false, error = $"Inner class not found: {innerClass}" };
                }

                instanceNode.ChangeInnerNode(innerClassNode);

                return new
                {
                    ok = true,
                    offset = $"0x{actualOffset:X}",
                    innerClass = innerClassNode.Name,
                    newSize = instanceNode.MemorySize
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a value to a node.
        /// </summary>
        [McpTool("write_node_value", Description = "Write a value to a node")]
        public object WriteNodeValue(string @class, int offset, string value)
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { ok = false, error = "No process attached" };
                }

                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                // Try to parse the address formula
                if (string.IsNullOrWhiteSpace(classNode.AddressFormula))
                {
                    return new { ok = false, error = "Class has no address formula" };
                }

                if (!AddressHelper.TryParse(classNode.AddressFormula, out var baseAddress))
                {
                    return new { ok = false, error = "Could not parse class address formula" };
                }

                if (baseAddress == IntPtr.Zero)
                {
                    return new { ok = false, error = "Class has no base address" };
                }

                var (targetNode, actualOffset) = FindNodeAtOffset(classNode, offset);
                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                var address = baseAddress + actualOffset;
                WriteNodeValueToMemory(targetNode, address, value);

                return new
                {
                    ok = true,
                    offset = $"0x{actualOffset:X}",
                    address = AddressHelper.ToHexString(address),
                    value
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        private void WriteNodeValueToMemory(BaseNode node, IntPtr address, string value)
        {
            switch (node)
            {
                case Int8Node _:
                    _host.Process.WriteRemoteMemory(address, new[] { (byte)sbyte.Parse(value) });
                    break;
                case UInt8Node _:
                    _host.Process.WriteRemoteMemory(address, new[] { byte.Parse(value) });
                    break;
                case Int16Node _:
                    _host.Process.WriteRemoteMemory(address, BitConverter.GetBytes(short.Parse(value)));
                    break;
                case UInt16Node _:
                    _host.Process.WriteRemoteMemory(address, BitConverter.GetBytes(ushort.Parse(value)));
                    break;
                case Int32Node _:
                    _host.Process.WriteRemoteMemory(address, BitConverter.GetBytes(int.Parse(value)));
                    break;
                case UInt32Node _:
                    _host.Process.WriteRemoteMemory(address, BitConverter.GetBytes(uint.Parse(value)));
                    break;
                case Int64Node _:
                    _host.Process.WriteRemoteMemory(address, BitConverter.GetBytes(long.Parse(value)));
                    break;
                case UInt64Node _:
                    _host.Process.WriteRemoteMemory(address, BitConverter.GetBytes(ulong.Parse(value)));
                    break;
                case FloatNode _:
                    _host.Process.WriteRemoteMemory(address, BitConverter.GetBytes(float.Parse(value)));
                    break;
                case DoubleNode _:
                    _host.Process.WriteRemoteMemory(address, BitConverter.GetBytes(double.Parse(value)));
                    break;
                case BoolNode _:
                    _host.Process.WriteRemoteMemory(address, new[] { bool.Parse(value) ? (byte)1 : (byte)0 });
                    break;
                default:
                    throw new NotSupportedException($"Cannot write to node type: {TypeConverter.GetTypeName(node)}");
            }
        }

        private ClassNode FindClass(string nameOrUuid)
        {
            var project = CurrentProject;
            if (project == null) return null;

            if (Guid.TryParse(nameOrUuid, out var uuid))
            {
                var byUuid = project.Classes.FirstOrDefault(c => c.Uuid == uuid);
                if (byUuid != null) return byUuid;
            }

            return project.Classes.FirstOrDefault(c =>
                c.Name.Equals(nameOrUuid, StringComparison.OrdinalIgnoreCase));
        }

        private (BaseNode node, int offset) FindNodeAtOffset(ClassNode classNode, int targetOffset)
        {
            int currentOffset = 0;
            foreach (var node in classNode.Nodes)
            {
                if (currentOffset == targetOffset)
                {
                    return (node, currentOffset);
                }

                if (currentOffset > targetOffset)
                {
                    break;
                }

                currentOffset += node.MemorySize;
            }

            return (null, -1);
        }

        private NodeInfoDto NodeToDto(BaseNode node, int offset)
        {
            var dto = new NodeInfoDto
            {
                Offset = offset,
                OffsetHex = $"0x{offset:X}",
                Name = node.Name,
                Type = TypeConverter.GetTypeName(node),
                Size = node.MemorySize,
                Comment = node.Comment
            };

            if (node is BaseWrapperNode wrapper && wrapper.InnerNode != null)
            {
                dto.InnerNode = NodeToDto(wrapper.InnerNode, 0);
            }

            if (node is BaseWrapperArrayNode arrayNode)
            {
                dto.ArrayCount = arrayNode.Count;
            }

            if (node is ClassInstanceNode classInstance && classInstance.InnerNode != null)
            {
                dto.InnerClass = classInstance.InnerNode.Name;
            }

            return dto;
        }

        private string FormatNodeValue(BaseNode node, byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                    return "(empty)";

                switch (node)
                {
                    case Int8Node _:
                        return ((sbyte)data[0]).ToString();
                    case UInt8Node _:
                        return data[0].ToString();
                    case Int16Node _:
                        return BitConverter.ToInt16(data, 0).ToString();
                    case UInt16Node _:
                        return BitConverter.ToUInt16(data, 0).ToString();
                    case Int32Node _:
                        return BitConverter.ToInt32(data, 0).ToString();
                    case UInt32Node _:
                        return BitConverter.ToUInt32(data, 0).ToString();
                    case Int64Node _:
                        return BitConverter.ToInt64(data, 0).ToString();
                    case UInt64Node _:
                        return BitConverter.ToUInt64(data, 0).ToString();
                    case FloatNode _:
                        return BitConverter.ToSingle(data, 0).ToString("G");
                    case DoubleNode _:
                        return BitConverter.ToDouble(data, 0).ToString("G");
                    case BoolNode _:
                        return (data[0] != 0).ToString();
                    default:
                        return BitConverter.ToString(data).Replace("-", " ");
                }
            }
            catch
            {
                return "(error)";
            }
        }

        private string GetNodeCategory(Type nodeType)
        {
            if (typeof(BaseNumericNode).IsAssignableFrom(nodeType))
                return "numeric";
            if (typeof(BaseTextNode).IsAssignableFrom(nodeType) ||
                typeof(BaseTextPtrNode).IsAssignableFrom(nodeType))
                return "text";
            if (typeof(BaseMatrixNode).IsAssignableFrom(nodeType))
                return "matrix";
            if (typeof(BaseHexNode).IsAssignableFrom(nodeType))
                return "hex";
            if (typeof(BaseWrapperNode).IsAssignableFrom(nodeType))
                return "wrapper";
            return "other";
        }
    }
}
