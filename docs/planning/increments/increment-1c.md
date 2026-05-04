# Increment 1c — Inclined / elliptical parking orbits

## Plan

> Written before delivery. Not edited afterward.

Decompose ejection burn into prograde/normal/radial components. Add golden-section search
for optimal ejection angle when parking inclination differs from transfer plane. Extend
inputs and results accordingly.

QA: inclined orbit raises Δv predictably vs equatorial baseline; compare against
alexmoon's site with inclined orbit setting.

---

## Actuals

> Post-delivery notes. Not edited after delivery.

**Ejection plane-change method**: the plan called for a golden-section search for the
optimal ejection angle. What was built is a direct analytic formula:
`deltaI = max(0, |α| − i_park)` where `α = asin(v∞_z / |v∞|)`. This is a closed-form
law-of-cosines computation, not a search — cheaper to evaluate and correct under the
standard assumption that the burn occurs at periapsis in the SOI entry plane. No
golden-section search exists in the codebase.

**Insertion inclination modelling**: the plan was silent on this. In delivery, insertion
was extended with the same law-of-cosines treatment as ejection:
`deltaI_arr = max(0, |α_arr| − i_dest)`. When `i_dest = 0` (default), the formula
collapses to pure deceleration, preserving backward compatibility with 1a/1b results.

**Burn vectors**: the plan scoped prograde/normal/radial decomposition to Increment 2 as
a Precise Maneuver prerequisite. In delivery they were produced in 1c and exposed in the
API and UI. `BurnVector` and `Burn` (grouping Δv scalar, burn UT, and vector) are now
first-class domain types in Core.

**Precise periapsis burn UT**: not mentioned in the plan for any increment. Added in 1c.
Computed via the hyperbolic transit time formula:
`|a| = μ/v∞²`, `e_hyp = 1 + r_peri/|a|`, `F = acosh((r_SOI/|a| + 1)/e_hyp)`,
`t = sqrt(|a|³/μ)·(e_hyp·sinh(F) − F)`.
Ejection: `burnUT = departureUT − t`. Insertion: `burnUT = arrivalUT + t`.

**`ManeuverCalculator` as a distinct component**: not in the original design. Extracted
as an `internal static class` in its own file, responsible solely for converting a
heliocentric transfer velocity vector into a `Burn`. Keeps `TransferComputer` focused on
the Lambert/Kepler orchestration loop.

**API DTO structure**: the plan implied flat scalar fields on `TransferResponse`.
Delivered with a nested `BurnDto` per burn (ejection and insertion), each carrying
`deltaV`, `burnUT`, `burnDate`, and a `BurnVectorDto`. Breaking change relative to the
1b API shape, but no external consumers existed at that point.

**Bruno API collection**: not planned. Added as a manual-testing artifact. Contains
10 LWP reference-case requests and 3 inclination-demo requests (Kerbin→Eeloo at
`i_dest = 0 / 3 / 10°`) demonstrating insertion inclination sensitivity.

**Impact on later increments**: recorded in each impacted increment's
"Inherited from prior increments" section.

---

## Inherited from prior increments

**From 1a**: multi-revolution Lambert solver and reference-data validation pattern
already in place. Mapper layer pattern reused in `TransferMapper.ToBurnDto`.

**From 1b**: parking orbit reuse convention (one orbit per body, shared across legs)
unchanged; round-trip tests updated in place to reference the new `Burn` record fields.
