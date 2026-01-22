using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using McpPlugin.Models.Dtos;
using McpPlugin.Server;
using McpPlugin.Utils;
using ReClassNET;
using ReClassNET.Memory;
using ReClassNET.Plugins;

namespace McpPlugin.Api
{
    /// <summary>
    /// Process management API for MCP.
    /// </summary>
    public class ProcessApi
    {
        private readonly IPluginHost _host;

        public ProcessApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>
        /// Lists available processes.
        /// </summary>
        [McpTool("list_processes", Description = "List available processes for attachment")]
        public object ListProcesses(string filter = null)
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p =>
                    {
                        try { return !string.IsNullOrEmpty(p.MainWindowTitle) || p.SessionId != 0; }
                        catch { return false; }
                    })
                    .Select(p =>
                    {
                        try
                        {
                            return new ProcessInfoDto
                            {
                                Id = p.Id,
                                Name = p.ProcessName,
                                Path = TryGetProcessPath(p),
                                IsValid = true
                            };
                        }
                        catch
                        {
                            return new ProcessInfoDto
                            {
                                Id = p.Id,
                                Name = p.ProcessName,
                                IsValid = false
                            };
                        }
                    });

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    processes = Pagination.Filter(processes, filter, p => p.Name);
                }

                return Pagination.Paginate(processes.ToList(), 0, 100);
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Attaches to a process by name or PID.
        /// </summary>
        [McpTool("attach_process", Description = "Attach to a process by name or PID")]
        public object AttachProcess(string target)
        {
            try
            {
                Process process = null;

                // Try as PID first
                if (int.TryParse(target, out var pid))
                {
                    process = Process.GetProcessById(pid);
                }
                else
                {
                    // Try as process name
                    var processes = Process.GetProcessesByName(target);
                    if (processes.Length == 0)
                    {
                        // Try partial match
                        processes = Process.GetProcesses()
                            .Where(p => p.ProcessName.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToArray();
                    }

                    if (processes.Length == 0)
                    {
                        return new { ok = false, error = $"Process not found: {target}" };
                    }

                    if (processes.Length > 1)
                    {
                        return new
                        {
                            ok = false,
                            error = "Multiple processes found",
                            matches = processes.Select(p => new { p.Id, p.ProcessName }).ToArray()
                        };
                    }

                    process = processes[0];
                }

                // Attach using ReClass.NET's process infrastructure
                var processInfo = new ProcessInfo(new IntPtr(process.Id), process.ProcessName, TryGetProcessPath(process));
                _host.Process.Open(processInfo);

                return new
                {
                    ok = true,
                    processId = process.Id,
                    name = process.ProcessName,
                    isValid = _host.Process.IsValid
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Detaches from the current process.
        /// </summary>
        [McpTool("detach_process", Description = "Detach from the current process")]
        public object DetachProcess()
        {
            try
            {
                _host.Process.Close();
                return new { ok = true };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets information about the current attached process.
        /// </summary>
        [McpTool("get_process_info", Description = "Get current process information")]
        public object GetProcessInfo()
        {
            try
            {
                var process = _host.Process;
                if (!process.IsValid)
                {
                    return new { attached = false, error = "No process attached" };
                }

                return new
                {
                    attached = true,
                    processId = process.UnderlayingProcess.Id.ToInt64(),
                    name = process.UnderlayingProcess.Name,
                    path = process.UnderlayingProcess.Path,
                    // ReClass.NET runs as x86 or x64 to match target process architecture
                    is64Bit = IntPtr.Size == 8,
                    platform = Constants.Platform,
                    isValid = process.IsValid
                };
            }
            catch (Exception ex)
            {
                return new { attached = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Pauses the current process.
        /// Maps to: RemoteProcess.ControlRemoteProcess(Suspend)
        /// </summary>
        [McpTool("pause_process", Description = "Pause/suspend the current process")]
        public object PauseProcess()
        {
            try
            {
                var process = _host.Process;
                if (!process.IsValid)
                {
                    return new { ok = false, error = "No process attached" };
                }

                process.ControlRemoteProcess(ReClassNET.Core.ControlRemoteProcessAction.Suspend);
                return new { ok = true };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Resumes the current process.
        /// Maps to: RemoteProcess.ControlRemoteProcess(Resume)
        /// </summary>
        [McpTool("resume_process", Description = "Resume the current process")]
        public object ResumeProcess()
        {
            try
            {
                var process = _host.Process;
                if (!process.IsValid)
                {
                    return new { ok = false, error = "No process attached" };
                }

                process.ControlRemoteProcess(ReClassNET.Core.ControlRemoteProcessAction.Resume);
                return new { ok = true };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Terminates the current process.
        /// Maps to: RemoteProcess.ControlRemoteProcess(Terminate)
        /// </summary>
        [McpTool("terminate_process", Description = "Terminate/kill the current process")]
        public object TerminateProcess()
        {
            try
            {
                var process = _host.Process;
                if (!process.IsValid)
                {
                    return new { ok = false, error = "No process attached" };
                }

                process.ControlRemoteProcess(ReClassNET.Core.ControlRemoteProcessAction.Terminate);
                return new { ok = true };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Lists modules in the attached process.
        /// </summary>
        [McpTool("list_modules", Description = "List loaded modules in the current process")]
        public object ListModules(string filter = null, int offset = 0, int count = 50)
        {
            try
            {
                var process = _host.Process;
                if (!process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                var modules = process.Modules.Select(m => new ModuleInfoDto
                {
                    Name = m.Name,
                    Path = m.Path,
                    Start = AddressHelper.ToHexString(m.Start),
                    End = AddressHelper.ToHexString(m.End),
                    Size = $"0x{m.Size:X}"
                });

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    modules = Pagination.Filter(modules, filter, m => m.Name);
                }

                return Pagination.Paginate(modules.ToList(), offset, count);
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Lists memory sections in the attached process.
        /// </summary>
        [McpTool("list_sections", Description = "List memory sections in the current process")]
        public object ListSections(string filter = null, int offset = 0, int count = 100)
        {
            try
            {
                var process = _host.Process;
                if (!process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                var sections = process.Sections.Select(s => new SectionInfoDto
                {
                    Start = AddressHelper.ToHexString(s.Start),
                    End = AddressHelper.ToHexString(s.End),
                    Size = $"0x{s.Size:X}",
                    Name = s.Name,
                    Category = s.Category.ToString(),
                    Protection = s.Protection.ToString(),
                    Type = s.Type.ToString(),
                    ModuleName = s.ModuleName
                });

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    sections = Pagination.Filter(sections, filter, s => s.Name ?? s.ModuleName);
                }

                return Pagination.Paginate(sections.ToList(), offset, count);
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Refreshes process information.
        /// </summary>
        [McpTool("refresh_process", Description = "Refresh process information (modules, sections)")]
        public object RefreshProcess()
        {
            try
            {
                var process = _host.Process;
                if (!process.IsValid)
                {
                    return new { ok = false, error = "No process attached" };
                }

                process.UpdateProcessInformations();

                return new
                {
                    ok = true,
                    moduleCount = process.Modules.Count(),
                    sectionCount = process.Sections.Count()
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        private string TryGetProcessPath(Process p)
        {
            try
            {
                return p.MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }
    }
}
