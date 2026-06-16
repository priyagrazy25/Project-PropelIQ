import { refreshAccessToken } from '../../features/identity/api/loginApi';

let accessToken: string | null = null;
let refreshPromise: Promise<string | null> | null = null;

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}

export function clearAccessToken(): void {
  accessToken = null;
}

async function tryRefresh(): Promise<string | null> {
  const result = await refreshAccessToken();
  if (result.success) {
    accessToken = result.accessToken;
    return result.accessToken;
  }
  accessToken = null;
  return null;
}

async function getValidToken(): Promise<string | null> {
  if (accessToken) {
    return accessToken;
  }

  if (!refreshPromise) {
    refreshPromise = tryRefresh().finally(() => {
      refreshPromise = null;
    });
  }

  return refreshPromise;
}

export async function authenticatedFetch(
  input: RequestInfo | URL,
  init?: RequestInit,
): Promise<Response> {
  const token = await getValidToken();

  const headers = new Headers(init?.headers);
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  let response = await fetch(input, {
    ...init,
    headers,
    credentials: 'include',
  });

  if (response.status === 401 && token) {
    const newToken = await tryRefresh();
    if (newToken) {
      headers.set('Authorization', `Bearer ${newToken}`);
      response = await fetch(input, {
        ...init,
        headers,
        credentials: 'include',
      });
    }
  }

  return response;
}
