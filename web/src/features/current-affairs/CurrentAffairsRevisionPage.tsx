import { Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import type { PagedResult } from '../../services/api'
import { currentAffairsApi, type CurrentAffairsNote, type CurrentAffairsRevision, type RevisionOutcome } from '../../services/currentAffairs'
import { categoryLabels, errorMessage, formatDate } from './currentAffairsFormat'

type Tab = 'due' | 'all' | 'bookmarks' | 'notes'
type BookmarkRow = { entryId: string; title: string; category: keyof typeof categoryLabels; eventDate: string; createdAt: string }

export function CurrentAffairsRevisionPage() {
  const { accessToken } = useAuth()
  const [tab, setTab] = useState<Tab>('due')
  const [page, setPage] = useState(1)
  const [reload, setReload] = useState(0)
  const [loaded, setLoaded] = useState<{ key: string; revisions?: PagedResult<CurrentAffairsRevision>; bookmarks?: PagedResult<BookmarkRow>; notes?: PagedResult<CurrentAffairsNote> } | null>(null)
  const [error, setError] = useState('')
  const key = `${tab}:${page}:${reload}`

  useEffect(() => {
    if (!accessToken) return
    let active = true
    const request = tab === 'bookmarks'
      ? currentAffairsApi.getBookmarks(page, accessToken).then((bookmarks) => ({ key, bookmarks }))
      : tab === 'notes'
        ? currentAffairsApi.getNotes(page, accessToken).then((notes) => ({ key, notes }))
        : currentAffairsApi.getRevisions(tab === 'due', page, accessToken).then((revisions) => ({ key, revisions }))
    request
      .then((value) => active && setLoaded(value))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load your revision data.')))
    return () => { active = false }
  }, [accessToken, tab, page, key])

  async function act(action: () => Promise<unknown>) {
    setError('')
    try {
      await action()
      setReload((count) => count + 1)
    } catch (reason) {
      setError(errorMessage(reason, 'The action could not be completed.'))
    }
  }

  if (!accessToken) return <><PageHeader title="Current affairs revision" /><Card><EmptyState title="Sign in to revise" description="Revision schedules, bookmarks, and notes are private to your account." /></Card></>

  const current = loaded?.key === key ? loaded : null
  const tabs: [Tab, string][] = [['due', 'Due now'], ['all', 'All in revision'], ['bookmarks', 'Bookmarks'], ['notes', 'Study notes']]

  return (
    <>
      <PageHeader title="Current affairs revision" description="Spaced revision: review an entry and rate your recall. Intervals follow your own revision settings, shared with the planner revision queue." />
      <div className="mb-4 flex flex-wrap gap-1">{tabs.map(([value, label]) => <button key={value} type="button" onClick={() => { setTab(value); setPage(1) }} className={`rounded-lg px-3 py-1.5 text-sm ${tab === value ? 'bg-emerald-700 text-white' : 'border border-slate-200 bg-white hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:hover:bg-slate-800'}`}>{label}</button>)}</div>
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      <Card className="overflow-hidden">
        <CardHeader title={tabs.find(([value]) => value === tab)?.[1] ?? ''} />
        {!current ? <LoadingState /> : <>
          {current.revisions && (current.revisions.items.length === 0
            ? <EmptyState title={tab === 'due' ? 'Nothing due' : 'No entries in revision'} description="Add entries to revision from their detail page." />
            : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{current.revisions.items.map((item) => <li key={item.entryId} className="flex flex-col gap-2 px-5 py-3 sm:flex-row sm:items-center">
              <div className="min-w-0 flex-1"><Link to={`/current-affairs/${item.entryId}`} className="font-medium hover:underline">{item.entryTitle}</Link><p className="text-xs text-slate-500">{categoryLabels[item.category]} · {formatDate(item.eventDate)} · {item.isDue ? <span className="font-medium text-amber-700 dark:text-amber-400">due</span> : `next ${formatDate(item.nextReviewOn)}`} · reviewed {item.reviewCount}×{item.lastOutcome ? ` (last: ${item.lastOutcome})` : ''}</p></div>
              <div className="flex items-center gap-1">{(['Again', 'Hard', 'Good', 'Easy'] as RevisionOutcome[]).map((outcome) => <Button key={outcome} size="sm" variant={outcome === 'Good' ? 'primary' : 'secondary'} onClick={() => void act(() => currentAffairsApi.review(item.entryId, outcome, accessToken))}>{outcome}</Button>)}<button type="button" aria-label="Remove from revision" className="ml-1 text-slate-400 hover:text-red-600" onClick={() => void act(() => currentAffairsApi.removeRevision(item.entryId, accessToken))}><Trash2 className="h-4 w-4" /></button></div>
            </li>)}</ul>)}
          {current.bookmarks && (current.bookmarks.items.length === 0
            ? <EmptyState title="No bookmarks" description="Bookmark entries to keep them close." />
            : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{current.bookmarks.items.map((item) => <li key={item.entryId} className="flex items-center gap-3 px-5 py-3"><div className="min-w-0 flex-1"><Link to={`/current-affairs/${item.entryId}`} className="font-medium hover:underline">{item.title}</Link><p className="text-xs text-slate-500"><Badge tone="info">{categoryLabels[item.category]}</Badge> {formatDate(item.eventDate)}</p></div><button type="button" aria-label="Remove bookmark" className="text-slate-400 hover:text-red-600" onClick={() => void act(() => currentAffairsApi.removeBookmark(item.entryId, accessToken))}><Trash2 className="h-4 w-4" /></button></li>)}</ul>)}
          {current.notes && (current.notes.items.length === 0
            ? <EmptyState title="No study notes" description="Write a note on any entry's page." />
            : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{current.notes.items.map((item) => <li key={item.entryId} className="px-5 py-3"><Link to={`/current-affairs/${item.entryId}`} className="font-medium hover:underline">{item.entryTitle}</Link><p className="text-xs text-slate-500">{formatDate(item.eventDate)} · updated {new Date(item.updatedAt).toLocaleDateString()}</p><p className="mt-1 line-clamp-3 whitespace-pre-wrap text-sm text-slate-600 dark:text-slate-300">{item.content}</p></li>)}</ul>)}
          {(() => {
            const paged = current.revisions ?? current.bookmarks ?? current.notes
            return paged ? <Pagination page={paged.pageNumber} totalPages={Math.max(paged.totalPages, 1)} onPageChange={setPage} /> : null
          })()}
        </>}
      </Card>
    </>
  )
}
