# docs/ — AGENTS.md

This folder contains the project planning and design documentation.
It is tracked in git (unlike `local-docs/`, which is gitignored).

---

## File map

| File | Contents |
|------|----------|
| [`initial-brief.md`](initial-brief.md) | The original brief as written before any planning — verbatim |
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

- **Plan sections**: written once at scoping time, never edited afterward.
- **Actuals sections**: written once at delivery, never edited afterward.
- **Inherited sections**: appended to when a prior increment's actuals identify an
  impact on this increment. Update both the source actuals and the target inherited
  section in the same commit.
- **Roadmap**: update the status column whenever an increment completes.
