import { apiFetch } from './apiClient';

export interface Workstation {
  id: string;
  shortName: string;
  name: string;
  pcName: string;
}

export interface WorkstationInput {
  shortName: string;
  name: string;
  pcName: string;
}

export function getWorkstations(): Promise<Workstation[]> {
  return apiFetch<Workstation[]>('/api/workstations');
}

export function createWorkstation(input: WorkstationInput): Promise<Workstation> {
  return apiFetch<Workstation>('/api/workstations', {
    method: 'POST',
    body: JSON.stringify(input)
  });
}

export function updateWorkstation(id: string, input: WorkstationInput): Promise<Workstation> {
  return apiFetch<Workstation>(`/api/workstations/${id}`, {
    method: 'PUT',
    body: JSON.stringify(input)
  });
}

export function deleteWorkstation(id: string): Promise<void> {
  return apiFetch<void>(`/api/workstations/${id}`, { method: 'DELETE' });
}
