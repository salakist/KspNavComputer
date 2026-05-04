# Porkchop Plot — Transfer Types and Grid

**Sources:** `src/Core/Porkchop/PorkchopComputer.cs`, `src/Core/Transfer/PlaneChangeComputer.cs`,
`src/Core/Transfer/TransferComputer.cs`

---

## Transfer types

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

## Porkchop grid (`PorkchopComputer`)

**Source:** `src/Core/Porkchop/PorkchopComputer.cs`

Computes a `rows × cols` grid of total Δv values over a departure-date × TOF window.

### TOF auto-range (`AutoTofRange`)

Based on LWP's range selection. Let $a_{dest}$ be destination semi-major axis, $T_{dest}$ the orbital period:

$$
a_{transfer} = \frac{a_{origin} + a_{dest}}{2}, \quad
t_{Hohmann} = \pi\sqrt{\frac{a_{transfer}^3}{\mu}}
$$

$$
t_{min} = \max\!\left(t_{Hohmann} - T_{dest},\ \frac{t_{Hohmann}}{2}\right)
$$

$$
t_{max} = t_{min} + \min(2 T_{dest},\ t_{Hohmann})
$$

### Grid indexing

- Row 0 → `minTof`, Row `rows−1` → `maxTof`
- Col 0 → `earliestDeparture`, Col `cols−1` → `latestDeparture`
- Failed cells (Lambert throws or returns empty) → `NaN`

### Log statistics

For colour normalisation in the porkchop plot, the result includes:

- `MeanLogDeltaV`, `StdLogDeltaV` — mean and standard deviation of $\ln(\Delta v)$ over valid cells
- Colour scale is clamped to $[\ln(\min \Delta v),\ \text{mean} + 2\sigma]$
