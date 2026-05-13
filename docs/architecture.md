# Architecture — Core Layer

> This document covers the Core library only (`src/Core/`).
> For the full tech stack and design principles see [overview.md](overview.md).

---

## Layer structure

```
src/Web/          Vite + React + TypeScript
      ↓  HTTP
src/Api/          ASP.NET Core 8 — thin endpoints + DTOs
      ↓  direct reference
src/Core/         C# .NET 8 class library — all orbital mechanics
```

`Core` has no dependencies on `Api` or `Web`. `Api` references `Core` only. `Web` talks
to `Api` only via HTTP.

---

## Core namespace map

| Namespace | Folder | Responsibility |
|-----------|--------|----------------|
| `Mechanics` | `src/Core/Mechanics/` | Kepler propagation, Lambert solver, `Vector3d` |
| `Maneuver` | `src/Core/Maneuver/` | Hyperbolic burns, ejection angle, PM formatter |
| `Transfer` | `src/Core/Transfer/` | Orchestration, plane change, round trip |
| `Porkchop` | `src/Core/Porkchop/` | Grid computation, TOF auto-range |
| `Bodies` | `src/Core/Bodies/` | `CelestialBody`, `OrbitalElements`, `BodyDatabase` |
| `Time` | `src/Core/Time/` | KSP calendar conversion |

---

## One-way transfer call graph

```
TransferComputer.Compute(TransferParameters)
├── KeplerSolver.StateAt(origin.Orbit, μ, departureUT)       → (r1, v1Body)
├── KeplerSolver.StateAt(destination.Orbit, μ, arrivalUT)    → (r2, v2Body)
├── LambertSolver.SolveAllRevolutions(r1, r2, tof, μ, prograde=true)   → ≤21 solutions
├── LambertSolver.SolveAllRevolutions(r1, r2, tof, μ, prograde=false)  → ≤21 solutions
│   (select best ejection + insertion pair from up to 42 candidates)
├── ManeuverComputer.Compute(ejection params)   → Burn (ejection)
├── ManeuverComputer.Compute(insertion params)  → Burn (insertion)
├── [optional] PlaneChangeComputer.Compute(PlaneChangeParameters)
│   ├── LambertSolver.SolveAllRevolutions(0-rev prograde, rotated r2)
│   └── → PlaneChangeResult (planeChange Burn, adjusted vT1/vT2)
└── [if origin has finite SOI] RefineEjection(10 iterations)
    ├── KeplerSolver.StateAt(origin.Orbit, μ, departureUT + tTransit)
    ├── LambertSolver.SolveAllRevolutions(r1Corr, r2, tofCorr, …)
    └── ManeuverComputer.Compute(corrected ejection)
```

Input type: `TransferParameters`  
Output type: `TransferResult`  
Algorithm detail: [algorithms/transfer.md](algorithms/transfer.md)

---

## Porkchop call graph

```
PorkchopComputer.Compute(PorkchopParameters)
├── AutoTofRange(origin, destination, μ)       → (minTof, maxTof)
└── for each (row=tof, col=departureUT):
    └── TransferComputer.Compute(TransferParameters)   → dv cell value
```

Input type: `PorkchopParameters`  
Output type: `PorkchopResult` (flat `double[]` grid + statistics)  
Algorithm detail: [algorithms/porkchop.md](algorithms/porkchop.md)

---

## Round-trip call graph

```
TransferComputer.ComputeRoundTrip(RoundTripParameters)
├── TransferComputer.Compute(outbound)   → TransferResult
└── TransferComputer.Compute(return leg, origin↔destination swapped)
```

Output type: `RoundTripResult`

---

## Key data flow (one-way transfer)

```
TransferParameters
  ├── Origin: CelestialBody (with Orbit)
  ├── Destination: CelestialBody (with Orbit)
  ├── DepartureUT: double
  ├── TimeOfFlight: double
  ├── OriginOrbit: ParkingOrbit (altitude, eccentricity, inclination)
  ├── DestinationOrbit: ParkingOrbit
  └── TransferType: Ballistic | MidCoursePlaneChange | Optimal

TransferResult
  ├── Ejection: Burn (DeltaV, BurnUT, BurnVector, EjectionDetails?)
  ├── Insertion: Burn
  ├── PlaneChange: Burn?
  ├── TotalDeltaV: double
  ├── PhaseAngleDeg, TransferAngleDeg
  └── TransferPeriapsis, TransferApoapsis, InsertionInclinationDeg
```

---

## Algorithm doc index

| Component | Source file | Algorithm doc |
|-----------|-------------|---------------|
| Kepler propagation | `KeplerSolver.cs` | [algorithms/kepler.md](algorithms/kepler.md) |
| Lambert solver | `LambertSolver.cs` | [algorithms/lambert.md](algorithms/lambert.md) |
| Maneuver calculation | `ManeuverComputer.cs` | [algorithms/maneuver.md](algorithms/maneuver.md) |
| Plane change | `PlaneChangeComputer.cs` | [algorithms/plane-change.md](algorithms/plane-change.md) |
| Transfer orchestration | `TransferComputer.cs` | [algorithms/transfer.md](algorithms/transfer.md) |
| Porkchop grid | `PorkchopComputer.cs` | [algorithms/porkchop.md](algorithms/porkchop.md) |
| Body / orbit data | `CelestialBody.cs`, `OrbitalElements.cs`, `BodyDatabase.cs` | [body-data-schema.md](body-data-schema.md) |
