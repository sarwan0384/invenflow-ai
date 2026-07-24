import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { AuthContext, type AuthUser } from './authContextInstance';

function getInitialAuthUser() {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const stored = window.localStorage.getItem('invenflow-auth');
    if (!stored) {
      return null;
    }

    const parsed = JSON.parse(stored) as { token: string; profile: AuthUser };
    return parsed.profile ?? null;
  } catch {
    window.localStorage.removeItem('invenflow-auth');
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(getInitialAuthUser);

  const value = useMemo(() => ({
    user,
    isAuthenticated: Boolean(user),
    login: (token: string, profile: AuthUser) => {
      localStorage.setItem('invenflow-auth', JSON.stringify({ token, profile }));
      setUser(profile);
    },
    logout: () => {
      localStorage.removeItem('invenflow-auth');
      setUser(null);
    },
  }), [user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
