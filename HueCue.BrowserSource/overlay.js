/**
 * overlay.js
 *
 * Renders compositional guide overlays (rule-of-thirds grid and heat-map
 * zones) onto the guide canvas and manages the CSS-based heat-map layer.
 *
 * Guide modes:
 *   'thirds'  – four red lines forming a 3×3 rule-of-thirds grid
 *   'heatmap' – semi-transparent coloured zone grid matching the WPF app
 *   'none'    – all overlays hidden
 */

// ── Rule-of-thirds ──────────────────────────────────────────────────────────

/**
 * Draw a rule-of-thirds grid on the canvas.
 *
 * @param {HTMLCanvasElement} canvas
 */
export function drawRuleOfThirds(canvas) {
  const w = canvas.width;
  const h = canvas.height;
  const ctx = canvas.getContext('2d');

  ctx.clearRect(0, 0, w, h);

  ctx.strokeStyle = 'rgba(255, 0, 0, 0.8)';
  ctx.lineWidth = 1.5;

  // Vertical lines at 1/3 and 2/3
  for (const frac of [1 / 3, 2 / 3]) {
    ctx.beginPath();
    ctx.moveTo(w * frac, 0);
    ctx.lineTo(w * frac, h);
    ctx.stroke();
  }

  // Horizontal lines at 1/3 and 2/3
  for (const frac of [1 / 3, 2 / 3]) {
    ctx.beginPath();
    ctx.moveTo(0, h * frac);
    ctx.lineTo(w, h * frac);
    ctx.stroke();
  }
}

/**
 * Clear the guide canvas.
 *
 * @param {HTMLCanvasElement} canvas
 */
export function clearGuideCanvas(canvas) {
  const ctx = canvas.getContext('2d');
  ctx.clearRect(0, 0, canvas.width, canvas.height);
}

// ── Heat-map ────────────────────────────────────────────────────────────────

/**
 * Build the heat-map DOM grid inside `container` if it hasn't been built yet.
 * Cells match the WPF app colour scheme.
 *
 * @param {HTMLElement} container   The #heatmap-overlay element
 */
export function buildHeatmapGrid(container) {
  if (container.childElementCount > 0) return; // already built

  // Row 0: top row – green shades
  const row0 = [
    { cls: 'hm-0-0', color: '#00b400' }, // medium green
    { cls: 'hm-0-1', color: '#00ff00' }, // bright green
    { cls: 'hm-0-2', color: '#00b400' }, // medium green
  ];
  // Row 1: middle row – yellow flanks, dark green centre
  const row1 = [
    { cls: 'hm-1-0', color: '#c8c800' }, // yellow
    { cls: 'hm-1-1', color: '#007800' }, // dark green
    { cls: 'hm-1-2', color: '#c8c800' }, // yellow
  ];
  // Row 2: bottom – full-width red
  const row2 = [{ cls: 'hm-2', color: '#ff0000' }];

  for (const cell of [...row0, ...row1, ...row2]) {
    const div = document.createElement('div');
    div.className = `hm-cell ${cell.cls}`;
    div.style.background = cell.color;
    div.style.opacity = '0.3';
    container.appendChild(div);
  }
}

// ── Guide mode management ───────────────────────────────────────────────────

/**
 * Apply the selected guide mode, showing/hiding the relevant elements.
 *
 * @param {'thirds'|'heatmap'|'none'} mode
 * @param {HTMLCanvasElement} guideCanvas
 * @param {HTMLElement} heatmapOverlay
 */
export function applyGuideMode(mode, guideCanvas, heatmapOverlay) {
  // Hide everything first
  guideCanvas.style.display = 'none';
  heatmapOverlay.style.display = 'none';
  clearGuideCanvas(guideCanvas);

  if (mode === 'thirds') {
    guideCanvas.style.display = 'block';
    drawRuleOfThirds(guideCanvas);
  } else if (mode === 'heatmap') {
    buildHeatmapGrid(heatmapOverlay);
    heatmapOverlay.style.display = 'grid';
  }
}
