import { ApiClient } from './api'

export interface AuthUser {
  id: string
  email: string
  displayName: string
  roles: string[]
  permissions: string[]
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: AuthUser
}

class AuthApi extends ApiClient {
  login(email: string, password: string) {
    return this.post<AuthResponse>('/auth/login', { email, password })
  }

  register(email: string, password: string, displayName: string) {
    return this.post<AuthResponse>('/auth/register', { email, password, displayName })
  }
}

export const authApi = new AuthApi()
