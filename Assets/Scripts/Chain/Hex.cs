using System;
using System.Globalization;
using System.Numerics;

namespace BomberGhst.Chain
{
    public static class Hex
    {
        public static string Encode(byte[] bytes)
        {
            if (bytes == null) return "0x";
            var sb = new System.Text.StringBuilder(bytes.Length * 2 + 2);
            sb.Append("0x");
            foreach (var b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        public static byte[] Decode(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);
            if (hex.Length % 2 != 0) hex = "0" + hex;
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return bytes;
        }

        public static string Strip(string hex) =>
            string.IsNullOrEmpty(hex) ? string.Empty
            : hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex.Substring(2) : hex;

        /// Parses an 0x quantity or data blob as an unsigned big integer.
        public static BigInteger ToBigInteger(string hex)
        {
            var raw = Strip(hex);
            if (raw.Length == 0) return BigInteger.Zero;
            // prepend 00 so the value is always read as positive
            return BigInteger.Parse("00" + raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        /// Minimal 0x quantity, as eth JSON-RPC expects for value/gas fields.
        public static string Quantity(BigInteger value)
        {
            if (value.IsZero) return "0x0";
            var s = value.ToString("x", CultureInfo.InvariantCulture).TrimStart('0');
            return "0x" + (s.Length == 0 ? "0" : s);
        }

        /// Left-pads to a full 32 byte ABI word.
        public static string Pad32(string hexNoPrefix)
        {
            var raw = Strip(hexNoPrefix);
            return raw.Length >= 64 ? raw.Substring(raw.Length - 64) : raw.PadLeft(64, '0');
        }

        public static bool IsZeroWord(string word) =>
            string.IsNullOrEmpty(word) || Strip(word).TrimStart('0').Length == 0;

        /// 0x1234…ABCD, for showing addresses on a 320px wide screen.
        public static string Shorten(string address, int lead = 6, int tail = 4)
        {
            if (string.IsNullOrEmpty(address)) return "";
            if (address.Length <= lead + tail + 2) return address;
            return address.Substring(0, lead) + ".." + address.Substring(address.Length - tail);
        }
    }
}
