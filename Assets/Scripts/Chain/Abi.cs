using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace BomberGhst.Chain
{
    /// One ABI argument. Constructed through the named helpers so call sites
    /// state the solidity type instead of relying on C# type guessing.
    public readonly struct AbiValue
    {
        public readonly bool IsDynamic;
        public readonly string Head;   // 64 hex chars for static values
        public readonly string Tail;   // hex payload for dynamic values

        AbiValue(bool dynamic, string head, string tail)
        {
            IsDynamic = dynamic; Head = head; Tail = tail;
        }

        public static AbiValue Uint(BigInteger v)
        {
            if (v.Sign < 0) throw new ArgumentOutOfRangeException(nameof(v), "uint256 cannot be negative");
            return new AbiValue(false, Hex.Pad32(v.ToString("x", CultureInfo.InvariantCulture)), null);
        }

        public static AbiValue Uint(long v) => Uint(new BigInteger(v));

        public static AbiValue Address(string address)
        {
            var raw = Hex.Strip(address);
            if (raw.Length != 40) throw new ArgumentException($"Bad address '{address}'", nameof(address));
            return new AbiValue(false, Hex.Pad32(raw), null);
        }

        public static AbiValue Bytes32(string hex)
        {
            var raw = Hex.Strip(hex);
            if (raw.Length > 64) throw new ArgumentException($"Bad bytes32 '{hex}'", nameof(hex));
            // bytes32 is right padded, unlike numbers
            return new AbiValue(false, raw.PadRight(64, '0'), null);
        }

        public static AbiValue Bool(bool v) => new AbiValue(false, Hex.Pad32(v ? "1" : "0"), null);

        public static AbiValue Str(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            var sb = new StringBuilder();
            sb.Append(Hex.Pad32(bytes.Length.ToString("x", CultureInfo.InvariantCulture)));
            var body = Hex.Strip(Hex.Encode(bytes));
            sb.Append(body);
            int pad = (32 - bytes.Length % 32) % 32;
            sb.Append(new string('0', pad * 2));
            return new AbiValue(true, null, sb.ToString());
        }
    }

    public static class Abi
    {
        /// First four bytes of keccak256 over the canonical signature.
        public static string Selector(string signature)
        {
            var hash = Keccak.Hash(signature);
            return "0x" + Hex.Strip(Hex.Encode(hash)).Substring(0, 8);
        }

        public static string EncodeCall(string signature, params AbiValue[] args)
        {
            args ??= Array.Empty<AbiValue>();
            var head = new StringBuilder();
            var tail = new StringBuilder();
            int dynamicBase = args.Length * 32;

            foreach (var arg in args)
            {
                if (!arg.IsDynamic)
                {
                    head.Append(arg.Head);
                    continue;
                }
                int offset = dynamicBase + tail.Length / 2;
                head.Append(Hex.Pad32(offset.ToString("x", CultureInfo.InvariantCulture)));
                tail.Append(arg.Tail);
            }

            return Selector(signature) + head + tail;
        }
    }

    /// Word-wise reader over an eth_call result.
    public class AbiReader
    {
        readonly string data;      // hex, no 0x
        readonly int baseOffset;   // byte offset this reader is rooted at

        public AbiReader(string hex, int baseOffset = 0)
        {
            data = Hex.Strip(hex);
            this.baseOffset = baseOffset;
        }

        public bool IsEmpty => data.Length == 0;
        public int WordCount => Math.Max(0, (data.Length / 2 - baseOffset) / 32);

        public string Word(int index)
        {
            int start = (baseOffset + index * 32) * 2;
            if (start < 0 || start + 64 > data.Length) return new string('0', 64);
            return data.Substring(start, 64);
        }

        public BigInteger Uint(int index) => Hex.ToBigInteger(Word(index));

        public bool Bool(int index) => !Hex.IsZeroWord(Word(index));

        public string Address(int index) => "0x" + Word(index).Substring(24);

        public string Bytes32(int index) => "0x" + Word(index);

        /// Re-roots at the byte offset stored in the given word, for following
        /// the pointer that fronts a dynamic value or tuple.
        public AbiReader Follow(int index)
        {
            int offset = (int)Uint(index);
            return new AbiReader(data, baseOffset + offset);
        }

        /// Reads a dynamic string whose offset lives in the given head word.
        public string StringAt(int index)
        {
            var body = Follow(index);
            int length = (int)body.Uint(0);
            if (length <= 0) return string.Empty;
            int start = (body.baseOffset + 32) * 2;
            if (start + length * 2 > data.Length) return string.Empty;
            var bytes = Hex.Decode(data.Substring(start, length * 2));
            return Encoding.UTF8.GetString(bytes);
        }

        /// Reads a dynamic bytes32[] whose offset lives in the given head word.
        public List<string> Bytes32ArrayAt(int index)
        {
            var body = Follow(index);
            int count = (int)body.Uint(0);
            var list = new List<string>(Math.Max(0, count));
            for (int i = 0; i < count; i++) list.Add(body.Bytes32(1 + i));
            return list;
        }
    }
}
