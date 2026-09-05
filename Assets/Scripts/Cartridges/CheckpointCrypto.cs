using System;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace BomberGhst.Cartridges
{
    /// The SIM backend rebuilds the checkpoint message from the game state it
    /// receives and rejects the save unless our message matches byte for byte,
    /// so the stringify, the hash and the message layout here all mirror
    /// lib/cartridgeSim.cjs exactly. Note the SIM hashes with SHA-256 while the
    /// cartridge diamond stores a keccak256 hash - they are not interchangeable.
    public static class CheckpointCrypto
    {
        /// Mirrors stableStringify: JSON.stringify(obj, Object.keys(obj).sort()),
        /// i.e. the same fields, ordered by key.
        public static string StableStringify(object value)
        {
            if (value == null) return "null";
            var fields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));

            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var field in fields)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(Escape(field.Name)).Append("\":");
                sb.Append(Literal(field.GetValue(value)));
            }
            return sb.Append('}').ToString();
        }

        static string Literal(object v)
        {
            switch (v)
            {
                case null: return "null";
                case string s: return "\"" + Escape(s) + "\"";
                case bool b: return b ? "true" : "false";
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case long l: return l.ToString(CultureInfo.InvariantCulture);
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                default:
                    throw new NotSupportedException(
                        "Checkpoint state may only hold strings, integers and bools: got " + v.GetType().Name);
            }
        }

        /// JSON string escaping as JSON.stringify does it.
        static string Escape(string s)
        {
            var sb = new StringBuilder(s.Length + 2);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        /// SIM state hash: 0x prefixed sha256 over the stable stringify.
        public static string HashGameState(object gameState)
        {
            using var sha = SHA256.Create();
            var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(StableStringify(gameState)));
            var sb = new StringBuilder("0x", 66);
            foreach (var b in digest) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        /// The exact message the SIM expects the cartridge owner to sign.
        public static string SimMessage(string cartridgeId, int nonce, string stateHash) =>
            "Aarcade cartridge checkpoint\n" +
            "cartridgeId: " + cartridgeId + "\n" +
            "nonce: " + nonce + "\n" +
            "stateHash: " + stateHash;

        /// The on-chain variant, kept separate because the field labels and the
        /// hash function both differ from the SIM one.
        public static string ChainMessage(string cartridgeId, int nonce, string keccakStateHash) =>
            "Aarcade cartridge checkpoint\n" +
            "Cartridge: " + cartridgeId + "\n" +
            "Nonce: " + nonce + "\n" +
            "State: " + keccakStateHash;
    }
}
