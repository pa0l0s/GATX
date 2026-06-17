const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

const TOKEN_KEY = 'gatx.token';
const USERNAME_KEY = 'gatx.username';

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
    this.name = 'ApiError';
  }
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getStoredUsername(): string | null {
  return localStorage.getItem(USERNAME_KEY);
}

export function storeSession(token: string, username: string): void {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USERNAME_KEY, username);
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USERNAME_KEY);
}

let unauthorizedHandler: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler;
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  const token = getToken();

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  if (init.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers });

  if (response.status === 401) {
    // A 401 while we held a token means the session lapsed — sign out.
    if (token) {
      clearSession();
      unauthorizedHandler?.();
    }
    throw new ApiError(401, await readMessage(response, 'Invalid username or password.'));
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readMessage(response, `Request failed with status ${response.status}`));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function readMessage(response: Response, fallback: string): Promise<string> {
  const problem = await response.json().catch(() => undefined);
  return problem?.detail ?? problem?.title ?? fallback;
}
