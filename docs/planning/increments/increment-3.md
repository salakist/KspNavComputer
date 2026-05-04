# Increment 3 — Transfer window porkchop plot

## Plan

> Written before delivery. Not edited afterward.

Compute and display the Δv / time-of-flight grid over a range of departure dates.
User clicks a point to select a transfer and populate the burn details.

---

## Inherited from prior increments

**From 1a**: multi-revolution Lambert solver (N = 0..10, prograde + retrograde) is
already in place. `TransferComputer.Compute` is the single call site per grid cell;
no solver changes are expected for the porkchop computation.

**From 1c**: `TransferComputer.Compute` now returns a richer `TransferResult` (with
`Burn` objects rather than scalars), but the call signature is unchanged. The porkchop
loop calls `Compute` per cell and reads only `TotalDeltaV` for the colour map; the
extra fields are free and available when the user selects a cell to populate burn details.

**Scope note (from 1b actuals)**: the porkchop plot will initially be one-way only.
Extending to round trips would require a 3-axis sweep (departure × outbound TOF ×
return TOF); that is deferred unless explicitly scoped in.

---

## Actuals

Delivered as planned. Significant scope additions emerged during implementation:

### Core additions beyond plan

- **Transfer types** (`TransferType` enum): `Ballistic`, `MidCoursePlaneChange`, `Optimal`.
  `PlaneChangeComputer` implements golden-section search for the optimal mid-course
  plane-change angle using Rodrigues rotation. `TransferComputer` now dispatches to the
  right path and picks the lower-Δv result for `Optimal`.
- **Extended `TransferResult`**: added `PlaneChangeBurn?`, `PhaseAngleDeg`, `TransferAngleDeg`,
  `TransferPeriapsis`, `TransferApoapsis`, `InsertionInclinationDeg`.
- **Phase angle fix**: `r2` at departure UT (not arrival UT) for phase angle, matching LWP.
- **Altitude convention**: `TransferPeriapsis`/`Apoapsis` are altitudes above Kerbol's surface,
  not distances from centre.
- **`PorkchopComputer`** with `AutoTofRange` (Hohmann formula, matches LWP).
  Grid uses endpoint-inclusive spacing (`col / (cols − 1)`).

### API additions

- `POST /api/porkchop` via `PorkchopEndpoints.cs`
- `TransferRequest` extended with `transferType` and `noInsertionBurn`
- `TransferResponse` extended with all new `TransferResult` fields

### Frontend

- `PorkchopForm`, `PorkchopPlot` (400×300 canvas, log-scale colour map, click handler),
  `TransferDetails` with PM copy buttons and conditional plane-change section
- App rewired to two-column layout (form left, plot + details right)

### Tests

- `ReferencePorkchopTests.cs` — 9 theory tests (3 cases) against LWP porkchop reference data
- Generator: `scripts/generate-reference-porkchop.js`; data: `tests/Core/Data/reference-porkchop.json`

### Known limitation (deferred to 3a)

Grid is 100×100 (vs LWP's 300×300); no SOI-exit correction (`refineTransfer`).
Burn UT is off by ~147 s for Kerbin→Duna Y3 window, preventing a direct KSP encounter.
See [increment-3a.md](increment-3a.md).
