import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { AuthContext, type AuthContextValue } from './AuthContext'
import { authApi, type AuthResponse } from '../services/auth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthResponse | null>(null)

  useEffect(() => {
    if (!session) return
    const expiresIn = Math.max(0, Date.parse(session.expiresAt) - Date.now())
    const timeout = window.setTimeout(() => setSession(null), expiresIn)
    return () => window.clearTimeout(timeout)
  }, [session])

  const value = useMemo<AuthContextValue>(() => ({
    accessToken: session?.accessToken ?? null,
    user: session?.user ?? null,
    login: async (email, password) => setSession(await authApi.login(email, password)),
    register: async (email, password, displayName) => setSession(await authApi.register(email, password, displayName)),
    signOut: () => setSession(null),
  }), [session])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
