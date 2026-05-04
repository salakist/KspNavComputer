import { useEffect, useRef, useCallback } from 'react';
import type { PorkchopResponse } from '../api/transferClient';

const KSP_DAY_S = 21_600;

/** Maps a relative delta-v value [0,1] to an RGB colour (blue→cyan→green→yellow→red). */
function dvToRgb(t: number): [number, number, number] {
  // Four-stop gradient matching LWP's colour scale
  const stops: [number, number, number, number][] = [
    [0,    0,   0, 255],  // blue
    [0.33, 0, 255, 255],  // cyan
    [0.66, 0, 255,   0],  // green
    [1,  255,   0,   0],  // red
  ];
  const clamped = Math.max(0, Math.min(1, t));
  for (let i = 1; i < stops.length; i++) {
    const [t0, r0, g0, b0] = stops[i - 1];
    const [t1, r1, g1, b1] = stops[i];
    if (clamped <= t1) {
      const f = (clamped - t0) / (t1 - t0);
      return [
        Math.round(r0 + f * (r1 - r0)),
        Math.round(g0 + f * (g1 - g0)),
        Math.round(b0 + f * (b1 - b0)),
      ];
    }
  }
  return [255, 0, 0];
}

function formatDepDate(ut: number): string {
  const year = Math.floor(ut / 9_203_400) + 1;
  const day  = Math.floor((ut % 9_203_400) / KSP_DAY_S) + 1;
  return `Y${year} D${String(day).padStart(3, '0')}`;
}

interface PorkchopPlotProps {
  data: PorkchopResponse;
  /** Called with { departureUT, tof } when user clicks a cell */
  onSelect: (departureUT: number, tof: number) => void;
  /** Currently selected cell (col, row) — highlighted with crosshair */
  selectedCol?: number;
  selectedRow?: number;
}

export default function PorkchopPlot({
  data, onSelect, selectedCol, selectedRow,
}: PorkchopPlotProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const CANVAS_W  = 400;
  const CANVAS_H  = 300;

  const cellW = CANVAS_W / data.cols;
  const cellH = CANVAS_H / data.rows;

  // Colour normalisation: log scale, clamped to [logMin, mean + 2σ]
  const logMin = Math.log(data.minDeltaV);
  const logMax = Math.min(
    Math.log(data.maxDeltaV),
    data.meanLogDeltaV + 2 * data.stdLogDeltaV,
  );
  const logRange = logMax - logMin || 1;

  // Draw the heatmap
  const draw = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const imgData = ctx.createImageData(CANVAS_W, CANVAS_H);
    const pixels  = imgData.data;

    for (let row = 0; row < data.rows; row++) {
      for (let col = 0; col < data.cols; col++) {
        const dv = data.deltaVs[row * data.cols + col];

        // Pixel rectangle for this cell
        const px0 = Math.round(col * cellW);
        const py0 = Math.round((data.rows - 1 - row) * cellH); // Y-flip: row=0 at bottom
        const px1 = Math.round((col + 1) * cellW);
        const py1 = Math.round((data.rows - row) * cellH);

        let r: number, g: number, b: number;
        if (isNaN(dv) || dv <= 0) {
          r = 40; g = 40; b = 40;
        } else {
          const logDv   = Math.log(dv);
          const rel     = (logDv - logMin) / logRange;
          [r, g, b] = dvToRgb(rel);
        }

        for (let py = py0; py < py1; py++) {
          for (let px = px0; px < px1; px++) {
            const idx = (py * CANVAS_W + px) * 4;
            pixels[idx]     = r;
            pixels[idx + 1] = g;
            pixels[idx + 2] = b;
            pixels[idx + 3] = 255;
          }
        }
      }
    }

    ctx.putImageData(imgData, 0, 0);

    // Optimal cell marker (white box)
    const ox = Math.round(data.optimalCol * cellW);
    const oy = Math.round((data.rows - 1 - data.optimalRow) * cellH);
    ctx.strokeStyle = 'white';
    ctx.lineWidth   = 1.5;
    ctx.strokeRect(ox, oy, Math.max(cellW, 2), Math.max(cellH, 2));

    // Selected cell crosshair (white lines)
    if (selectedCol !== undefined && selectedRow !== undefined) {
      const sx = Math.round((selectedCol + 0.5) * cellW);
      const sy = Math.round((data.rows - 0.5 - selectedRow) * cellH);
      ctx.strokeStyle = 'rgba(255,255,255,0.7)';
      ctx.lineWidth   = 1;
      ctx.beginPath();
      ctx.moveTo(sx, 0); ctx.lineTo(sx, CANVAS_H);
      ctx.moveTo(0, sy); ctx.lineTo(CANVAS_W, sy);
      ctx.stroke();
    }
  }, [data, cellW, cellH, logMin, logRange, selectedCol, selectedRow]);

  useEffect(() => { draw(); }, [draw]);

  const handleClick = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const rect  = canvasRef.current!.getBoundingClientRect();
    const scaleX = CANVAS_W / rect.width;
    const scaleY = CANVAS_H / rect.height;
    const px     = (e.clientX - rect.left) * scaleX;
    const py     = (e.clientY - rect.top)  * scaleY;

    const col = Math.min(Math.floor(px / cellW), data.cols - 1);
    // Y is flipped: row=0 at bottom of canvas
    const row = Math.min(Math.floor((CANVAS_H - py) / cellH), data.rows - 1);

    const depUT = data.earliestDeparture
                + (data.latestDeparture - data.earliestDeparture) * col / Math.max(data.cols - 1, 1);
    const tof   = data.minTof
                + (data.maxTof - data.minTof) * row / Math.max(data.rows - 1, 1);

    onSelect(depUT, tof);
  };

  // Axis labels
  const depStart  = formatDepDate(data.earliestDeparture);
  const depEnd    = formatDepDate(data.latestDeparture);
  const tofMinDays = (data.minTof / KSP_DAY_S).toFixed(0);
  const tofMaxDays = (data.maxTof / KSP_DAY_S).toFixed(0);

  return (
    <div className="porkchop-wrapper">
      <div className="porkchop-canvas-area">
        {/* Y axis label (TOF) */}
        <div className="porkchop-yaxis">
          <span>{tofMaxDays}d</span>
          <span className="porkchop-axis-title">Time of Flight</span>
          <span>{tofMinDays}d</span>
        </div>

        <canvas
          ref={canvasRef}
          width={CANVAS_W}
          height={CANVAS_H}
          className="porkchop-canvas"
          onClick={handleClick}
          title="Click to select a transfer"
        />
      </div>

      {/* X axis labels */}
      <div className="porkchop-xaxis">
        <span>{depStart}</span>
        <span className="porkchop-axis-title">Departure Date</span>
        <span>{depEnd}</span>
      </div>
    </div>
  );
}
