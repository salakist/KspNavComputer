const API_BASE = 'http://localhost:5000';

export interface TransferRequest {
  origin: string;
  destination: string;
  departureUT: number;
  timeOfFlight: number;
  originAltitude: number;
  destinationAltitude: number;
}

export interface TransferResponse {
  departureUT: number;
  departureDate: string;
  arrivalUT: number;
  arrivalDate: string;
  ejectionDeltaV: number;
  insertionDeltaV: number;
  totalDeltaV: number;
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
