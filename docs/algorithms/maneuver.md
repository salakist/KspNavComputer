# Maneuver Calculation

**Source:** `src/Core/Maneuver/ManeuverComputer.cs`  
**Algorithm reference:** LWP `src/orbit.coffee` — `ejectionDetails`, `ejectionAngleFromPeriapsis`,
`ejectionPeriapsisDirection`, `ejectionAngleToPrograde`

---

## Overview

`ManeuverComputer.Compute` takes a heliocentric transfer velocity **v_transfer** and the
body's heliocentric velocity **v_body** and produces a `Burn` (Δv scalar, burn UT, burn
vector).

---

## Hyperbolic excess velocity

$$
\mathbf{v_\infty} = \mathbf{v_{transfer}} - \mathbf{v_{body}}
$$

$$
v_{hyp,peri} = \sqrt{|\mathbf{v_\infty}|^2 + \frac{2\mu}{r_{peri}} - \frac{2\mu}{r_{SOI}}}
$$

$v_{hyp,peri}$ is the speed at periapsis of the escape/capture hyperbola.

---

## Parking orbit periapsis speed

For a parking orbit with eccentricity `e` and periapsis radius $r_{peri}$:

$$
v_{park} = \sqrt{\frac{\mu (1 + e)}{r_{peri}}}
$$

When $e = 0$ this reduces to circular orbital speed $\sqrt{\mu / r_{peri}}$.

---

## Burn Δv — no inclination change

$$
\Delta v_{ej} = v_{hyp,peri} - v_{park} \qquad \Delta v_{ins} = v_{park} - v_{hyp,peri}
$$

---

## Inclination correction

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

---

## Precise burn UT

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

---

## Burn vector components

All burns have `Radial = 0`. The prograde and normal components are as above. These values
are formatted by `PreciseManeuverFormatter` into the Precise Maneuver copy-paste block.

---

## Ejection angle and inclination (`ComputeEjectionDetails`)

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
