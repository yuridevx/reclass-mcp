using System;
using System.Text;
using ReClassNET.Extensions;
using McpPlugin.Server;
using McpPlugin.Utils;
using ReClassNET.Plugins;

namespace McpPlugin.Api
{
    /// <summary>
    /// Memory read/write API for MCP.
    /// Exposes ReClass.NET's IRemoteMemoryReader/Writer methods.
    /// </summary>
    public class MemoryApi
    {
        private readonly IPluginHost _host;

        public MemoryApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>
        /// Reads raw bytes from memory.
        /// Maps to: RemoteProcess.ReadRemoteMemory(IntPtr, int)
        /// </summary>
        [McpTool("read_memory", Description = "Read raw bytes from memory")]
        public object ReadMemory(string addr, int size)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                if (size <= 0 || size > 0x10000)
                    return new { error = "Invalid size (max 64KB)" };

                var address = AddressHelper.Parse(addr);
                var data = _host.Process.ReadRemoteMemory(address, size);

                return new
                {
                    addr = AddressHelper.ToHexString(address),
                    size = data.Length,
                    hex = BitConverter.ToString(data).Replace("-", " ")
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads a signed 8-bit integer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteInt8
        /// </summary>
        [McpTool("read_int8", Description = "Read signed 8-bit integer")]
        public object ReadInt8(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteInt8(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads an unsigned 8-bit integer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteUInt8
        /// </summary>
        [McpTool("read_uint8", Description = "Read unsigned 8-bit integer")]
        public object ReadUInt8(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteUInt8(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads a signed 16-bit integer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteInt16
        /// </summary>
        [McpTool("read_int16", Description = "Read signed 16-bit integer")]
        public object ReadInt16(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteInt16(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteUInt16
        /// </summary>
        [McpTool("read_uint16", Description = "Read unsigned 16-bit integer")]
        public object ReadUInt16(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteUInt16(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads a signed 32-bit integer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteInt32
        /// </summary>
        [McpTool("read_int32", Description = "Read signed 32-bit integer")]
        public object ReadInt32(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteInt32(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteUInt32
        /// </summary>
        [McpTool("read_uint32", Description = "Read unsigned 32-bit integer")]
        public object ReadUInt32(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteUInt32(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads a signed 64-bit integer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteInt64
        /// </summary>
        [McpTool("read_int64", Description = "Read signed 64-bit integer")]
        public object ReadInt64(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteInt64(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteUInt64
        /// </summary>
        [McpTool("read_uint64", Description = "Read unsigned 64-bit integer")]
        public object ReadUInt64(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteUInt64(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads a 32-bit float from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteFloat
        /// </summary>
        [McpTool("read_float", Description = "Read 32-bit float")]
        public object ReadFloat(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteFloat(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads a 64-bit double from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteDouble
        /// </summary>
        [McpTool("read_double", Description = "Read 64-bit double")]
        public object ReadDouble(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteDouble(address);

                return new { addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads a pointer from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteIntPtr
        /// </summary>
        [McpTool("read_intptr", Description = "Read pointer value")]
        public object ReadIntPtr(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var value = _host.Process.ReadRemoteIntPtr(address);

                return new
                {
                    addr = AddressHelper.ToHexString(address),
                    value = AddressHelper.ToHexString(value),
                    isNull = value == IntPtr.Zero
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Reads a string from memory.
        /// Maps to: IRemoteMemoryReaderExtension.ReadRemoteString
        /// </summary>
        [McpTool("read_string", Description = "Read string from memory")]
        public object ReadString(string addr, string encoding = "utf8", int length = 256)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                if (length <= 0 || length > 4096)
                    length = 256;

                var address = AddressHelper.Parse(addr);
                var enc = GetEncoding(encoding);

                // Use ReClass.NET's ReadRemoteString extension method
                var value = _host.Process.ReadRemoteString(address, enc, length);

                return new
                {
                    addr = AddressHelper.ToHexString(address),
                    encoding,
                    length = value.Length,
                    value
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Writes raw bytes to memory.
        /// Maps to: RemoteProcess.WriteRemoteMemory(IntPtr, byte[])
        /// </summary>
        [McpTool("write_memory", Description = "Write raw bytes to memory")]
        public object WriteMemory(string addr, string hex)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var bytes = ParseHexBytes(hex);

                _host.Process.WriteRemoteMemory(address, bytes);

                return new { ok = true, addr = AddressHelper.ToHexString(address), size = bytes.Length };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a signed 8-bit integer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(sbyte)
        /// </summary>
        [McpTool("write_int8", Description = "Write signed 8-bit integer")]
        public object WriteInt8(string addr, sbyte value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes an unsigned 8-bit integer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(byte)
        /// </summary>
        [McpTool("write_uint8", Description = "Write unsigned 8-bit integer")]
        public object WriteUInt8(string addr, byte value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a signed 16-bit integer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(short)
        /// </summary>
        [McpTool("write_int16", Description = "Write signed 16-bit integer")]
        public object WriteInt16(string addr, short value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes an unsigned 16-bit integer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(ushort)
        /// </summary>
        [McpTool("write_uint16", Description = "Write unsigned 16-bit integer")]
        public object WriteUInt16(string addr, ushort value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a signed 32-bit integer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(int)
        /// </summary>
        [McpTool("write_int32", Description = "Write signed 32-bit integer")]
        public object WriteInt32(string addr, int value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes an unsigned 32-bit integer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(uint)
        /// </summary>
        [McpTool("write_uint32", Description = "Write unsigned 32-bit integer")]
        public object WriteUInt32(string addr, uint value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a signed 64-bit integer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(long)
        /// </summary>
        [McpTool("write_int64", Description = "Write signed 64-bit integer")]
        public object WriteInt64(string addr, long value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes an unsigned 64-bit integer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(ulong)
        /// </summary>
        [McpTool("write_uint64", Description = "Write unsigned 64-bit integer")]
        public object WriteUInt64(string addr, ulong value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a 32-bit float to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(float)
        /// </summary>
        [McpTool("write_float", Description = "Write 32-bit float")]
        public object WriteFloat(string addr, float value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a 64-bit double to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(double)
        /// </summary>
        [McpTool("write_double", Description = "Write 64-bit double")]
        public object WriteDouble(string addr, double value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                _host.Process.WriteRemoteMemory(address, value);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a pointer to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(IntPtr)
        /// </summary>
        [McpTool("write_intptr", Description = "Write pointer value")]
        public object WriteIntPtr(string addr, string value)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var ptrValue = AddressHelper.Parse(value);
                _host.Process.WriteRemoteMemory(address, ptrValue);

                return new { ok = true, addr = AddressHelper.ToHexString(address), value = AddressHelper.ToHexString(ptrValue) };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Writes a string to memory.
        /// Maps to: IRemoteMemoryWriterExtension.WriteRemoteMemory(string, Encoding)
        /// </summary>
        [McpTool("write_string", Description = "Write string to memory")]
        public object WriteString(string addr, string value, string encoding = "utf8", bool nullTerminate = true)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { ok = false, error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var enc = GetEncoding(encoding);
                var str = nullTerminate ? value + '\0' : value;
                _host.Process.WriteRemoteMemory(address, str, enc);

                return new { ok = true, addr = AddressHelper.ToHexString(address), length = value.Length };
            }
            catch (Exception ex)
            {
                return new { ok = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Reads RTTI class name from a vtable pointer.
        /// Maps to: RemoteProcess.ReadRemoteRuntimeTypeInformation
        /// </summary>
        [McpTool("read_rtti", Description = "Read RTTI class name from vtable pointer")]
        public object ReadRtti(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var className = _host.Process.ReadRemoteRuntimeTypeInformation(address);

                return new
                {
                    addr = AddressHelper.ToHexString(address),
                    className,
                    found = !string.IsNullOrEmpty(className)
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Parses an address formula using ReClass.NET's address parser.
        /// Maps to: RemoteProcess.ParseAddress
        /// Supports: &lt;module&gt;+offset, &lt;module&gt;, or hex/decimal math expressions.
        /// </summary>
        [McpTool("parse_address", Description = "Parse address formula (e.g., '<module>+0x1234' or hex expression)")]
        public object ParseAddress(string formula)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                // ReClass.NET's ParseAddress expects <module> syntax for module names
                // or pure math expressions with hex/decimal numbers
                var result = _host.Process.ParseAddress(formula);

                return new
                {
                    formula,
                    result = AddressHelper.ToHexString(result),
                    isNull = result == IntPtr.Zero
                };
            }
            catch (Exception ex)
            {
                return new { formula, error = ex.Message };
            }
        }

        private Encoding GetEncoding(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "utf8" or "utf-8" => Encoding.UTF8,
                "utf16" or "utf-16" or "unicode" => Encoding.Unicode,
                "utf32" or "utf-32" => Encoding.UTF32,
                "ascii" => Encoding.ASCII,
                _ => Encoding.UTF8
            };
        }

        private byte[] ParseHexBytes(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "").Replace("0x", "");
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Invalid hex string length");

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}
