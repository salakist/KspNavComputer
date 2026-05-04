# Increment 9 — In-game mod

## Plan

> Written before delivery. Not edited afterward.

KSP plugin that reuses the Core library directly, replacing the need for the desktop app
during active gameplay. Architecture of the Core library (no web dependencies, pure .NET)
is designed to support this from the start.

---

## Inherited from prior increments

**From 2 (Precise Maneuver integration)**: the clipboard text produced by
`PreciseManeuverFormatter` intentionally omits the `Ejection Angle:` and `Ejection Inc.:`
lines. The PM mod's `NodeManager.ChangeNodeFromString` treats those lines as node
repositioning commands (not annotations), which shifts the burn UT when pasted. The
ejection geometry is display-only; the in-game mod must preserve this constraint if it
generates or forwards PM clipboard text.
