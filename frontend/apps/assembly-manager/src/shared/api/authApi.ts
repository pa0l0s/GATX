import { apiFetch } from './apiClient';

export interface AuthResult {
  token: string;
  username: string;
}

export function login(username: string, password: string): Promise<AuthResult> {
  return apiFetch<AuthResult>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password })
  });
}
