# Increment 2 — Precise Maneuver export

## Plan

> Written before delivery. Not edited afterward.

Format each burn as the Precise Maneuver plaintext block (UT, prograde/normal/radial Δv,
total Δv in KSP calendar format). One-click copy-to-clipboard per burn.

Format example (without ejection) :
````
Precise Maneuver Information
Depart at:      2y, 266d, 0h, 47m, 43s
       UT:      24151663
Prograde Δv:    741.1 m/s
Normal Δv:      0.0 m/s
Radial Δv:      0.0 m/s
Total Δv:       741 m/s
````

Format example (with ejection) :
````
Precise Maneuver Information
Depart at:      2y, 174d, 2h, 35m, 31s
       UT:      22170932
Ejection Angle: 113.73° to retrograde
Ejection Inc.:  0.02°
Prograde Δv:    1161.4 m/s
Normal Δv:      0.0 m/s
Radial Δv:      0.0 m/s
Total Δv:       1161 m/s
````

---

## Inherited from prior increments

**From 1c**: `Burn.BurnUT`, `Burn.DeltaV`, and `Burn.Vector` (prograde/normal/radial
components) are already computed by `ManeuverCalculator` and exposed on both the
`TransferResult` and the API `BurnDto`. Increment 2 only needs to format these values
into the Precise Maneuver plaintext block and add the copy-to-clipboard UI.
No Core changes are expected.

---

## Actuals

Delivered scope matches the plan plus an unplanned-but-necessary extension: ejection
angle and inclination (the lines the PM mod includes when the ejection orbit is known).

### Core additions

- **`EjectionDetails.cs`** — new `record EjectionDetails(double AngleDeg, double InclinationDeg)`.
  `AngleDeg` is signed: positive = to prograde, negative = to retrograde (matches PM mod convention).
- **`PreciseManeuverFormatter.cs`** — static `Format(Burn, CelestialBody)` method.
  Returns the full PM plaintext block. Uses `CultureInfo.InvariantCulture` throughout
  (required: developer machine uses French locale).
- **`Burn.cs`** — optional `EjectionDetails? Ejection = null` parameter added (backward compatible).
- **`ManeuverCalculator.cs`** — new private `ComputeEjectionDetails` method (~90 lines).
  Ports the LWP `ejectionDetails` / `ejectionAngleFromPeriapsis` / `ejectionPeriapsisDirection` /
  `ejectionAngleToPrograde` algorithm from `alexmoon/ksp` `src/orbit.coffee` into C#.
  Returns `null` for degenerate cases (zero v∞, no equatorial periapsis, numerical failure).

### API additions

- `EjectionDetailsDto` record added to `TransferDtos.cs`.
- `BurnDto` gains `PreciseManeuverText` (string) and `EjectionDetails` (`EjectionDetailsDto?`).
- `TransferMapper.ToBurnDto` calls the formatter and maps ejection geometry.

### Frontend additions

- `EjectionDetailsDto` TypeScript interface + `ejectionDetails: EjectionDetailsDto | null` on `BurnDto`.
- `formatEjectionAngle(angleDeg)` helper formats to e.g. `"113.73° to retrograde"`.
- `TransferResultPanel` shows "Ejection angle" and "Ejection inc." rows after the Total Δv row
  for the ejection burn, when data is available.
- Copy-to-clipboard buttons (per burn) copy `preciseManeuverText` from the API response.

### Tests

- **`PreciseManeuverFormatterTests.cs`** — 9 tests: KSP calendar formatting, UT formatting,
  ejection/insertion blocks, ejection angle lines (prograde/retrograde), line ordering.
- **`ReferenceTransferTests.cs`** — extended to assert `EjectionAngleDeg` and
  `EjectionInclinationDeg` within ±0.5° of LWP reference values for all 10 one-way cases.
- **`ReferenceRoundTripTests.cs`** — extended similarly for both legs of 2 round-trip cases.
- **`scripts/generate-reference-transfers.js`** and **`generate-round-trip-references.js`** —
  extended to emit `ejectionAngleDeg` / `ejectionInclinationDeg` fields (angle mapping from
  LWP's [0, 2π) radians to signed degrees is done in the script to match C# convention).

### Scope note

The Precise Maneuver mod's in-game clipboard format also includes `Ejection Angle:` and
`Ejection Inc.:` lines. The PM mod computes these at runtime from the actual vessel position;
we compute them pre-flight from hyperbola geometry — the same algorithm as LWP.
The LWP reference data confirms our values agree within ±0.5°.
