import { createContext } from 'react';

export type AuthUser = {
  userName: string;
  role: string;
  tenantId: string;
  tenantName: string;
};

export type AuthContextValue = {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (token: string, profile: AuthUser) => void;
  logout: () => void;
};

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);
