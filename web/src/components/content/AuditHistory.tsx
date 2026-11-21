import { History } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { adminApi, type AuditEntry } from '../../services/admin'

/**
 * Change history for one record, from the server audit log. Shown only to holders of audit.read; the API enforces
 * the same permission, so hiding it here is a convenience, not the security boundary.
 */
export function AuditHistory({ entityType, entityId }: { entityType: string; entityId: string }) {
  const { accessToken, user } = useAuth()
  const allowed = user?.permissions.includes('audit.read') ?? false
  const [entries, setEntries] = useState<{ key: string; items: AuditEntry[] } | null>(null)
  const key = `${entityType}:${entityId}`

  useEffect(() => {
    if (!accessToken || !allowed) return
    let alive = true
    adminApi.getAudit({ entityType, entityId, pageSize: 50 }, accessToken)
      .then((result) => alive && setEntries({ key, items: result.items }))
      .catch(() => alive && setEntries({ key, items: [] }))
    return () => { alive = false }
  }, [accessToken, allowed, entityType, entityId, key])

  if (!allowed) return null
  const items = entries?.key === key ? entries.items : null
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900" aria-label="Change history">
      <h2 className="flex items-center gap-2 font-semibold"><History className="h-4 w-4" />Change history</h2>
      {!items ? <p className="mt-2 text-xs text-slate-500">Loading…</p> : items.length === 0 ? <p className="mt-2 text-xs text-slate-500">No recorded changes.</p>
        : <ol className="mt-3 space-y-2 border-l border-slate-200 pl-4 dark:border-slate-700">{items.map((item) => <li key={item.id} className="text-xs"><p className="font-medium text-slate-700 dark:text-slate-200">{item.action}</p><p className="text-slate-500">{new Date(item.occurredAt).toLocaleString()} · {item.actorName ?? item.actorEmail ?? 'System'}</p></li>)}</ol>}
    </section>
  )
}
