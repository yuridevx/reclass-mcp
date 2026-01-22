using System;
using System.Collections.Generic;
using System.Linq;
using McpPlugin.Server;
using McpPlugin.Utils;
using ReClassNET.Memory;
using ReClassNET.Plugins;

namespace McpPlugin.Api
{
    /// <summary>
    /// Disassembly API for MCP.
    /// Exposes ReClass.NET's Disassembler functionality.
    /// </summary>
    public class DisassemblerApi
    {
        private readonly IPluginHost _host;
        private Disassembler _disassembler;

        public DisassemblerApi(IPluginHost host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        private Disassembler GetDisassembler()
        {
            if (_disassembler == null)
            {
                _disassembler = new Disassembler(_host.Process.CoreFunctions);
            }
            return _disassembler;
        }

        /// <summary>
        /// Disassembles code at the specified address.
        /// Maps to: Disassembler.RemoteDisassembleCode
        /// </summary>
        [McpTool("disassemble", Description = "Disassemble code at address")]
        public object Disassemble(string addr, int length = 256, int maxInstructions = 50)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                if (length <= 0 || length > 0x10000)
                    length = 256;

                if (maxInstructions <= 0 || maxInstructions > 1000)
                    maxInstructions = 50;

                var address = AddressHelper.Parse(addr);
                var disasm = GetDisassembler();

                var instructions = disasm.RemoteDisassembleCode(_host.Process, address, length, maxInstructions);

                return new
                {
                    addr = AddressHelper.ToHexString(address),
                    count = instructions.Count,
                    instructions = instructions.Select(i => new
                    {
                        addr = AddressHelper.ToHexString(i.Address),
                        length = i.Length,
                        bytes = BitConverter.ToString(i.Data.Take(i.Length).ToArray()).Replace("-", " "),
                        instruction = i.Instruction
                    }).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Disassembles an entire function starting at the given address.
        /// Maps to: Disassembler.RemoteDisassembleFunction
        /// </summary>
        [McpTool("disassemble_function", Description = "Disassemble entire function at address")]
        public object DisassembleFunction(string addr, int maxLength = 4096)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                if (maxLength <= 0 || maxLength > 0x100000)
                    maxLength = 4096;

                var address = AddressHelper.Parse(addr);
                var disasm = GetDisassembler();

                var instructions = disasm.RemoteDisassembleFunction(_host.Process, address, maxLength);

                int totalBytes = instructions.Sum(i => i.Length);

                return new
                {
                    addr = AddressHelper.ToHexString(address),
                    count = instructions.Count,
                    totalBytes,
                    instructions = instructions.Select(i => new
                    {
                        addr = AddressHelper.ToHexString(i.Address),
                        length = i.Length,
                        bytes = BitConverter.ToString(i.Data.Take(i.Length).ToArray()).Replace("-", " "),
                        instruction = i.Instruction
                    }).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Gets the previous instruction before the given address.
        /// Maps to: Disassembler.RemoteGetPreviousInstruction
        /// </summary>
        [McpTool("get_previous_instruction", Description = "Get previous instruction before address")]
        public object GetPreviousInstruction(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var disasm = GetDisassembler();

                var instruction = disasm.RemoteGetPreviousInstruction(_host.Process, address);

                if (instruction == null || !instruction.IsValid)
                {
                    return new { found = false, addr = AddressHelper.ToHexString(address) };
                }

                return new
                {
                    found = true,
                    instruction = new
                    {
                        addr = AddressHelper.ToHexString(instruction.Address),
                        length = instruction.Length,
                        bytes = BitConverter.ToString(instruction.Data.Take(instruction.Length).ToArray()).Replace("-", " "),
                        instruction = instruction.Instruction
                    }
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Tries to find the start address of a function.
        /// Maps to: Disassembler.RemoteGetFunctionStartAddress
        /// </summary>
        [McpTool("get_function_start", Description = "Find function start address")]
        public object GetFunctionStart(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var disasm = GetDisassembler();

                var startAddress = disasm.RemoteGetFunctionStartAddress(_host.Process, address);

                return new
                {
                    addr = AddressHelper.ToHexString(address),
                    startAddress = AddressHelper.ToHexString(startAddress),
                    found = startAddress != IntPtr.Zero,
                    offset = startAddress != IntPtr.Zero ? (address.ToInt64() - startAddress.ToInt64()) : 0
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        /// <summary>
        /// Disassembles a single instruction at the given address.
        /// </summary>
        [McpTool("disassemble_instruction", Description = "Disassemble single instruction at address")]
        public object DisassembleInstruction(string addr)
        {
            try
            {
                if (!_host.Process.IsValid)
                    return new { error = "No process attached" };

                var address = AddressHelper.Parse(addr);
                var disasm = GetDisassembler();

                // Read max instruction length and get first instruction
                var instructions = disasm.RemoteDisassembleCode(_host.Process, address, Disassembler.MaximumInstructionLength, 1);

                if (instructions.Count == 0)
                {
                    return new { found = false, addr = AddressHelper.ToHexString(address) };
                }

                var instruction = instructions[0];
                return new
                {
                    found = true,
                    addr = AddressHelper.ToHexString(instruction.Address),
                    length = instruction.Length,
                    bytes = BitConverter.ToString(instruction.Data.Take(instruction.Length).ToArray()).Replace("-", " "),
                    instruction = instruction.Instruction,
                    nextAddr = AddressHelper.ToHexString(instruction.Address + instruction.Length)
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }
    }
}
