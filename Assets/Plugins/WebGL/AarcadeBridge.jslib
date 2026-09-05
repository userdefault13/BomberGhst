mergeInto(LibraryManager.library, {
  AarcadeNotifyWalletReady: function (walletPtr) {
    var wallet = UTF8ToString(walletPtr);
    try {
      if (window.AarcadeBridge && typeof window.AarcadeBridge.OnWalletReady === 'function') {
        window.AarcadeBridge.OnWalletReady(wallet);
      }
    } catch (e) {}
  },

  WebGL_GetPendingAarcadeSession: function (buffer, bufferSize) {
    var json = '';
    try {
      if (typeof window !== 'undefined') {
        var pending = window.__AARCADE_PENDING_SESSION || window.__ROFL_PENDING_SESSION;
        if (pending) {
          json = typeof pending === 'string' ? pending : JSON.stringify(pending);
        }
      }
    } catch (e) {}

    var sessionText = json || '';
    var sessionBytes = lengthBytesUTF8(sessionText) + 1;
    if (sessionBytes > bufferSize) {
      console.warn('[BomberGhst] Pending session too large for buffer - skipped');
      stringToUTF8('', buffer, bufferSize);
    } else {
      stringToUTF8(sessionText, buffer, bufferSize);
    }
  }
});
