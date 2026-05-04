import { useState, useEffect } from 'react';
import {
  fetchBodies, computeTransfer, computeRoundTrip,
  type BodySummary, type TransferResponse, type RoundTripResponse,
} from '../api/transferClient';

const KSP_YEAR_S  = 9_203_400;
const KSP_DAY_S   = 21_600;

// Stock bodies that orbit Kerbol directly (valid transfer origins/destinations)
const KERBOL_CHILDREN = ['Moho', 'Eve', 'Kerbin', 'Duna', 'Dres', 'Jool', 'Eeloo'];

export default function TransferForm() {
  const [bodies,        setBodies]        = useState<BodySummary[]>([]);
  const [origin,        setOrigin]        = useState('Kerbin');
  const [destination,   setDestination]   = useState('Duna');
  const [depYear,       setDepYear]       = useState(1);
  const [depDay,        setDepDay]        = useState(1);
  const [tofDays,       setTofDays]       = useState(211);
  const [originAlt,     setOriginAlt]     = useState(100);   // km
  const [originInc,     setOriginInc]     = useState(0);     // degrees
  const [originEcc,     setOriginEcc]     = useState(0);
  const [destAlt,       setDestAlt]       = useState(60);    // km
  const [destInc,       setDestInc]       = useState(0);     // degrees
  const [destEcc,       setDestEcc]       = useState(0);
  const [roundTrip,     setRoundTrip]     = useState(false);
  const [stayDays,      setStayDays]      = useState(90);    // days at destination
  const [returnTofDays, setReturnTofDays] = useState(211);   // days return TOF
  const [result,        setResult]        = useState<TransferResponse | null>(null);
  const [rtResult,      setRtResult]      = useState<RoundTripResponse | null>(null);
  const [error,         setError]         = useState<string | null>(null);
  const [loading,       setLoading]       = useState(false);

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
    setRtResult(null);
    setLoading(true);

    const departureUT  = (depYear - 1) * KSP_YEAR_S + (depDay - 1) * KSP_DAY_S;
    const timeOfFlight = tofDays * KSP_DAY_S;

    try {
      if (roundTrip) {
        const res = await computeRoundTrip({
          origin,
          destination,
          departureUT,
          outboundTimeOfFlight: timeOfFlight,
          stayDuration:         stayDays      * KSP_DAY_S,
          returnTimeOfFlight:   returnTofDays * KSP_DAY_S,
          originAltitude:       originAlt * 1_000,
          destinationAltitude:  destAlt   * 1_000,
          originInclination:      originInc,
          destinationInclination: destInc,
          originEccentricity:     originEcc,
          destinationEccentricity: destEcc,
        });
        setRtResult(res);
      } else {
        const res = await computeTransfer({
          origin,
          destination,
          departureUT,
          timeOfFlight,
          originAltitude:      originAlt * 1_000,
          destinationAltitude: destAlt  * 1_000,
          originInclination:      originInc,
          destinationInclination: destInc,
          originEccentricity:     originEcc,
          destinationEccentricity: destEcc,
        });
        setResult(res);
      }
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
          <label>
            Inclination (°)
            <input type="number" min={0} max={180} step={0.1} value={originInc}
              onChange={e => setOriginInc(Number(e.target.value))} />
          </label>
          <label>
            Eccentricity
            <input type="number" min={0} max={0.99} step={0.01} value={originEcc}
              onChange={e => setOriginEcc(Number(e.target.value))} />
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
          <label>
            Inclination (°)
            <input type="number" min={0} max={180} step={0.1} value={destInc}
              onChange={e => setDestInc(Number(e.target.value))} />
          </label>
          <label>
            Eccentricity
            <input type="number" min={0} max={0.99} step={0.01} value={destEcc}
              onChange={e => setDestEcc(Number(e.target.value))} />
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
            Outbound TOF (days)
            <input type="number" min={1} value={tofDays}
              onChange={e => setTofDays(Number(e.target.value))} />
          </label>
        </div>

        <div className="form-row">
          <label className="checkbox-label">
            <input type="checkbox" checked={roundTrip}
              onChange={e => setRoundTrip(e.target.checked)} />
            Round trip
          </label>
        </div>

        {roundTrip && (
          <div className="form-row">
            <label>
              Stay duration (days)
              <input type="number" min={1} value={stayDays}
                onChange={e => setStayDays(Number(e.target.value))} />
            </label>
            <label>
              Return TOF (days)
              <input type="number" min={1} value={returnTofDays}
                onChange={e => setReturnTofDays(Number(e.target.value))} />
            </label>
          </div>
        )}

        <button type="submit" disabled={loading}>
          {loading ? 'Computing…' : roundTrip ? 'Compute Round Trip' : 'Compute Transfer'}
        </button>
      </form>

      {error    && <p className="error">{error}</p>}
      {result   && <TransferResultPanel title="Transfer Result" result={result} />}
      {rtResult && <RoundTripResultPanel result={rtResult} />}
    </div>
  );
}

function TransferResultPanel({ title, result }: { title: string; result: TransferResponse }) {
  return (
    <div className="result-panel">
      <h2>{title}</h2>
      <table>
        <tbody>
          <tr><th>Departure</th><td>{result.departureDate}</td></tr>
          <tr><th>Arrival</th>  <td>{result.arrivalDate}</td></tr>
          <tr className="section-header"><th colSpan={2}>Ejection burn</th></tr>
          <tr><th>Periapsis UT</th><td>{result.ejection.burnDate}</td></tr>
          <tr><th>Prograde Δv</th> <td>{result.ejection.vector.prograde.toFixed(1)} m/s</td></tr>
          <tr><th>Normal Δv</th>   <td>{result.ejection.vector.normal.toFixed(1)} m/s</td></tr>
          <tr><th>Radial Δv</th>   <td>{result.ejection.vector.radial.toFixed(1)} m/s</td></tr>
          <tr><th>Total</th>       <td>{result.ejection.deltaV.toFixed(1)} m/s</td></tr>
          <tr className="section-header"><th colSpan={2}>Insertion burn</th></tr>
          <tr><th>Periapsis UT</th><td>{result.insertion.burnDate}</td></tr>
          <tr><th>Prograde Δv</th> <td>{result.insertion.vector.prograde.toFixed(1)} m/s</td></tr>
          <tr><th>Normal Δv</th>   <td>{result.insertion.vector.normal.toFixed(1)} m/s</td></tr>
          <tr><th>Radial Δv</th>   <td>{result.insertion.vector.radial.toFixed(1)} m/s</td></tr>
          <tr><th>Total</th>       <td>{result.insertion.deltaV.toFixed(1)} m/s</td></tr>
          <tr className="total"><th>Mission Δv</th><td>{result.totalDeltaV.toFixed(1)} m/s</td></tr>
        </tbody>
      </table>
    </div>
  );
}

function RoundTripResultPanel({ result }: { result: RoundTripResponse }) {
  return (
    <div className="result-panel round-trip">
      <TransferResultPanel title="Outbound Leg" result={result.outbound} />
      <TransferResultPanel title="Return Leg"   result={result.return} />
      <div className="mission-total">
        <strong>Mission total Δv:</strong> {result.totalDeltaV.toFixed(1)} m/s
      </div>
    </div>
  );
}
