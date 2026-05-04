# KSP Navigation Computer — Roadmap

> Status board. Update the Status column when an increment completes.
> Full details for each increment: `increment-N.md` in this folder.

---

## Increment status

| Increment | Description | Status |
|-----------|-------------|--------|
| 1a | Scaffold + one-way circular transfer | complete |
| 1b | Round trip | complete |
| 1c | Inclined / elliptical parking orbits + burn vectors | complete |
| 2  | Precise Maneuver export | not started |
| 3  | Transfer window porkchop plot | not started |
| 4  | OPM + MPE body support | not started |
| 5  | KSP save file import | not started |
| 6  | Landed start support | not started |
| 7  | Integrated delta-V map | not started |
| 8  | Sub-system transfers | not started |
| 9  | In-game mod | not started |

---

## One-line descriptions

| Increment | Description |
|-----------|-------------|
| **1a** | Full repo scaffold; Kepler + Lambert solvers; one-way transfer for circular equatorial orbits; minimal API + React UI |
| **1b** | Return-leg computation; round-trip result record; UI shows both legs and total Δv |
| **1c** | Inclined/elliptical parking orbits; ejection + insertion burn vectors (prograde/normal/radial); precise periapsis burn UT |
| **2** | Format burns as Precise Maneuver plaintext block; one-click copy-to-clipboard per burn |
| **3** | Porkchop plot: Δv / TOF grid over departure date range; click to populate burn details |
| **4** | Parse Kopernicus `.cfg` files from local GameData to add OPM + MPE bodies |
| **5** | Parse `.sfs` save files; vessel selection dropdown pre-fills departure parameters |
| **6** | Δv budget from surface launch to parking orbit (gravity + drag losses) integrated into transfer plan |
| **7** | Interactive delta-V map embedded in UI, consistent with OPM/MPE bodies |
| **8** | Transfers between bodies sharing an SOI (Kerbin → Mun, Jool → Laythe) via patched-conic approximation |
| **9** | KSP plugin reusing Core library directly, replacing the desktop app during gameplay |
