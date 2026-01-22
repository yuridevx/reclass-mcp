using System;
using System.Globalization;

namespace McpPlugin.Utils
{
    /// <summary>
    /// Utility for parsing address strings in various formats.
    /// </summary>
    public static class AddressHelper
    {
        /// <summary>
        /// Parses an address string to IntPtr.
        /// Supports formats: 0x1234, 1234, 0x1234ABCD
        /// </summary>
        public static IntPtr Parse(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be empty", nameof(address));

            address = address.Trim();

            // Handle hex prefix
            if (address.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                address.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
            {
                address = address.Substring(2);
            }

            // Try parse as hex first, then as decimal
            if (long.TryParse(address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
            {
                return new IntPtr(hexValue);
            }

            if (long.TryParse(address, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decValue))
            {
                return new IntPtr(decValue);
            }

            throw new ArgumentException($"Invalid address format: {address}", nameof(address));
        }

        /// <summary>
        /// Tries to parse an address string.
        /// </summary>
        public static bool TryParse(string address, out IntPtr result)
        {
            result = IntPtr.Zero;
            try
            {
                result = Parse(address);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Formats an IntPtr as a hex string.
        /// </summary>
        public static string ToHexString(IntPtr address)
        {
#if RECLASSNET64
            return $"0x{address.ToInt64():X16}";
#else
            return $"0x{address.ToInt32():X8}";
#endif
        }

        /// <summary>
        /// Formats a long as a hex string appropriate for the platform.
        /// </summary>
        public static string ToHexString(long address)
        {
#if RECLASSNET64
            return $"0x{address:X16}";
#else
            return $"0x{(int)address:X8}";
#endif
        }
    }
}
