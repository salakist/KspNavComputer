/**
 * Generates round-trip reference Δv values using alexmoon's Launch Window Planner
 * JavaScript source (C:\Users\simon\Repos\ksp\javascripts\).
 *
 * Each case is computed as two independent one-way transfers:
 *   1. Outbound: origin → destination at departureUT
 *   2. Return:   destination → origin at (departureUT + outboundTOF + stayDuration)
 *
 * Run from repo root:
 *   node scripts/generate-round-trip-references.js
 *
 * Outputs JSON to stdout. Pipe to tests/Core/Data/reference-round-trips.json.
 */

'use strict';

const path = require('path');
const fs   = require('fs');

// ---- Minimal DOM/module shim so LWP's IIFE-wrapped files load in Node ----
global.window = global;

const vm = require('vm');

// Load LWP_DIR from .env or environment
if (!process.env.LWP_DIR) {
    const envFile = path.resolve(__dirname, '..', '.env');
    if (fs.existsSync(envFile)) {
        for (const line of fs.readFileSync(envFile, 'utf8').split(/\r?\n/)) {
            const m = line.match(/^([^#=\s][^=]*)=(.*)$/);
            if (m) process.env[m[1].trim()] = m[2].trim();
        }
    }
}
const lwpDir = process.env.LWP_DIR
    ? path.resolve(process.env.LWP_DIR)
    : (() => { throw new Error('LWP_DIR is not set. Add it to .env or set it in the environment.'); })();

function loadLwp(file) {
    const src = fs.readFileSync(path.join(lwpDir, file), 'utf8');
    vm.runInThisContext(src, { filename: file });
}

loadLwp('numeric-1.2.6.min.js');
loadLwp('quaternion.js');
loadLwp('roots.js');
loadLwp('lambert.js');
loadLwp('orbit.js');
loadLwp('celestialbodies.js');

// ---- Helper: compute one-way transfer via LWP ----------------------------
function oneWay(originName, destName, departureUT, tof, r0, r1) {
    const origin = CelestialBody[originName];
    const dest   = CelestialBody[destName];
    const v0     = origin.circularOrbitVelocity(r0);
    const v1     = dest.circularOrbitVelocity(r1);
    const t = Orbit.transfer(
        'ballistic',
        origin, dest,
        departureUT, tof,
        v0, v1,
        null, null, null, null, null
    );

    // Apply SOI-exit correction (matches our C# RefineEjection)
    const refined = Orbit.refineTransfer(t, 'ballistic', origin, dest, departureUT, tof, v0, v1);

    // Convert LWP ejection angle [0, 2π) rad → signed degrees matching C# convention:
    //   (0°, 180°] → positive (prograde side)
    //   (180°, 360°) → negative (retrograde side), via angleDeg = 180 − angleDeg
    let ejectionAngleDeg = null;
    let ejectionInclinationDeg = null;
    if (refined.ejectionAngle != null && !isNaN(refined.ejectionAngle)) {
        let angleDeg = refined.ejectionAngle * 180 / Math.PI;
        if (angleDeg > 180) angleDeg = 180 - angleDeg;
        ejectionAngleDeg = angleDeg;
    }
    if (refined.ejectionInclination != null && !isNaN(refined.ejectionInclination)) {
        ejectionInclinationDeg = refined.ejectionInclination * 180 / Math.PI;
    }

    return {
        ejectionDeltaV:  refined.ejectionDeltaV,
        insertionDeltaV: refined.insertionDeltaV,
        totalDeltaV:     refined.deltaV,
        ejectionAngleDeg,
        ejectionInclinationDeg,
    };
}

// ---- Round-trip cases ------------------------------------------------------
// Parameters chosen so both legs fall on physically reasonable (non-degenerate)
// transfer geometry.  Outbound windows match the one-way reference suite where
// possible so the outbound leg values are already cross-validated.
//
// stayDuration and returnTimeOfFlight are chosen so the return departure falls
// near a plausible Duna/Eve → Kerbin transfer window (not necessarily optimal,
// but numerically well-behaved).

const cases = [
    // Kerbin → Duna → Kerbin
    // Outbound matches 1a reference entry (5 091 600 / 5 184 000).
    // Stay ≈ 90 Kerbin days; return ≈ 250 Kerbin days.
    {
        label:           'Kerbin→Duna→Kerbin',
        origin:          'Kerbin',
        destination:     'Duna',
        departureUT:      5_091_600,
        outboundTof:      5_184_000,   // 240 Kerbin days
        stayDuration:     1_944_000,   //  90 Kerbin days
        returnTof:        5_400_000,   // 250 Kerbin days
        r0:                 100_000,
        r1:                 100_000,
    },
    // Kerbin → Eve → Kerbin
    // Outbound matches 1a reference entry (3 000 000 / 4 320 000).
    // Stay ≈ 120 Kerbin days; return ≈ 250 Kerbin days.
    {
        label:           'Kerbin→Eve→Kerbin',
        origin:          'Kerbin',
        destination:     'Eve',
        departureUT:      3_000_000,
        outboundTof:      4_320_000,   // 200 Kerbin days
        stayDuration:     2_592_000,   // 120 Kerbin days
        returnTof:        5_400_000,   // 250 Kerbin days
        r0:                 100_000,
        r1:                 100_000,
    },
];

// ---- Run -------------------------------------------------------------------
const results = cases.map(c => {
    const returnDepartureUT = c.departureUT + c.outboundTof + c.stayDuration;

    const outbound = oneWay(c.origin,      c.destination, c.departureUT,    c.outboundTof, c.r0, c.r1);
    const ret      = oneWay(c.destination, c.origin,      returnDepartureUT, c.returnTof,  c.r1, c.r0);

    return {
        source:                    'alexmoon/ksp LWP (javascripts/, ballistic, refineTransfer applied)',
        description:               c.label,
        origin:                    c.origin,
        destination:               c.destination,
        departureUT:               c.departureUT,
        outboundTimeOfFlight:      c.outboundTof,
        stayDuration:              c.stayDuration,
        returnTimeOfFlight:        c.returnTof,
        parkingOrbitAltitude:      c.r0,
        destinationOrbitAltitude:  c.r1,
        outboundEjectionDeltaV:        outbound.ejectionDeltaV,
        outboundInsertionDeltaV:       outbound.insertionDeltaV,
        outboundTotalDeltaV:           outbound.totalDeltaV,
        outboundEjectionAngleDeg:      outbound.ejectionAngleDeg,
        outboundEjectionInclinationDeg: outbound.ejectionInclinationDeg,
        returnEjectionDeltaV:          ret.ejectionDeltaV,
        returnInsertionDeltaV:         ret.insertionDeltaV,
        returnTotalDeltaV:             ret.totalDeltaV,
        returnEjectionAngleDeg:        ret.ejectionAngleDeg,
        returnEjectionInclinationDeg:  ret.ejectionInclinationDeg,
        totalDeltaV:                   outbound.totalDeltaV + ret.totalDeltaV,
    };
});

console.log(JSON.stringify(results, null, 2));
