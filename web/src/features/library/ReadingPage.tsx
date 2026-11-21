import { Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import type { PagedResult } from '../../services/api'
import { libraryApi, type LibraryBookmark, type LibraryProgress, type LibraryReadingStatus } from '../../services/library'
import { SignInRequired } from './LibraryPage'
import { errorMessage, kindLabels, readingStatusLabels } from './libraryFormat'

export function ReadingPage() {
  const { accessToken } = useAuth()
  const [status, setStatus] = useState<LibraryReadingStatus | ''>('')
  const [progressPage, setProgressPage] = useState(1)
  const [bookmarkPage, setBookmarkPage] = useState(1)
  const [progress, setProgress] = useState<PagedResult<LibraryProgress> | null>(null)
  const [bookmarks, setBookmarks] = useState<PagedResult<LibraryBookmark> | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    libraryApi.getProgressList({ status, pageNumber: progressPage, pageSize: 20 }, accessToken)
      .then((result) => active && setProgress(result))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load reading progress.')))
    return () => { active = false }
  }, [accessToken, status, progressPage])

  const [bookmarkReload, setBookmarkReload] = useState(0)
  const loadBookmarks = () => setBookmarkReload((count) => count + 1)

  useEffect(() => {
    if (!accessToken) return
    let active = true
    libraryApi.getBookmarks({ pageNumber: bookmarkPage, pageSize: 20 }, accessToken)
      .then((result) => active && setBookmarks(result))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load bookmarks.')))
    return () => { active = false }
  }, [accessToken, bookmarkPage, bookmarkReload])

  if (!accessToken) return <><PageHeader title="Reading & bookmarks" /><SignInRequired /></>

  return (
    <>
      <PageHeader title="Reading & bookmarks" description="Track reading progress across books and documents, and jump back to bookmarked pages and chapters." />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      <div className="grid gap-5 xl:grid-cols-2">
        <Card className="overflow-hidden">
          <CardHeader title="Reading progress" action={<select aria-label="Filter by status" value={status} onChange={(event) => { setStatus(event.target.value as LibraryReadingStatus | ''); setProgressPage(1) }} className="h-8 rounded-lg border border-slate-200 bg-white px-2 text-xs dark:border-slate-700 dark:bg-slate-900"><option value="">All statuses</option>{Object.entries(readingStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>} />
          {!progress ? <LoadingState /> : progress.items.length === 0 ? <EmptyState title="No reading progress yet" description="Record progress from any resource page." />
            : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{progress.items.map((item) => <li key={item.resourceId}><Link to={`/library/resources/${item.resourceId}${item.currentPage ? `?page=${item.currentPage}` : ''}`} className="block px-5 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/60"><div className="flex items-center justify-between gap-3"><span className="truncate text-sm font-medium">{item.resourceTitle}</span><span className="shrink-0 text-xs text-slate-500">{readingStatusLabels[item.status]} · {item.percentComplete}%</span></div><div className="mt-2 h-1.5 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-1.5 rounded-full bg-emerald-600" style={{ width: `${item.percentComplete}%` }} /></div><p className="mt-1 text-xs text-slate-500">{[kindLabels[item.resourceKind], item.currentChapterTitle, item.currentPage && `page ${item.currentPage}${item.totalPages ? ` of ${item.totalPages}` : ''}`, `updated ${new Date(item.updatedAt).toLocaleDateString()}`].filter(Boolean).join(' · ')}</p></Link></li>)}</ul>}
          {progress && <Pagination page={progress.pageNumber} totalPages={Math.max(progress.totalPages, 1)} onPageChange={setProgressPage} />}
        </Card>
        <Card className="overflow-hidden">
          <CardHeader title="Bookmarks" />
          {!bookmarks ? <LoadingState /> : bookmarks.items.length === 0 ? <EmptyState title="No bookmarks yet" description="Bookmark a resource, chapter or page from its detail page." />
            : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{bookmarks.items.map((bookmark) => <li key={bookmark.id} className="flex items-center gap-3 px-5 py-3"><Link to={`/library/resources/${bookmark.resourceId}${bookmark.page ? `?page=${bookmark.page}` : ''}`} className="min-w-0 flex-1"><span className="block truncate text-sm font-medium hover:underline">{bookmark.label ?? bookmark.resourceTitle}</span><span className="block truncate text-xs text-slate-500">{[bookmark.label ? bookmark.resourceTitle : null, bookmark.chapterTitle, bookmark.page && `p. ${bookmark.page}`, new Date(bookmark.createdAt).toLocaleDateString()].filter(Boolean).join(' · ')}</span></Link><button type="button" aria-label="Delete bookmark" className="text-slate-400 hover:text-red-600" onClick={() => void libraryApi.deleteBookmark(bookmark.id, accessToken).then(loadBookmarks)}><Trash2 className="h-4 w-4" /></button></li>)}</ul>}
          {bookmarks && <Pagination page={bookmarks.pageNumber} totalPages={Math.max(bookmarks.totalPages, 1)} onPageChange={setBookmarkPage} />}
        </Card>
      </div>
    </>
  )
}
