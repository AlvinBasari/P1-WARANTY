import React, { createContext, useContext, useState, useEffect } from 'react';
import api from '../services/api';

export interface User {
  id: number;
  name: string;
  email: string;
  phone?: string;
  address?: string;
  role: 'admin' | 'technician' | 'customer';
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (token: string, user: User) => void;
  logout: () => void;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(() => {
    try {
      const saved = localStorage.getItem('auth_user');
      if (!saved || saved === 'undefined' || saved === 'null') return null;
      return JSON.parse(saved);
    } catch (err) {
      console.warn('Gagal membaca data auth_user dari localStorage, membersihkan cache:', err);
      localStorage.removeItem('auth_user');
      return null;
    }
  });
  const [token, setToken] = useState<string | null>(() => {
    const saved = localStorage.getItem('auth_token');
    return saved && saved !== 'undefined' && saved !== 'null' ? saved : null;
  });
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    if (token) {
      api.get('/auth/me')
        .then((res) => {
          setUser(res.data.user);
          localStorage.setItem('auth_user', JSON.stringify(res.data.user));
        })
        .catch(() => {
          logout();
        })
        .finally(() => setIsLoading(false));
    } else {
      setIsLoading(false);
    }
  }, [token]);

  const login = (newToken: string, newUser: User) => {
    localStorage.setItem('auth_token', newToken);
    localStorage.setItem('auth_user', JSON.stringify(newUser));
    setToken(newToken);
    setUser(newUser);
  };

  const logout = () => {
    if (token) {
      api.post('/auth/logout').catch(() => {});
    }
    localStorage.removeItem('auth_token');
    localStorage.removeItem('auth_user');
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, token, login, logout, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
};
