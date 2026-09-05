using System;
using System.Collections;

namespace BomberGhst.Cartridges
{
    /// Coroutine shaped so the same interface works in WebGL, where async/await
    /// over the wallet is awkward. Reads never need a signer; the write calls
    /// report the transaction hash once the wallet accepts.
    public interface ICartridgeProvider
    {
        string Label { get; }
        bool CanWrite { get; }

        IEnumerator LoadForPlayer(string owner, Action<CartridgeSnapshot> done, Action<string> fail);
        IEnumerator LoadById(string cartridgeId, Action<CartridgeSnapshot> done, Action<string> fail);

        IEnumerator Mint(string owner, Action<string> done, Action<string> fail);
        IEnumerator PayLineA(CartridgeSnapshot cart, Action<string> done, Action<string> fail);
        IEnumerator BindOwned(CartridgeSnapshot cart, string sourceTokenId, Action<string> done, Action<string> fail);
        IEnumerator BindStarter(CartridgeSnapshot cart, string templateId, Action<string> done, Action<string> fail);
        IEnumerator SaveCheckpoint(CartridgeSnapshot cart, BomberGhstSave save, string label,
            Action<string> done, Action<string> fail);
    }
}
