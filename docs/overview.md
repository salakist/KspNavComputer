# KSP Navigation Computer — Overview

> Cross-cutting decisions, architecture, and principles.
> This section does not belong to any single increment.
> See [`roadmap.md`](planning/roadmap.md) for delivery status.

---

## Problem

No single existing tool covers the full workflow: transfer window timing, landed starts,
sub-system transfers (e.g. Kerbin → Mun), OPM/MPE body support, and direct copy-paste
into Precise Maneuver. The app fills a real gap.

---

## Architecture

See [architecture.md](architecture.md) for the Core layer call graph and component map.
The overall stack is: Core library (C# .NET 8) ← ASP.NET Core 8 API ← Vite + React + TypeScript frontend.

---

## Design principles

- SOLID, clean layering. No full DDD ceremony.
- Incremental delivery with a human QA pass on each increment.
- Repo maintains nested `AGENTS.md` files (repo-level, module-level) that evolve
  alongside the codebase.
- Unit tests are a first-class deliverable alongside the core library.

---

## Licensing and code reuse

- The math (Lambert's problem, Kepler's equation) is implemented independently, using
  publicly available orbital mechanics literature as reference (primarily Braeunig's
  *Rocket and Space Technology*, Sun/F.T. 1979 Lambert formulation).
- alexmoon's Launch Window Planner and TriggerAu's Transfer Window Planner are credited
  as algorithmic inspiration and validation reference only — no code copied.
- Stock body orbital data sourced from KSP wiki values (community-verified against the
  game binary); hardcoded in Increment 1.
- OPM and MPE body orbital data will be parsed from the Kopernicus `.cfg` files in the
  local GameData folder (Increment 4).

---

## Technical decisions (at planning time)

- `Vector3d` implemented in-house as a double-precision struct (dot, cross, norm,
  arithmetic); no external math library dependency.
- All internal time in UT seconds. KSP calendar (6-hour days, 426-day years) used
  only for display and input.
- Lambert solver: planned as 0-revolution elliptical solutions only.
  *Actual: multi-revolution Sun/Gooding (1979) implemented from the start — see
  [increment-1a.md](planning/increments/increment-1a.md) actuals.*

---

## KSP save file handling

- The app will accept a `.sfs` save file import (Increment 5).
- A vessel selection dropdown will allow choosing the active vessel as the departure context.
- Live in-game integration is deferred to the future mod phase (Increment 9).

---

## Deferred to later increments

- Sub-system transfers (planet → moon, moon → moon): algorithmically distinct,
  added once the interplanetary core is solid (Increment 8).
- KSP in-game mod integration: architecture supports it, implementation is future work
  (Increment 9).

---

## Credits and references

See [README.md — Credits and inspirations](../README.md#credits-and-inspirations) for the full reference list.
