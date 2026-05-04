import { useState } from 'react';
import type { TransferResponse, BurnDto, PlaneChangeBurnDto } from '../api/transferClient';

const KSP_MM = 1e6;  // metres per Mm

function formatDist(m: number): string {
  if (m >= 1e9) return `${(m / 1e9).toFixed(3)} Gm`;
  return `${(m / KSP_MM).toFixed(1)} Mm`;
}

function formatEjectionAngle(angleDeg: number): string {
  if (angleDeg >= 0) return `${angleDeg.toFixed(2)}° to prograde`;
  return `${(-angleDeg).toFixed(2)}° to retrograde`;
}

function tofString(departureUT: number, arrivalUT: number): string {
  const s = Math.abs(arrivalUT - departureUT);
  const days  = Math.floor(s / 21_600);
  const hours = Math.floor((s % 21_600) / 3_600);
  const mins  = Math.floor((s % 3_600)  / 60);
  return `${days}d ${hours}h ${mins}m`;
}

interface CopyButtonProps {
  text: string;
  label?: string;
}

function CopyButton({ text, label = 'Copy PM' }: CopyButtonProps) {
  const [copied, setCopied] = useState(false);
  const handle = async () => {
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  return (
    <button className="copy-btn" onClick={handle}>
      {copied ? 'Copied!' : label}
    </button>
  );
}

function BurnSection({
  title, burn, isEjection,
}: {
  title: string;
  burn: BurnDto;
  isEjection?: boolean;
}) {
  return (
    <>
      <tr className="section-header"><th colSpan={2}>{title}</th></tr>
      <tr><th>UT</th><td>{burn.burnDate}</td></tr>
      <tr><th>Prograde Δv</th><td>{burn.vector.prograde.toFixed(1)} m/s</td></tr>
      <tr><th>Normal Δv</th>  <td>{burn.vector.normal.toFixed(1)} m/s</td></tr>
      <tr><th>Radial Δv</th>  <td>{burn.vector.radial.toFixed(1)} m/s</td></tr>
      <tr><th>Total</th>      <td>{burn.deltaV.toFixed(1)} m/s</td></tr>
      {isEjection && burn.ejectionDetails && (
        <>
          <tr><th>Ejection angle</th><td>{formatEjectionAngle(burn.ejectionDetails.angleDeg)}</td></tr>
          <tr><th>Ejection inc.</th> <td>{burn.ejectionDetails.inclinationDeg.toFixed(2)}°</td></tr>
        </>
      )}
      <tr><td colSpan={2}>
        <CopyButton text={burn.preciseManeuverText} />
      </td></tr>
    </>
  );
}

function PlaneChangeBurnSection({ burn }: { burn: PlaneChangeBurnDto }) {
  return (
    <>
      <tr className="section-header"><th colSpan={2}>Mid-course plane change</th></tr>
      <tr><th>UT</th>         <td>{burn.burnDate}</td></tr>
      <tr><th>Prograde Δv</th><td>{burn.vector.prograde.toFixed(1)} m/s</td></tr>
      <tr><th>Normal Δv</th>  <td>{burn.vector.normal.toFixed(1)} m/s</td></tr>
      <tr><th>Total</th>      <td>{burn.deltaV.toFixed(1)} m/s</td></tr>
      <tr><td colSpan={2}>
        <CopyButton text={burn.preciseManeuverText} label="Copy PM (plane change)" />
      </td></tr>
    </>
  );
}

interface TransferDetailsProps {
  result: TransferResponse;
  title?: string;
}

export default function TransferDetails({ result, title = 'Selected transfer' }: TransferDetailsProps) {
  const tof = tofString(result.departureUT, result.arrivalUT);

  return (
    <div className="result-panel">
      <h2>{title}</h2>
      <table>
        <tbody>
          <tr><th>Departure</th>    <td>{result.departureDate}</td></tr>
          <tr><th>Arrival</th>      <td>{result.arrivalDate}</td></tr>
          <tr><th>Time of flight</th><td>{tof}</td></tr>
          <tr><th>Phase angle</th>  <td>{result.phaseAngleDeg.toFixed(2)}°</td></tr>
          <tr><th>Transfer angle</th><td>{result.transferAngleDeg.toFixed(1)}°</td></tr>
          <tr><th>Transfer periapsis</th><td>{formatDist(result.transferPeriapsis)}</td></tr>
          <tr><th>Transfer apoapsis</th> <td>{formatDist(result.transferApoapsis)}</td></tr>

          <BurnSection title="Ejection burn" burn={result.ejection} isEjection />

          {result.planeChange && (
            <PlaneChangeBurnSection burn={result.planeChange} />
          )}

          <tr><th>Insertion inc.</th><td>{result.insertionInclinationDeg.toFixed(2)}°</td></tr>
          <BurnSection title="Insertion burn" burn={result.insertion} />

          <tr className="total"><th>Total Δv</th><td>{result.totalDeltaV.toFixed(1)} m/s</td></tr>
        </tbody>
      </table>
    </div>
  );
}
