import { useCallback, useEffect, useState } from 'react';
import { BASE } from '../config';

export interface AuthUser {
  authenticated: boolean;
  userId?: string;
  email?: string;
  displayName?: string;
  isAdmin?: boolean;
}

export interface AuthProviders {
  google: boolean;
  facebook: boolean;
}

async function parseJsonError(res: Response): Promise<string> {
  try {
    const data = await res.json();
    if (data.errors) return Array.isArray(data.errors) ? data.errors.join(', ') : String(data.errors);
    if (data.error) return data.error;
    return `Request failed (${res.status})`;
  } catch {
    return `Request failed (${res.status})`;
  }
}

export function useAuth() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [providers, setProviders] = useState<AuthProviders>({ google: false, facebook: false });
  const [loading, setLoading] = useState(true);

  const refreshMe = useCallback(async () => {
    const res = await fetch(`${BASE}api/account/me`, { credentials: 'include' });
    const data: AuthUser = await res.json();
    setUser(data);
    return data;
  }, []);

  useEffect(() => {
    Promise.all([
      refreshMe(),
      fetch(`${BASE}api/account/providers`, { credentials: 'include' })
        .then(r => r.json())
        .then(setProviders)
        .catch(() => {}),
    ]).finally(() => setLoading(false));
  }, [refreshMe]);

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const res = await fetch(`${BASE}api/account/register`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password, displayName }),
    });
    if (!res.ok) throw new Error(await parseJsonError(res));
    return res.json();
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await fetch(`${BASE}api/account/login`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) throw new Error(await parseJsonError(res));
    const data = await res.json();
    setUser(data);
    return data;
  }, []);

  const logout = useCallback(async () => {
    await fetch(`${BASE}api/account/logout`, { method: 'POST', credentials: 'include' });
    setUser({ authenticated: false });
  }, []);

  const confirmEmail = useCallback(async (userId: string, token: string) => {
    const res = await fetch(
      `${BASE}api/account/confirm-email?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`,
      { credentials: 'include' },
    );
    if (!res.ok) throw new Error(await parseJsonError(res));
    return res.json();
  }, []);

  function loginWithGoogle() {
    window.location.href = `${BASE}api/account/login/google?returnUrl=${encodeURIComponent(window.location.origin + BASE)}`;
  }

  function loginWithFacebook() {
    window.location.href = `${BASE}api/account/login/facebook?returnUrl=${encodeURIComponent(window.location.origin + BASE)}`;
  }

  return { user, providers, loading, refreshMe, register, login, logout, confirmEmail, loginWithGoogle, loginWithFacebook };
}
