# KspNavComputer.Web — AGENTS.md

## Purpose

Vite + React + TypeScript frontend. Communicates with the ASP.NET Core API
at `http://localhost:5000` during development.

## Project layout

```
src/Web/
├── src/
│   ├── api/
│   │   └── transferClient.ts   typed fetch wrappers for /api/transfer, /api/transfer/roundtrip,
│   │                           /api/bodies, and /api/porkchop
│   ├── components/
│   │   ├── PorkchopForm.tsx    origin/dest dropdowns, orbit inputs, departure date range,
│   │   │                       transfer type selector, “Plot it!” button
│   │   ├── PorkchopPlot.tsx    400×300 canvas heatmap; log-scale colour normalisation;
│   │   │                       click handler; optimal cell marker; selected cell crosshair
│   │   └── TransferDetails.tsx all burn output fields + PM copy buttons;
│   │                           conditional plane-change section; CopyButton component
│   ├── App.tsx             two-column layout: PorkchopForm left; PorkchopPlot + TransferDetails right;
│   │                       auto-fetches optimal transfer on plot; click fetches selected cell
│   ├── App.css             full layout CSS for two-column design
│   ├── index.css           global reset/base styles
│   └── main.tsx            React entry point
├── index.html
├── package.json
├── vite.config.ts
└── tsconfig*.json
```

## Dev workflow

```bash
cd src/Web
npm install      # first time only
npm run dev      # starts Vite dev server on http://localhost:5173
npm run build    # production build → dist/
```

The API must also be running for the UI to work:
```bash
cd src/Api
dotnet run       # starts on http://localhost:5000
```

## Conventions

- API base URL is hardcoded to `http://localhost:5000` in `transferClient.ts`.
  Change to an env var (`import.meta.env.VITE_API_BASE`) in a future increment.
- All time inputs to the API are in UT seconds; the UI converts year/day inputs
  using the KSP calendar constants (1 year = 9 203 400 s, 1 day = 21 600 s).
- Altitude inputs are in km in the UI; multiplied by 1 000 before sending to API.
- Inclination inputs are in degrees in the UI; the API converts to radians internally.
- Eccentricity inputs are dimensionless (0 = circular).
- No global state management library — React `useState`/`useEffect` only.
- `TransferDetails` shows ejection angle/inclination from `ejectionDetails` when available;
  `formatEjectionAngle(angleDeg)` formats e.g. `"113.73° to retrograde"`.
- Copy-to-clipboard buttons use `preciseManeuverText` from `BurnDto`.
- Porkchop colour map: log-scale, blue→cyan→green→red; normalised to [minLog, meanLog + 2σ];
  Y-axis flipped on canvas (row 0 drawn at bottom = minTof).
- `gridCols`/`gridRows` default to 100 in `PorkchopRequest`; sent explicitly from the form.
