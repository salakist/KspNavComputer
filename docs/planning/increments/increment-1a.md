# Increment 1a — One-way transfer, circular equatorial parking orbits

## Plan

> Written before delivery. Not edited afterward.

Scaffold the full repository (Core library, ASP.NET Core API, React/TS frontend, xUnit
test project). Implement celestial body data model (17 stock bodies), KSP time utilities,
Kepler solver, Lambert solver, and transfer computation for one-way circular equatorial
transfers. Expose via a minimal API endpoint and a basic React UI showing ejection Δv,
insertion Δv, total Δv, and arrival date.

QA: match a Kerbin→Duna result from alexmoon's Launch Window Planner for the same inputs.

---

## Actuals

> Post-delivery notes. Not edited after delivery.

**Lambert solver scope**: the planning doc stated "Lambert solver: 0-revolution elliptical
solutions only." What was actually implemented is the full Sun (1979) / Gooding
multi-revolution solver (N = 0..10), evaluating both prograde and retrograde arcs and
selecting the minimum total Δv. This matches alexmoon's LWP approach directly and costs
negligible extra CPU. The "0-rev only" note was a conservative planning assumption, not a
design constraint; multi-rev was included from the start.

**Reference validation data pipeline**: not in the original plan. A Node.js generator
script (`scripts/generate-reference-transfers.js`) calls the LWP JavaScript source
directly to produce `tests/Core/Data/reference-transfers.json`. The xUnit test
`ReferenceTransferTests` reads this JSON and cross-validates every case to within ±1 %.
This became the primary QA mechanism for 1a and was reused in 1b and 1c.

**Mapper layer in the API**: the plan described a "minimal API endpoint" without
prescribing internal structure. A `Mappers/` folder with `TransferMapper.cs` was
introduced to keep endpoint handlers thin and mapping logic testable. Carried forward
through 1b and 1c.

**`run-checks.ps1` quality gate**: root AGENTS.md states this was "created end of
Increment 1" but the file does not exist in the repository. It was referenced in
documentation but never delivered. Known gap.

**Impact on later increments**: recorded in each impacted increment's
"Inherited from prior increments" section.
