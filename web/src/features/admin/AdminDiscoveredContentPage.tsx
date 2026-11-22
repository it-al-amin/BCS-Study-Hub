import { ExternalLink } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Card } from '../../components/ui/Card'
import { Dialog } from '../../components/ui/Dialog'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { Table } from '../../components/ui/Table'
import { discoveredContentApi, type DiscoveredContent, type DiscoveredContentDetail } from '../../services/contentSources'
import { errorMessage } from '../current-affairs/currentAffairsFormat'

const statuses = ['PendingReview', 'Discovered', 'Updated', 'Verified', 'Published', 'Rejected', 'Archived', 'Duplicate', 'Unavailable', 'FailedProcessing']
const dateTime = (value: string | null) => (value ? new Date(value).toLocaleString('en-GB', { timeZone: 'Asia/Dhaka', dateStyle: 'medium', timeStyle: 'short' }) : '')

export function AdminDiscoveredContentPage() {
  const { accessToken } = useAuth()
  const [items, setItems] = useState<DiscoveredContent[] | null>(null)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [error, setError] = useState('')
  const [detail, setDetail] = useState<DiscoveredContentDetail | null>(null)

  useEffect(() => {
    if (!accessToken) return
    let active = true
    discoveredContentApi.list({ search, status }, accessToken)
      .then((result) => active && setItems(result.items))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load discovered content.')))
    return () => { active = false }
  }, [accessToken, search, status])

  async function open(id: string) {
    if (!accessToken) return
    try {
      setDetail(await discoveredContentApi.get_(id, accessToken))
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to load the item.'))
    }
  }

  if (!accessToken) return null

  return (
    <>
      <PageHeader
        title="Discovered content"
        description="Items found by source scans. Everything here is private and unreviewed. Only what the source's retention policy allows is stored: a link and title, a short excerpt, or full text."
      />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      <Card className="overflow-hidden">
        <div className="flex flex-wrap gap-2 border-b border-slate-100 p-4 dark:border-slate-800">
          <input aria-label="Search" placeholder="Search title or URL" value={search} onChange={(event) => setSearch(event.target.value)} className="h-9 w-56 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800" />
          <select aria-label="Status" value={status} onChange={(event) => setStatus(event.target.value)} className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">All statuses</option>{statuses.map((value) => <option key={value}>{value}</option>)}</select>
        </div>
        {!items ? <LoadingState /> : items.length === 0 ? <EmptyState title="Nothing discovered yet" description="Enable a source and queue a scan from Content sources." />
          : <Table headers={['Title', 'Source', 'Status', 'Found']}>
            {items.map((item) => <tr key={item.id} className="cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-900" onClick={() => void open(item.id)}>
              <td className="px-5 py-3"><p className="font-medium">{item.title}</p><p className="break-all text-xs text-slate-500">{item.canonicalUrl}</p></td>
              <td className="px-5 py-3 text-xs text-slate-500">{item.sourceName}</td>
              <td className="px-5 py-3 text-xs"><Badge tone={item.status === 'PendingReview' ? 'warning' : item.status === 'Duplicate' ? 'neutral' : 'info'}>{item.status}</Badge><span className="ml-1"><Badge tone="neutral">{item.visibility}</Badge></span>{item.version > 1 && <p className="mt-1 text-slate-500">version {item.version}</p>}</td>
              <td className="px-5 py-3 text-xs text-slate-500">{dateTime(item.discoveredAt)}</td>
            </tr>)}
          </Table>}
      </Card>

      <Dialog open={detail !== null} title={detail?.content.title ?? ''} onClose={() => setDetail(null)}>
        {detail && <div className="space-y-3 text-sm">
          <a href={detail.content.canonicalUrl} target="_blank" rel="noopener noreferrer nofollow" className="inline-flex items-center gap-1 break-all text-emerald-700 hover:underline dark:text-emerald-400"><ExternalLink className="h-3 w-3" />{detail.content.canonicalUrl}</a>
          <p className="text-xs text-slate-500">{detail.content.sourceName} · {detail.content.kind} · {detail.content.language ?? 'language unknown'} · found {dateTime(detail.content.discoveredAt)}</p>
          {detail.content.excerpt && <p className="whitespace-pre-wrap rounded-lg bg-slate-50 p-3 dark:bg-slate-900">{detail.content.excerpt}</p>}
          {detail.textContent && <details><summary className="cursor-pointer text-xs font-medium">Stored text</summary><p className="mt-2 max-h-64 overflow-auto whitespace-pre-wrap text-xs">{detail.textContent}</p></details>}
          <div><p className="text-xs font-medium">Versions</p><ul className="mt-1 space-y-1 text-xs text-slate-500">{detail.versions.map((version) => <li key={version.version}>v{version.version} · {dateTime(version.capturedAt)} · {version.title}</li>)}</ul></div>
        </div>}
      </Dialog>
    </>
  )
}
