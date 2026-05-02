# KspNavComputer.Core.Tests — AGENTS.md

## Purpose

xUnit test project for `KspNavComputer.Core`.

## Test files

| File | What it covers |
|------|---------------|
| `KeplerSolverTests.cs` | Eccentric anomaly solver; vis-viva speed verification for Kerbin and Mun |
| `LambertSolverTests.cs` | 90° circular-orbit quarter-turn; departure & arrival speed = circular speed |
| `TransferComputerTests.cs` | Kerbin→Duna Δv in plausible range; Kerbin→Jool finite positive; bad-parent guard |

## Known limitations in tests

- Lambert tests use **90° geometry** (not 180°) because the exact anti-podal
  geometry is a mathematical singularity of the universal-variable formulation
  (A = sin(Δν)·… = 0). Any test using exactly-opposite vectors will produce
  incorrect results — this is expected and documented in `LambertSolverTests.cs`.
- Assertion precision on Lambert speed tests is `precision: 1` (±0.05 m/s)
  to avoid rounding-at-0.5 false failures from the bisection convergence tolerance.

## Validation references

- Kerbin→Duna typical total Δv: 1 000 – 3 500 m/s (alexmoon Launch Window Planner).
  The test uses this as a sanity-check range; it does not import values from alexmoon.
- Vis-viva equation `v = √(μ(2/r − 1/a))` used as ground truth for Kepler tests.
