# KspNavComputer.Core — AGENTS.md

## Purpose

Pure .NET 8 class library implementing KSP orbital mechanics from scratch.
No third-party math libraries; all algorithms implemented in-house.

## Project layout

```
src/Core/
├── Bodies/
│   ├── CelestialBody.cs      record: Name, GravParam, Radius, SOI, RotPeriod, Parent, Orbit
│   ├── OrbitalElements.cs    record: SMA, e, i, Ω, ω, M₀, epoch
│   └── BodyDatabase.cs       static registry of all 17 stock KSP bodies
├── Mechanics/
│   ├── Vector3d.cs           in-house double-precision 3D vector struct
│   ├── KeplerSolver.cs       Kepler's equation (Newton-Raphson), state vectors, perifocal→inertial rotation
│   └── LambertSolver.cs      Sun/Gooding (1979) multi-revolution Lambert solver (Brent root-finding)
├── Time/
│   └── KspTime.cs            UT↔KSP calendar (426-day year, 6-hour day); Format helper
└── Transfer/
    ├── ParkingOrbit.cs          record: Altitude, Inclination (reserved), Eccentricity (reserved)
    ├── TransferParameters.cs    record: Origin, Destination, DepartureUT, TOF, parking orbits
    ├── TransferResult.cs        record: departure/arrival UT, ejection/insertion/total Δv
    ├── RoundTripParameters.cs   record: Origin, Destination, DepartureUT, OutboundTOF, StayDuration,
    │                                    ReturnTOF, OriginOrbit, DestinationOrbit
    ├── RoundTripResult.cs       record: Outbound (TransferResult), Return (TransferResult), TotalDeltaV
    └── TransferComputer.cs      orchestrates Kepler → Lambert → Δv; also ComputeRoundTrip (two legs)
```

## Algorithm references

- Kepler solver: Newton-Raphson on M = E − e·sin(E); tolerance 1e-10.
- Lambert solver: Sun (1979) / Gooding formulation with Brent's method root-finding.
  - N=0 (zero-rev): Brent tolerance 1e-6.
  - N≥1 (multi-rev): Brent tolerance 1e-4; two roots exist per N, both candidates evaluated.
  - Arc selection: `angleParameter = ±sqrt(n/m)`; negative for long-way (transfer angle > π).
  - `FdtE(x, N)` time-of-flight equation includes `N·π` term for multi-revolution orbits.
  - `TransferComputer` solves for both prograde and retrograde and picks the cheapest total Δv.
  - **Known limitation**: the 180° (anti-podal) geometry (transfer angle = π) is a degenerate
    case where the orbit plane is undefined. Do not pass exactly-opposite position vectors.
- Perifocal → inertial rotation: standard 3-1-3 Euler sequence R_z(-Ω)·R_x(-i)·R_z(-ω).
- Ejection Δv: `sqrt(v_circ² + v_peri² − 2·v_circ·v_peri·cos(i_ej))` (law of cosines for
  plane change when ejection inclination ≠ 0), where `v_peri = sqrt(v∞² + 2μ/r − 2μ/r_SOI)`.
- Insertion Δv: `|sqrt(v∞² + 2·v_circ² − 2μ/r_SOI) − v_circ|` (no plane-change term;
  matches LWP `insertionToCircularDeltaV`).

## Body data

- All 17 stock bodies hardcoded from KSP wiki values (community-verified).
- Moons hold a reference to their parent body; Kerbol parent is null.
- OPM / MPE body support deferred to Increment 4.

## Units (always SI internally)

| Quantity | Unit |
|----------|------|
| Distance | m    |
| Speed    | m/s  |
| Time     | s (UT) |
| Gravity param μ | m³/s² |
| Angles   | rad  |

Convert only at input/output boundaries (e.g. km → m for altitude inputs).

## Constraints

- No code copied from reference implementations.
- No third-party math libraries (Vector3d is in-house).
- Time always in UT seconds; KSP calendar strings only for display.
- Lambert: multi-revolution support implemented (N = 0 … maxRevs, default 10).
