/**
 * obs-source.js
 *
 * Connects to the OBS WebSocket v5 server and repeatedly captures
 * screenshots of the current program scene.  Dispatches a custom
 * "obsframe" event on the document with { imageBitmap } for other
 * modules to consume.
 *
 * OBS WebSocket v5 opcode reference:
 *   0  Hello         – server → client after TCP open
 *   1  Identify      – client → server (auth response)
 *   2  Identified    – server → client on success
 *   6  Request       – client → server
 *   7  RequestResponse – server → client
 */

export class ObsSource {
  /** @param {object} opts */
  constructor(opts = {}) {
    this._host = opts.host ?? 'localhost';
    this._port = opts.port ?? '4455';
    this._password = opts.password ?? '';
    this._fps = Math.max(1, Math.min(60, Number(opts.fps ?? 30)));

    this._ws = null;
    this._connected = false;
    this._pendingRequests = new Map(); // requestId → { resolve, reject }
    this._captureTimer = null;
    this._sceneName = null;

    this._onStatus = opts.onStatus ?? (() => {});
  }

  // ── Public API ─────────────────────────────────────────────────────────────

  async connect() {
    const url = `ws://${this._host}:${this._port}`;
    this._onStatus(`Connecting to OBS at ${url}…`);

    return new Promise((resolve, reject) => {
      this._ws = new WebSocket(url);

      this._ws.addEventListener('open', () => {
        // Hello will arrive shortly; handled in onmessage
      });

      this._ws.addEventListener('message', async (evt) => {
        let msg;
        try { msg = JSON.parse(evt.data); } catch { return; }
        await this._handleMessage(msg, resolve, reject);
      });

      this._ws.addEventListener('error', (e) => {
        this._onStatus('WebSocket error – is OBS running?');
        reject(new Error('WebSocket error'));
      });

      this._ws.addEventListener('close', () => {
        this._connected = false;
        this._stopCapture();
        this._onStatus('Disconnected from OBS');
      });
    });
  }

  disconnect() {
    this._stopCapture();
    if (this._ws) {
      this._ws.close();
      this._ws = null;
    }
  }

  // ── OBS WebSocket v5 message handling ──────────────────────────────────────

  async _handleMessage(msg, connectResolve, connectReject) {
    switch (msg.op) {
      case 0: // Hello
        await this._identify(msg.d);
        break;

      case 2: // Identified
        this._connected = true;
        this._onStatus('Connected to OBS');
        connectResolve();
        this._startCapture();
        break;

      case 7: // RequestResponse
        this._handleRequestResponse(msg.d);
        break;

      default:
        break;
    }
  }

  async _identify(helloData) {
    const payload = { rpcVersion: 1 };

    if (helloData.authentication && this._password) {
      payload.authentication = await this._buildAuth(
        this._password,
        helloData.authentication.salt,
        helloData.authentication.challenge,
      );
    }

    this._send(1, payload);
  }

  /** OBS WebSocket v5 auth: base64(sha256(base64(sha256(password+salt))+challenge)) */
  async _buildAuth(password, salt, challenge) {
    const encoder = new TextEncoder();

    const secret = await crypto.subtle.digest(
      'SHA-256',
      encoder.encode(password + salt),
    );
    const secretB64 = btoa(String.fromCharCode(...new Uint8Array(secret)));

    const response = await crypto.subtle.digest(
      'SHA-256',
      encoder.encode(secretB64 + challenge),
    );
    return btoa(String.fromCharCode(...new Uint8Array(response)));
  }

  _send(opcode, data) {
    if (this._ws?.readyState === WebSocket.OPEN) {
      this._ws.send(JSON.stringify({ op: opcode, d: data }));
    }
  }

  /** Send a request and return a promise resolving to responseData */
  _request(requestType, requestData = {}) {
    return new Promise((resolve, reject) => {
      const requestId = crypto.randomUUID();
      this._pendingRequests.set(requestId, { resolve, reject });
      this._send(6, { requestType, requestId, requestData });

      // Timeout after 5 s
      setTimeout(() => {
        if (this._pendingRequests.has(requestId)) {
          this._pendingRequests.delete(requestId);
          reject(new Error(`Request ${requestType} timed out`));
        }
      }, 5000);
    });
  }

  _handleRequestResponse(d) {
    const entry = this._pendingRequests.get(d.requestId);
    if (!entry) return;
    this._pendingRequests.delete(d.requestId);
    if (d.requestStatus?.result) {
      entry.resolve(d.responseData ?? {});
    } else {
      entry.reject(new Error(`OBS request failed: ${d.requestStatus?.comment ?? 'unknown'}`));
    }
  }

  // ── Frame capture loop ─────────────────────────────────────────────────────

  _startCapture() {
    const intervalMs = Math.round(1000 / this._fps);
    this._captureTimer = setInterval(() => this._captureFrame(), intervalMs);
  }

  _stopCapture() {
    if (this._captureTimer !== null) {
      clearInterval(this._captureTimer);
      this._captureTimer = null;
    }
  }

  async _captureFrame() {
    if (!this._connected) return;

    try {
      // Refresh scene name periodically (it may change)
      if (!this._sceneName) {
        const sceneData = await this._request('GetCurrentProgramScene');
        this._sceneName = sceneData.currentProgramSceneName;
      }

      if (!this._sceneName) return;

      const shotData = await this._request('GetSourceScreenshot', {
        sourceName: this._sceneName,
        imageFormat: 'jpeg',
        imageWidth: 1920,
        imageHeight: 1080,
        imageCompressionQuality: 80,
      });

      const dataUrl = shotData.imageData;
      if (!dataUrl) return;

      // Decode to ImageBitmap off the main thread
      const blob = await fetch(dataUrl).then((r) => r.blob());
      const imageBitmap = await createImageBitmap(blob);

      document.dispatchEvent(
        new CustomEvent('obsframe', { detail: { imageBitmap } }),
      );
    } catch (err) {
      // Scene may have changed; reset so we re-query next tick
      this._sceneName = null;
      console.warn('[obs-source] frame capture error:', err.message);
    }
  }
}
