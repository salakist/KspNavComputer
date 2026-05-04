# Body Data Schema

**Sources:** `src/Core/Bodies/CelestialBody.cs`, `src/Core/Bodies/OrbitalElements.cs`,
`src/Core/Bodies/BodyDatabase.cs`

---

## CelestialBody

| Field | Type | Unit | Notes |
|-------|------|------|-------|
| `Name` | string | — | Display name (e.g. `"Kerbin"`) |
| `GravParam` | double | m³/s² | μ = GM |
| `Radius` | double | m | Equatorial radius |
| `SphereOfInfluence` | double | m | SOI radius |
| `SiderealRotationPeriod` | double | s | Stored but not yet consumed (used in Increment 6) |
| `Parent` | CelestialBody? | — | `null` for Kerbol (the central star) |
| `Orbit` | OrbitalElements? | — | `null` for Kerbol; required for all other bodies |

**Invariants:**
- Exactly one body has `Parent = null` — Kerbol.
- Every body with `Parent != null` must have `Orbit != null`.
- `SphereOfInfluence` is not physically meaningful for Kerbol and is set to `double.MaxValue` / not used.

---

## OrbitalElements

All angles in radians, distances in metres, time in seconds (UT).

| Field | Symbol | Unit | Notes |
|-------|--------|------|-------|
| `SemiMajorAxis` | a | m | |
| `Eccentricity` | e | — | 0 = circular |
| `Inclination` | i | rad | Relative to Kerbol's equatorial plane |
| `LongitudeOfAscendingNode` | Ω | rad | |
| `ArgumentOfPeriapsis` | ω | rad | |
| `MeanAnomalyAtEpoch` | M₀ | rad | Mean anomaly at `Epoch` |
| `Epoch` | t₀ | s UT | UT at which M₀ is defined; 0 for all stock bodies |

Values are sourced from the KSP wiki (community-verified against the game binary).

---

## Stock body inventory

17 stock bodies are hardcoded in `BodyDatabase`:

| Parent | Bodies |
|--------|--------|
| — (star) | Kerbol |
| Kerbol | Moho, Eve, Kerbin, Duna, Dres, Jool, Eeloo |
| Eve | Gilly |
| Kerbin | Mun, Minmus |
| Jool | Laythe, Vall, Tylo, Bop, Pol |

---

## Increment 4 notes

OPM and MPE body data will be parsed from Kopernicus `.cfg` files in the local
`GameData` folder rather than hardcoded. The `CelestialBody` / `OrbitalElements` schema
above is stable; `BodyDatabase` will need to become dynamic (loadable at runtime) rather
than a static initialiser.

The `SiderealRotationPeriod` field is present but unused until Increment 6
(landed start support).
