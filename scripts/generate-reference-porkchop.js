/**
 * Generates reference porkchop Δv grids using alexmoon's Launch Window Planner
 * JavaScript source (C:\Users\simon\Repos\ksp\javascripts\).
 *
 * Run from repo root:
 *   node scripts/generate-reference-porkchop.js
 *
 * Outputs JSON to stdout. Pipe to tests/Core/Data/reference-porkchop.json.
 *
 * Grid convention: identical to PorkchopComputer.cs —
 *   col spacing: depUT  = earliestDeparture + col * (latestDeparture - earliestDeparture) / (cols - 1)
 *   row spacing: tof    = minTof + row * (maxTof - minTof) / (rows - 1)
 *   row 0 → minTof,  row rows-1 → maxTof
 *   col 0 → earliestDeparture,  col cols-1 → latestDeparture
 *
 * TOF range: same Hohmann-based formula as PorkchopComputer.AutoTofRange and LWP missionform.
 */

'use strict';

const path = require('path');
const fs   = require('fs');

// ---- Minimal DOM/module shim so LWP's IIFE-wrapped files load in Node ----
global.window = global;

const vm = require('vm');

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

// ---- TOF auto-range (mirrors PorkchopComputer.AutoTofRange and LWP missionform) ----
function autoTofRange(origin, destination) {
    const mu = origin.orbit.referenceBody.gravitationalParameter;
    const rOrigin = origin.orbit.semiMajorAxis;
    const rDest   = destination.orbit.semiMajorAxis;

    // Hohmann transfer semi-major axis and TOF
    const hohmannA   = (rOrigin + rDest) / 2;
    const hohmannTof = Math.PI * Math.sqrt(hohmannA * hohmannA * hohmannA / mu);

    // Destination orbital period
    const destPeriod = 2 * Math.PI * Math.sqrt(rDest * rDest * rDest / mu);

    const minTof = Math.max(hohmannTof - destPeriod, hohmannTof / 2);
    const maxTof = minTof + Math.min(2 * destPeriod, hohmannTof);

    return { minTof, maxTof };
}

// ---- Cases ----
// Each case mirrors parameters the user would enter in the UI.
// Grid size: 20×20 (manageable JSON, enough resolution for testing).
const GRID = 20;

const cases = [
    {
        origin:             'Kerbin',
        destination:        'Duna',
        originAltitude:     80e3,    // 80 km
        destinationAltitude: 60e3,   // 60 km
        earliestDeparture:  22_705_200,  // Y3 D200
        latestDeparture:    34_068_600,  // Y4 D300
        transferType:       'ballistic',
        description:        'Kerbin→Duna ballistic, Y3D200–Y4D300 (matches screenshot comparison)',
    },
    {
        origin:             'Kerbin',
        destination:        'Eeloo',
        originAltitude:     100e3,
        destinationAltitude: 100e3,
        earliestDeparture:  18_406_800,  // Y3 D001
        latestDeparture:    46_017_000,  // Y5 D200
        transferType:       'ballistic',
        description:        'Kerbin→Eeloo ballistic (inclined destination — broad window)',
    },
    {
        origin:             'Kerbin',
        destination:        'Duna',
        originAltitude:     80e3,
        destinationAltitude: 60e3,
        earliestDeparture:  5_000_000,   // near Y1 window
        latestDeparture:    12_000_000,
        transferType:       'ballistic',
        description:        'Kerbin→Duna ballistic, Y1 window',
    },
];

// ---- Compute ----
const results = cases.map(c => {
    const origin      = CelestialBody[c.origin];
    const destination = CelestialBody[c.destination];

    if (!origin)      throw new Error(`Unknown body: ${c.origin}`);
    if (!destination) throw new Error(`Unknown body: ${c.destination}`);

    const { minTof, maxTof } = autoTofRange(origin, destination);

    const v0 = origin.circularOrbitVelocity(c.originAltitude);
    const v1 = destination.circularOrbitVelocity(c.destinationAltitude);

    const depRange = c.latestDeparture - c.earliestDeparture;
    const tofRange = maxTof - minTof;

    const cells = [];
    let lwpMinDv   = Infinity;
    let lwpOptRow  = 0;
    let lwpOptCol  = 0;

    for (let row = 0; row < GRID; row++) {
        const tof = minTof + tofRange * row / (GRID - 1);

        for (let col = 0; col < GRID; col++) {
            const depUT = c.earliestDeparture + depRange * col / (GRID - 1);

            let dv;
            try {
                const transfer = Orbit.transfer(
                    c.transferType,
                    origin, destination,
                    depUT, tof,
                    v0, v1,
                    null, null, null, null, null
                );
                dv = transfer.deltaV;
            } catch (e) {
                dv = NaN;
            }

            // Store NaN as null for clean JSON
            cells.push(Number.isFinite(dv) ? dv : null);

            if (Number.isFinite(dv) && dv < lwpMinDv) {
                lwpMinDv  = dv;
                lwpOptRow = row;
                lwpOptCol = col;
            }
        }
    }

    // Compute the true fine-grained optimal using a 300×300 LWP grid for reference
    let fineMinDv  = Infinity;
    const FINE = 300;
    const fineDepRange = c.latestDeparture - c.earliestDeparture;
    for (let fy = 0; fy < FINE; fy++) {
        const tof = minTof + tofRange * fy / (FINE - 1);
        for (let fx = 0; fx < FINE; fx++) {
            const depUT = c.earliestDeparture + fineDepRange * fx / (FINE - 1);
            try {
                const transfer = Orbit.transfer(c.transferType, origin, destination, depUT, tof, v0, v1,
                    null, null, null, null, null);
                if (Number.isFinite(transfer.deltaV) && transfer.deltaV < fineMinDv)
                    fineMinDv = transfer.deltaV;
            } catch {}
        }
    }

    // Spot-check cells: a 5×5 neighbourhood centred on the optimal cell.
    // These are in the unambiguous short-way Lambert region where both
    // implementations pick the same branch, so they can be validated at 1 %.
    const spotChecks = [];
    for (let dr = -2; dr <= 2; dr++) {
        for (let dc = -2; dc <= 2; dc++) {
            const r = lwpOptRow + dr;
            const rc = lwpOptCol + dc;
            if (r < 0 || r >= GRID || rc < 0 || rc >= GRID) continue;
            const dv = cells[r * GRID + rc];
            if (dv == null) continue;
            spotChecks.push({ row: r, col: rc, deltaV: dv });
        }
    }

    return {
        source:             'alexmoon/ksp LWP (javascripts/)',
        description:        c.description,
        origin:             c.origin,
        destination:        c.destination,
        originAltitude:     c.originAltitude,
        destinationAltitude: c.destinationAltitude,
        earliestDeparture:  c.earliestDeparture,
        latestDeparture:    c.latestDeparture,
        transferType:       c.transferType,
        minTof,
        maxTof,
        gridCols:           GRID,
        gridRows:           GRID,
        // row-major [row * GRID + col]; null = NaN/failed
        cells,
        // 20×20 optimal
        coarseOptimalRow:   lwpOptRow,
        coarseOptimalCol:   lwpOptCol,
        coarseOptimalDeltaV: lwpMinDv,
        // 300×300 fine-grained optimal — upper bound for our grid's minimum
        fineOptimalDeltaV:  fineMinDv,
        // near-optimal spot-check cells (5×5 around optimal) for cell-by-cell validation
        spotChecks,
    };
});

console.log(JSON.stringify(results, null, 2));
