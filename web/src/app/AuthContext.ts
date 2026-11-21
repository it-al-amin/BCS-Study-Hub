import { createContext } from 'react'
import type { AuthUser } from '../services/auth'

export interface AuthContextValue {
  accessToken: string | null
  user: AuthUser | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  signOut: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
