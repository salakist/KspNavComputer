# Increment 3a — Transfer accuracy improvements

## Scope

Two precision improvements that close the gap between our tool and LWP's "Refine Transfer" output:

1. **300×300 porkchop grid** — increase the default from 100×100 to 300×300 to match LWP's
   resolution. Reduces optimal-cell selection error and eliminates the ~2° phase angle
   discrepancy seen when comparing outputs side by side.

2. **SOI-exit correction (`refineTransfer`)** — account for Kerbin's movement during the
   ~6–8 h SOI transit. Without this, the ejection burn direction is slightly wrong and the
   spacecraft does not achieve a Duna encounter without manual adjustment (~147 s / ~28°
   observed in testing).

---

## Known issue motivating this increment

When pasting burn details from the tool into KSP's Precise Maneuver mod, no Duna encounter
appears. Moving the maneuver node ~147 s earlier (shifting ejection angle by ~28°) fixes it.
The Δv components are identical — only the burn UT needs correction. This is entirely caused
by the missing SOI-exit correction.

---

## 1 — 300×300 grid

**What to change:**
- `PorkchopParameters.cs`: change defaults `GridCols = 300, GridRows = 300`
- `src/Web/src/api/transferClient.ts`: update `PorkchopRequest` default or send explicit 300
- Reference tests: no changes needed; `ReferencePorkchopTests.cs` already validates against
  a pre-computed 300×300 LWP fine optimal

---

## 2 — SOI-exit correction

**Algorithm (LWP `Orbit.refineTransfer`, 10 iterations):**

1. Compute ejection hyperbola from the current ejection Δv vector
2. Find the true anomaly at SOI exit → integrate hyperbola time to get actual exit time $t_1$
3. Get origin body's heliocentric position at $t_1$ (it has moved since `departureUT`)
4. Corrected heliocentric start position = origin SOI centre + hyperbola exit position vector
5. Corrected TOF = original TOF − $(t_1 − t_0)$
6. Re-solve Lambert from corrected position/time → new ejection velocity
7. Repeat from step 1 until convergence (10 iterations sufficient)

**What to add:**
- `TransferComputer.cs`: new private method `RefineTransfer` implementing the loop above;
  called after the initial Lambert solve when origin has an SOI radius
- `TransferResult.cs`: no new fields needed — the refined ejection burn replaces the
  unrefined one in the same `Ejection` field

**Notes:**
- Only applies to ejection from a body with a finite SOI (Kerbin, Eve, etc.). Insertion
  correction (at destination) is less critical and can be deferred.
- The correction only affects burn UT and burn vector direction — total Δv magnitude
  changes by < 0.1%.
