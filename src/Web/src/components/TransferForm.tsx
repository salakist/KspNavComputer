import { useState, useEffect } from 'react';
import { fetchBodies, computeTransfer, type BodySummary, type TransferResponse } from '../api/transferClient';

const KSP_YEAR_S  = 9_203_400;
const KSP_DAY_S   = 21_600;

// Stock bodies that orbit Kerbol directly (valid transfer origins/destinations)
const KERBOL_CHILDREN = ['Moho', 'Eve', 'Kerbin', 'Duna', 'Dres', 'Jool', 'Eeloo'];

export default function TransferForm() {
  const [bodies,      setBodies]      = useState<BodySummary[]>([]);
  const [origin,      setOrigin]      = useState('Kerbin');
  const [destination, setDestination] = useState('Duna');
  const [depYear,     setDepYear]     = useState(1);
  const [depDay,      setDepDay]      = useState(1);
  const [tofDays,     setTofDays]     = useState(211);
  const [originAlt,   setOriginAlt]   = useState(100);   // km
  const [destAlt,     setDestAlt]     = useState(60);    // km
  const [result,      setResult]      = useState<TransferResponse | null>(null);
  const [error,       setError]       = useState<string | null>(null);
  const [loading,     setLoading]     = useState(false);

  useEffect(() => {
    fetchBodies().then(setBodies).catch(() => {/* API might not be running */});
  }, []);

  // Derive available bodies: those that orbit Kerbol (parent = 'Kerbol')
  const kerbolarBodies = bodies.length > 0
    ? bodies.filter(b => b.parent === 'Kerbol').map(b => b.name)
    : KERBOL_CHILDREN;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setResult(null);
    setLoading(true);

    const departureUT  = (depYear - 1) * KSP_YEAR_S + (depDay - 1) * KSP_DAY_S;
    const timeOfFlight = tofDays * KSP_DAY_S;

    try {
      const res = await computeTransfer({
        origin,
        destination,
        departureUT,
        timeOfFlight,
        originAltitude:      originAlt * 1_000,
        destinationAltitude: destAlt  * 1_000,
      });
      setResult(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="transfer-form">
      <form onSubmit={handleSubmit}>
        <div className="form-row">
          <label>
            Origin
            <select value={origin} onChange={e => setOrigin(e.target.value)}>
              {kerbolarBodies.map(b => <option key={b} value={b}>{b}</option>)}
            </select>
          </label>
          <label>
            Parking orbit altitude (km)
            <input type="number" min={1} value={originAlt}
              onChange={e => setOriginAlt(Number(e.target.value))} />
          </label>
        </div>

        <div className="form-row">
          <label>
            Destination
            <select value={destination} onChange={e => setDestination(e.target.value)}>
              {kerbolarBodies.map(b => <option key={b} value={b}>{b}</option>)}
            </select>
          </label>
          <label>
            Capture orbit altitude (km)
            <input type="number" min={1} value={destAlt}
              onChange={e => setDestAlt(Number(e.target.value))} />
          </label>
        </div>

        <div className="form-row">
          <label>
            Departure year
            <input type="number" min={1} value={depYear}
              onChange={e => setDepYear(Number(e.target.value))} />
          </label>
          <label>
            Departure day
            <input type="number" min={1} max={426} value={depDay}
              onChange={e => setDepDay(Number(e.target.value))} />
          </label>
          <label>
            Time of flight (days)
            <input type="number" min={1} value={tofDays}
              onChange={e => setTofDays(Number(e.target.value))} />
          </label>
        </div>

        <button type="submit" disabled={loading}>
          {loading ? 'Computing…' : 'Compute Transfer'}
        </button>
      </form>

      {error  && <p className="error">{error}</p>}
      {result && <TransferResultPanel result={result} />}
    </div>
  );
}

function TransferResultPanel({ result }: { result: TransferResponse }) {
  return (
    <div className="result-panel">
      <h2>Transfer Result</h2>
      <table>
        <tbody>
          <tr><th>Departure</th><td>{result.departureDate}</td></tr>
          <tr><th>Arrival</th>  <td>{result.arrivalDate}</td></tr>
          <tr><th>Ejection Δv</th>  <td>{result.ejectionDeltaV.toFixed(1)} m/s</td></tr>
          <tr><th>Insertion Δv</th> <td>{result.insertionDeltaV.toFixed(1)} m/s</td></tr>
          <tr className="total"><th>Total Δv</th><td>{result.totalDeltaV.toFixed(1)} m/s</td></tr>
        </tbody>
      </table>
    </div>
  );
}
