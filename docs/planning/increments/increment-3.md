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
