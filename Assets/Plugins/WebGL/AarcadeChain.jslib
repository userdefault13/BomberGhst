mergeInto(LibraryManager.library, {
  AarcadeChain_HasProvider: function () {
    try {
      if (typeof window === 'undefined') return 0;
      if (window.AarcadeChain && window.AarcadeChain.provider) return 1;
      if (window.ethereum) return 1;
      return 0;
    } catch (e) {
      return 0;
    }
  },

  AarcadeChain_Request: function (idPtr, methodPtr, paramsPtr, goPtr, cbPtr) {
    var id = UTF8ToString(idPtr);
    var method = UTF8ToString(methodPtr);
    var paramsJson = UTF8ToString(paramsPtr);
    var go = UTF8ToString(goPtr);
    var cb = UTF8ToString(cbPtr);

    function reply(ok, value, error) {
      try {
        SendMessage(go, cb, JSON.stringify({
          id: id,
          ok: !!ok,
          value: value === undefined || value === null ? '' : String(value),
          error: error || ''
        }));
      } catch (e) {}
    }

    var provider = null;
    try {
      provider = (window.AarcadeChain && window.AarcadeChain.provider) || window.ethereum || null;
    } catch (e) {}

    if (!provider || typeof provider.request !== 'function') {
      reply(false, '', 'No wallet provider available in this frame.');
      return;
    }

    var args = [];
    try {
      args = paramsJson ? JSON.parse(paramsJson) : [];
    } catch (e) {
      reply(false, '', 'Bad request params.');
      return;
    }

    try {
      provider
        .request({ method: method, params: args })
        .then(function (res) {
          reply(true, typeof res === 'string' ? res : JSON.stringify(res), '');
        })
        .catch(function (err) {
          reply(false, '', err && err.message ? err.message : String(err));
        });
    } catch (e) {
      reply(false, '', e && e.message ? e.message : String(e));
    }
  }
});
