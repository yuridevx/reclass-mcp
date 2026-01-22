using System;
using System.Collections.Generic;
using System.Linq;
using McpPlugin.Server;
using McpPlugin.Utils;
using ReClassNET.Nodes;
using ReClassNET.Plugins;
using ReClassNET.Project;

namespace McpPlugin.Api
{
    /// <summary>
    /// Enum management API for MCP.
    /// Exposes ReClass.NET's enum/EnumDescription functionality.
    /// </summary>
    public class EnumApi
    {
        private readonly IPluginHost _host;

        public EnumApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        private ReClassNetProject CurrentProject => _host.MainWindow.CurrentProject;

        /// <summary>
        /// Lists all enums in the project.
        /// </summary>
        [McpTool("list_enums", Description = "List all enums in the project")]
        public object ListEnums()
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { error = "No project loaded" };
                }

                var enums = project.Enums.Select(e => new
                {
                    name = e.Name,
                    size = (int)e.Size,
                    useFlagsMode = e.UseFlagsMode,
                    valueCount = e.Values.Count
                });

                return new { enums };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Gets detailed information about an enum.
        /// </summary>
        [McpTool("get_enum", Description = "Get detailed information about an enum")]
        public object GetEnum(string name)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { error = "No project loaded" };
                }

                var enumDesc = FindEnum(name);
                if (enumDesc == null)
                {
                    return new { error = $"Enum not found: {name}" };
                }

                return new
                {
                    name = enumDesc.Name,
                    size = (int)enumDesc.Size,
                    sizeDescription = GetSizeDescription(enumDesc.Size),
                    useFlagsMode = enumDesc.UseFlagsMode,
                    values = enumDesc.Values.Select(v => new
                    {
                        name = v.Key,
                        value = v.Value,
                        hex = $"0x{v.Value:X}"
                    })
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Creates a new enum.
        /// </summary>
        [McpTool("create_enum", Description = "Create a new enum")]
        public object CreateEnum(string name, int size = 4, bool useFlagsMode = false)
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
                    return new { ok = false, error = "Enum name cannot be empty" };
                }

                // Check if enum already exists
                if (FindEnum(name) != null)
                {
                    return new { ok = false, error = $"Enum already exists: {name}" };
                }

                var enumSize = ParseSize(size);
                if (enumSize == null)
                {
                    return new { ok = false, error = "Invalid size. Valid sizes: 1, 2, 4, 8" };
                }

                var enumDesc = new EnumDescription { Name = name };

                // Initialize with a default value to avoid empty sequence errors
                var initialValues = new List<KeyValuePair<string, long>>
                {
                    new KeyValuePair<string, long>("None", 0)
                };
                enumDesc.SetData(useFlagsMode, enumSize.Value, initialValues);

                project.AddEnum(enumDesc);

                return new
                {
                    ok = true,
                    name = enumDesc.Name,
                    size = (int)enumDesc.Size,
                    useFlagsMode = enumDesc.UseFlagsMode
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Deletes an enum.
        /// </summary>
        [McpTool("delete_enum", Description = "Delete an enum")]
        public object DeleteEnum(string name)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                var enumDesc = FindEnum(name);
                if (enumDesc == null)
                {
                    return new { ok = false, error = $"Enum not found: {name}" };
                }

                project.RemoveEnum(enumDesc);

                return new { ok = true, name };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Adds a value to an enum.
        /// </summary>
        [McpTool("add_enum_value", Description = "Add a value to an enum")]
        public object AddEnumValue(string enumName, string valueName, long value)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                var enumDesc = FindEnum(enumName);
                if (enumDesc == null)
                {
                    return new { ok = false, error = $"Enum not found: {enumName}" };
                }

                if (string.IsNullOrWhiteSpace(valueName))
                {
                    return new { ok = false, error = "Value name cannot be empty" };
                }

                // Check if value name already exists
                if (enumDesc.Values.Any(v => v.Key.Equals(valueName, StringComparison.OrdinalIgnoreCase)))
                {
                    return new { ok = false, error = $"Value name already exists: {valueName}" };
                }

                var values = enumDesc.Values.ToList();
                values.Add(new KeyValuePair<string, long>(valueName, value));

                enumDesc.SetData(enumDesc.UseFlagsMode, enumDesc.Size, values);

                return new
                {
                    ok = true,
                    enumName,
                    valueName,
                    value,
                    hex = $"0x{value:X}"
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Removes a value from an enum.
        /// </summary>
        [McpTool("remove_enum_value", Description = "Remove a value from an enum")]
        public object RemoveEnumValue(string enumName, string valueName)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                var enumDesc = FindEnum(enumName);
                if (enumDesc == null)
                {
                    return new { ok = false, error = $"Enum not found: {enumName}" };
                }

                var values = enumDesc.Values.ToList();
                var removed = values.RemoveAll(v => v.Key.Equals(valueName, StringComparison.OrdinalIgnoreCase));

                if (removed == 0)
                {
                    return new { ok = false, error = $"Value not found: {valueName}" };
                }

                enumDesc.SetData(enumDesc.UseFlagsMode, enumDesc.Size, values);

                return new { ok = true, enumName, valueName };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Updates an enum value.
        /// </summary>
        [McpTool("update_enum_value", Description = "Update an enum value")]
        public object UpdateEnumValue(string enumName, string valueName, string newName = null, long? newValue = null)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                var enumDesc = FindEnum(enumName);
                if (enumDesc == null)
                {
                    return new { ok = false, error = $"Enum not found: {enumName}" };
                }

                var values = enumDesc.Values.ToList();
                var index = values.FindIndex(v => v.Key.Equals(valueName, StringComparison.OrdinalIgnoreCase));

                if (index < 0)
                {
                    return new { ok = false, error = $"Value not found: {valueName}" };
                }

                var existingValue = values[index];
                var updatedName = string.IsNullOrWhiteSpace(newName) ? existingValue.Key : newName;
                var updatedValue = newValue ?? existingValue.Value;

                values[index] = new KeyValuePair<string, long>(updatedName, updatedValue);

                enumDesc.SetData(enumDesc.UseFlagsMode, enumDesc.Size, values);

                return new
                {
                    ok = true,
                    enumName,
                    oldName = valueName,
                    newName = updatedName,
                    value = updatedValue,
                    hex = $"0x{updatedValue:X}"
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Renames an enum.
        /// </summary>
        [McpTool("rename_enum", Description = "Rename an enum")]
        public object RenameEnum(string name, string newName)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                var enumDesc = FindEnum(name);
                if (enumDesc == null)
                {
                    return new { ok = false, error = $"Enum not found: {name}" };
                }

                if (string.IsNullOrWhiteSpace(newName))
                {
                    return new { ok = false, error = "New name cannot be empty" };
                }

                // Check if new name already exists
                if (FindEnum(newName) != null)
                {
                    return new { ok = false, error = $"Enum already exists: {newName}" };
                }

                var oldName = enumDesc.Name;
                enumDesc.Name = newName;

                return new { ok = true, oldName, newName };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Sets all values for an enum at once.
        /// </summary>
        [McpTool("set_enum_values", Description = "Set all values for an enum")]
        public object SetEnumValues(string enumName, Dictionary<string, long> values, int? size = null, bool? useFlagsMode = null)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                var enumDesc = FindEnum(enumName);
                if (enumDesc == null)
                {
                    return new { ok = false, error = $"Enum not found: {enumName}" };
                }

                var enumSize = size.HasValue ? ParseSize(size.Value) ?? enumDesc.Size : enumDesc.Size;
                var flagsMode = useFlagsMode ?? enumDesc.UseFlagsMode;

                var valueList = values?.Select(kv => new KeyValuePair<string, long>(kv.Key, kv.Value)).ToList()
                    ?? new List<KeyValuePair<string, long>>();

                enumDesc.SetData(flagsMode, enumSize, valueList);

                return new
                {
                    ok = true,
                    enumName,
                    valueCount = valueList.Count,
                    size = (int)enumDesc.Size,
                    useFlagsMode = enumDesc.UseFlagsMode
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Sets an enum node to use a specific enum type.
        /// </summary>
        [McpTool("set_node_enum", Description = "Set a node to use a specific enum type")]
        public object SetNodeEnum(string @class, int offset, string enumName)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                var classNode = project.Classes.FirstOrDefault(c =>
                    c.Name.Equals(@class, StringComparison.OrdinalIgnoreCase));

                if (classNode == null)
                {
                    return new { ok = false, error = $"Class not found: {@class}" };
                }

                var enumDesc = FindEnum(enumName);
                if (enumDesc == null)
                {
                    return new { ok = false, error = $"Enum not found: {enumName}" };
                }

                // Find node at offset
                int currentOffset = 0;
                BaseNode targetNode = null;
                foreach (var node in classNode.Nodes)
                {
                    if (currentOffset == offset)
                    {
                        targetNode = node;
                        break;
                    }
                    currentOffset += node.MemorySize;
                }

                if (targetNode == null)
                {
                    return new { ok = false, error = $"No node at offset 0x{offset:X}" };
                }

                if (!(targetNode is EnumNode enumNode))
                {
                    // Replace with enum node
                    enumNode = new EnumNode();
                    enumNode.Name = targetNode.Name;
                    enumNode.Comment = targetNode.Comment;
                    classNode.ReplaceChildNode(targetNode, enumNode);
                }

                enumNode.ChangeEnum(enumDesc);

                return new
                {
                    ok = true,
                    @class,
                    offset = $"0x{offset:X}",
                    enumName = enumDesc.Name
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        private EnumDescription FindEnum(string name)
        {
            var project = CurrentProject;
            if (project == null) return null;

            return project.Enums.FirstOrDefault(e =>
                e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private EnumDescription.UnderlyingTypeSize? ParseSize(int size)
        {
            return size switch
            {
                1 => EnumDescription.UnderlyingTypeSize.OneByte,
                2 => EnumDescription.UnderlyingTypeSize.TwoBytes,
                4 => EnumDescription.UnderlyingTypeSize.FourBytes,
                8 => EnumDescription.UnderlyingTypeSize.EightBytes,
                _ => null
            };
        }

        private string GetSizeDescription(EnumDescription.UnderlyingTypeSize size)
        {
            return size switch
            {
                EnumDescription.UnderlyingTypeSize.OneByte => "1 byte (int8)",
                EnumDescription.UnderlyingTypeSize.TwoBytes => "2 bytes (int16)",
                EnumDescription.UnderlyingTypeSize.FourBytes => "4 bytes (int32)",
                EnumDescription.UnderlyingTypeSize.EightBytes => "8 bytes (int64)",
                _ => "unknown"
            };
        }
    }
}
