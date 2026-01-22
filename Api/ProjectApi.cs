using System;
using System.Linq;
using ReClassNET.DataExchange.ReClass;
using McpPlugin.Models.Dtos;
using McpPlugin.Server;
using McpPlugin.Utils;
using ReClassNET.Plugins;
using ReClassNET.Project;

namespace McpPlugin.Api
{
    /// <summary>
    /// Project management API for MCP.
    /// Exposes ReClass.NET's ReClassNetProject and related functionality.
    /// </summary>
    public class ProjectApi
    {
        private readonly IPluginHost _host;

        public ProjectApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        private ReClassNetProject CurrentProject => _host.MainWindow.CurrentProject;

        /// <summary>
        /// Gets current project information.
        /// Maps to: ReClassNetProject properties
        /// </summary>
        [McpTool("get_project_info", Description = "Get current project information")]
        public object GetProjectInfo()
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { hasProject = false };
                }

                return new
                {
                    hasProject = true,
                    path = project.Path,
                    classCount = project.Classes.Count,
                    enumCount = project.Enums.Count
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Lists all classes in the project.
        /// Maps to: ReClassNetProject.Classes
        /// </summary>
        [McpTool("list_classes", Description = "List all classes in the project")]
        public object ListClasses(string filter = null, int offset = 0, int count = 50)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { error = "No project loaded" };
                }

                var classes = project.Classes.Select(c => new ClassInfoDto
                {
                    Uuid = c.Uuid.ToString(),
                    Name = c.Name,
                    AddressFormula = c.AddressFormula,
                    Size = c.MemorySize,
                    Comment = c.Comment
                });

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    classes = Pagination.Filter(classes, filter, c => c.Name);
                }

                return Pagination.Paginate(classes.ToList(), offset, count);
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Lists all enums in the project.
        /// Maps to: ReClassNetProject.Enums
        /// </summary>
        [McpTool("list_enums", Description = "List all enums in the project")]
        public object ListEnums(string filter = null, int offset = 0, int count = 50)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { error = "No project loaded" };
                }

                var enums = project.Enums.Select(e => new EnumInfoDto
                {
                    Name = e.Name,
                    Size = (int)e.Size,
                    UseFlagsMode = e.UseFlagsMode,
                    Values = e.Values.Select(v => new EnumValueDto
                    {
                        Name = v.Key,
                        Value = v.Value
                    }).ToArray()
                });

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    enums = Pagination.Filter(enums, filter, e => e.Name);
                }

                return Pagination.Paginate(enums.ToList(), offset, count);
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Creates a new empty project.
        /// Maps to: MainForm.SetProject with new ReClassNetProject
        /// </summary>
        [McpTool("new_project", Description = "Create a new empty project")]
        public object NewProject()
        {
            try
            {
                var project = new ReClassNetProject();
                _host.MainWindow.SetProject(project);

                return new
                {
                    ok = true,
                    classCount = 0,
                    enumCount = 0
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Loads a project from file.
        /// Maps to: ReClassNetFile.Load and MainForm.SetProject
        /// </summary>
        [McpTool("load_project", Description = "Load a project from file")]
        public object LoadProject(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return new { ok = false, error = "Path is required" };
                }

                if (!System.IO.File.Exists(path))
                {
                    return new { ok = false, error = $"File not found: {path}" };
                }

                var project = new ReClassNetProject();
                var file = new ReClassNetFile(project);
                file.Load(path, _host.Logger);
                project.Path = path;

                // Set the project as the current project in MainForm
                _host.MainWindow.SetProject(project);

                return new
                {
                    ok = true,
                    path,
                    classCount = project.Classes.Count,
                    enumCount = project.Enums.Count
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Saves the current project.
        /// Maps to: ReClassNetFile.Save
        /// </summary>
        [McpTool("save_project", Description = "Save the current project")]
        public object SaveProject(string path = null)
        {
            try
            {
                var project = CurrentProject;
                if (project == null)
                {
                    return new { ok = false, error = "No project loaded" };
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    path = project.Path;
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    return new { ok = false, error = "No path specified and project has no path" };
                }

                var file = new ReClassNetFile(project);
                file.Save(path, _host.Logger);
                project.Path = path;

                return new { ok = true, path };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets named addresses from the process.
        /// Maps to: RemoteProcess.NamedAddresses
        /// </summary>
        [McpTool("get_named_addresses", Description = "Get named addresses from the process")]
        public object GetNamedAddresses()
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                var addresses = _host.Process.NamedAddresses
                    .Select(kvp => new
                    {
                        address = AddressHelper.ToHexString(kvp.Key),
                        name = kvp.Value
                    })
                    .ToArray();

                return new { count = addresses.Length, addresses };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Sets a named address in the process.
        /// Maps to: RemoteProcess.NamedAddresses dictionary
        /// </summary>
        [McpTool("set_named_address", Description = "Set a named address in the process")]
        public object SetNamedAddress(string addr, string name)
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { ok = false, error = "No process attached" };
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    return new { ok = false, error = "Name is required" };
                }

                var address = AddressHelper.Parse(addr);
                _host.Process.NamedAddresses[address] = name;

                return new { ok = true, address = AddressHelper.ToHexString(address), name };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Removes a named address from the process.
        /// Maps to: RemoteProcess.NamedAddresses.Remove
        /// </summary>
        [McpTool("remove_named_address", Description = "Remove a named address from the process")]
        public object RemoveNamedAddress(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { ok = false, error = "No process attached" };
                }

                var address = AddressHelper.Parse(addr);
                var removed = _host.Process.NamedAddresses.Remove(address);

                return new { ok = removed, address = AddressHelper.ToHexString(address) };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets the name for an address if it exists.
        /// Maps to: RemoteProcess.GetNamedAddress
        /// </summary>
        [McpTool("get_named_address", Description = "Get the name for a specific address")]
        public object GetNamedAddress(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                var address = AddressHelper.Parse(addr);
                var name = _host.Process.GetNamedAddress(address);

                return new
                {
                    address = AddressHelper.ToHexString(address),
                    name,
                    found = !string.IsNullOrEmpty(name)
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }
    }
}
