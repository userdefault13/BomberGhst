using System;
using System.Collections.Generic;
using System.Numerics;
using BomberGhst.Chain;
using UnityEditor;
using UnityEngine;

namespace BomberGhst.EditorTools
{
    /// Checks the hand rolled keccak and ABI codec against vectors produced by
    /// foundry's cast, so a silent change there cannot ship wrong selectors.
    public static class ChainSelfTest
    {
        static readonly List<string> failures = new List<string>();

        [MenuItem("BomberGhst/Run Chain Self Test")]
        public static void Run()
        {
            failures.Clear();

            // keccak256, including an input longer than the 136 byte rate
            Eq("keccak('')", Keccak.HashHex(""),
                "0xc5d2460186f7233c927e7db2dcc703c0e500b653ca82273b7bfad8045d85a470");
            Eq("keccak('abc')", Keccak.HashHex("abc"),
                "0x4e03657aea45a94fc7d47ba826c8d667c0d1e6e33a64a036ec44f58fa12d6c45");
            Eq("keccak(gameId)", Keccak.HashHex("bomberghst"),
                "0xc73acbd1c412746eaf7be7a38955802a0c7707aa1e558fb1ea76c64bb9e9bd1e");
            Eq("keccak(multi block)", Keccak.HashHex(Repeat("bomberghst-", 20)),
                "0x00e9481f09a2f540d9e61b8dc34b0727aa0cd2bb4d85f2ce4858316ac7e64e2c");

            // function selectors
            Eq("sel playerCartridge", Abi.Selector("playerCartridge(bytes32,address)"), "0x97ace413");
            Eq("sel ownerOf", Abi.Selector("ownerOf(uint256)"), "0x6352211e");
            Eq("sel getGameId", Abi.Selector("getGameId(uint256)"), "0x534ab7cb");
            Eq("sel lineAPaid", Abi.Selector("lineAPaid(uint256)"), "0x184014a2");
            Eq("sel pocketBalance", Abi.Selector("pocketBalance(uint256,bytes32)"), "0x21c10a14");
            Eq("sel getCheckpoint", Abi.Selector("getCheckpoint(uint256)"), "0x20fc4881");
            Eq("sel getActiveHeroId", Abi.Selector("getActiveHeroId(uint256)"), "0xe6468b9a");
            Eq("sel mintCartridge", Abi.Selector("mintCartridge(bytes32,address)"), "0xd4eaad10");
            Eq("sel bindOwned", Abi.Selector("bindOwned(uint256,uint256)"), "0x75002762");
            Eq("sel bindStarter", Abi.Selector("bindStarter(uint256,bytes32,address)"), "0x100cc61b");
            Eq("sel checkpointSave", Abi.Selector("checkpointSave(uint256,bytes32,string)"), "0xc133a58f");
            Eq("sel payLineA", Abi.Selector("payLineA(uint256)"), "0x41c366b5");

            // static argument encoding
            Eq("encode playerCartridge",
                Abi.EncodeCall("playerCartridge(bytes32,address)",
                    AbiValue.Bytes32(Keccak.HashHex("bomberghst")),
                    AbiValue.Address("0x2127AA7265D573Aa467f1D73554D17890b872E76")),
                "0x97ace413c73acbd1c412746eaf7be7a38955802a0c7707aa1e558fb1ea76c64bb9e9bd1e" +
                "0000000000000000000000002127aa7265d573aa467f1d73554d17890b872e76");

            // dynamic string encoding
            Eq("encode checkpointSave",
                Abi.EncodeCall("checkpointSave(uint256,bytes32,string)",
                    AbiValue.Uint(7),
                    AbiValue.Bytes32("0x1122334455667788990011223344556677889900112233445566778899001122"),
                    AbiValue.Str("aarcade://bomberghst/7/3")),
                "0xc133a58f0000000000000000000000000000000000000000000000000000000000000007" +
                "1122334455667788990011223344556677889900112233445566778899001122" +
                "0000000000000000000000000000000000000000000000000000000000000060" +
                "0000000000000000000000000000000000000000000000000000000000000018" +
                "616172636164653a2f2f626f6d626572676873742f372f330000000000000000");

            // decoding the checkpoint tuple, first the live empty one from Base
            // Sepolia cartridge #1, then a populated one
            var empty = new AbiReader(
                "0x0000000000000000000000000000000000000000000000000000000000000020" +
                new string('0', 64 * 5)).Follow(0);
            Eq("decode empty nonce", empty.Uint(0).ToString(), "0");
            Eq("decode empty uri", "[" + empty.StringAt(2) + "]", "[]");

            var full = new AbiReader(
                "0x0000000000000000000000000000000000000000000000000000000000000020" +
                "0000000000000000000000000000000000000000000000000000000000000009" +
                "00000000000000000000000000000000000000000000000000000000000000ff" +
                "0000000000000000000000000000000000000000000000000000000000000080" +
                "000000000000000000000000000000000000000000000000000000006553f100" +
                "000000000000000000000000000000000000000000000000000000000000000b" +
                "697066733a2f2f63702f39000000000000000000000000000000000000000000").Follow(0);
            Eq("decode nonce", full.Uint(0).ToString(), "9");
            Eq("decode hash", full.Bytes32(1),
                "0x00000000000000000000000000000000000000000000000000000000000000ff");
            Eq("decode uri", full.StringAt(2), "ipfs://cp/9");
            Eq("decode savedAt", full.Uint(3).ToString(), "1700000000");

            // misc helpers
            Eq("uint decode", new AbiReader(
                "0x0000000000000000000000000000000000000000000000056bc75e2d63100000").Uint(0).ToString(),
                "100000000000000000000");
            Eq("address decode", new AbiReader(
                "0x0000000000000000000000002127aa7265d573aa467f1d73554d17890b872e76").Address(0),
                "0x2127aa7265d573aa467f1d73554d17890b872e76");
            Eq("quantity", Hex.Quantity(new BigInteger(84532)), "0x14a34");
            Eq("bool decode true", new AbiReader(
                "0x0000000000000000000000000000000000000000000000000000000000000001").Bool(0).ToString(), "True");

            // SIM checkpoint crypto, against lib/cartridgeSim.cjs run in node
            var save = new Cartridges.BomberGhstSave
            {
                schemaVersion = 1, matchesPlayed = 12, roundsWon = 31, matchesWon = 4,
                bombsPlaced = 987, suddenDeaths = 3,
                lastPlayedUtc = "2026-09-04T12:00:00.0000000Z",
            };
            Eq("sim stableStringify", Cartridges.CheckpointCrypto.StableStringify(save),
                "{\"bombsPlaced\":987,\"lastPlayedUtc\":\"2026-09-04T12:00:00.0000000Z\"," +
                "\"matchesPlayed\":12,\"matchesWon\":4,\"roundsWon\":31,\"schemaVersion\":1," +
                "\"suddenDeaths\":3}");
            Eq("sim state hash", Cartridges.CheckpointCrypto.HashGameState(save),
                "0xa29898f57b56fda21d9c823d890f59026c7be44a64d6617bfc411216b33ecde5");
            Eq("sim message",
                Cartridges.CheckpointCrypto.SimMessage("sim-abc123", 4,
                    Cartridges.CheckpointCrypto.HashGameState(save)),
                "Aarcade cartridge checkpoint\ncartridgeId: sim-abc123\nnonce: 4\n" +
                "stateHash: 0xa29898f57b56fda21d9c823d890f59026c7be44a64d6617bfc411216b33ecde5");

            var fresh = new Cartridges.BomberGhstSave { lastPlayedUtc = "" };
            Eq("sim hash of a fresh save", Cartridges.CheckpointCrypto.HashGameState(fresh),
                "0x02562729acec4a5a73937879121b5cc85cb4e629e00774e48a0fec4981472ee4");

            // pocket units: the SIM reports whole tokens, the game keeps wei
            Eq("sim pocket to wei", Cartridges.SimApi.ToWei("100"), "100000000000000000000");
            Eq("sim pocket fraction", Cartridges.SimApi.ToWei("1.5"), "1500000000000000000");

            // bound hero collateral -> sprite row. Paaint names haunt 1 art ma*
            // and haunt 2 am*, while a bound hero carries the bare symbol.
            Eq("row weth haunt1", Row("weth", 1, 0), "maweth");
            Eq("row dai haunt1", Row("dai", 1, 0), "madai");
            Eq("row dai haunt2", Row("dai", 2, 0), "amdai");
            Eq("row matic haunt2", Row("matic", 2, 0), "amwmatic");
            Eq("row already prefixed", Row("madai", 1, 0), "madai");
            Eq("row unbound falls back to slot 0", Row(null, 1, 0), "mauni");
            Eq("row unknown falls back to slot 2", Row("notacollateral", 1, 2), "mausdt");

            if (failures.Count == 0)
            {
                Debug.Log("ChainSelfTest: all checks passed.");
                return;
            }

            foreach (var f in failures) Debug.LogError("ChainSelfTest: " + f);
            Debug.LogError($"ChainSelfTest: {failures.Count} check(s) failed.");
        }

        /// Batch entry point: non zero exit when anything fails.
        public static void RunHeadless()
        {
            Run();
            EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
        }

        /// Resolves a collateral to the sprite row's collateral name, so the
        /// expectations below read as art rather than as indices.
        static string Row(string collateral, int haunt, int slot)
        {
            int row = GotchiArt.RowFor(collateral, haunt, slot);
            var manifest = UnityEngine.Resources.Load<TextAsset>("GotchiManifest");
            if (manifest == null) return "<no manifest>";
            var match = System.Text.RegularExpressions.Regex.Match(
                manifest.text, "\\{\\s*\"row\": " + row + ",\\s*\"collateral\": \"([a-zA-Z]+)\"");
            return match.Success ? match.Groups[1].Value : "<row " + row + ">";
        }

        static string Repeat(string s, int times)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < times; i++) sb.Append(s);
            return sb.ToString();
        }

        static void Eq(string what, string actual, string expected)
        {
            if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) return;
            failures.Add($"{what}\n  expected {expected}\n  actual   {actual}");
        }
    }
}
