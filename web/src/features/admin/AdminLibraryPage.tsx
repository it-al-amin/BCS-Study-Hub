import { Archive, Pencil, Plus, Search, Send, ShieldCheck } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { Table } from '../../components/ui/Table'
import type { PagedResult } from '../../services/api'
import { libraryApi, type LibraryResourceSummary } from '../../services/library'
import { categoryLabels, errorMessage, kindLabels, statusTone } from '../library/libraryFormat'
import { KindIcon } from '../library/libraryUi'

export function AdminLibraryPage() {
  const { accessToken } = useAuth()
  const [result, setResult] = useState<PagedResult<LibraryResourceSummary> | null>(null)
  const [search, setSearch] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [status, setStatus] = useState('')
  const [kind, setKind] = useState('')
  const [page, setPage] = useState(1)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const [reloadCount, setReloadCount] = useState(0)

  useEffect(() => {
    if (!accessToken) return
    let active = true
    libraryApi.getAdminResources({ search: appliedSearch || undefined, status, kind, pageNumber: page, pageSize: 25 }, accessToken)
      .then((value) => active && setResult(value))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load shared resources.')))
    return () => { active = false }
  }, [accessToken, appliedSearch, status, kind, page, reloadCount])

  async function changeStatus(resource: LibraryResourceSummary, action: 'publish' | 'archive') {
    if (!accessToken) return
    setError('')
    setMessage('')
    try {
      await libraryApi.setStatus(resource.id, action, accessToken)
      setMessage(`${resource.title} ${action === 'publish' ? 'published' : 'archived'}.`)
      setReloadCount((count) => count + 1)
    } catch (reason) {
      setError(errorMessage(reason, 'Status change failed.'))
    }
  }

  return (
    <>
      <PageHeader
        title="Library management"
        description="Manage the shared library: catalogue books, upload redistributable files, attach chapters and topics, then publish to learners."
        action={<Link to="/admin/library/new"><Button><Plus className="h-4 w-4" />New shared resource</Button></Link>}
      />
      <Card className="mb-5 flex gap-3 border-amber-200 bg-amber-50 p-4 text-sm text-amber-900 dark:border-amber-900/60 dark:bg-amber-950/30 dark:text-amber-200">
        <ShieldCheck className="mt-0.5 h-5 w-5 shrink-0" />
        <div><p className="font-medium">Copyright rules for shared files</p><p className="mt-1">Only upload files that are your own work, public domain, openly licensed, government publications released for public distribution, or shared with the rights holder’s permission, and record the licence or terms. Commercial BCS guide books can be catalogued as metadata or linked to the publisher, but not uploaded here. Learners’ private uploads are not visible to administrators.</p></div>
      </Card>
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      <Card className="overflow-hidden">
        <form className="flex flex-col gap-2 border-b border-slate-100 p-4 sm:flex-row dark:border-slate-800" onSubmit={(event) => { event.preventDefault(); setPage(1); setAppliedSearch(search.trim()) }}>
          <div className="relative flex-1"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><input aria-label="Search shared resources" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search title, author or source" className="h-10 w-full rounded-lg border border-slate-200 bg-white pl-9 pr-3 text-sm dark:border-slate-700 dark:bg-slate-800" /></div>
          <select aria-label="Status" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }} className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">All statuses</option><option value="Draft">Draft</option><option value="Published">Published</option><option value="Archived">Archived</option></select>
          <select aria-label="Kind" value={kind} onChange={(event) => { setKind(event.target.value); setPage(1) }} className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">All kinds</option>{Object.entries(kindLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>
          <Button type="submit" variant="secondary">Search</Button>
        </form>
        {!result ? <LoadingState label="Loading shared resources..." /> : result.items.length === 0 ? <EmptyState title="No shared resources" description="Nothing is seeded. Create the first shared resource to start the library." />
          : <Table headers={['Resource', 'Category', 'Files', 'Status', 'Updated', 'Actions']}>
            {result.items.map((resource) => <tr key={resource.id}>
              <td className="px-5 py-3"><div className="flex items-start gap-3"><span className="mt-0.5"><KindIcon resource={resource} /></span><div className="min-w-0"><Link to={`/admin/library/${resource.id}/edit`} className="font-medium hover:underline">{resource.title}</Link><p className="truncate text-xs text-slate-500">{[kindLabels[resource.kind], resource.authors, resource.sourceName].filter(Boolean).join(' · ')}</p></div></div></td>
              <td className="px-5 py-3 text-slate-500">{categoryLabels[resource.category]}</td>
              <td className="px-5 py-3 text-xs text-slate-500">{resource.primaryFormat ?? '—'}{resource.coverFileId ? ' + cover' : ''}{resource.chapterCount > 0 ? ` · ${resource.chapterCount} ch.` : ''}</td>
              <td className="px-5 py-3"><Badge tone={statusTone(resource.status)}>{resource.status}</Badge></td>
              <td className="px-5 py-3 text-xs text-slate-500">{new Date(resource.updatedAt).toLocaleDateString()}</td>
              <td className="px-5 py-3"><div className="flex gap-1"><Link to={`/admin/library/${resource.id}/edit`}><Button size="sm" variant="ghost" aria-label={`Edit ${resource.title}`}><Pencil className="h-4 w-4" /></Button></Link>{resource.status !== 'Published' && <Button size="sm" variant="ghost" aria-label={`Publish ${resource.title}`} onClick={() => void changeStatus(resource, 'publish')}><Send className="h-4 w-4" /></Button>}{resource.status !== 'Archived' && <Button size="sm" variant="ghost" aria-label={`Archive ${resource.title}`} onClick={() => void changeStatus(resource, 'archive')}><Archive className="h-4 w-4" /></Button>}</div></td>
            </tr>)}
          </Table>}
        {result && <Pagination page={result.pageNumber} totalPages={Math.max(result.totalPages, 1)} onPageChange={setPage} />}
      </Card>
    </>
  )
}
