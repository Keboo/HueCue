/**
 * histogram.js
 *
 * Computes an RGB histogram from an ImageBitmap and renders it onto a
 * <canvas> element.  The histogram matches the style produced by the
 * WPF app: R, G and B lines drawn on a black background.
 */

const HIST_BINS = 256;
const LINE_WIDTH = 1.5;

/**
 * Compute per-channel histograms from an ImageBitmap.
 *
 * @param {ImageBitmap} imageBitmap
 * @returns {{ r: Float32Array, g: Float32Array, b: Float32Array }}
 */
function computeHistogram(imageBitmap) {
  // Offscreen canvas for pixel access
  const oc = new OffscreenCanvas(imageBitmap.width, imageBitmap.height);
  const ctx = oc.getContext('2d');
  ctx.drawImage(imageBitmap, 0, 0);

  const { data } = ctx.getImageData(0, 0, oc.width, oc.height);

  const r = new Float32Array(HIST_BINS);
  const g = new Float32Array(HIST_BINS);
  const b = new Float32Array(HIST_BINS);

  for (let i = 0; i < data.length; i += 4) {
    r[data[i]]++;
    g[data[i + 1]]++;
    b[data[i + 2]]++;
  }

  // Normalise to [0, 1]
  const maxR = Math.max(...r) || 1;
  const maxG = Math.max(...g) || 1;
  const maxB = Math.max(...b) || 1;

  for (let i = 0; i < HIST_BINS; i++) {
    r[i] /= maxR;
    g[i] /= maxG;
    b[i] /= maxB;
  }

  return { r, g, b };
}

/**
 * Render a pre-computed histogram onto a canvas element.
 *
 * @param {HTMLCanvasElement} canvas
 * @param {{ r: Float32Array, g: Float32Array, b: Float32Array }} hist
 */
function renderHistogram(canvas, hist) {
  const w = canvas.width;
  const h = canvas.height;
  const ctx = canvas.getContext('2d');

  ctx.clearRect(0, 0, w, h);

  // Black background (semi-transparent)
  ctx.fillStyle = 'rgba(0,0,0,0.75)';
  ctx.fillRect(0, 0, w, h);

  const binW = w / HIST_BINS;

  /** Draw one channel */
  function drawChannel(values, color) {
    ctx.beginPath();
    ctx.strokeStyle = color;
    ctx.lineWidth = LINE_WIDTH;

    for (let i = 0; i < HIST_BINS; i++) {
      const x = i * binW;
      const y = h - values[i] * (h - 2);
      if (i === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
    ctx.stroke();
  }

  drawChannel(hist.b, '#4477ff'); // blue
  drawChannel(hist.g, '#44ff44'); // green
  drawChannel(hist.r, '#ff4444'); // red
}

/**
 * Update the histogram canvas from an ImageBitmap.
 * Safe to call every frame; work is minimal when nothing has changed.
 *
 * @param {HTMLCanvasElement} canvas
 * @param {ImageBitmap} imageBitmap
 */
export function updateHistogram(canvas, imageBitmap) {
  const hist = computeHistogram(imageBitmap);
  renderHistogram(canvas, hist);
}
