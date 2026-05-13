# Transfer Computer

**Source:** `src/Core/Transfer/TransferComputer.cs`

---

## Overview

`TransferComputer.Compute` converts
`(departureUT, timeOfFlight, originParkingOrbit, destinationParkingOrbit)` into ejection
and insertion `Burn` records via four steps:

| Step | Component | Doc |
|------|-----------|-----|
| 1 | Kepler propagation — body state vectors at departure and arrival UT | [kepler.md](kepler.md) |
| 2 | Lambert solver — heliocentric transfer velocities | [lambert.md](lambert.md) |
| 3 | Maneuver calculation — hyperbolic excess velocity → periapsis Δv + burn UT | [maneuver.md](maneuver.md) |
| 4 | SOI-exit correction — refine ejection for origin body movement during SOI transit | below |

See [architecture.md](../architecture.md) for the full call graph.

---

## Step 2 — Lambert candidate selection

`SolveAllRevolutions` is called twice (prograde and retrograde, N = 0..10), yielding up to
42 candidate pairs — 21 per direction. The pair with the lowest combined ejection + insertion
Δv is retained.

---

## Transfer types

`TransferComputer` accepts a `TransferType` enum: `Ballistic`, `MidCoursePlaneChange`, `Optimal`.

### Ballistic

Multi-rev Lambert solve (prograde + retrograde, all revolutions). Best result = lowest total Δv.

### Mid-course plane change

See [plane-change.md](plane-change.md). `PlaneChangeComputer.Compute` returns `null` when
the plane change is not beneficial (coplanar bodies, transfer angle ≤ 90°, or Lambert fails);
the caller falls back to ballistic in that case.

### Optimal

Runs both ballistic and mid-course plane change; returns whichever has lower total Δv.

---

## `TransferResult` output fields

| Field | Description |
|-------|-------------|
| `PhaseAngleDeg` | Signed phase angle at departure; outer → inner has 2π subtracted (negative), matching LWP |
| `TransferAngleDeg` | CCW heliocentric transfer angle (degrees) |
| `TransferPeriapsis/Apoapsis` | Transfer ellipse periapsis/apoapsis distance from Kerbol (m) |
| `InsertionInclinationDeg` | $\arcsin(v_{\infty,z} / v_\infty)$ at arrival — angle of the insertion hyperbola relative to the ecliptic |

---

## Step 4 — SOI-exit correction (`RefineEjection`)

**Source:** `TransferComputer.cs` — `RefineEjection` (private), called from `Compute` when origin
has a finite SOI.  
**Algorithm reference:** LWP `src/orbit.coffee` — `Orbit.refineTransfer` (10 iterations).

The initial Lambert solve assumes the spacecraft departs from the origin body's heliocentric
centre at `departureUT`. In reality the spacecraft fires at periapsis inside the SOI
(`burnUT = departureUT − t_transit`) and exits the SOI at `departureUT + t_transit`. During
this ~6-8 h transit the origin body moves, shifting the true heliocentric departure point by
up to ~234,000 km for a Kerbin-class body.

### Per-iteration algorithm

1. **Hyperbola geometry** from current `vInf = vT1 − v1Body`:
$$
v_{peri} = \sqrt{v_\infty^2 + 2\mu_0\!\left(\frac{1}{r_{peri}} - \frac{1}{r_{SOI}}\right)},
\quad e = \frac{r_{peri}\,v_{peri}^2}{\mu_0} - 1, \quad |a| = \frac{\mu_0}{v_\infty^2}
$$

2. **True anomaly at SOI exit:**
$$
\cos\nu_{SOI} = \frac{r_{peri}(1+e) - r_{SOI}}{e\,r_{SOI}}
$$

3. **Transit time** (periapsis → SOI exit) via hyperbolic eccentric anomaly $H$:
$$
H = \text{acosh}\!\left(\frac{e + \cos\nu_{SOI}}{1 + e\cos\nu_{SOI}}\right), \quad
t_{transit} = \frac{e\sinh H - H}{n}, \quad n = \sqrt{\frac{\mu_0}{|a|^3}}
$$

4. **Periapsis direction** $\hat{p} = (p_x, p_y, 0)$ (equatorial) satisfying
   $\hat{p}\cdot\hat{v}_\infty = \cos\nu_\infty = -1/e$, CCW orbit condition
   (same quadratic solver as §Ejection angle above but with asymptote angle instead of
   zenith-corrected θ).

5. **SOI-exit position relative to origin body:**
$$
\hat{n} = \text{normalize}(\hat{p} \times \mathbf{v_\infty}), \quad
\hat{t} = \hat{n} \times \hat{p}
$$
$$
\mathbf{r}_{SOI,rel} = r_{SOI}\!\left(\cos\nu_{SOI}\,\hat{p} + \sin\nu_{SOI}\,\hat{t}\right)
$$

6. **Corrected heliocentric departure:**
$$
t_1 = departureUT + t_{transit}, \quad
\mathbf{r}_{1,corr} = \mathbf{r}_{origin}(t_1) + \mathbf{r}_{SOI,rel}
$$

7. **Corrected TOF:** $\Delta t_{corr} = TOF - t_{transit}$ (arrival time unchanged).

8. **Re-solve Lambert** from $\mathbf{r}_{1,corr}$ to $\mathbf{r}_2$ over $\Delta t_{corr}$;
   pick the solution with minimum ejection Δv.

9. **LWP averaging** (even iterations): average current $\mathbf{v}_{T1}$ with the odd-iteration
   value to damp potential oscillation in the series.

After 10 iterations the final `ManeuverComputer.Compute` call uses `v1Body` (at the original
`departureUT`) as the prograde reference for the ejection angle, matching LWP's
`normalize(prograde_at_t0)` convention. The burn UT and Δv magnitude use the corrected
body velocity at `t_1`.

Only the `Ejection` field in `TransferResult` is updated; insertion is unchanged.
Insertion-side correction is deferred (less critical, < 0.1% Δv error).
