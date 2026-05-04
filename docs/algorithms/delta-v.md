# Transfer Δv Pipeline

**Sources:** `src/Core/Transfer/TransferComputer.cs`, `src/Core/Transfer/ManeuverCalculator.cs`,
`src/Core/Mechanics/KeplerSolver.cs`

---

## Overview

Three steps convert `(departureUT, timeOfFlight, originParkingOrbit, destinationParkingOrbit)`
into ejection and insertion `Burn` records:

1. **Kepler propagation** — body state vectors at departure and arrival UT
2. **Lambert solver** — heliocentric transfer velocities at those positions
3. **Maneuver calculation** — convert hyperbolic excess velocity to a periapsis Δv and
   precise burn UT

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

`ManeuverCalculator.Compute` takes the heliocentric transfer velocity **v_transfer** and the
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
are consumed directly by Increment 2 (Precise Maneuver copy-paste export).
