export function LoadingState({ label = 'Loading...' }: { label?: string }) {
  return <div className="grid min-h-48 place-items-center text-sm text-slate-500" role="status">{label}</div>
}

export function EmptyState({ title, description }: { title: string; description: string }) {
  return <div className="grid min-h-48 place-items-center px-6 text-center"><div><div className="mx-auto mb-3 h-10 w-10 rounded-full bg-slate-100 dark:bg-slate-800" /><h3 className="font-medium text-slate-900 dark:text-white">{title}</h3><p className="mt-1 max-w-sm text-sm text-slate-500">{description}</p></div></div>
}

export function ErrorState({ title = 'Unable to load content', description = 'Please try again.' }: { title?: string; description?: string }) {
  return <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm dark:border-red-900/60 dark:bg-red-950/30"><strong className="text-red-800 dark:text-red-300">{title}</strong><p className="mt-1 text-red-700 dark:text-red-400">{description}</p></div>
}

export function Skeleton({ className = '' }: { className?: string }) {
  return <div className={`animate-pulse rounded-md bg-slate-200 dark:bg-slate-800 ${className}`} />
}
