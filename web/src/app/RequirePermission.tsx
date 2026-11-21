import { Navigate, useLocation } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from './useAuth'

export function RequirePermission({ permission, children }: { permission: string; children: ReactNode }) {
  const { accessToken, user } = useAuth()
  const location = useLocation()
  if (!accessToken || !user) return <Navigate to="/login" replace state={{ from: location }} />
  if (!user.permissions.includes(permission)) {
    return (
      <div className="rounded-xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-900 dark:border-amber-900/60 dark:bg-amber-950/30 dark:text-amber-200">
        Your account does not have permission to open this area.
      </div>
    )
  }
  return children
}
