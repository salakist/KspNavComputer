# Porkchop Plot — Grid

**Source:** `src/Core/Porkchop/PorkchopComputer.cs`

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
