# Increment 4 — OPM + MPE body support

## Plan

> Written before delivery. Not edited afterward.

Parse Kopernicus `.cfg` files from the local GameData folder to expand the body database
with all Outer Planets Mod and Minor Planets Expansion bodies.

Local GameData path:
`C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program\GameData`

---

## Inherited from prior increments

**From 1a**: `BodyDatabase` is a static registry of hardcoded records. Increment 4 will
need to make the database dynamic (or supplementary) so parsed bodies can be added at
runtime alongside the 17 stock bodies. The reference-data validation pattern
(generator script → JSON → xUnit theory) is available to reuse for OPM/MPE body
cross-validation if LWP reference values exist.
