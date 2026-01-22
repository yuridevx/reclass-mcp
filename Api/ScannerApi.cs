using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using McpPlugin.Models.Dtos;
using McpPlugin.Server;
using McpPlugin.Utils;
using ReClassNET.Memory;
using ReClassNET.MemoryScanner;
using ReClassNET.MemoryScanner.Comparer;
using ReClassNET.Plugins;
using ReClassNET.Util.Conversion;

namespace McpPlugin.Api
{
    /// <summary>
    /// Memory scanner API for MCP.
    /// Exposes ReClass.NET's Scanner and PatternScanner functionality.
    /// </summary>
    public class ScannerApi
    {
        private readonly IPluginHost _host;
        private Scanner _scanner;
        private ScanSettings _scanSettings;

        public ScannerApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>
        /// Performs an AOB (Array of Bytes) pattern scan.
        /// Maps to: PatternScanner.FindPattern
        /// </summary>
        [McpTool("pattern_scan", Description = "Scan for byte pattern (AOB scan)")]
        public object PatternScan(string pattern, string module = null, int maxResults = 100)
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                // Parse the pattern
                var bytePattern = BytePattern.Parse(pattern);
                if (bytePattern.Length == 0)
                {
                    return new { error = "Invalid pattern format" };
                }

                if (maxResults <= 0 || maxResults > 10000)
                    maxResults = 100;

                var matches = new List<PatternMatchDto>();

                // Determine sections to scan
                var sectionsToScan = _host.Process.Sections;
                if (!string.IsNullOrWhiteSpace(module))
                {
                    sectionsToScan = sectionsToScan.Where(s =>
                        (s.ModuleName?.IndexOf(module, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (s.Name?.IndexOf(module, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
                }

                foreach (var section in sectionsToScan.Where(s => s.Protection.HasFlag(SectionProtection.Read)))
                {
                    if (matches.Count >= maxResults)
                        break;

                    try
                    {
                        // Read the entire section
                        var data = _host.Process.ReadRemoteMemory(section.Start, (int)section.Size);

                        // Find all matches in this section using byte array overload
                        int offset = 0;
                        while (offset < data.Length && matches.Count < maxResults)
                        {
                            var result = FindPatternInData(bytePattern, data, offset);
                            if (result == -1)
                                break;

                            matches.Add(new PatternMatchDto
                            {
                                Address = AddressHelper.ToHexString(section.Start + result),
                                Module = section.ModuleName,
                                Section = section.Name,
                                Offset = $"0x{result:X}"
                            });

                            offset = result + 1;
                        }
                    }
                    catch
                    {
                        // Skip sections we can't read
                    }
                }

                return new
                {
                    pattern,
                    module,
                    count = matches.Count,
                    matches,
                    truncated = matches.Count >= maxResults
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Simple pattern search in byte array.
        /// Uses BytePattern.Equals method for pattern matching.
        /// </summary>
        private int FindPatternInData(BytePattern pattern, byte[] data, int startOffset)
        {
            if (pattern.Length == 0 || data.Length < pattern.Length)
                return -1;

            for (int i = startOffset; i <= data.Length - pattern.Length; i++)
            {
                if (pattern.Equals(data, i))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Initializes a new value scan with the given settings.
        /// Maps to: Scanner constructor
        /// </summary>
        [McpTool("init_scan", Description = "Initialize a new value scan")]
        public object InitScan(string valueType, string startAddr = null, string stopAddr = null,
            bool scanWritable = true, bool scanExecutable = false, bool scanCopyOnWrite = false)
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                var scanValueType = ParseValueType(valueType);

                _scanSettings = new ScanSettings
                {
                    ValueType = scanValueType,
                    StartAddress = string.IsNullOrWhiteSpace(startAddr) ? IntPtr.Zero : AddressHelper.Parse(startAddr),
                    StopAddress = string.IsNullOrWhiteSpace(stopAddr) ? (IntPtr)long.MaxValue : AddressHelper.Parse(stopAddr),
                    ScanWritableMemory = scanWritable ? SettingState.Yes : SettingState.Indeterminate,
                    ScanExecutableMemory = scanExecutable ? SettingState.Yes : SettingState.Indeterminate,
                    ScanCopyOnWriteMemory = scanCopyOnWrite ? SettingState.Yes : SettingState.Indeterminate,
                    ScanPrivateMemory = true,
                    ScanImageMemory = true,
                    ScanMappedMemory = false
                };

                _scanner?.Dispose();
                _scanner = new Scanner(_host.Process, _scanSettings);

                return new
                {
                    ok = true,
                    valueType = valueType,
                    startAddr = AddressHelper.ToHexString(_scanSettings.StartAddress),
                    stopAddr = AddressHelper.ToHexString(_scanSettings.StopAddress)
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Performs a first scan with the given value.
        /// Maps to: Scanner.Search (first scan)
        /// </summary>
        [McpTool("first_scan", Description = "Perform first scan for value")]
        public object FirstScan(string value, string compareType = "equal")
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                if (_scanner == null || _scanSettings == null)
                {
                    return new { error = "Scanner not initialized. Call init_scan first." };
                }

                var comparer = CreateComparer(_scanSettings.ValueType, compareType, value);
                if (comparer == null)
                {
                    return new { error = "Invalid compare type or value" };
                }

                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var task = _scanner.Search(comparer, null, cts.Token);
                task.Wait();

                return new
                {
                    ok = task.Result,
                    resultCount = _scanner.TotalResultCount,
                    canUndoLastScan = _scanner.CanUndoLastScan
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Performs a next scan to filter results.
        /// Maps to: Scanner.Search (next scan)
        /// </summary>
        [McpTool("next_scan", Description = "Perform next scan to filter results")]
        public object NextScan(string value, string compareType = "equal")
        {
            try
            {
                if (!_host.Process.IsValid)
                {
                    return new { error = "No process attached" };
                }

                if (_scanner == null || _scanSettings == null)
                {
                    return new { error = "Scanner not initialized. Call init_scan and first_scan first." };
                }

                if (_scanner.TotalResultCount == 0)
                {
                    return new { error = "No results to filter. Perform first_scan first." };
                }

                var comparer = CreateComparer(_scanSettings.ValueType, compareType, value);
                if (comparer == null)
                {
                    return new { error = "Invalid compare type or value" };
                }

                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var task = _scanner.Search(comparer, null, cts.Token);
                task.Wait();

                return new
                {
                    ok = task.Result,
                    resultCount = _scanner.TotalResultCount,
                    canUndoLastScan = _scanner.CanUndoLastScan
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets the current scan results.
        /// Maps to: Scanner.GetResults
        /// </summary>
        [McpTool("get_scan_results", Description = "Get scan results")]
        public object GetScanResults(int offset = 0, int count = 100)
        {
            try
            {
                if (_scanner == null)
                {
                    return new { error = "Scanner not initialized" };
                }

                if (count <= 0 || count > 10000)
                    count = 100;

                var results = _scanner.GetResults()
                    .Skip(offset)
                    .Take(count)
                    .Select(r => new
                    {
                        addr = AddressHelper.ToHexString(r.Address),
                        value = FormatScanResult(r)
                    })
                    .ToArray();

                return new
                {
                    totalCount = _scanner.TotalResultCount,
                    offset,
                    count = results.Length,
                    results
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Undoes the last scan.
        /// Maps to: Scanner.UndoLastScan
        /// </summary>
        [McpTool("undo_scan", Description = "Undo the last scan")]
        public object UndoScan()
        {
            try
            {
                if (_scanner == null)
                {
                    return new { ok = false, error = "Scanner not initialized" };
                }

                if (!_scanner.CanUndoLastScan)
                {
                    return new { ok = false, error = "Cannot undo last scan" };
                }

                _scanner.UndoLastScan();

                return new
                {
                    ok = true,
                    resultCount = _scanner.TotalResultCount,
                    canUndoLastScan = _scanner.CanUndoLastScan
                };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Clears the current scan and disposes resources.
        /// </summary>
        [McpTool("clear_scan", Description = "Clear current scan")]
        public object ClearScan()
        {
            try
            {
                _scanner?.Dispose();
                _scanner = null;
                _scanSettings = null;

                return new { ok = true };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Gets information about the current scan state.
        /// </summary>
        [McpTool("get_scan_info", Description = "Get current scan state")]
        public object GetScanInfo()
        {
            try
            {
                if (_scanner == null || _scanSettings == null)
                {
                    return new { initialized = false };
                }

                return new
                {
                    initialized = true,
                    valueType = _scanSettings.ValueType.ToString(),
                    resultCount = _scanner.TotalResultCount,
                    canUndoLastScan = _scanner.CanUndoLastScan
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        private ScanValueType ParseValueType(string type)
        {
            return type.ToLowerInvariant() switch
            {
                "byte" or "int8" or "uint8" => ScanValueType.Byte,
                "short" or "int16" or "ushort" or "uint16" => ScanValueType.Short,
                "int" or "int32" or "uint" or "uint32" => ScanValueType.Integer,
                "long" or "int64" or "ulong" or "uint64" => ScanValueType.Long,
                "float" => ScanValueType.Float,
                "double" => ScanValueType.Double,
                "string" => ScanValueType.String,
                "regex" => ScanValueType.Regex,
                "aob" or "bytes" => ScanValueType.ArrayOfBytes,
                _ => ScanValueType.Integer
            };
        }

        private IScanComparer CreateComparer(ScanValueType valueType, string compareType, string value)
        {
            var scanCompareType = ParseCompareType(compareType);
            var bitConverter = EndianBitConverter.System;

            try
            {
                switch (valueType)
                {
                    case ScanValueType.Byte:
                        return new ByteMemoryComparer(scanCompareType, byte.Parse(value), 0);

                    case ScanValueType.Short:
                        return new ShortMemoryComparer(scanCompareType, short.Parse(value), 0, bitConverter);

                    case ScanValueType.Integer:
                        return new IntegerMemoryComparer(scanCompareType, int.Parse(value), 0, bitConverter);

                    case ScanValueType.Long:
                        return new LongMemoryComparer(scanCompareType, long.Parse(value), 0, bitConverter);

                    case ScanValueType.Float:
                        return new FloatMemoryComparer(scanCompareType, ScanRoundMode.Normal, 2, float.Parse(value), 0, bitConverter);

                    case ScanValueType.Double:
                        return new DoubleMemoryComparer(scanCompareType, ScanRoundMode.Normal, 2, double.Parse(value), 0, bitConverter);

                    case ScanValueType.String:
                        return new StringMemoryComparer(value, System.Text.Encoding.UTF8, false);

                    case ScanValueType.ArrayOfBytes:
                        return new ArrayOfBytesMemoryComparer(BytePattern.Parse(value));

                    default:
                        return new IntegerMemoryComparer(scanCompareType, int.Parse(value), 0, bitConverter);
                }
            }
            catch
            {
                return null;
            }
        }

        private ScanCompareType ParseCompareType(string type)
        {
            return type.ToLowerInvariant() switch
            {
                "equal" or "eq" or "==" => ScanCompareType.Equal,
                "notequal" or "neq" or "!=" => ScanCompareType.NotEqual,
                "greater" or "gt" or ">" => ScanCompareType.GreaterThan,
                "greaterorequal" or "gte" or ">=" => ScanCompareType.GreaterThanOrEqual,
                "less" or "lt" or "<" => ScanCompareType.LessThan,
                "lessorequal" or "lte" or "<=" => ScanCompareType.LessThanOrEqual,
                "between" => ScanCompareType.Between,
                "betweenorequal" => ScanCompareType.BetweenOrEqual,
                "changed" => ScanCompareType.Changed,
                "notchanged" or "unchanged" => ScanCompareType.NotChanged,
                "increased" => ScanCompareType.Increased,
                "increasedorequal" => ScanCompareType.IncreasedOrEqual,
                "decreased" => ScanCompareType.Decreased,
                "decreasedorequal" => ScanCompareType.DecreasedOrEqual,
                _ => ScanCompareType.Equal
            };
        }

        private string FormatScanResult(ScanResult result)
        {
            try
            {
                // Read current value at address
                var data = _host.Process.ReadRemoteMemory(result.Address, result.ValueSize);
                return BitConverter.ToString(data).Replace("-", " ");
            }
            catch
            {
                return "(error)";
            }
        }
    }
}
