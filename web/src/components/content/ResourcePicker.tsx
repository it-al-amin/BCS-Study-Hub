import { Search, X } from 'lucide-react'
import { useState } from 'react'
import { Button } from '../ui/Button'

export interface PickedResource { id: string; label: string }
export interface ResourceOption { id: string; title: string; detail?: string | null }

/**
 * Reusable picker for attaching library resources (or any searchable records) to content. The caller supplies the
 * search function, so each module keeps its own server-side permission and visibility rules.
 */
export function ResourcePicker({
  title,
  description,
  value,
  onChange,
  search,
  max = 20,
}: {
  title: string
  description?: string
  value: PickedResource[]
  onChange: (value: PickedResource[]) => void
  search: (term: string) => Promise<ResourceOption[]>
  max?: number
}) {
  const [term, setTerm] = useState('')
  const [results, setResults] = useState<ResourceOption[] | null>(null)
  const [error, setError] = useState('')

  async function find() {
    setError('')
    try {
      setResults(await search(term.trim()))
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Search failed.')
    }
  }

  return (
    <div className="space-y-3">
      <div><h3 className="text-sm font-semibold">{title}</h3>{description && <p className="text-xs text-slate-500">{description}</p>}</div>
      <ul className="space-y-1">{value.map((item) => <li key={item.id} className="flex items-center gap-2 rounded-lg bg-slate-50 px-2 py-1 text-sm dark:bg-slate-800/60"><span className="min-w-0 flex-1 truncate">{item.label}</span><button type="button" aria-label={`Remove ${item.label}`} className="text-slate-400 hover:text-red-600" onClick={() => onChange(value.filter((picked) => picked.id !== item.id))}><X className="h-4 w-4" /></button></li>)}{value.length === 0 && <li className="text-xs text-slate-500">Nothing attached.</li>}</ul>
      <div className="flex gap-2"><input aria-label={`Search ${title}`} placeholder="Search by title" className="h-9 min-w-0 flex-1 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={term} onChange={(event) => setTerm(event.target.value)} onKeyDown={(event) => { if (event.key === 'Enter') { event.preventDefault(); void find() } }} /><Button type="button" size="sm" variant="secondary" onClick={() => void find()} aria-label="Search"><Search className="h-4 w-4" /></Button></div>
      {error && <p className="text-xs text-red-600">{error}</p>}
      {results && (results.length === 0 ? <p className="text-xs text-slate-500">No matches.</p> : <ul className="max-h-48 space-y-1 overflow-auto">{results.map((result) => {
        const added = value.some((item) => item.id === result.id)
        return <li key={result.id}><button type="button" disabled={added || value.length >= max} onClick={() => onChange([...value, { id: result.id, label: result.title }])} className="w-full rounded px-2 py-1 text-left text-sm hover:bg-slate-50 disabled:opacity-50 dark:hover:bg-slate-800">{result.title}{result.detail && <span className="ml-1 text-xs text-slate-500">{result.detail}</span>}{added && <span className="ml-1 text-xs text-emerald-700">attached</span>}</button></li>
      })}</ul>)}
    </div>
  )
}
