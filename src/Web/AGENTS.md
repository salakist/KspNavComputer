# KspNavComputer.Web — AGENTS.md

## Purpose

Vite + React + TypeScript frontend. Communicates with the ASP.NET Core API
at `http://localhost:5000` during development.

## Project layout

```
src/Web/
├── src/
│   ├── api/
│   │   └── transferClient.ts   typed fetch wrappers for /api/transfer, /api/transfer/roundtrip, and /api/bodies
│   ├── components/
│   │   └── TransferForm.tsx    form + result panels; round-trip checkbox toggles extra fields and shows both legs
│   ├── App.tsx                 root component — header + <TransferForm>
│   ├── App.css                 minimal utility styles
│   ├── index.css               global reset/base styles
│   └── main.tsx                React entry point
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
- No global state management library — React `useState`/`useEffect` only for 1a.
