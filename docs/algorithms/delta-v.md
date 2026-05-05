# Transfer Δv Pipeline

**Sources:** `src/Core/Transfer/TransferComputer.cs`, `src/Core/Maneuver/ManeuverComputer.cs`,
`src/Core/Mechanics/KeplerSolver.cs`

---

## Overview

Four steps convert `(departureUT, timeOfFlight, originParkingOrbit, destinationParkingOrbit)`
into ejection and insertion `Burn` records:

1. **Kepler propagation** — body state vectors at departure and arrival UT
2. **Lambert solver** — heliocentric transfer velocities at those positions
3. **Maneuver calculation** — convert hyperbolic excess velocity to a periapsis Δv and
   precise burn UT
4. **SOI-exit correction** — refine ejection burn to account for origin body movement
   during the ~6-8 h SOI transit (`RefineEjection`, 10 iterations)

---

## Step 1 — Kepler propagation

`KeplerSolver.StateAt(orbit, μ, ut)` propagates an `OrbitalElements` record to a given UT:

1. **Mean anomaly:** $M = M_0 + n \cdot (t - t_0)$, where $n = \sqrt{\mu / a^3}$
2. **Eccentric anomaly:** Newton-Raphson on Kepler's equation $M = E - e \sin E$, tolerance $10^{-10}$
3. **True anomaly:** $\nu = \text{atan2}\!\left(\sqrt{1-e^2}\sin E,\ \cos E - e\right)$
4. **State vectors:** computed in the perifocal frame, then rotated by $(ω, i, Ω)$ to the
   reference frame (ecliptic-aligned for Kerbol)

---

## Step 2 — Lambert → heliocentric velocities

See [lambert.md](lambert.md). `TransferComputer` enumerates N = 0..10 solutions for both
prograde and retrograde arcs (up to 22 candidate pairs) and retains the pair with the
minimum total Δv.

---

## Step 3 — Maneuver calculation

`ManeuverComputer.Compute` takes the heliocentric transfer velocity **v_transfer** and the
body's heliocentric velocity **v_body** and produces a `Burn` (Δv scalar, burn UT, burn
vector).

### Hyperbolic excess velocity

$$
\mathbf{v_\infty} = \mathbf{v_{transfer}} - \mathbf{v_{body}}
$$

$$
v_{hyp,peri} = \sqrt{|\mathbf{v_\infty}|^2 + \frac{2\mu}{r_{peri}} - \frac{2\mu}{r_{SOI}}}
$$

$v_{hyp,peri}$ is the speed at periapsis of the escape/capture hyperbola.

### Parking orbit periapsis speed

For a parking orbit with eccentricity `e` and periapsis radius $r_{peri}$:

$$
v_{park} = \sqrt{\frac{\mu (1 + e)}{r_{peri}}}
$$

When $e = 0$ this reduces to circular orbital speed $\sqrt{\mu / r_{peri}}$.

### Burn Δv — no inclination change

$$
\Delta v_{ej} = v_{hyp,peri} - v_{park} \qquad \Delta v_{ins} = v_{park} - v_{hyp,peri}
$$

### Inclination correction

The out-of-ecliptic component of **v∞** gives a declination angle:

$$
\alpha = \arcsin\!\left(\frac{v_{\infty,z}}{|\mathbf{v_\infty}|}\right)
$$

Required inclination change over and above the parking orbit inclination $i_{park}$:

$$
\Delta I = \max\!\left(0,\ |\alpha| - i_{park}\right)
$$

Combined Δv at periapsis (simultaneous speed-change and plane-change):

$$
\Delta v_{pro} = v_{hyp,peri}\cos\Delta I - v_{park}, \quad
\Delta v_{nor} = v_{hyp,peri}\sin\Delta I \cdot \text{sign}(\alpha)
$$

$$
\Delta v = \sqrt{\Delta v_{pro}^2 + \Delta v_{nor}^2}
$$

Insertion uses the same formula with signs reversed (deceleration). When $i_{dest} = 0$,
insertion is treated as pure deceleration (no plane-change term) — compatible with the
pre-1c behaviour.

### Precise burn UT

The burn occurs at periapsis of the hyperbolic flyby arc. Transit time from SOI crossing
to periapsis:

$$
|a_{hyp}| = \frac{\mu}{|\mathbf{v_\infty}|^2}, \quad
e_{hyp} = 1 + \frac{r_{peri}}{|a_{hyp}|}
$$

$$
F_{SOI} = \text{acosh}\!\left(\frac{r_{SOI}/|a_{hyp}| + 1}{e_{hyp}}\right), \quad
t_{transit} = \sqrt{\frac{|a_{hyp}|^3}{\mu}} \cdot \left(e_{hyp}\sinh F_{SOI} - F_{SOI}\right)
$$

- **Ejection:** $burnUT = departureUT - t_{transit}$ (burn before SOI exit)
- **Insertion:** $burnUT = arrivalUT + t_{transit}$ (burn after SOI entry)

### Burn vector components

All burns have `Radial = 0`. The prograde and normal components are as above. These values
are formatted by `PreciseManeuverFormatter` (Increment 2) into the Precise Maneuver
copy-paste block.

---

## Ejection angle and inclination (`ComputeEjectionDetails`)

**Source file:** `ManeuverComputer.cs`  
**Algorithm reference:** LWP `src/orbit.coffee` — `ejectionDetails`, `ejectionAngleFromPeriapsis`,
`ejectionPeriapsisDirection`, `ejectionAngleToPrograde`

Computed for ejection burns only; returns `null` for degenerate cases. Results are stored in
`EjectionDetails` (record with `AngleDeg`, `InclinationDeg`).

### 1 — Hyperbola geometry

Speed at periapsis of the escape hyperbola (same as $v_{hyp,peri}$ above):

$$
v_1 = \sqrt{v_\infty^2 + \frac{2\mu}{r_{peri}} - \frac{2\mu}{r_{SOI}}}
$$

Hyperbola eccentricity and semi-latus rectum:

$$
e = \frac{r_{peri} v_1^2}{\mu} - 1, \qquad p = r_{peri}(1 + e)
$$

True anomaly at SOI exit:

$$
\theta = \arccos\!\left(\frac{p - r_{SOI}}{e \cdot r_{SOI}}\right)
$$

Zenith angle correction (LWP eq 4.23):

$$
\theta \mathrel{+}= \arcsin\!\left(\frac{v_1 \, r_{peri}}{v_\infty \, r_{SOI}}\right)
$$

### 2 — Periapsis direction

The periapsis direction is the unit vector $\hat{p} = (p_x, p_y, 0)$ satisfying:

$$
\hat{p} \cdot \hat{e}_{jDir} = \cos\theta, \quad |\hat{p}| = 1, \quad p_z = 0
$$

where $\hat{e}_{jDir} = \mathbf{v_\infty}/v_\infty$. A valid (equatorial) solution exists
only when $|\sin\theta| \geq |e_{jDir,z}|$.

Substituting $g = -e_{jDir,x}/e_{jDir,y}$ gives a quadratic in $p_x$ (Numerical Recipes
eq. 5.6.4 for numerical stability). Of the two solutions, the one giving a CCW orbit
($(\hat{p} \times \hat{e}_{jDir})_z > 0$) is chosen.

### 3 — Ejection inclination

Orbital-plane normal $\hat{n} = \text{normalize}(\hat{p} \times \hat{e}_{jDir})$:

$$
i_{ej} = \arccos(n_z) \cdot \text{sign}(\pi - \theta) \cdot \text{sign}(e_{jDir,z})
$$

Positive = north-tilted, negative = south-tilted. Units: degrees in `EjectionDetails.InclinationDeg`.

### 4 — Ejection angle to prograde

Project body's heliocentric velocity onto the XY plane to get $\hat{p}_{prog}$:

$$
\phi = \arccos(\hat{p} \cdot \hat{p}_{prog})
$$

Adjust to $[0, 2\pi)$ based on $(\hat{p} \times \hat{p}_{prog})_z$, then map to signed convention:

$$
\phi_{deg} > 180° \Rightarrow \phi_{deg} = 180° - \phi_{deg} \quad (\text{retrograde side becomes negative})
$$

Positive = to prograde, negative = to retrograde. Matches the Precise Maneuver mod format string
`{0:0.00° to prograde;0.00° to retrograde}`.

---

## Transfer types and plane change

`TransferComputer` accepts a `TransferType` enum (`Ballistic`, `MidCoursePlaneChange`, `Optimal`).

### Ballistic

Multi-rev Lambert solve (prograde + retrograde, all revolutions). Best result = lowest total Δv.
The `TransferResult` output includes:

| Field | Description |
|-------|-------------|
| `PhaseAngleDeg` | Phase angle at departure (negative for inner → outer is positive here; outer → inner has 2π subtracted to be negative, matching LWP) |
| `TransferAngleDeg` | CCW heliocentric transfer angle (degrees) |
| `TransferPeriapsis/Apoapsis` | Transfer ellipse periapsis/apoapsis distance from Kerbol (m) |
| `InsertionInclinationDeg` | Angle between transfer velocity and destination velocity vectors at arrival |

### Mid-course plane change (`PlaneChangeComputer`)

**Source:** `src/Core/Transfer/PlaneChangeComputer.cs`  
**Algorithm reference:** LWP `src/orbit.coffee` — `Orbit.transfer()` with type `optimal` mid-course plane change.

The transfer is split at a point `θ` (angle of the plane-change maneuver measured from the destination's
current position along its orbit). At that point a burn rotates the transfer orbit into the destination
body's plane.

1. **Relative inclination:** $\Delta i = $ angle between transfer orbit normal and destination orbit normal
2. **Optimal θ:** golden-section search over $[\pi/2, 2\pi]$ minimising burn magnitude:

$$
\Delta v_{pc} = 2 \cdot v(\nu_{pc}) \cdot \left|\sin\!\frac{\Delta i}{2}\right|
$$

where $v(\nu_{pc})$ is the transfer orbit speed at the true anomaly where the burn occurs.

3. **Burn components** (from LWP `porkchop.coffee`):

$$
\Delta v_{pro} = -\Delta v_{pc} \cdot \left|\sin\!\frac{\Delta i}{2}\right|, \quad
\Delta v_{nor} = \Delta v_{pc} \cdot \text{sign}(\Delta i) \cdot \cos\!\frac{\Delta i}{2}
$$

4. **Geometry:** `r2` is rotated into the departure plane by the conjugate rotation (Rodrigues
   with axis = intersection of the two planes, angle = −Δi), then Lambert is solved for the
   rotated r2. The insertion velocity is rotated back to the destination plane.

Returns `null` (falls back to ballistic) when: planes are coplanar ($|\Delta i| < 10^{-6}$),
transfer angle ≤ 90°, or Lambert fails.

### Optimal

Runs both ballistic and mid-course plane change. Returns whichever has lower total Δv.

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
