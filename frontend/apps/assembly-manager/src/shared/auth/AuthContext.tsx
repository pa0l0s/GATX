import { createContext, ReactNode, useContext, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  clearSession,
  getStoredUsername,
  getToken,
  setUnauthorizedHandler,
  storeSession
} from '../api/apiClient';
import { login as loginRequest } from '../api/authApi';

interface AuthContextValue {
  username: string | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const [token, setTokenState] = useState<string | null>(() => getToken());
  const [username, setUsername] = useState<string | null>(() => getStoredUsername());

  useEffect(() => {
    setUnauthorizedHandler(() => {
      setTokenState(null);
      setUsername(null);
      navigate('/login', { replace: true });
    });
    return () => setUnauthorizedHandler(null);
  }, [navigate]);

  const value = useMemo<AuthContextValue>(
    () => ({
      username,
      isAuthenticated: token !== null,
      async login(name: string, password: string) {
        const result = await loginRequest(name, password);
        storeSession(result.token, result.username);
        setTokenState(result.token);
        setUsername(result.username);
      },
      logout() {
        clearSession();
        setTokenState(null);
        setUsername(null);
        navigate('/login', { replace: true });
      }
    }),
    [token, username, navigate]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
