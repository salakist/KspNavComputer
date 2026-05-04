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
    ├── ParkingOrbit.cs          record: Altitude [m], Inclination [rad, default 0], Eccentricity [default 0]
    ├── TransferParameters.cs    record: Origin, Destination, DepartureUT, TOF, parking orbits
    ├── BurnVector.cs            record: Prograde, Normal, Radial [m/s]; Magnitude property; Zero sentinel
    ├── Burn.cs                  record: DeltaV [m/s], BurnUT [s UT], Vector (BurnVector)
    ├── TransferResult.cs        record: DepartureUT, ArrivalUT, Ejection (Burn), Insertion (Burn), TotalDeltaV
    ├── RoundTripParameters.cs   record: Origin, Destination, DepartureUT, OutboundTOF, StayDuration,
    │                                    ReturnTOF, OriginOrbit, DestinationOrbit
    ├── RoundTripResult.cs       record: Outbound (TransferResult), Return (TransferResult), TotalDeltaV
    ├── ManeuverCalculator.cs    internal static: converts heliocentric transfer velocity → Burn
    └── TransferComputer.cs      orchestrates Kepler → Lambert → ManeuverCalculator; ComputeRoundTrip
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
- Parking orbit speed at periapsis: `v_park = sqrt(μ·(1+e)/r_peri)`.  For circular
  orbits (e=0) this equals `v_circ`.  Burn is always executed at periapsis.
- Ejection Δv + burn vector: `ManeuverCalculator` decomposes into prograde/normal components
  using law-of-cosines plane change with effective angle
  `deltaI = max(0, |alpha| − i_park)`, where `alpha = asin(v∞_z / |v∞|)` and `i_park`
  is the parking orbit inclination.
  `prograde = v_hyper·cos(deltaI) − v_park`,  `normal = v_hyper·sin(deltaI)·sign(alpha)`,
  `v_hyper = sqrt(v∞² + 2μ/r_peri − 2μ/r_SOI)`.
  Collapses to pure prograde `|v_hyper − v_park|` when deltaI = 0.
- Insertion Δv + burn vector: symmetric law-of-cosines when `i_dest > 0`:
  `deltaI_arr = max(0, |alpha_arr| − i_dest)`.
  `prograde = v_park − v_hyper·cos(deltaI_arr)`,  `normal = −v_hyper·sin(deltaI_arr)·sign(alpha_arr)`.
  When `i_dest = 0` (default): pure deceleration `v_park − v_hyper` (backward compatible,
  matches LWP `insertionToCircularDeltaV`).
- Precise periapsis burn UT: `|a| = μ/v∞²`, `e_hyp = 1 + r_peri/|a|`,
  `F = acosh((r_SOI/|a| + 1) / e_hyp)`, `t = sqrt(|a|³/μ)·(e_hyp·sinh(F) − F)`.
  Ejection: `burnUT = departureUT − t`.  Insertion: `burnUT = arrivalUT + t`.

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
