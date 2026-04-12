# HueCue – OBS Browser Source

A self-contained HTML/CSS/JS overlay that runs **natively inside OBS Studio** as a Browser Source, providing real-time RGB histogram monitoring, compositional guide overlays, and face detection — without requiring the WPF desktop application to be open.

---

## Features

| Feature | Description |
|---|---|
| RGB Histogram | Live per-channel (R/G/B) histogram, displayed to the right or below the scene |
| Rule of Thirds | Red 3×3 grid guide overlay |
| Heat Map | Coloured zone overlay matching the WPF app palette |
| Face Detection | Bounding boxes + keypoints via MediaPipe BlazeFace (requires internet on first load) |
| URL configuration | All settings are query-string parameters – no code changes needed |

---

## Prerequisites

- **OBS Studio 28.0 or later** (includes the built-in OBS WebSocket server)
- OBS WebSocket server **enabled** (Tools → WebSocket Server Settings → Enable WebSocket server)
- The OBS WebSocket default port is **4455**

---

## Quick-start

### Option A – Local file (recommended for offline/LAN use)

1. Clone or download this repository.
2. In OBS, add a **Browser Source**.
3. Check **Local file** and browse to `HueCue.BrowserSource/index.html`.
4. Set the width/height to match your canvas resolution (e.g., 1920 × 1080).
5. In the **Custom CSS** field add (optional, ensures transparency):
   ```css
   body { background: transparent !important; }
   ```
6. Click **OK** – HueCue will connect to the OBS WebSocket automatically.

### Option B – Hosted URL

Serve the `HueCue.BrowserSource/` folder with any static web server (e.g., `npx serve .`) and point the OBS Browser Source at the URL, appending query parameters as needed.

---

## Configuration via URL parameters

All settings are controlled through query-string parameters so you can create pre-configured Browser Source instances.

| Parameter | Default | Values | Description |
|---|---|---|---|
| `obsHost` | `localhost` | hostname or IP | OBS WebSocket host |
| `obsPort` | `4455` | port number | OBS WebSocket port |
| `obsPassword` | *(empty)* | string | OBS WebSocket password |
| `histogram` | `right` | `right` \| `below` \| `none` | Histogram panel position |
| `guide` | `none` | `none` \| `thirds` \| `heatmap` | Guide overlay type |
| `faceDetection` | `true` | `true` \| `false` | Enable/disable face detection |
| `fps` | `30` | 1–60 | Screenshot capture rate |

**Example URL with all parameters:**

```
file:///path/to/HueCue.BrowserSource/index.html?obsHost=localhost&obsPort=4455&obsPassword=secret&histogram=right&guide=thirds&faceDetection=true&fps=30
```

---

## Interactive settings panel (browser preview)

When opening `index.html` in a **regular browser** (not inside OBS), a settings panel appears in the bottom-right corner.  Use it to:

1. Adjust all configuration options live.
2. Click **Apply / Reconnect** – the URL with all parameters is shown so you can copy it into OBS.

The panel is automatically hidden when running inside OBS (`window.obsstudio` is detected).

---

## How it works

```
OBS Studio
│
├─ WebSocket Server (port 4455)
│   └─ Responds to GetCurrentProgramScene + GetSourceScreenshot requests
│
└─ Browser Source (HueCue)
    ├─ obs-source.js   → connects via WebSocket, requests screenshots at ~30 FPS
    ├─ histogram.js    → computes RGB histogram from each frame, renders to canvas
    ├─ overlay.js      → draws rule-of-thirds grid or heat-map zone overlay
    └─ face-detector.js → runs MediaPipe face detection, draws boxes on canvas
```

The Browser Source connects back to the OBS WebSocket server and requests a JPEG screenshot of the current **program scene** on every tick.  Each frame is decoded into an `ImageBitmap`, processed (histogram + face detection), and the results are drawn onto transparent canvas layers that OBS composites over the scene.

> **Recursive-capture note:** because the Browser Source itself is part of the scene, each screenshot will include the overlay from the previous frame.  For a clean signal, place the Browser Source in a **dedicated monitoring scene** (or a scene transition/Studio Mode preview) rather than in your live program scene.

---

## Face detection

Face detection uses [TensorFlow.js](https://www.tensorflow.org/js) with the [MediaPipe BlazeFace](https://google.github.io/mediapipe/solutions/face_detection) short-range model.

- The model weights (~1 MB) are downloaded from jsDelivr CDN on first use and cached by the browser.
- **Internet access is required the first time.**  Subsequent launches use the browser cache and work offline.
- To disable face detection permanently, add `?faceDetection=false` to the URL.
- To disable the CDN dependencies entirely, remove the three `<script>` tags in `index.html` and set `faceDetection=false`.

---

## Deployment with HueCue releases

The GitHub Actions `build_app.yml` workflow automatically bundles the `HueCue.BrowserSource/` folder as a `BrowserSource` artifact alongside each release.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| "Connection failed" | OBS WebSocket not enabled | Tools → WebSocket Server Settings → Enable WebSocket server |
| "Connection failed" | Wrong port or password | Check OBS WebSocket settings; use URL params to match |
| No histogram / black canvas | No active scene or scene has no sources | Ensure a scene with at least one visible source is active in OBS |
| Face detection not working | CDN unavailable | Check internet access; or disable face detection with `?faceDetection=false` |
| Overlay appears in screenshot | Browser Source is in the same scene being captured | Move the Browser Source to a monitoring scene |
