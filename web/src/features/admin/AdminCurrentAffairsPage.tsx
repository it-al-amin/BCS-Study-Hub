import { Pencil, Plus, Search } from 'lucide-react'
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
import { currentAffairsApi, currentAffairsCategories, type CurrentAffairsSummary } from '../../services/currentAffairs'
import { categoryLabels, errorMessage, formatDate } from '../current-affairs/currentAffairsFormat'

export function AdminCurrentAffairsPage() {
  const { accessToken } = useAuth()
  const [result, setResult] = useState<PagedResult<CurrentAffairsSummary> | null>(null)
  const [search, setSearch] = useState('')
  const [applied, setApplied] = useState('')
  const [status, setStatus] = useState('')
  const [category, setCategory] = useState('')
  const [page, setPage] = useState(1)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    currentAffairsApi.getAdminEntries({ search: applied || undefined, status, category, pageNumber: page }, accessToken)
      .then((value) => active && setResult(value))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load entries.')))
    return () => { active = false }
  }, [accessToken, applied, status, category, page])

  return (
    <>
      <PageHeader
        title="Current affairs management"
        description="Curate entries from official and reputable sources. Write summaries in your own words, cite the source, and confirm the source before publishing. Automated scraping is not used."
        action={<div className="flex gap-2"><Link to="/admin/official-sources"><Button variant="secondary">Official directory</Button></Link><Link to="/admin/current-affairs/new"><Button><Plus className="h-4 w-4" />New entry</Button></Link></div>}
      />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      <Card className="overflow-hidden">
        <form className="flex flex-col gap-2 border-b border-slate-100 p-4 sm:flex-row dark:border-slate-800" onSubmit={(event) => { event.preventDefault(); setPage(1); setApplied(search.trim()) }}>
          <div className="relative flex-1"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><input aria-label="Search entries" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search title, summary or source" className="h-10 w-full rounded-lg border border-slate-200 bg-white pl-9 pr-3 text-sm dark:border-slate-700 dark:bg-slate-800" /></div>
          <select aria-label="Status" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1) }} className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">All statuses</option><option value="Draft">Draft</option><option value="Published">Published</option><option value="Archived">Archived</option></select>
          <select aria-label="Category" value={category} onChange={(event) => { setCategory(event.target.value); setPage(1) }} className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">All categories</option>{currentAffairsCategories.map((value) => <option key={value} value={value}>{categoryLabels[value]}</option>)}</select>
          <Button type="submit" variant="secondary">Search</Button>
        </form>
        {!result ? <LoadingState /> : result.items.length === 0 ? <EmptyState title="No entries" description="Nothing is seeded. Create the first curated entry." />
          : <Table headers={['Entry', 'Category', 'Event date', 'Source', 'Status', '']}>
            {result.items.map((entry) => <tr key={entry.id}>
              <td className="px-5 py-3"><Link to={`/admin/current-affairs/${entry.id}/edit`} className="font-medium hover:underline">{entry.title}</Link></td>
              <td className="px-5 py-3 text-slate-500">{categoryLabels[entry.category]}</td>
              <td className="px-5 py-3 text-slate-500">{formatDate(entry.eventDate)}</td>
              <td className="px-5 py-3 text-xs"><span className="text-slate-600 dark:text-slate-300">{entry.sourceName}</span> <Badge tone={entry.sourceStatus === 'Verified' ? 'success' : 'warning'}>{entry.sourceStatus}</Badge></td>
              <td className="px-5 py-3"><Badge tone={entry.status === 'Published' ? 'success' : entry.status === 'Draft' ? 'warning' : 'neutral'}>{entry.status}</Badge></td>
              <td className="px-5 py-3"><Link to={`/admin/current-affairs/${entry.id}/edit`}><Button size="sm" variant="ghost" aria-label={`Edit ${entry.title}`}><Pencil className="h-4 w-4" /></Button></Link></td>
            </tr>)}
          </Table>}
        {result && <Pagination page={result.pageNumber} totalPages={Math.max(result.totalPages, 1)} onPageChange={setPage} />}
      </Card>
    </>
  )
}
