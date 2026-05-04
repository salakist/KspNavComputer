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

### POST /api/transfer

Request (JSON):
```json
{
  "origin": "Kerbin",
  "destination": "Duna",
  "departureUT": 4536870,
  "timeOfFlight": 4557600,
  "originAltitude": 100000,
  "destinationAltitude": 60000
}
```

Response (JSON):
```json
{
  "departureUT": 4536870,
  "departureDate": "Y1 D211 20:34:30",
  "arrivalUT": 9094470,
  "arrivalDate": "Y1 D422 18:34:30",
  "ejectionDeltaV": 950.1,
  "insertionDeltaV": 620.4,
  "totalDeltaV": 1570.5
}
```

Error responses: 400 Bad Request with plain-text message.

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
  "destinationAltitude": 60000
}
```

Response (JSON): `{ outbound, return }` each being a `TransferResponse` object,
plus `totalDeltaV` (sum of both legs).

Error responses: 400 Bad Request with plain-text message.

### GET /api/bodies

Returns array of `{ name, parent, radius }` objects, ordered by name,
excluding Kerbol itself.

## File structure

```
src/Api/
├── Dtos/
│   ├── TransferDtos.cs      — TransferRequest, TransferResponse records
│   ├── RoundTripDtos.cs     — RoundTripRequest, RoundTripResponse records
│   └── BodyDtos.cs          — BodySummary record
├── Endpoints/
│   ├── TransferEndpoints.cs — Map() registers /api/transfer and /api/transfer/roundtrip
│   └── BodiesEndpoints.cs   — Map() registers /api/bodies
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
