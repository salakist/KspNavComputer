# KSP Navigation Computer — Initial Brief

> This is the original brief as written before any planning or implementation.
> It is preserved verbatim as the authoritative statement of the original problem.

---

## Original issue

I'm currently playing the game Kerbal Space Program.

I'm using the Transfer Window Planner mod + the Precise Maneuver mod + a separate delta v
map on the side to handle navigation between bodies.

I'm also playing with the mods Outer Planets Expansion (OPE) and Minor Planets Expansion
(MPE).

I have a few issues with that:
- The Transfer Window Planner mod only handles the computations from orbit to orbit and
  doesn't handle landings/takeoffs
- It also doesn't handle transfers between bodies orbiting one another, like Kerbin and
  the Mun
- The burn details copy-paste into the Precise Maneuver Mod doesn't work and allow copy
  of all details not burn by burn if that makes sense
- I have to have the delta-V map opened on the side which is annoying

I'd like to have a single app to handle all of that.

I'm not opposed to making it an in-game mod later.

## Required features

- Calculator for transfer windows and associated burns, be feature equivalent to the
  TransferWindowPlanner mod
- Also handle calculations for landed starts
- Allow copy-paste of individual burns into the Precise Maneuver mod
- Handle computations for the bodies added by the OPE and MPE mods
- Have an integrated delta-V map
- Handle KSP saves to have computation start time

## Technical architecture

It should probably be a simple desktop app to start, with a minimal user interface.

I think we should focus on having the core computation feature down before we start
thinking about UX/UI too much.

Language-wise, I'm feeling comfortable with C#, JS, TS & React.

I'm strict in regards to DDD and SOLID principles.

Implementation should be incremental with human QA pass on each increment.

## External resources

- Website that approaches what I'd like: https://suppise-dv-calculator.com/
- Transfer Window Planner mod repository: https://github.com/TriggerAu/TransferWindowPlanner
- The Launch Window Planner website on which the Transfer Window Planner mod is based:
  https://alexmoon.github.io/ksp/
- And its repository: https://github.com/alexmoon/ksp
- Precise Maneuver mod repository: https://github.com/zer0Kerbal/PreciseManeuver
- MPE mod repository: https://github.com/ExosLab/Minor-Planets-Expansion
- My local GameData KSP folder with relevant mod folders: Squad (stock),
  TransferWindowPlanner, OPM, MPE:
  `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program\GameData`
- deltaV map example (the one that I use):
  https://www.reddit.com/media?url=https%3A%2F%2Fi.redd.it%2Fq8i47o8prlz41.png
