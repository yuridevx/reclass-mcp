using System;
using System.Linq;
using McpPlugin.Models.Dtos;
using McpPlugin.Server;
using McpPlugin.Utils;
using ReClassNET.Nodes;
using ReClassNET.Plugins;
using ReClassNET.Project;

namespace McpPlugin.Api
{
    /// <summary>
    /// Class/structure manipulation API for MCP.
    /// </summary>
    public class ClassApi
    {
        private readonly IPluginHost _host;

        public ClassApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        private ReClassNetProject CurrentProject => _host.MainWindow.CurrentProject;

        /// <summary>
        /// Creates a new class.
        /// </summary>
        [McpTool("create_class", Description = "Create a new class")]
        public object CreateClass(string name, string address = null, int size = 0)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    return new { ok = false, error = "Name is required" };
                }

                // Check for duplicate name
                if (project.Classes.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return new { ok = false, error = $"Class '{name}' already exists" };
                }

                var classNode = ClassNode.Create();
                classNode.Name = name;

                if (!string.IsNullOrWhiteSpace(address))
                {
                    classNode.AddressFormula = address;
                }

                // Add padding bytes if size specified
                if (size > 0)
                {
                    classNode.AddBytes(size);
                }

                project.AddClass(classNode);

                return new
                {
                    ok = true,
                    @class = new ClassInfoDto
                    {
                        Uuid = classNode.Uuid.ToString(),
                        Name = classNode.Name,
                        AddressFormula = classNode.AddressFormula,
                        Size = classNode.MemorySize
                    }
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets detailed class information.
        /// </summary>
        [McpTool("get_class", Description = "Get detailed class information")]
        public object GetClass(string name)
        {
            try
            {
                var classNode = FindClass(name);
                if (classNode == null)
                {
                    return new { error = $"Class not found: {name}" };
                }

                return new ClassInfoDto
                {
                    Uuid = classNode.Uuid.ToString(),
                    Name = classNode.Name,
                    AddressFormula = classNode.AddressFormula,
                    Size = classNode.MemorySize,
                    Comment = classNode.Comment,
                    Nodes = classNode.Nodes.Select((n, i) => NodeToDto(n, GetNodeOffset(classNode, i))).ToList()
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Deletes a class.
        /// </summary>
        [McpTool("delete_class", Description = "Delete a class")]
        public object DeleteClass(string name)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                var classNode = FindClass(name);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {name}" };
                }

                project.Remove(classNode);

                return new { ok = true, name = classNode.Name };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Sets the class address.
        /// </summary>
        [McpTool("set_class_address", Description = "Set the base address for a class")]
        public object SetClassAddress(string @class, string address)
        {
            try
            {
                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                classNode.AddressFormula = address;

                return new
                {
                    ok = true,
                    name = classNode.Name,
                    addressFormula = classNode.AddressFormula
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Renames a class.
        /// </summary>
        [McpTool("rename_class", Description = "Rename a class")]
        public object RenameClass(string @class, string newName)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                if (string.IsNullOrWhiteSpace(newName))
                {
                    return new { ok = false, error = "New name is required" };
                }

                var classNode = FindClass(@class);
                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                // Check for duplicate name
                if (project.Classes.Any(c => c != classNode &&
                    c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    return new { ok = false, error = $"Class '{newName}' already exists" };
                }

                var oldName = classNode.Name;
                classNode.Name = newName;

                return new { ok = true, oldName, newName };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        private ClassNode FindClass(string nameOrUuid)
        {
            var project = CurrentProject;
            if (project == null) return null;

            // Try by UUID first
            if (Guid.TryParse(nameOrUuid, out var uuid))
            {
                var byUuid = project.Classes.FirstOrDefault(c => c.Uuid == uuid);
                if (byUuid != null) return byUuid;
            }

            // Try by name
            return project.Classes.FirstOrDefault(c =>
                c.Name.Equals(nameOrUuid, StringComparison.OrdinalIgnoreCase));
        }

        private int GetNodeOffset(ClassNode classNode, int index)
        {
            int offset = 0;
            for (int i = 0; i < index && i < classNode.Nodes.Count; i++)
            {
                offset += classNode.Nodes[i].MemorySize;
            }
            return offset;
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

            // Handle wrapper nodes
            if (node is BaseWrapperNode wrapper && wrapper.InnerNode != null)
            {
                dto.InnerNode = NodeToDto(wrapper.InnerNode, 0);
            }

            // Handle array nodes
            if (node is BaseWrapperArrayNode arrayNode)
            {
                dto.ArrayCount = arrayNode.Count;
            }

            // Handle class instance nodes
            if (node is ClassInstanceNode classInstance && classInstance.InnerNode != null)
            {
                dto.InnerClass = classInstance.InnerNode.Name;
            }

            return dto;
        }
    }
}
