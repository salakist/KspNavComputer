# Increment 1b — Round trip

## Plan

> Written before delivery. Not edited afterward.

Add return-leg computation. UI shows both legs and total mission Δv.

QA: verify return window is physically coherent (dates, bodies, Δv).

---

## Actuals

> Post-delivery notes. Not edited after delivery.

**Parking orbit reuse across legs**: the plan was silent on how the return leg parking
orbit is defined. In delivery, both legs reuse the same two `ParkingOrbit` objects: the
origin orbit is used as the departure orbit outbound and the capture orbit on return; the
destination orbit is used as the capture orbit outbound and the departure orbit on return.
This is physically reasonable and simplifies the input model — the user specifies one
orbit per body rather than four per trip.

**Reference validation pipeline extended**: a second generator script
(`scripts/generate-round-trip-references.js`) produces
`tests/Core/Data/reference-round-trips.json` using the same LWP-call approach as 1a.
`ReferenceRoundTripTests` validates each leg to within ±1 %. The test data is the sum
of two independent LWP one-way calls; stay duration only shifts the return departure date.

**No structural surprises**: `TransferComputer.ComputeRoundTrip` is a thin wrapper over
two `Compute` calls with the legs swapped. No new domain types introduced in 1b.

**Impact on later increments**: recorded in each impacted increment's
"Inherited from prior increments" section.

---

## Inherited from prior increments

**From 1a**: multi-revolution Lambert solver already in place. The reference-data
validation pattern (generator script → JSON → xUnit theory) is established and reused
here without modification.
