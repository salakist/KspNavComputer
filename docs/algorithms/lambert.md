# Lambert Solver

**Source:** `src/Core/Mechanics/LambertSolver.cs`
**Reference:** Sun — *"On the Minimum Time Trajectory and Multiple Solutions of Lambert's Problem"* (AAS 79-164, 1979); implementation cross-checked against alexmoon's Launch Window Planner (LWP).

---

## Problem statement

Given two position vectors **r₁** and **r₂** in a central gravity field (gravitational
parameter μ), and a time of flight Δt, find the departure velocity **v₁** and arrival
velocity **v₂** of the connecting conic arc.

---

## Normalised variables

| Symbol | Definition | Notes |
|--------|-----------|-------|
| `c` | `|r₂ − r₁|` | chord length |
| `m` | `|r₁| + |r₂| + c` | semi-perimeter sum |
| `n` | `|r₁| + |r₂| − c` | semi-perimeter difference |
| `angleParameter` | `±√(n/m)` | negative when transfer angle > π |
| `normalizedTime` | `4·Δt·√(μ/m³)` | dimensionless time of flight |

---

## Arc selection

`angleParameter` encodes both the transfer geometry and the prograde/retrograde convention,
matching LWP:

- **Short-way (prograde):** transfer angle < 180° → positive `angleParameter`
- **Long-way (retrograde):** transfer angle > 180° → negative `angleParameter`

The sign propagates through the `Fy` function, which maps the universal variable `x` to
`y`. `TransferComputer` calls `SolveAllRevolutions` twice (prograde and retrograde) and
keeps the pair with the lowest total Δv.

---

## Universal variable x

The solver iterates on a dimensionless variable `x`:

| Range | Regime |
|-------|--------|
| `x ∈ (−1, 1)` | Elliptic |
| `x = 1` | Parabolic (handled as a special case) |
| `x > 1` | Hyperbolic (separate `FdtH` branch) |

The elliptic time-of-flight residual for N complete revolutions is:

$$
F(x, N) = \frac{\text{acot}(x/g) - \arctan(h/y) - xg + yh + N\pi}{g^3} - \tau = 0
$$

where $g = \sqrt{1 - x^2}$, $h = \sqrt{1 - y^2}$, and $\tau$ is `normalizedTime`.

Root-finding uses **Brent's method** (500-iteration cap):
- N = 0: tolerance `1e-6`
- N ≥ 1: tolerance `1e-4`; two roots exist per N, both evaluated as separate candidates.

---

## Multi-revolution solutions

The maximum revolution count is capped at `min(maxRevs, floor(normalizedTime / π))`.
`TransferComputer` calls with `maxRevs = 10`.

For each N ≥ 1, the minimum-time solution `xMT` (where $\partial\tau/\partial x = 0$) is
found first by solving $\Phi(x) = \Phi(y) + N\pi = 0$. If the requested TOF falls below
this minimum, no solution exists for that N and iteration stops. Otherwise Brent's method
is applied to both `(0, xMT)` and `(xMT, 1)` intervals.

---

## Velocity reconstruction

From the converged root `(x, y)`:

$$
v_c = \sqrt{\mu} \cdot \left(\frac{y}{\sqrt{n}} + \frac{x}{\sqrt{m}}\right), \quad
v_r = \sqrt{\mu} \cdot \left(\frac{y}{\sqrt{n}} - \frac{x}{\sqrt{m}}\right)
$$

$$
\mathbf{v_1} = \hat{\Delta r} \cdot v_c + \hat{r_1} \cdot v_r, \quad
\mathbf{v_2} = \hat{\Delta r} \cdot v_c - \hat{r_2} \cdot v_r
$$

where $\hat{\Delta r} = (\mathbf{r_2} - \mathbf{r_1}) / c$.

---

## Degenerate cases

- **180° (anti-podal) geometry:** the orbit plane is undefined; the Lagrange `g` coefficient
  is zero for any exactly-opposite position vectors. Do not pass exactly-opposite **r₁** and
  **r₂**. Tests use a 90° quarter-orbit geometry to avoid this.
- **Parabolic TOF:** handled via `RelErr(normalizedTime, parabolicNormalizedTime) < 1e-6`
  special-case branch.
- **Near-zero chord / very short TOF:** not separately guarded; minimum physical transfer
  times in the Kerbol system are well above any degenerate threshold.
