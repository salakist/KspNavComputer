# Increment 2 — Precise Maneuver export

## Plan

> Written before delivery. Not edited afterward.

Format each burn as the Precise Maneuver plaintext block (UT, prograde/normal/radial Δv,
total Δv in KSP calendar format). One-click copy-to-clipboard per burn.

---

## Inherited from prior increments

**From 1c**: `Burn.BurnUT`, `Burn.DeltaV`, and `Burn.Vector` (prograde/normal/radial
components) are already computed by `ManeuverCalculator` and exposed on both the
`TransferResult` and the API `BurnDto`. Increment 2 only needs to format these values
into the Precise Maneuver plaintext block and add the copy-to-clipboard UI.
No Core changes are expected.
