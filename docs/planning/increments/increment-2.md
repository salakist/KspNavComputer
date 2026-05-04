# Increment 2 — Precise Maneuver export

## Plan

> Written before delivery. Not edited afterward.

Format each burn as the Precise Maneuver plaintext block (UT, prograde/normal/radial Δv,
total Δv in KSP calendar format). One-click copy-to-clipboard per burn.

Format example (without ejection) :
````
Precise Maneuver Information
Depart at:      2y, 265d, 0h, 47m, 43s
       UT:      24133663
Prograde Δv:    741.1 m/s
Normal Δv:      0.0 m/s
Radial Δv:      0.0 m/s
Total Δv:       741 m/s
````

Format example (with ejection — clipboard text is identical; ejection lines are
displayed in the UI table but **not** included in the clipboard block) :
````
Precise Maneuver Information
Depart at:      2y, 174d, 2h, 35m, 31s
       UT:      22170932
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

- **`PreciseManeuverFormatterTests.cs`** — 8 tests: KSP calendar formatting (both year and day
  0-indexed), UT formatting, ejection/insertion blocks, ejection data absent from clipboard
  text (3 tests assert `DoesNotContain` / 7-line count), line ordering.
- **`ReferenceTransferTests.cs`** — extended to assert `EjectionAngleDeg` and
  `EjectionInclinationDeg` within ±0.5° of LWP reference values for all 10 one-way cases.
- **`ReferenceRoundTripTests.cs`** — extended similarly for both legs of 2 round-trip cases.
- **`scripts/generate-reference-transfers.js`** and **`generate-round-trip-references.js`** —
  extended to emit `ejectionAngleDeg` / `ejectionInclinationDeg` fields (angle mapping from
  LWP's [0, 2π) radians to signed degrees is done in the script to match C# convention).

### Scope note

Ejection angle and inclination are computed pre-flight from hyperbola geometry (LWP algorithm)
and displayed in the UI table. They are intentionally **excluded** from the clipboard text
because the Precise Maneuver mod's `NodeManager.ChangeNodeFromString` treats an
`Ejection Angle:` line as a node repositioning command — it moves the node to match that
angle on the vessel's live orbit — which shifts the burn UT and breaks the encounter.

KSP day display convention: days are 0-indexed (Day 0 = first day of year). The formatter
uses `c.Day - 1` so the output matches the PM mod's display (e.g. KSP Year 3 Day 266
→ `"2y, 265d, ..."`).
