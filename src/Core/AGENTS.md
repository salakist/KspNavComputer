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
│   └── LambertSolver.cs      Universal-variable Lambert solver (0-rev elliptic); Stumpff C/S functions
├── Time/
│   └── KspTime.cs            UT↔KSP calendar (426-day year, 6-hour day); Format helper
└── Transfer/
    ├── ParkingOrbit.cs       record: Altitude, Inclination (reserved), Eccentricity (reserved)
    ├── TransferParameters.cs record: Origin, Destination, DepartureUT, TOF, parking orbits
    ├── TransferResult.cs     record: departure/arrival UT, ejection/insertion/total Δv
    └── TransferComputer.cs   orchestrates Kepler → Lambert → hyperbolic-excess → parking-orbit Δv
```

## Algorithm references

- Kepler solver: Newton-Raphson on M = E − e·sin(E); tolerance 1e-10.
- Lambert solver: Bate/Mueller/White §5.3 universal-variable / bisection; tolerance 1e-6.
  **Known limitation**: the 180° (anti-podal) geometry is a mathematical singularity
  (sin(Δν) = 0 ⇒ A = 0 ⇒ g = 0). Do not pass exactly-opposite position vectors.
- Perifocal → inertial rotation: standard 3-1-3 Euler sequence R_z(-Ω)·R_x(-i)·R_z(-ω).
- Ejection/insertion Δv: vis-viva hyperbolic-excess method (v∞ → v_periapsis → v_circular).

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
- Lambert: 0-revolution solutions only (Increment 1a/1b). Multi-rev deferred.
