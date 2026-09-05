using System;
using System.Text;

namespace BomberGhst.Chain
{
    /// Keccak-256 as Ethereum uses it (0x01 padding, not SHA3's 0x06).
    /// Needed for function selectors and for hashing game ids into bytes32.
    public static class Keccak
    {
        const int Rate = 136;   // 1088 bits for a 256 bit digest

        static readonly ulong[] Rc =
        {
            0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808aUL, 0x8000000080008000UL,
            0x000000000000808bUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
            0x000000000000008aUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000aUL,
            0x000000008000808bUL, 0x800000000000008bUL, 0x8000000000008089UL, 0x8000000000008003UL,
            0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800aUL, 0x800000008000000aUL,
            0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL,
        };

        // rotation offsets indexed as x + 5*y
        static readonly int[] Rot =
        {
             0,  1, 62, 28, 27,
            36, 44,  6, 55, 20,
             3, 10, 43, 25, 39,
            41, 45, 15, 21,  8,
            18,  2, 61, 56, 14,
        };

        public static byte[] Hash(byte[] input)
        {
            input ??= Array.Empty<byte>();
            var state = new ulong[25];

            int offset = 0;
            while (input.Length - offset >= Rate)
            {
                Absorb(state, input, offset);
                Permute(state);
                offset += Rate;
            }

            var tail = new byte[Rate];
            int remaining = input.Length - offset;
            Buffer.BlockCopy(input, offset, tail, 0, remaining);
            tail[remaining] = 0x01;
            tail[Rate - 1] |= 0x80;
            Absorb(state, tail, 0);
            Permute(state);

            var digest = new byte[32];
            for (int i = 0; i < 4; i++)
                for (int b = 0; b < 8; b++)
                    digest[i * 8 + b] = (byte)(state[i] >> (8 * b));
            return digest;
        }

        public static byte[] Hash(string utf8) => Hash(Encoding.UTF8.GetBytes(utf8 ?? string.Empty));

        /// keccak256 of the UTF-8 bytes, as an 0x-prefixed bytes32.
        public static string HashHex(string utf8) => Hex.Encode(Hash(utf8));

        static void Absorb(ulong[] state, byte[] data, int offset)
        {
            for (int i = 0; i < Rate / 8; i++)
            {
                ulong lane = 0;
                for (int b = 0; b < 8; b++) lane |= (ulong)data[offset + i * 8 + b] << (8 * b);
                state[i] ^= lane;
            }
        }

        static ulong Rotl(ulong v, int n) => n == 0 ? v : (v << n) | (v >> (64 - n));

        static void Permute(ulong[] a)
        {
            var c = new ulong[5];
            var d = new ulong[5];
            var b = new ulong[25];

            for (int round = 0; round < 24; round++)
            {
                // theta
                for (int x = 0; x < 5; x++)
                    c[x] = a[x] ^ a[x + 5] ^ a[x + 10] ^ a[x + 15] ^ a[x + 20];
                for (int x = 0; x < 5; x++)
                    d[x] = c[(x + 4) % 5] ^ Rotl(c[(x + 1) % 5], 1);
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 5; x++)
                        a[x + 5 * y] ^= d[x];

                // rho + pi
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 5; x++)
                        b[y + 5 * ((2 * x + 3 * y) % 5)] = Rotl(a[x + 5 * y], Rot[x + 5 * y]);

                // chi
                for (int y = 0; y < 5; y++)
                    for (int x = 0; x < 5; x++)
                        a[x + 5 * y] = b[x + 5 * y] ^ (~b[(x + 1) % 5 + 5 * y] & b[(x + 2) % 5 + 5 * y]);

                // iota
                a[0] ^= Rc[round];
            }
        }
    }
}
