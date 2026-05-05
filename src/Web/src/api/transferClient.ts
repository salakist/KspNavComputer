const API_BASE = 'http://localhost:5000';

export interface TransferRequest {
  origin: string;
  destination: string;
  departureUT: number;
  timeOfFlight: number;
  originAltitude: number;
  destinationAltitude: number;
  originInclination?: number;       // degrees, default 0
  destinationInclination?: number;  // degrees, default 0
  originEccentricity?: number;      // default 0
  destinationEccentricity?: number; // default 0
  transferType?: string;            // "Ballistic" | "MidCoursePlaneChange" | "Optimal"
  noInsertionBurn?: boolean;
}

export interface BurnVectorDto {
  prograde: number;
  normal: number;
  radial: number;
}

export interface EjectionDetailsDto {
  angleDeg: number;
  inclinationDeg: number;
}

export interface PlaneChangeBurnDto {
  deltaV: number;
  burnUT: number;
  burnDate: string;
  vector: BurnVectorDto;
  preciseManeuverText: string;
}

export interface BurnDto {
  deltaV: number;
  burnUT: number;
  burnDate: string;
  vector: BurnVectorDto;
  preciseManeuverText: string;
  ejectionDetails: EjectionDetailsDto | null;
}

export interface TransferResponse {
  departureUT: number;
  departureDate: string;
  arrivalUT: number;
  arrivalDate: string;
  ejection: BurnDto;
  insertion: BurnDto;
  totalDeltaV: number;
  planeChange: PlaneChangeBurnDto | null;
  phaseAngleDeg: number;
  transferAngleDeg: number;
  transferPeriapsis: number;   // [m] from central body
  transferApoapsis: number;    // [m] from central body
  insertionInclinationDeg: number;
}

export interface BodySummary {
  name: string;
  parent: string;
  radius: number;
}

export interface PorkchopRequest {
  origin: string;
  destination: string;
  originAltitude: number;
  destinationAltitude: number;
  earliestDeparture: number;  // [s UT]
  latestDeparture: number;    // [s UT]
  originInclination?: number;
  destinationInclination?: number;
  originEccentricity?: number;
  destinationEccentricity?: number;
  noInsertionBurn?: boolean;
  transferType?: string;
  gridCols?: number;  // default 300
  gridRows?: number;  // default 300
}

export interface PorkchopResponse {
  deltaVs: number[];         // flat row-major [rows × cols], NaN for failed cells
  rows: number;
  cols: number;
  earliestDeparture: number; // [s UT]
  latestDeparture: number;   // [s UT]
  minTof: number;            // [s]
  maxTof: number;            // [s]
  minDeltaV: number;
  maxDeltaV: number;
  meanLogDeltaV: number;
  stdLogDeltaV: number;
  optimalRow: number;
  optimalCol: number;
}

export interface RoundTripRequest {
  origin: string;
  destination: string;
  departureUT: number;
  outboundTimeOfFlight: number;
  stayDuration: number;
  returnTimeOfFlight: number;
  originAltitude: number;
  destinationAltitude: number;
  originInclination?: number;       // degrees, default 0
  destinationInclination?: number;  // degrees, default 0
  originEccentricity?: number;      // default 0
  destinationEccentricity?: number; // default 0
}

export interface RoundTripResponse {
  outbound: TransferResponse;
  return: TransferResponse;
  totalDeltaV: number;
}

export async function fetchBodies(): Promise<BodySummary[]> {
  const res = await fetch(`${API_BASE}/api/bodies`);
  if (!res.ok) throw new Error(`Failed to fetch bodies: ${res.statusText}`);
  return res.json();
}

export async function computeTransfer(req: TransferRequest): Promise<TransferResponse> {
  const res = await fetch(`${API_BASE}/api/transfer`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
  if (!res.ok) {
    const msg = await res.text();
    throw new Error(msg || res.statusText);
  }
  return res.json();
}

export async function computePorkchop(req: PorkchopRequest): Promise<PorkchopResponse> {
  const res = await fetch(`${API_BASE}/api/porkchop`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
  if (!res.ok) {
    const msg = await res.text();
    throw new Error(msg || res.statusText);
  }
  return res.json();
}

export async function computeRoundTrip(req: RoundTripRequest): Promise<RoundTripResponse> {
  const res = await fetch(`${API_BASE}/api/transfer/roundtrip`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
  if (!res.ok) {
    const msg = await res.text();
    throw new Error(msg || res.statusText);
  }
  return res.json();
}

