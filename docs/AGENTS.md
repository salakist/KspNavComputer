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
| [`algorithms/lambert.md`](algorithms/lambert.md) | Lambert solver: normalised variables, arc selection, multi-revolution, velocity reconstruction |
| [`algorithms/delta-v.md`](algorithms/delta-v.md) | Transfer Δv pipeline: Kepler propagation, Lambert, maneuver calculation, burn UT |
| [`algorithms/porkchop.md`](algorithms/porkchop.md) | Transfer types (ballistic/plane-change/optimal) and porkchop grid algorithm |
| [`planning/overview.md`](planning/overview.md) | Problem statement, architecture decisions, design principles, licensing |
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
| `LambertSolver.cs`, `KeplerSolver.cs` | [`algorithms/lambert.md`](algorithms/lambert.md) |
| `ManeuverCalculator.cs`, `TransferComputer.cs` | [`algorithms/delta-v.md`](algorithms/delta-v.md) |
| `PlaneChangeComputer.cs`, `PorkchopComputer.cs`, `TransferType.cs` | [`algorithms/porkchop.md`](algorithms/porkchop.md) |
| `CelestialBody.cs`, `OrbitalElements.cs`, `BodyDatabase.cs` | [`body-data-schema.md`](body-data-schema.md) |
| Commit/PR workflow changes | [`commit-policy.md`](commit-policy.md) |
