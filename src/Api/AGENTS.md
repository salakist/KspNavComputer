# KspNavComputer.Api — AGENTS.md

## Purpose

Thin ASP.NET Core 8 minimal-API wrapper around `KspNavComputer.Core`.
Runs on `http://localhost:5000` during development.

## Endpoints

| Method | Path                    | Description                                |
|--------|-------------------------|-----------------------------------------|
| POST   | /api/transfer           | Compute one-way interplanetary transfer    |
| POST   | /api/transfer/roundtrip | Compute round-trip interplanetary transfer |
| GET    | /api/bodies             | List all bodies that orbit a parent body   |
| POST   | /api/porkchop           | Compute porkchop Δv grid (rows×cols)       |

### POST /api/transfer

Request (JSON):
```json
{
  "origin": "Kerbin",
  "destination": "Duna",
  "departureUT": 4536870,
  "timeOfFlight": 4557600,
  "originAltitude": 100000,
  "destinationAltitude": 60000,
  "originInclination": 0,
  "destinationInclination": 0,
  "originEccentricity": 0,
  "destinationEccentricity": 0
}
```

The four inclination/eccentricity fields are **optional** (default 0 — circular equatorial).
Inclination is in **degrees**; converted to radians before passing to `ParkingOrbit`.

Response (JSON):
```json
{
  "departureUT": 4536870,
  "departureDate": "Y1 D211 20:34:30",
  "arrivalUT": 9094470,
  "arrivalDate": "Y1 D422 18:34:30",
  "ejection": {
    "deltaV": 950.1,
    "burnUT": 4534210,
    "burnDate": "Y1 D211 19:50:10",
    "vector": { "prograde": 948.3, "normal": -58.1, "radial": 0.0 },
    "preciseManeuverText": "Precise Maneuver Information\nDepart at: ...",
    "ejectionDetails": { "angleDeg": 113.73, "inclinationDeg": 0.02 }
  },
  "insertion": {
    "deltaV": 620.4,
    "burnUT": 9098120,
    "burnDate": "Y1 D422 19:35:20",
    "vector": { "prograde": -620.4, "normal": 0.0, "radial": 0.0 },
    "preciseManeuverText": "Precise Maneuver Information\nDepart at: ...",
    "ejectionDetails": null
  },
  "totalDeltaV": 1570.5,
  "transferType": "Ballistic",
  "phaseAngleDeg": 35.2,
  "transferAngleDeg": 178.3,
  "transferPeriapsis": 13_599_840_256,
  "transferApoapsis": 20_726_155_264,
  "insertionInclinationDeg": 0.9,
  "planeChange": null
}
```

`planeChange` is non-null only when a mid-course plane-change burn was computed and selected.
It has the same shape as a `BurnDto` but without `ejectionDetails` or `preciseManeuverText`.

Additional request fields (all optional, default shown):
```json
{
  "transferType": "Optimal",
  "noInsertionBurn": false
}
```
`transferType` accepts `"Ballistic"`, `"MidCoursePlaneChange"`, or `"Optimal"` (default).

### POST /api/transfer/roundtrip

Request (JSON):
```json
{
  "origin": "Kerbin",
  "destination": "Duna",
  "departureUT": 4536870,
  "outboundTimeOfFlight": 4557600,
  "stayDuration": 7776000,
  "returnTimeOfFlight": 5400000,
  "originAltitude": 100000,
  "destinationAltitude": 60000,
  "originInclination": 0,
  "destinationInclination": 0,
  "originEccentricity": 0,
  "destinationEccentricity": 0
}
```

The four inclination/eccentricity fields are **optional** (default 0).
Inclination is in **degrees**.

Response (JSON): `{ outbound, return }` each being a `TransferResponse` object,
plus `totalDeltaV` (sum of both legs).

Error responses: 400 Bad Request with plain-text message.

### POST /api/porkchop

Request (JSON):
```json
{
  "origin": "Kerbin",
  "destination": "Duna",
  "earliestDeparture": 22705200,
  "latestDeparture": 34068600,
  "originAltitude": 80000,
  "destinationAltitude": 60000,
  "transferType": "Ballistic",
  "gridCols": 100,
  "gridRows": 100
}
```

`transferType`, `gridCols`, `gridRows`, `originAltitude`, `destinationAltitude` are optional.
`destinationAltitude` = 0 (omitted) → fly-by / no insertion burn.

Response (JSON):
```json
{
  "deltaVs": [1234.5, ...],
  "rows": 100, "cols": 100,
  "earliestDeparture": 22705200, "latestDeparture": 34068600,
  "minTof": 3262001, "maxTof": 9786004,
  "minDeltaV": 1677.7, "maxDeltaV": 18540.0,
  "meanLogDeltaV": 7.88, "stdLogDeltaV": 0.42,
  "optimalRow": 14, "optimalCol": 8
}
```

`deltaVs` is a flat row-major array (row 0 = minTof, col 0 = earliestDeparture).
Failed cells (degenerate Lambert geometry) are `null` in JSON (NaN in C#).

---

### GET /api/bodies

Returns array of `{ name, parent, radius }` objects, ordered by name,
excluding Kerbol itself.

## File structure

```
src/Api/
├── Dtos/
│   ├── TransferDtos.cs      — TransferRequest (+ transferType, noInsertionBurn),
│   │                           BurnVectorDto, PlaneChangeBurnDto, BurnDto, TransferResponse
│   │                           (+ planeChange, phaseAngleDeg, transferAngleDeg,
│   │                              transferPeriapsis, transferApoapsis, insertionInclinationDeg)
│   ├── RoundTripDtos.cs     — RoundTripRequest, RoundTripResponse records
│   ├── BodyDtos.cs          — BodySummary record
│   └── PorkchopDtos.cs      — PorkchopRequest, PorkchopResponse records
├── Endpoints/
│   ├── TransferEndpoints.cs — Map() registers /api/transfer and /api/transfer/roundtrip
│   ├── BodiesEndpoints.cs   — Map() registers /api/bodies
│   └── PorkchopEndpoints.cs — Map() registers /api/porkchop
├── Mappers/
└──   TransferMapper.cs    — ToResponse(TransferResult), ToBurnDto(Burn), ParseTransferType,
                                 ToPlaneChangeBurnDto(Burn)
└── Program.cs               — bootstrap only: CORS + Map() calls + app.Run()
```

## Conventions

- Minimal API pattern — no Controllers folder.
- Each endpoint group is a `static class` with a `public static void Map(WebApplication app)` method.
- DTOs are C# `record` types in `KspNavComputer.Api.Dtos` namespace.
- Endpoints are in `KspNavComputer.Api.Endpoints` namespace.
- CORS allows `http://localhost:5173` and `http://127.0.0.1:5173` (Vite dev server).
- No authentication — personal desktop tool only.
- HTTPS redirection is disabled for local dev simplicity.
