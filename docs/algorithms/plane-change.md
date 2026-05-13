# Mid-Course Plane Change

**Source:** `src/Core/Transfer/PlaneChangeComputer.cs`  
**Algorithm reference:** LWP `src/orbit.coffee` — `Orbit.transfer()` with type `optimalPlaneChange`

---

## Overview

`PlaneChangeComputer.Compute` splits the transfer at an intermediate point where a burn
rotates the transfer orbit into the destination body's plane, potentially reducing total
Δv compared to a pure ballistic arc.

Returns `null` (caller falls back to ballistic) when:
- Transfer angle ≤ 90°, or
- Planes are coplanar ($|\Delta i| < 10^{-6}$), or
- Lambert solver fails for the rotated geometry.

---

## 1 — Relative inclination

Origin orbital-plane normal $\hat{n}_0 = \text{normalize}(\mathbf{r_1} \times \mathbf{v_1})$:

$$
\Delta i = \arcsin\!\left(\frac{\mathbf{r_2} \cdot \hat{n}_0}{|\mathbf{r_2}|}\right)
$$

---

## 2 — Optimal θ (golden-section search)

`θ` is the angle of the plane-change burn measured back from the destination position along
the transfer orbit. Search bounds depend on transfer direction:

- Higher destination orbit: $\theta \in [0,\ \pi/2]$
- Lower destination orbit: $\theta \in [\pi/2,\ \pi]$

At each `θ`, the effective plane-change angle is:

$$
\varphi(\theta) = \arctan\!\left(\frac{\tan\Delta i}{\sin\theta}\right)
$$

Objective function minimised over `θ`:

$$
\Delta v_{pc} = 2 \cdot v(\nu_{arr} - \theta) \cdot \left|\sin\!\frac{\varphi}{2}\right|
$$

where $v(\nu_{arr} - \theta)$ is the transfer orbit speed at the true anomaly of the burn.

The search runs twice: once on an approximate orbit (Lambert solve with `r2` rotated into
the origin plane by $-\Delta i$), and once on a refined orbit built at the first-pass `θ*`,
to compensate for the orbit shape changing as `θ` varies.

---

## 3 — Burn components

From LWP `porkchop.coffee`. Components use $\varphi$, not $\Delta i$:

$$
\Delta v_{pro} = -\Delta v_{pc} \cdot \left|\sin\!\frac{\varphi}{2}\right|, \quad
\Delta v_{nor} = \Delta v_{pc} \cdot \text{sign}(\varphi) \cdot \cos\!\frac{\varphi}{2}
$$

---

## 4 — Geometry

`r2` is rotated into the departure plane by the conjugate Rodrigues rotation (axis =
`RotateByAxisAngle(project(r2, n0), n0, −θ)`, angle = $-\varphi$), then Lambert is solved
for the rotated geometry (0-revolution prograde only, matching LWP). The insertion velocity
is rotated back to the real frame by the forward rotation ($+\varphi$).
