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
    ├── ParkingOrbit.cs           record: Altitude [m], Inclination [rad, default 0], Eccentricity [default 0]
    ├── TransferType.cs           enum: Ballistic | MidCoursePlaneChange | Optimal
    ├── TransferParameters.cs     record: Origin, Destination, DepartureUT, TOF, parking orbits,
    │                                     TransferType (default Optimal), NoInsertionBurn (default false)
    ├── BurnVector.cs             record: Prograde, Normal, Radial [m/s]; Magnitude property; Zero sentinel
    ├── EjectionDetails.cs        record: AngleDeg (signed°), InclinationDeg (°); see docs/algorithms/delta-v.md
    ├── Burn.cs                   record: DeltaV [m/s], BurnUT [s UT], Vector (BurnVector), Ejection (EjectionDetails?)
    ├── PlaneChangeResult.cs      internal record: DepartureVelocity, ArrivalVelocity, PlaneChange (Burn)
    ├── TransferResult.cs         record: DepartureUT, ArrivalUT, Ejection, Insertion, TotalDeltaV,
    │                                     PlaneChange (Burn?), PhaseAngleDeg, TransferAngleDeg,
    │                                     TransferPeriapsis, TransferApoapsis, InsertionInclinationDeg
    ├── RoundTripParameters.cs    record: Origin, Destination, DepartureUT, OutboundTOF, StayDuration,
    │                                     ReturnTOF, OriginOrbit, DestinationOrbit
    ├── RoundTripResult.cs        record: Outbound (TransferResult), Return (TransferResult), TotalDeltaV
    ├── PorkchopParameters.cs     record: Origin, Destination, parking orbits, EarliestDeparture,
    │                                     LatestDeparture, TransferType, GridCols (default 100), GridRows (default 100)
    ├── PorkchopResult.cs         record: flat row-major DeltaVs[], Rows, Cols, departure/TOF bounds,
    │                                     MinDeltaV, MaxDeltaV, MeanLogDeltaV, StdLogDeltaV, OptimalRow, OptimalCol
    ├── ManeuverCalculator.cs     internal static: converts heliocentric transfer velocity → Burn;
    │                                     includes ComputeEjectionDetails (LWP algorithm port)
    ├── PlaneChangeComputer.cs    static: golden-section search for optimal plane-change angle;
    │                                     Rodrigues rotation; returns null when coplanar/degenerate
    ├── PorkchopComputer.cs       static: computes rows×cols Δv grid; AutoTofRange (Hohmann formula);
    │                                     calls TransferComputer per cell; NaN on failure
    ├── PreciseManeuverFormatter.cs   static Format(Burn) → PM plaintext block (for API + future mod DLL)
    └── TransferComputer.cs       orchestrates Kepler → Lambert → ManeuverCalculator → optional
                                        PlaneChangeComputer; ComputeRoundTrip
```

## Algorithm references

Full algorithm documentation lives in `docs/algorithms/`:
- [`docs/algorithms/lambert.md`](../../docs/algorithms/lambert.md) — Lambert solver (Sun/Gooding 1979, arc selection, multi-revolution, velocity reconstruction)
- [`docs/algorithms/delta-v.md`](../../docs/algorithms/delta-v.md) — Δv pipeline (Kepler propagation, ManeuverCalculator formulas, precise burn UT)
- [`docs/algorithms/porkchop.md`](../../docs/algorithms/porkchop.md) — transfer types and porkchop grid algorithm

Key implementation notes for agents touching these files:
- Kepler solver tolerance: `1e-10`.
- Lambert `TransferComputer` enumerates both prograde and retrograde, picks minimum total Δv.
- **Known limitation**: 180° (anti-podal) geometry is degenerate — do not pass exactly-opposite position vectors.
- Update `docs/algorithms/` when changing any of these files: `LambertSolver.cs`, `KeplerSolver.cs`, `ManeuverCalculator.cs`, `TransferComputer.cs`.

## Body data

See [`docs/body-data-schema.md`](../../docs/body-data-schema.md) for the full field reference and body inventory.

- All 17 stock bodies hardcoded in `BodyDatabase.cs` from KSP wiki values (community-verified).
- Moons hold a reference to their parent body; Kerbol parent is `null`.
- OPM / MPE body support deferred to Increment 4.
- Update `docs/body-data-schema.md` when changing `CelestialBody.cs`, `OrbitalElements.cs`, or `BodyDatabase.cs`.

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
- Transfer types: `Ballistic` skips plane-change; `MidCoursePlaneChange` always applies it; `Optimal` picks whichever has lower total Δv.
- Phase angle uses r2 at **departureUT** (not arrivalUT). TransferPeriapsis/Apoapsis are altitudes above Kerbol's surface (subtract Kerbol.Radius from semi-latus rectum extrema).
- Porkchop grid spacing: endpoint-inclusive (`col / (cols − 1)`). Grid default is 100×100; `AutoTofRange` is always applied (PorkchopParameters does not carry MinTof/MaxTof).
