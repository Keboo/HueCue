/**
 * face-detector.js
 *
 * Lightweight face detection using the MediaPipe FaceDetection model via
 * TensorFlow.js.  Draws coloured bounding boxes and facial landmarks on a
 * canvas, matching the visual style of the WPF app.
 *
 * External dependencies (loaded by index.html via CDN):
 *   @tensorflow/tfjs-core
 *   @tensorflow/tfjs-backend-webgl
 *   @tensorflow-models/face-detection  (MediaPipe BlazeFace short-range)
 *
 * Internet access is required the first time the model is downloaded;
 * subsequent loads use the browser cache.
 *
 * If TensorFlow.js is unavailable the module degrades gracefully – faces
 * simply will not be detected and no error is thrown.
 */

// 12 visually distinct colours to cycle through (matching the WPF palette)
const FACE_COLORS = [
  '#00ff00', // green
  '#4488ff', // blue
  '#ff4444', // red
  '#ffff00', // yellow
  '#ff00ff', // magenta
  '#00ffff', // cyan
  '#ff8800', // orange
  '#aa00aa', // purple
  '#ffccdd', // pink
  '#00ff88', // spring green
  '#ffffff', // white
  '#40e0d0', // turquoise
];

export class FaceDetector {
  constructor() {
    this._model = null;
    this._loading = false;
    this._available = false;
  }

  // ── Initialisation ─────────────────────────────────────────────────────────

  /**
   * Load the MediaPipe FaceDetection model.  Safe to call multiple times –
   * subsequent calls are no-ops.
   *
   * @returns {Promise<boolean>}  true if the model loaded successfully
   */
  async load() {
    if (this._model) return true;
    if (this._loading) return false;

    this._loading = true;
    try {
      // These globals are injected by the CDN scripts in index.html
      if (typeof faceDetection === 'undefined') {
        console.warn('[face-detector] faceDetection global not found – face detection disabled');
        this._available = false;
        return false;
      }

      const model = faceDetection.SupportedModels.MediaPipeFaceDetector;
      const detectorConfig = {
        runtime: 'tfjs',
        modelType: 'short', // short-range, optimised for ~2 m distance
      };

      this._model = await faceDetection.createDetector(model, detectorConfig);
      this._available = true;
      console.info('[face-detector] MediaPipe FaceDetector loaded');
      return true;
    } catch (err) {
      console.warn('[face-detector] failed to load model:', err.message);
      this._available = false;
      return false;
    } finally {
      this._loading = false;
    }
  }

  get isAvailable() {
    return this._available;
  }

  // ── Detection + rendering ──────────────────────────────────────────────────

  /**
   * Detect faces in `imageBitmap` and draw the results on `canvas`.
   * The canvas should be positioned over the video feed at the same size.
   *
   * @param {HTMLCanvasElement} canvas
   * @param {ImageBitmap}       imageBitmap
   */
  async detectAndDraw(canvas, imageBitmap) {
    if (!this._model) return;

    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    let faces;
    try {
      faces = await this._model.estimateFaces(imageBitmap);
    } catch (err) {
      console.warn('[face-detector] estimation error:', err.message);
      return;
    }

    if (!faces || faces.length === 0) return;

    // Scale factors from the ImageBitmap dimensions to the canvas dimensions
    const scaleX = canvas.width / imageBitmap.width;
    const scaleY = canvas.height / imageBitmap.height;

    faces.forEach((face, idx) => {
      const color = FACE_COLORS[idx % FACE_COLORS.length];
      const box = face.box; // { xMin, yMin, width, height }

      const x = box.xMin * scaleX;
      const y = box.yMin * scaleY;
      const w = box.width * scaleX;
      const h = box.height * scaleY;

      // Bounding box
      ctx.strokeStyle = color;
      ctx.lineWidth = 2;
      ctx.strokeRect(x, y, w, h);

      // Score label
      if (face.score != null) {
        const score = Array.isArray(face.score) ? face.score[0] : face.score;
        ctx.fillStyle = color;
        ctx.font = '14px sans-serif';
        ctx.fillText(score.toFixed(2), x, y > 16 ? y - 4 : y + 16);
      }

      // Facial keypoints (eyes, nose, mouth, ears)
      if (face.keypoints) {
        ctx.fillStyle = '#0044ff';
        for (const kp of face.keypoints) {
          ctx.beginPath();
          ctx.arc(kp.x * scaleX, kp.y * scaleY, 3, 0, Math.PI * 2);
          ctx.fill();
        }
      }
    });
  }
}
