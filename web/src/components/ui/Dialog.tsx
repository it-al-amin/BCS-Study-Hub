import type { ReactNode } from 'react'
import { X } from 'lucide-react'

export function Dialog({ open, title, children, onClose }: { open: boolean; title: string; children: ReactNode; onClose: () => void }) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-[60] grid place-items-center p-4">
      <button aria-label="Close dialog" className="absolute inset-0 bg-slate-950/50" onClick={onClose} />
      <div role="dialog" aria-modal="true" aria-label={title} className="relative w-full max-w-lg rounded-xl border border-slate-200 bg-white shadow-2xl dark:border-slate-700 dark:bg-slate-900">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4 dark:border-slate-800"><h2 className="font-semibold">{title}</h2><button aria-label="Close" onClick={onClose} className="rounded-md p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"><X className="h-5 w-5" /></button></div>
        <div className="p-5">{children}</div>
      </div>
    </div>
  )
}
