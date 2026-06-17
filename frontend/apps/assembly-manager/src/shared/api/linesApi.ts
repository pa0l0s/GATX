import { apiFetch } from './apiClient';

export interface AssemblyLine {
  id: string;
  name: string;
  active: boolean;
  productId: string;
  productName: string;
  workstationCount: number;
}

export interface AssemblyLineInput {
  productId: string;
  name: string;
  active: boolean;
}

export interface Allocation {
  workstationId: string;
  shortName: string;
  name: string;
  pcName: string;
  position: number;
}

export function getAssemblyLines(productId?: string): Promise<AssemblyLine[]> {
  const query = productId ? `?productId=${productId}` : '';
  return apiFetch<AssemblyLine[]>(`/api/assembly-lines${query}`);
}

export function getAssemblyLine(id: string): Promise<AssemblyLine> {
  return apiFetch<AssemblyLine>(`/api/assembly-lines/${id}`);
}

export function createAssemblyLine(input: AssemblyLineInput): Promise<AssemblyLine> {
  return apiFetch<AssemblyLine>('/api/assembly-lines', {
    method: 'POST',
    body: JSON.stringify(input)
  });
}

export function updateAssemblyLine(id: string, input: AssemblyLineInput): Promise<AssemblyLine> {
  return apiFetch<AssemblyLine>(`/api/assembly-lines/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input)
  });
}

export function deleteAssemblyLine(id: string): Promise<void> {
  return apiFetch<void>(`/api/assembly-lines/${id}`, { method: 'DELETE' });
}

export function getAllocations(lineId: string): Promise<Allocation[]> {
  return apiFetch<Allocation[]>(`/api/assembly-lines/${lineId}/workstations`);
}

export function allocateWorkstation(lineId: string, workstationId: string): Promise<Allocation[]> {
  return apiFetch<Allocation[]>(`/api/assembly-lines/${lineId}/workstations`, {
    method: 'POST',
    body: JSON.stringify({ workstationId })
  });
}

export function reorderAllocations(lineId: string, workstationIds: string[]): Promise<Allocation[]> {
  return apiFetch<Allocation[]>(`/api/assembly-lines/${lineId}/workstations/order`, {
    method: 'PUT',
    body: JSON.stringify({ workstationIds })
  });
}

export function removeAllocation(lineId: string, workstationId: string): Promise<Allocation[]> {
  return apiFetch<Allocation[]>(`/api/assembly-lines/${lineId}/workstations/${workstationId}`, {
    method: 'DELETE'
  });
}
