# Kepler Propagation

**Source:** `src/Core/Mechanics/KeplerSolver.cs`

---

## Problem statement

Given an `OrbitalElements` record (semi-major axis, eccentricity, inclination, RAAN,
argument of periapsis, mean anomaly at epoch, epoch) and a Universal Time `ut`, compute
the Cartesian position and velocity vectors in the central-body inertial frame.

---

## Algorithm

`KeplerSolver.StateAt(orbit, μ, ut)`:

1. **Mean anomaly:** $M = M_0 + n \cdot (t - t_0)$, where $n = \sqrt{\mu / a^3}$
2. **Eccentric anomaly:** Newton-Raphson on Kepler's equation $M = E - e \sin E$,
   tolerance $10^{-10}$, max 100 iterations
3. **True anomaly:** $\nu = \text{atan2}\!\left(\sqrt{1-e^2}\sin E,\ \cos E - e\right)$
4. **Perifocal frame vectors:** position and velocity in the frame where $\hat{x}$ points
   toward periapsis and $\hat{y}$ is 90° ahead in the orbit direction
5. **Rotation to inertial frame:** standard perifocal-to-inertial rotation using
   $(ω, i, Ω)$

---

## Perifocal frame

Semi-latus rectum $p = a(1 - e^2)$, orbital radius:

$$
r = \frac{p}{1 + e\cos\nu}
$$

$$
\mathbf{r}_{pf} = r\,(\cos\nu,\ \sin\nu,\ 0)
$$

Specific angular momentum $h = \sqrt{\mu\, p}$:

$$
\mathbf{v}_{pf} = \frac{\mu}{h}\,(-\sin\nu,\ e + \cos\nu,\ 0)
$$

---

## Perifocal → inertial rotation

Standard rotation $R_z(-\Omega) \cdot R_x(-i) \cdot R_z(-\omega)$ applied to both
$\mathbf{r}_{pf}$ and $\mathbf{v}_{pf}$:

$$
\mathbf{r} = \mathbf{R}\,\mathbf{r}_{pf}, \qquad \mathbf{v} = \mathbf{R}\,\mathbf{v}_{pf}
$$

$x$ points toward the vernal equinox; $z$ toward celestial north (ecliptic normal for
the Kerbol reference frame).

---

## Helper methods

`SolveEccentricAnomaly(M, e)` — standalone Newton-Raphson, used independently by
`KeplerSolverTests` to validate round-trip accuracy.

`TrueAnomalyFromEccentric(E, e)` — standalone conversion, used to build test state vectors.

`PositionInOrbitalPlane` / `VelocityInOrbitalPlane` — perifocal-only variants, used when
rotation is not needed.
