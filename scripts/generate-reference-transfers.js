/**
 * Generates reference transfer Δv values using alexmoon's Launch Window Planner
 * JavaScript source (C:\Users\simon\Repos\ksp\javascripts\).
 *
 * Run from repo root:
 *   node scripts/generate-reference-transfers.js
 *
 * Outputs JSON to stdout. Pipe to tests/Core/Data/reference-transfers.json.
 */

'use strict';

const path = require('path');
const fs   = require('fs');

// ---- Minimal DOM/module shim so LWP's IIFE-wrapped files load in Node ----
global.window = global;

const vm = require('vm');

// Load LWP dependencies in order
// Read LWP_DIR from environment (or .env in repo root as fallback)
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

loadLwp('numeric-1.2.6.min.js');  // sets global.numeric
loadLwp('quaternion.js');          // sets global.quaternion
loadLwp('roots.js');               // sets global.roots (brentsMethod etc.)
loadLwp('lambert.js');             // sets global.lambert
loadLwp('orbit.js');               // sets global.Orbit
loadLwp('celestialbodies.js');     // sets global.CelestialBody

// ---- Transfer cases --------------------------------------------------------
// Each case: origin, destination, departureUT (s), timeOfFlight (s),
//            initialOrbitAltitude (m), finalOrbitAltitude (m)
//
// Departure UTs chosen to be close to low-Δv windows so tests are numerically
// stable (Lambert solver well away from degenerate geometry).

const cases = [
    // Kerbin → Duna  (near Hohmann window ~Y1D237, outer planet)
    { origin: 'Kerbin', destination: 'Duna',  departureUT:  5_091_600, tof:  5_184_000, r0: 100_000, r1: 100_000 },
    // Kerbin → Jool  (outer giant)
    { origin: 'Kerbin', destination: 'Jool',  departureUT: 12_960_000, tof: 20_736_000, r0: 100_000, r1: 100_000 },
    // Kerbin → Dres  (outer asteroid belt, well-separated geometry)
    { origin: 'Kerbin', destination: 'Dres',  departureUT: 40_000_000, tof: 10_000_000, r0: 100_000, r1: 100_000 },
    // Kerbin → Eeloo (outer edge)
    { origin: 'Kerbin', destination: 'Eeloo', departureUT: 20_000_000, tof: 40_000_000, r0: 100_000, r1: 100_000 },
    // Kerbin → Eve   (inner planet — long-way arc is cheaper; validates retrograde Lambert)
    { origin: 'Kerbin', destination: 'Eve',   departureUT:  3_000_000, tof:  4_320_000, r0: 100_000, r1: 100_000 },
    // Kerbin → Moho  (inner, high-eccentricity — another retrograde-arc validation)
    { origin: 'Kerbin', destination: 'Moho',  departureUT:  1_800_000, tof:  3_456_000, r0: 100_000, r1: 100_000 },

    // ---- Additional cases added for 1c baseline ----
    // Kerbin → Duna second window (Y2-ish) — validates same geometry at a later epoch
    { origin: 'Kerbin', destination: 'Duna',  departureUT: 14_256_000, tof:  5_616_000, r0: 100_000, r1:  60_000,
      description: 'Kerbin→Duna second window, 60 km capture orbit — validates different epoch and non-100km altitude' },
    // Kerbin → Jool second geometry (shorter TOF, more expensive) — different Lambert arc
    { origin: 'Kerbin', destination: 'Jool',  departureUT: 30_000_000, tof: 16_416_000, r0: 100_000, r1: 200_000,
      description: 'Kerbin→Jool second geometry, 200 km Jool orbit — shorter TOF, higher cost, different arc' },
    // Kerbin → Eve  short TOF (more expensive insertion) — stresses high insertion Δv
    { origin: 'Kerbin', destination: 'Eve',   departureUT: 20_000_000, tof:  2_160_000, r0: 100_000, r1: 100_000,
      description: 'Kerbin→Eve short TOF — stresses high insertion Δv from steep approach' },
    // Duna → Kerbin (non-Kerbin origin — validates origin body generalisation)
    { origin: 'Duna',   destination: 'Kerbin', departureUT: 10_000_000, tof:  5_184_000, r0:  60_000, r1: 100_000,
      description: 'Duna→Kerbin — non-Kerbin origin, validates origin body generalisation' },
];

// ---- Run -------------------------------------------------------------------
const results = cases.map(c => {
    const origin      = CelestialBody[c.origin];
    const destination = CelestialBody[c.destination];

    const v0 = origin.circularOrbitVelocity(c.r0);           // initial circular speed
    const v1 = destination.circularOrbitVelocity(c.r1);       // final circular speed

    const transfer = Orbit.transfer(
        'ballistic',
        origin,
        destination,
        c.departureUT,
        c.tof,
        v0,   // initialOrbitalVelocity
        v1,   // finalOrbitalVelocity
        null, null, null, null, null  // let Orbit.transfer compute positions/velocities
    );

    // Convert LWP ejection angle [0, 2π) rad → signed degrees matching our C# convention:
    //   (0°, 180°] → positive (prograde side)
    //   (180°, 360°) → negative (retrograde side), via angleDeg = 180 − angleDeg
    let ejectionAngleDeg = null;
    let ejectionInclinationDeg = null;
    if (transfer.ejectionAngle != null && !isNaN(transfer.ejectionAngle)) {
        let angleDeg = transfer.ejectionAngle * 180 / Math.PI;
        if (angleDeg > 180) angleDeg = 180 - angleDeg;
        ejectionAngleDeg = angleDeg;
    }
    if (transfer.ejectionInclination != null && !isNaN(transfer.ejectionInclination)) {
        ejectionInclinationDeg = transfer.ejectionInclination * 180 / Math.PI;
    }

    const result = {
        source:              'alexmoon/ksp LWP (javascripts/, ballistic, no plane change)',
        origin:              c.origin,
        destination:         c.destination,
        departureUT:         c.departureUT,
        timeOfFlight:        c.tof,
        parkingOrbitAltitude: c.r0,
        destinationOrbitAltitude: c.r1,
        ejectionDeltaV:      transfer.ejectionDeltaV,
        insertionDeltaV:     transfer.insertionDeltaV,
        totalDeltaV:         transfer.deltaV,
        ejectionAngleDeg,
        ejectionInclinationDeg,
    };
    if (c.description) result.description = c.description;
    return result;
});

console.log(JSON.stringify(results, null, 2));
