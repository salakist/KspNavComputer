# docs/ — AGENTS.md

This folder contains the project planning and design documentation.
It is tracked in git (unlike `local-docs/`, which is gitignored).

---

## File map

| File | Contents |
|------|----------|
| [`initial-brief.md`](initial-brief.md) | The original brief as written before any planning — verbatim |
| [`commit-policy.md`](commit-policy.md) | Agent commit policy: git identity, branch naming, commit format, PR workflow |
| [`body-data-schema.md`](body-data-schema.md) | CelestialBody and OrbitalElements field reference; body inventory |
| [`overview.md`](overview.md) | Problem statement, design principles, licensing, technical decisions |
| [`architecture.md`](architecture.md) | Core layer call graphs, namespace map, key data types, algorithm doc index |
| [`algorithms/kepler.md`](algorithms/kepler.md) | Kepler propagation: mean/eccentric/true anomaly, perifocal → inertial rotation |
| [`algorithms/lambert.md`](algorithms/lambert.md) | Lambert solver: normalised variables, arc selection, multi-revolution, velocity reconstruction |
| [`algorithms/maneuver.md`](algorithms/maneuver.md) | Maneuver calculation: hyperbolic excess, Δv, burn UT, ejection angle and inclination |
| [`algorithms/plane-change.md`](algorithms/plane-change.md) | Mid-course plane change: relative inclination, golden-section search, Rodrigues rotation |
| [`algorithms/transfer.md`](algorithms/transfer.md) | Transfer orchestration: Lambert selection, transfer types, SOI-exit correction |
| [`algorithms/porkchop.md`](algorithms/porkchop.md) | Porkchop grid: TOF auto-range, grid indexing, log statistics |
| [`planning/roadmap.md`](planning/roadmap.md) | Increment status table and one-line description of each increment |
| `planning/increments/increment-N.md` | Plan + actuals for each increment (see below) |

---

## Increment file structure

Each `increment-N.md` file follows this layout:

```
## Plan
(Original scope written before the increment started — never edited after delivery)

## Actuals
(Post-delivery notes: what differed from the plan, why, and any design decisions made)

## Inherited from prior increments
(Constraints, completed work, or changed context from earlier increments that
affects this one — written at the time the source increment ships)
```

Not-started increments have a **Plan** section only.
The **Inherited** section is populated when a prior increment's actuals identify an impact.
The **Actuals** section is populated when this increment is delivered.

---

## Update policy

All doc updates must land in the **same commit** as the triggering change.

**Increment files:**

| Event | Action |
|-------|--------|
| Increment scoped | Add `## Plan` to `planning/increments/increment-N.md`; update `planning/roadmap.md` |
| Increment delivered | Add `## Actuals` to `planning/increments/increment-N.md`; update `planning/roadmap.md` |
| Prior increment actuals affect a future increment | Add `## Inherited` to the future `increment-N.md` in the same commit as the actuals |

Plan sections are written once and never edited. Actuals sections are written once at delivery and never edited.

**Code-coupled docs:**

| Changed file(s) | Doc to update |
|-----------------|---------------|
| `KeplerSolver.cs` | [`algorithms/kepler.md`](algorithms/kepler.md) |
| `LambertSolver.cs` | [`algorithms/lambert.md`](algorithms/lambert.md) |
| `ManeuverComputer.cs` | [`algorithms/maneuver.md`](algorithms/maneuver.md) |
| `PlaneChangeComputer.cs` | [`algorithms/plane-change.md`](algorithms/plane-change.md) |
| `TransferComputer.cs` | [`algorithms/transfer.md`](algorithms/transfer.md) |
| `PorkchopComputer.cs`, `TransferType.cs` | [`algorithms/porkchop.md`](algorithms/porkchop.md) |
| `CelestialBody.cs`, `OrbitalElements.cs`, `BodyDatabase.cs` | [`body-data-schema.md`](body-data-schema.md) |
| Commit/PR workflow changes | [`commit-policy.md`](commit-policy.md) |
