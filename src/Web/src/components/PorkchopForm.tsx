import { useState, useEffect } from 'react';
import { fetchBodies, type BodySummary, type PorkchopRequest, type PorkchopResponse, computePorkchop } from '../api/transferClient';

const KSP_YEAR_S = 9_203_400;
const KSP_DAY_S  = 21_600;

function toUT(year: number, day: number): number {
  return (year - 1) * KSP_YEAR_S + (day - 1) * KSP_DAY_S;
}

interface PorkchopFormProps {
  onResult: (result: PorkchopResponse, req: PorkchopRequest) => void;
}

export default function PorkchopForm({ onResult }: PorkchopFormProps) {
  const [bodies,         setBodies]         = useState<BodySummary[]>([]);
  const [origin,         setOrigin]         = useState('Kerbin');
  const [destination,    setDestination]    = useState('Duna');
  const [originAlt,      setOriginAlt]      = useState(100);   // km
  const [originInc,      setOriginInc]      = useState(0);
  const [originEcc,      setOriginEcc]      = useState(0);
  const [destAlt,        setDestAlt]        = useState(60);    // km
  const [destInc,        setDestInc]        = useState(0);
  const [destEcc,        setDestEcc]        = useState(0);
  const [noInsertion,    setNoInsertion]    = useState(false);
  const [earliestYear,   setEarliestYear]   = useState(1);
  const [earliestDay,    setEarliestDay]    = useState(1);
  const [latestYear,     setLatestYear]     = useState(1);
  const [latestDay,      setLatestDay]      = useState(300);
  const [transferType,   setTransferType]   = useState('Optimal');
  const [loading,        setLoading]        = useState(false);
  const [error,          setError]          = useState<string | null>(null);

  useEffect(() => {
    fetchBodies().then(setBodies).catch(() => {});
  }, []);

  const kerbolarBodies = bodies.length > 0
    ? bodies.filter(b => b.parent === 'Kerbol').map(b => b.name)
    : ['Moho', 'Eve', 'Kerbin', 'Duna', 'Dres', 'Jool', 'Eeloo'];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    const earliestUT = toUT(earliestYear, earliestDay);
    const latestUT   = toUT(latestYear, latestDay);
    if (latestUT <= earliestUT) {
      setError('Latest departure must be after earliest departure.');
      return;
    }
    const req: PorkchopRequest = {
      origin,
      destination,
      originAltitude:      originAlt * 1000,
      destinationAltitude: destAlt * 1000,
      earliestDeparture:   earliestUT,
      latestDeparture:     latestUT,
      originInclination:   originInc,
      originEccentricity:  originEcc,
      noInsertionBurn:     noInsertion,
      transferType,
      gridCols: 100,
      gridRows: 100,
      ...(noInsertion ? {} : {
        destinationInclination: destInc,
        destinationEccentricity: destEcc,
      }),
    };
    setLoading(true);
    try {
      const result = await computePorkchop(req);
      onResult(result, req);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="porkchop-form">
      <h2>Porkchop Planner</h2>
      <form onSubmit={handleSubmit}>
        <div className="form-row">
          <label>
            Origin
            <select value={origin} onChange={e => setOrigin(e.target.value)}>
              {kerbolarBodies.map(b => <option key={b}>{b}</option>)}
            </select>
          </label>
          <label>
            Destination
            <select value={destination} onChange={e => setDestination(e.target.value)}>
              {kerbolarBodies.map(b => <option key={b}>{b}</option>)}
            </select>
          </label>
        </div>

        <div className="form-row">
          <label>Origin alt (km) <input type="number" value={originAlt} min={0} onChange={e => setOriginAlt(+e.target.value)} /></label>
          <label>Origin inc (°)  <input type="number" value={originInc} step={0.1} onChange={e => setOriginInc(+e.target.value)} /></label>
          <label>Origin ecc      <input type="number" value={originEcc} step={0.01} min={0} max={0.99} onChange={e => setOriginEcc(+e.target.value)} /></label>
        </div>

        <label className="checkbox-row">
          <input type="checkbox" checked={noInsertion} onChange={e => setNoInsertion(e.target.checked)} />
          No insertion burn
        </label>

        {!noInsertion && (
          <div className="form-row">
            <label>Dest alt (km)  <input type="number" value={destAlt} min={0} onChange={e => setDestAlt(+e.target.value)} /></label>
            <label>Dest inc (°)   <input type="number" value={destInc} step={0.1} onChange={e => setDestInc(+e.target.value)} /></label>
            <label>Dest ecc       <input type="number" value={destEcc} step={0.01} min={0} max={0.99} onChange={e => setDestEcc(+e.target.value)} /></label>
          </div>
        )}

        <div className="form-row">
          <label>Earliest departure
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <input type="number" value={earliestYear} min={1} style={{ width: 60 }} onChange={e => setEarliestYear(+e.target.value)} placeholder="Year" />
              <input type="number" value={earliestDay}  min={1} max={426} style={{ width: 60 }} onChange={e => setEarliestDay(+e.target.value)} placeholder="Day" />
            </div>
          </label>
          <label>Latest departure
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <input type="number" value={latestYear} min={1} style={{ width: 60 }} onChange={e => setLatestYear(+e.target.value)} placeholder="Year" />
              <input type="number" value={latestDay}  min={1} max={426} style={{ width: 60 }} onChange={e => setLatestDay(+e.target.value)} placeholder="Day" />
            </div>
          </label>
        </div>

        <div className="form-row">
          <label>
            Transfer type
            <select value={transferType} onChange={e => setTransferType(e.target.value)}>
              <option value="Optimal">Optimal</option>
              <option value="Ballistic">Ballistic</option>
              <option value="MidCoursePlaneChange">Mid-course plane change</option>
            </select>
          </label>
        </div>

        {error && <div className="error">{error}</div>}

        <button type="submit" disabled={loading}>
          {loading ? 'Computing…' : 'Plot it!'}
        </button>
      </form>
    </div>
  );
}
