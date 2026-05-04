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
}

export interface BurnVectorDto {
  prograde: number;
  normal: number;
  radial: number;
}

export interface TransferResponse {
  departureUT: number;
  departureDate: string;
  arrivalUT: number;
  arrivalDate: string;
  ejectionDeltaV: number;
  insertionDeltaV: number;
  totalDeltaV: number;
  ejectionBurnUT: number;
  ejectionBurnDate: string;
  ejectionBurnVector: BurnVectorDto;
  insertionBurnUT: number;
  insertionBurnDate: string;
  insertionBurnVector: BurnVectorDto;
}

export interface BodySummary {
  name: string;
  parent: string;
  radius: number;
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
