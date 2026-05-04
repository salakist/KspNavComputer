# KSP Navigation Computer — Overview

> Cross-cutting decisions, architecture, and principles.
> This section does not belong to any single increment.
> See [`roadmap.md`](roadmap.md) for delivery status.

---

## Problem

No single existing tool covers the full workflow: transfer window timing, landed starts,
sub-system transfers (e.g. Kerbin → Mun), OPM/MPE body support, and direct copy-paste
into Precise Maneuver. The app fills a real gap.

---

## Architecture

- **Core library**: pure C# (.NET 8) class library, no web dependencies — contains all
  orbital mechanics. Designed so it can later be consumed by both the desktop app and a
  KSP plugin.
- **Backend**: ASP.NET Core 8 API wrapping the core library.
- **Frontend**: Vite + React + TypeScript, communicating with the local API.

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
  [increment-1a.md](increments/increment-1a.md) actuals.*

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

| Reference | Used for |
|-----------|----------|
| [Transfer Window Planner](https://github.com/TriggerAu/TransferWindowPlanner) (TriggerAu) | Cross-reference for Lambert solver and short-way/long-way arc selection; inspiration for porkchop-plot visualisation (Increment 3) |
| [Launch Window Planner](https://alexmoon.github.io/ksp/) (alexmoon) | Primary reference for Lambert solver; validation oracle for all reference transfer test cases; inspiration for mission-planning UX |
| Sun — *"On the Minimum Time Trajectory and Multiple Solutions of Lambert's Problem"* (AAS 79-164, 1979) | Lambert solver algorithm |
| [KSP Wiki — Celestial bodies](https://wiki.kerbalspaceprogram.com/wiki/Category:Celestial_bodies) | Gravitational parameters, radii, SOI radii, and orbital elements for all stock bodies |
| [Precise Maneuver mod](https://github.com/hxtk/KSP-Precise-Maneuver) | Target format for maneuver-node copy-paste export (Increment 2) |
| [Outer Planets Mod](https://github.com/Kopernicus/Outer-Planets-Mod) | Body data for OPM bodies (Increment 4) |
| [Minor Planets Expansion](https://github.com/ProximaCentauri-star/MinorPlanetsExpansion) | Body data for MPE bodies (Increment 4) |
