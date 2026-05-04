import { useState } from 'react';
import PorkchopForm from './components/PorkchopForm';
import PorkchopPlot from './components/PorkchopPlot';
import TransferDetails from './components/TransferDetails';
import TransferForm from './components/TransferForm';
import {
  computeTransfer,
  type PorkchopRequest,
  type PorkchopResponse,
  type TransferResponse,
} from './api/transferClient';
import './App.css';

function App() {
  const [porkchop,     setPorkchop]     = useState<PorkchopResponse | null>(null);
  const [porkchopReq,  setPorkchopReq]  = useState<PorkchopRequest | null>(null);
  const [transfer,     setTransfer]     = useState<TransferResponse | null>(null);
  const [selCol,       setSelCol]       = useState<number | undefined>();
  const [selRow,       setSelRow]       = useState<number | undefined>();
  const [selError,     setSelError]     = useState<string | null>(null);

  const handlePorkchopResult = async (result: PorkchopResponse, req: PorkchopRequest) => {
    setPorkchop(result);
    setPorkchopReq(req);
    setTransfer(null);
    setSelError(null);
    // Auto-fetch optimal cell
    await fetchCell(result, req, result.optimalCol, result.optimalRow);
    setSelCol(result.optimalCol);
    setSelRow(result.optimalRow);
  };

  const fetchCell = async (
    pc: PorkchopResponse,
    req: PorkchopRequest,
    col: number,
    row: number,
  ) => {
    const depUT = pc.earliestDeparture
                + (pc.latestDeparture - pc.earliestDeparture) * col / Math.max(pc.cols - 1, 1);
    const tof   = pc.minTof
                + (pc.maxTof - pc.minTof) * row / Math.max(pc.rows - 1, 1);
    try {
      setSelError(null);
      const tr = await computeTransfer({
        origin:                req.origin,
        destination:           req.destination,
        departureUT:           depUT,
        timeOfFlight:          tof,
        originAltitude:        req.originAltitude,
        destinationAltitude:   req.destinationAltitude,
        originInclination:     req.originInclination,
        destinationInclination: req.destinationInclination,
        originEccentricity:    req.originEccentricity,
        destinationEccentricity: req.destinationEccentricity,
        transferType:          req.transferType,
        noInsertionBurn:       req.noInsertionBurn,
      });
      setTransfer(tr);
    } catch (err) {
      setSelError(err instanceof Error ? err.message : String(err));
    }
  };

  const handleCellSelect = async (depUT: number, tof: number) => {
    if (!porkchop || !porkchopReq) return;
    // Reverse-map depUT/tof to col/row for highlight
    const col = Math.round(
      (depUT - porkchop.earliestDeparture)
      / (porkchop.latestDeparture - porkchop.earliestDeparture)
      * (porkchop.cols - 1),
    );
    const row = Math.round(
      (tof - porkchop.minTof)
      / (porkchop.maxTof - porkchop.minTof)
      * (porkchop.rows - 1),
    );
    setSelCol(col);
    setSelRow(row);
    await fetchCell(porkchop, porkchopReq, col, row);
  };

  return (
    <>
      <header>
        <h1>KSP Navigation Computer</h1>
        <p>Interplanetary transfer planner — stock bodies</p>
      </header>
      <main className="app-layout">
        <aside className="left-panel">
          <PorkchopForm onResult={handlePorkchopResult} />
          <hr />
          <details>
            <summary>Manual transfer (legacy)</summary>
            <TransferForm />
          </details>
        </aside>
        <section className="right-panel">
          {porkchop && (
            <PorkchopPlot
              data={porkchop}
              onSelect={handleCellSelect}
              selectedCol={selCol}
              selectedRow={selRow}
            />
          )}
          {selError && <div className="error">{selError}</div>}
          {transfer && <TransferDetails result={transfer} />}
        </section>
      </main>
    </>
  );
}

export default App;