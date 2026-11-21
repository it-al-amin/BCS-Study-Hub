import { ChevronLeft, ChevronRight, Printer } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { currentAffairsApi, currentAffairsCategories, type CurrentAffairsDigest, type DigestPeriod } from '../../services/currentAffairs'
import { categoryLabels, errorMessage, formatDate, periodLabels } from './currentAffairsFormat'
import { EntryCard } from './currentAffairsUi'

const periods: DigestPeriod[] = ['Day', 'Week', 'Month', 'Year']

/** Daily, weekly, monthly, and yearly collections. The layout doubles as a printable summary. */
export function DigestPage() {
  const { accessToken } = useAuth()
  const [params, setParams] = useSearchParams()
  const period = (periods.includes(params.get('period') as DigestPeriod) ? params.get('period') : 'Day') as DigestPeriod
  const date = params.get('date') ?? undefined
  const category = params.get('category') ?? undefined
  const key = useMemo(() => JSON.stringify({ period, date, category }), [period, date, category])
  const [loaded, setLoaded] = useState<{ key: string; digest: CurrentAffairsDigest | null; error: string } | null>(null)

  useEffect(() => {
    let active = true
    currentAffairsApi.getDigest(period, date, category, accessToken)
      .then((digest) => active && setLoaded({ key, digest, error: '' }))
      .catch((reason: unknown) => active && setLoaded({ key, digest: null, error: errorMessage(reason, 'Unable to load the collection.') }))
    return () => { active = false }
  }, [period, date, category, accessToken, key])

  function update(changes: Record<string, string | null>) {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([name, value]) => (value ? next.set(name, value) : next.delete(name)))
    setParams(next)
  }

  const current = loaded?.key === key ? loaded : null
  const digest = current?.digest ?? null

  return (
    <>
      <div className="mb-5 flex flex-col gap-3 print:hidden sm:flex-row sm:items-center sm:justify-between">
        <div className="flex rounded-lg border border-slate-200 bg-white p-1 dark:border-slate-700 dark:bg-slate-900">{periods.map((value) => <button key={value} type="button" onClick={() => update({ period: value })} className={`rounded-md px-3 py-1.5 text-sm ${value === period ? 'bg-emerald-700 text-white' : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'}`}>{periodLabels[value]}</button>)}</div>
        <div className="flex flex-wrap items-center gap-2">
          <select aria-label="Category" value={category ?? ''} onChange={(event) => update({ category: event.target.value || null })} className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-900"><option value="">All categories</option>{currentAffairsCategories.map((value) => <option key={value} value={value}>{categoryLabels[value]}</option>)}</select>
          <input type="date" aria-label="Jump to date" value={date ?? ''} onChange={(event) => update({ date: event.target.value || null })} className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-900" />
          <Button size="sm" variant="secondary" onClick={() => window.print()} disabled={!digest || digest.total === 0}><Printer className="h-4 w-4" />Print</Button>
        </div>
      </div>
      {!current ? <LoadingState label="Loading collection..." /> : current.error ? <ErrorState description={current.error} /> : digest && <>
        <header className="mb-5 flex items-center justify-between gap-3">
          <Button variant="ghost" size="sm" aria-label="Previous period" className="print:hidden" onClick={() => update({ date: digest.previousAnchor })}><ChevronLeft className="h-4 w-4" /></Button>
          <div className="text-center">
            <p className="text-xs uppercase tracking-wide text-slate-500">{periodLabels[digest.period]} current affairs{category ? ` · ${categoryLabels[category as keyof typeof categoryLabels]}` : ''}</p>
            <h1 className="text-2xl font-semibold tracking-tight">{digest.label}</h1>
            <p className="text-xs text-slate-500">{digest.from === digest.to ? formatDate(digest.from) : `${formatDate(digest.from)} – ${formatDate(digest.to)}`} · {digest.total} entr{digest.total === 1 ? 'y' : 'ies'}</p>
          </div>
          <Button variant="ghost" size="sm" aria-label="Next period" className="print:hidden" onClick={() => update({ date: digest.nextAnchor })}><ChevronRight className="h-4 w-4" /></Button>
        </header>
        {digest.total === 0 ? <Card><EmptyState title="Nothing published for this period" description="Use the arrows or the date picker to browse other periods, or open the archive." /></Card>
          : <div className="space-y-5">
            {digest.groups.map((group) => <Card key={group.category} className="overflow-hidden print:border-0 print:shadow-none">
              <h2 className="border-b border-slate-100 px-5 py-3 font-semibold dark:border-slate-800 print:px-0">{categoryLabels[group.category]} <span className="text-sm font-normal text-slate-500">({group.entries.length})</span></h2>
              <div className="divide-y divide-slate-100 px-5 dark:divide-slate-800 print:px-0">{group.entries.map((entry) => <EntryCard key={entry.id} entry={entry} compact />)}</div>
            </Card>)}
            {digest.total > digest.groups.reduce((sum, group) => sum + group.entries.length, 0) && <p className="text-xs text-slate-500">Showing the first 1,000 entries. Narrow by category for the rest.</p>}
          </div>}
        <p className="mt-6 hidden text-xs text-slate-500 print:block">Printed from BCS Study Hub. Each entry cites its original source; check the source before relying on figures.</p>
        <p className="mt-6 text-center text-xs print:hidden"><Link to="/current-affairs" className="text-slate-500 hover:underline">Back to all current affairs</Link></p>
      </>}
    </>
  )
}
