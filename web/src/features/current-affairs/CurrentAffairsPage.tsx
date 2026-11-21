import { Bookmark, CalendarDays, Landmark, RotateCcw, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import type { PagedResult } from '../../services/api'
import { currentAffairsApi, type CurrentAffairsCategory, type CurrentAffairsQuery, type CurrentAffairsSummary } from '../../services/currentAffairs'
import { categoryLabels, errorMessage, monthName, monthRange, periodLabels } from './currentAffairsFormat'
import { EntryCard } from './currentAffairsUi'

const inputClass = 'h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800'

export function CurrentAffairsPage() {
  const { accessToken } = useAuth()
  const [params, setParams] = useSearchParams()
  const [searchDraft, setSearchDraft] = useState(params.get('search') ?? '')
  const [result, setResult] = useState<PagedResult<CurrentAffairsSummary> | null>(null)
  const [loadedKey, setLoadedKey] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [categories, setCategories] = useState<{ category: CurrentAffairsCategory; count: number }[]>([])
  const [archive, setArchive] = useState<{ year: number; month: number; count: number }[]>([])
  const [tags, setTags] = useState<string[]>([])

  const query = useMemo<CurrentAffairsQuery>(() => ({
    search: params.get('search') ?? undefined,
    category: params.get('category') ?? undefined,
    from: params.get('from') ?? undefined,
    to: params.get('to') ?? undefined,
    tag: params.get('tag') ?? undefined,
    subjectId: params.get('subjectId') ?? undefined,
    syllabusItemId: params.get('syllabusItemId') ?? undefined,
    bookmarkedOnly: params.get('bookmarkedOnly') === 'true' && Boolean(accessToken),
    sortBy: (params.get('sortBy') as CurrentAffairsQuery['sortBy']) ?? 'recent',
    pageNumber: Number(params.get('page') ?? '1') || 1,
    pageSize: 20,
  }), [params, accessToken])
  const queryKey = JSON.stringify(query)

  useEffect(() => {
    let active = true
    Promise.all([currentAffairsApi.getCategories(), currentAffairsApi.getArchive(), currentAffairsApi.getTags()])
      .then(([categoryResult, archiveResult, tagResult]) => {
        if (!active) return
        setCategories(categoryResult)
        setArchive(archiveResult)
        setTags(tagResult)
      })
      .catch(() => undefined)
    return () => { active = false }
  }, [])

  useEffect(() => {
    let active = true
    currentAffairsApi.getEntries(query, accessToken)
      .then((value) => {
        if (!active) return
        setResult(value)
        setError('')
        setLoadedKey(queryKey)
      })
      .catch((reason: unknown) => {
        if (!active) return
        setError(errorMessage(reason, 'Unable to load current affairs.'))
        setLoadedKey(queryKey)
      })
    return () => { active = false }
  }, [query, queryKey, accessToken])

  function update(changes: Record<string, string | null>) {
    const next = new URLSearchParams(params)
    Object.entries(changes).forEach(([key, value]) => (value ? next.set(key, value) : next.delete(key)))
    if (!('page' in changes)) next.delete('page')
    setParams(next)
  }

  const years = [...new Set(archive.map((item) => item.year))]
  const filterCount = ['search', 'category', 'from', 'to', 'tag', 'subjectId', 'syllabusItemId', 'bookmarkedOnly'].filter((key) => params.get(key)).length
  const loading = loadedKey !== queryKey

  return (
    <>
      <PageHeader
        title="Current Affairs"
        description="Curated, source-cited current affairs mapped to the BCS syllabus. Entries are written by editors from official and reputable sources; nothing is scraped."
        action={<div className="flex flex-wrap gap-2">
          <Link to="/current-affairs/digest?period=Day"><Button variant="secondary"><CalendarDays className="h-4 w-4" />Today</Button></Link>
          {accessToken && <Link to="/current-affairs/revision"><Button variant="secondary"><RotateCcw className="h-4 w-4" />Revision</Button></Link>}
          <Link to="/official-sources"><Button variant="secondary"><Landmark className="h-4 w-4" />Official sources</Button></Link>
        </div>}
      />
      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_18rem]">
        <div className="min-w-0 space-y-5">
          <Card className="p-4">
            <form className="flex gap-2" onSubmit={(event) => { event.preventDefault(); update({ search: searchDraft.trim() || null }) }}>
              <div className="relative flex-1"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><input aria-label="Search current affairs" value={searchDraft} onChange={(event) => setSearchDraft(event.target.value)} placeholder="Search titles, summaries, sources or an exact tag" className={`${inputClass} w-full pl-9`} /></div>
              <Button type="submit" variant="secondary" disabled={searchDraft.trim().length === 1}>Search</Button>
            </form>
            <div className="mt-3 grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
              <select aria-label="Category" className={inputClass} value={query.category ?? ''} onChange={(event) => update({ category: event.target.value || null })}><option value="">All categories</option>{categories.map((item) => <option key={item.category} value={item.category}>{categoryLabels[item.category]} ({item.count})</option>)}</select>
              <label className="flex items-center gap-1 text-xs text-slate-500">From<input type="date" aria-label="From date" className={`${inputClass} w-full`} value={query.from ?? ''} onChange={(event) => update({ from: event.target.value || null })} /></label>
              <label className="flex items-center gap-1 text-xs text-slate-500">To<input type="date" aria-label="To date" className={`${inputClass} w-full`} value={query.to ?? ''} onChange={(event) => update({ to: event.target.value || null })} /></label>
              <select aria-label="Tag" className={inputClass} value={query.tag ?? ''} onChange={(event) => update({ tag: event.target.value || null })}><option value="">Any tag</option>{tags.map((tag) => <option key={tag} value={tag}>{tag}</option>)}</select>
              <select aria-label="Sort" className={inputClass} value={query.sortBy} onChange={(event) => update({ sortBy: event.target.value === 'recent' ? null : event.target.value })}><option value="recent">Newest first</option><option value="oldest">Oldest first</option><option value="updated">Recently updated</option></select>
            </div>
            <div className="mt-3 flex flex-wrap items-center gap-3 text-xs">
              {accessToken && <label className="flex items-center gap-1.5"><input type="checkbox" checked={query.bookmarkedOnly} onChange={(event) => update({ bookmarkedOnly: event.target.checked ? 'true' : null })} /><Bookmark className="h-3.5 w-3.5" />Bookmarked only</label>}
              {params.get('syllabusItemId') && <span className="rounded-full bg-sky-50 px-2 py-0.5 text-sky-800 dark:bg-sky-500/10 dark:text-sky-300">Filtered by syllabus topic</span>}
              {params.get('subjectId') && <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-emerald-800 dark:bg-emerald-500/10 dark:text-emerald-300">Filtered by subject</span>}
              {filterCount > 0 && <button type="button" className="inline-flex items-center gap-1 text-slate-500 hover:text-slate-800 dark:hover:text-slate-200" onClick={() => { setSearchDraft(''); setParams(new URLSearchParams()) }}><X className="h-3 w-3" />Clear filters</button>}
            </div>
          </Card>
          {error && <ErrorState description={error} />}
          <Card className="overflow-hidden">
            {loading && !result ? <LoadingState label="Loading current affairs..." />
              : !result || result.items.length === 0 ? <EmptyState title="No entries found" description={filterCount > 0 ? 'Try widening the date range or clearing filters.' : 'No current-affairs entries have been published yet. Editors add verified, source-cited entries from the admin area.'} />
                : <>
                  <div className={`divide-y divide-slate-100 px-5 dark:divide-slate-800 ${loading ? 'opacity-60' : ''}`}>{result.items.map((entry) => <EntryCard key={entry.id} entry={entry} />)}</div>
                  <Pagination page={result.pageNumber} totalPages={Math.max(result.totalPages, 1)} onPageChange={(page) => update({ page: String(page) })} />
                </>}
          </Card>
        </div>
        <aside className="space-y-5">
          <Card className="p-5">
            <h2 className="font-semibold">Collections</h2>
            <div className="mt-3 grid grid-cols-2 gap-2">{(['Day', 'Week', 'Month', 'Year'] as const).map((period) => <Link key={period} to={`/current-affairs/digest?period=${period}`} className="rounded-lg border border-slate-200 px-3 py-2 text-center text-sm hover:border-emerald-600 dark:border-slate-700">{periodLabels[period]}</Link>)}</div>
            <p className="mt-2 text-xs text-slate-500">Printable summaries grouped by category. Weeks run Monday to Sunday.</p>
          </Card>
          <Card className="overflow-hidden">
            <CardHeader title="Archive" subtitle="Published entries by event month" />
            {years.length === 0 ? <p className="p-5 text-sm text-slate-500">No archive yet.</p>
              : <div className="max-h-[60vh] overflow-auto p-3">{years.map((year) => <div key={year} className="mb-3"><Link to={`/current-affairs/digest?period=Year&date=${year}-01-01`} className="px-2 text-sm font-semibold hover:underline">{year}</Link><ul className="mt-1">{archive.filter((item) => item.year === year).map((item) => {
                const range = monthRange(item.year, item.month)
                const active = query.from === range.from && query.to === range.to
                return <li key={item.month}><button type="button" onClick={() => update(range)} className={`flex w-full justify-between rounded px-2 py-1 text-sm ${active ? 'bg-emerald-50 text-emerald-800 dark:bg-emerald-500/10 dark:text-emerald-300' : 'hover:bg-slate-50 dark:hover:bg-slate-800'}`}><span>{monthName(item.month)}</span><span className="text-xs text-slate-500">{item.count}</span></button></li>
              })}</ul></div>)}</div>}
          </Card>
        </aside>
      </div>
    </>
  )
}
