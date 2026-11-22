import { Archive, ArchiveRestore, ExternalLink, History, Pencil, Play, Plus, Power, Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Dialog } from '../../components/ui/Dialog'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { Table } from '../../components/ui/Table'
import {
  contentSourcesApi, extractionMethods, retentionPolicies, sourceTypes,
  type ContentSource, type ContentSourceInput, type ContentSourceScan, type ContentSourceSummary, type SourceHealth,
} from '../../services/contentSources'
import { errorMessage } from '../current-affairs/currentAffairsFormat'

const fieldClass = 'mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950'
const areaClass = 'mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950'
const labelClass = 'block text-xs font-medium text-slate-600 dark:text-slate-300'
const blank: ContentSourceInput = {
  name: '', localizedName: null, sourceType: 'Website', extractionMethod: 'ManualOnly', baseUrl: '', feedUrl: null, sitemapUrl: null,
  category: 'General', language: 'bn', priority: 100, reliability: 3, scanIntervalMinutes: 1440, retentionPolicy: 'LinkAndMetadataOnly',
  includePatterns: null, excludePatterns: null, requiresVerification: true, rightsNote: null, notes: null,
}
const healthTone: Record<SourceHealth, 'neutral' | 'success' | 'warning' | 'danger'> = { NeverScanned: 'neutral', Healthy: 'success', NeedsAttention: 'warning', Failing: 'danger' }
const scanTone = { Queued: 'neutral', Running: 'info', Succeeded: 'success', PartiallySucceeded: 'warning', Failed: 'danger', Cancelled: 'neutral' } as const
const dateTime = (value: string | null) => (value ? new Date(value).toLocaleString('en-GB', { timeZone: 'Asia/Dhaka', dateStyle: 'medium', timeStyle: 'short' }) : '')

function toInput(source: ContentSource): ContentSourceInput {
  const { id: _id, isEnabled: _e, status: _s, health: _h, lastScanAt: _l, lastSuccessAt: _ls, lastError: _le, consecutiveFailures: _c, createdAt: _ca, updatedAt: _u, ...input } = source
  return input
}

export function AdminContentSourcesPage() {
  const { accessToken } = useAuth()
  const [sources, setSources] = useState<ContentSource[] | null>(null)
  const [summary, setSummary] = useState<ContentSourceSummary | null>(null)
  const [scans, setScans] = useState<ContentSourceScan[] | null>(null)
  const [scanTitle, setScanTitle] = useState('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('Active')
  const [reload, setReload] = useState(0)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [editing, setEditing] = useState<{ id: string | null; input: ContentSourceInput } | null>(null)

  useEffect(() => {
    if (!accessToken) return
    let active = true
    Promise.all([contentSourcesApi.list({ search, status }, accessToken), contentSourcesApi.summary(accessToken)])
      .then(([list, stats]) => { if (active) { setSources(list.items); setSummary(stats) } })
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load content sources.')))
    return () => { active = false }
  }, [accessToken, search, status, reload])

  async function act(action: () => Promise<unknown>, success: string) {
    setError('')
    setMessage('')
    try {
      await action()
      setMessage(success)
      setReload((count) => count + 1)
      return true
    } catch (reason) {
      setError(errorMessage(reason, 'The action could not be completed.'))
      return false
    }
  }

  async function showScans(source?: ContentSource) {
    if (!accessToken) return
    setScans(null)
    setScanTitle(source ? `Scan history: ${source.name}` : 'Scan history: all sources')
    try {
      setScans((await contentSourcesApi.scans({ sourceId: source?.id }, accessToken)).items)
    } catch (reason) {
      setScans([])
      setError(errorMessage(reason, 'Unable to load scan history.'))
    }
  }

  async function save() {
    if (!accessToken || !editing) return
    const ok = await act(
      () => (editing.id ? contentSourcesApi.update(editing.id, editing.input, accessToken) : contentSourcesApi.create(editing.input, accessToken)),
      editing.id ? 'Source saved.' : 'Source created. It is disabled until you enable it.',
    )
    if (ok) setEditing(null)
  }

  function setInput<K extends keyof ContentSourceInput>(key: K, value: ContentSourceInput[K]) {
    setEditing((current) => current && { ...current, input: { ...current.input, [key]: value } })
  }
  const text = (key: 'localizedName' | 'feedUrl' | 'sitemapUrl' | 'includePatterns' | 'excludePatterns' | 'rightsNote' | 'notes', value: string) => setInput(key, value || null)

  if (!accessToken) return null

  return (
    <>
      <PageHeader
        title="Content sources"
        description="Websites, feeds, sitemaps and documents the platform may monitor. New sources start disabled. Only HTTPS public hosts are accepted, and a source's retention policy limits what may be kept. Automatic scanning is added in a later phase; a manual scan is recorded as queued."
        action={<div className="flex gap-2"><Button variant="ghost" onClick={() => void showScans()}><History className="h-4 w-4" />Scan history</Button><Button onClick={() => setEditing({ id: null, input: blank })}><Plus className="h-4 w-4" />Add source</Button></div>}
      />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      {summary && <div className="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-5">
        {[['Total', summary.total], ['Enabled', summary.enabled], ['Disabled', summary.disabled], ['Need attention', summary.needsAttention], ['Queued scans', summary.queuedScans]].map(([label, value]) =>
          <Card key={label} className="p-4"><p className="text-xs text-slate-500">{label}</p><p className="text-2xl font-semibold">{value}</p></Card>)}
      </div>}
      <Card className="overflow-hidden">
        <div className="flex flex-wrap gap-2 border-b border-slate-100 p-4 dark:border-slate-800">
          <input aria-label="Search sources" placeholder="Search name or URL" value={search} onChange={(event) => setSearch(event.target.value)} className="h-9 w-56 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800" />
          <select aria-label="Status" value={status} onChange={(event) => setStatus(event.target.value)} className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="Active">Active</option><option value="Archived">Archived</option><option value="">All</option></select>
        </div>
        {!sources ? <LoadingState /> : sources.length === 0 ? <EmptyState title="No sources" description="Add a source to begin." />
          : <Table headers={['Source', 'Method', 'Health', 'Enabled', 'Actions']}>
            {sources.map((source) => <tr key={source.id}>
              <td className="px-5 py-3"><p className="font-medium">{source.name}{source.localizedName && <span className="ml-2 text-slate-500">{source.localizedName}</span>}</p><a href={source.baseUrl} target="_blank" rel="noopener noreferrer nofollow" className="inline-flex items-center gap-1 break-all text-xs text-emerald-700 hover:underline dark:text-emerald-400"><ExternalLink className="h-3 w-3" />{source.baseUrl}</a><p className="text-xs text-slate-500">{source.category} · {source.language} · priority {source.priority} · reliability {source.reliability}/5</p></td>
              <td className="px-5 py-3 text-xs text-slate-500">{source.extractionMethod}<br />{source.retentionPolicy}<br />every {source.scanIntervalMinutes >= 1440 ? `${Math.round(source.scanIntervalMinutes / 1440)} day(s)` : `${source.scanIntervalMinutes} min`}</td>
              <td className="px-5 py-3 text-xs"><Badge tone={healthTone[source.health]}>{source.health}</Badge>{source.lastScanAt && <p className="mt-1 text-slate-500">Last scan {dateTime(source.lastScanAt)}</p>}{source.lastError && <p className="mt-1 max-w-xs text-red-700 dark:text-red-400">{source.lastError}</p>}</td>
              <td className="px-5 py-3"><Badge tone={source.isEnabled ? 'success' : 'neutral'}>{source.isEnabled ? 'On' : 'Off'}</Badge>{source.status === 'Archived' && <span className="ml-1"><Badge tone="warning">Archived</Badge></span>}</td>
              <td className="px-5 py-3"><div className="flex gap-1">
                {source.status === 'Active' && <Button size="sm" variant="ghost" aria-label={`${source.isEnabled ? 'Disable' : 'Enable'} ${source.name}`} onClick={() => void act(() => contentSourcesApi.action(source.id, source.isEnabled ? 'disable' : 'enable', accessToken), source.isEnabled ? 'Disabled.' : 'Enabled.')}><Power className="h-4 w-4" /></Button>}
                {source.status === 'Active' && <Button size="sm" variant="ghost" aria-label={`Queue scan for ${source.name}`} onClick={() => void act(() => contentSourcesApi.requestScan(source.id, accessToken), 'Scan queued.')}><Play className="h-4 w-4" /></Button>}
                <Button size="sm" variant="ghost" aria-label={`Scan history for ${source.name}`} onClick={() => void showScans(source)}><History className="h-4 w-4" /></Button>
                <Button size="sm" variant="ghost" aria-label={`Edit ${source.name}`} onClick={() => setEditing({ id: source.id, input: toInput(source) })}><Pencil className="h-4 w-4" /></Button>
                {source.status === 'Active' ? <Button size="sm" variant="ghost" aria-label={`Archive ${source.name}`} onClick={() => void act(() => contentSourcesApi.action(source.id, 'archive', accessToken), 'Archived.')}><Archive className="h-4 w-4" /></Button>
                  : <><Button size="sm" variant="ghost" aria-label={`Restore ${source.name}`} onClick={() => void act(() => contentSourcesApi.action(source.id, 'restore', accessToken), 'Restored.')}><ArchiveRestore className="h-4 w-4" /></Button>
                    <Button size="sm" variant="ghost" aria-label={`Delete ${source.name}`} onClick={() => window.confirm(`Delete ${source.name} and its scan history?`) && void act(() => contentSourcesApi.remove(source.id, accessToken), 'Deleted.')}><Trash2 className="h-4 w-4" /></Button></>}
              </div></td>
            </tr>)}
          </Table>}
      </Card>

      <Dialog open={editing !== null} title={editing?.id ? 'Edit source' : 'Add source'} onClose={() => setEditing(null)}>
        {editing && <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void save() }}>
          <label className={labelClass}>Name<input required minLength={2} maxLength={200} className={fieldClass} value={editing.input.name} onChange={(event) => setInput('name', event.target.value)} /></label>
          <label className={labelClass}>Bangla name<input maxLength={200} className={fieldClass} value={editing.input.localizedName ?? ''} onChange={(event) => text('localizedName', event.target.value)} /></label>
          <label className={labelClass}>Base URL (https only)<input type="url" required maxLength={512} className={fieldClass} value={editing.input.baseUrl} onChange={(event) => setInput('baseUrl', event.target.value)} /></label>
          <div className="grid grid-cols-2 gap-2">
            <label className={labelClass}>Source type<select className={fieldClass} value={editing.input.sourceType} onChange={(event) => setInput('sourceType', event.target.value as ContentSourceInput['sourceType'])}>{sourceTypes.map((value) => <option key={value}>{value}</option>)}</select></label>
            <label className={labelClass}>Extraction method<select className={fieldClass} value={editing.input.extractionMethod} onChange={(event) => setInput('extractionMethod', event.target.value as ContentSourceInput['extractionMethod'])}>{extractionMethods.map((value) => <option key={value}>{value}</option>)}</select></label>
            <label className={labelClass}>Feed URL<input type="url" maxLength={512} className={fieldClass} value={editing.input.feedUrl ?? ''} onChange={(event) => text('feedUrl', event.target.value)} /></label>
            <label className={labelClass}>Sitemap URL<input type="url" maxLength={512} className={fieldClass} value={editing.input.sitemapUrl ?? ''} onChange={(event) => text('sitemapUrl', event.target.value)} /></label>
            <label className={labelClass}>Category<input required minLength={2} maxLength={60} className={fieldClass} value={editing.input.category} onChange={(event) => setInput('category', event.target.value)} /></label>
            <label className={labelClass}>Language<select className={fieldClass} value={editing.input.language} onChange={(event) => setInput('language', event.target.value as ContentSourceInput['language'])}><option value="bn">Bangla</option><option value="en">English</option><option value="mixed">Mixed</option></select></label>
            <label className={labelClass}>Priority (1 first)<input type="number" min={1} max={1000} className={fieldClass} value={editing.input.priority} onChange={(event) => setInput('priority', Number(event.target.value) || 100)} /></label>
            <label className={labelClass}>Reliability (1-5)<input type="number" min={1} max={5} className={fieldClass} value={editing.input.reliability} onChange={(event) => setInput('reliability', Number(event.target.value) || 3)} /></label>
            <label className={labelClass}>Scan every (minutes)<input type="number" min={15} max={43200} className={fieldClass} value={editing.input.scanIntervalMinutes} onChange={(event) => setInput('scanIntervalMinutes', Number(event.target.value) || 1440)} /></label>
            <label className={labelClass}>Retention<select className={fieldClass} value={editing.input.retentionPolicy} onChange={(event) => setInput('retentionPolicy', event.target.value as ContentSourceInput['retentionPolicy'])}>{retentionPolicies.map((value) => <option key={value}>{value}</option>)}</select></label>
          </div>
          <label className={labelClass}>Include URL patterns (one per line)<textarea rows={2} maxLength={2000} className={areaClass} value={editing.input.includePatterns ?? ''} onChange={(event) => text('includePatterns', event.target.value)} /></label>
          <label className={labelClass}>Exclude URL patterns (one per line)<textarea rows={2} maxLength={2000} className={areaClass} value={editing.input.excludePatterns ?? ''} onChange={(event) => text('excludePatterns', event.target.value)} /></label>
          <label className={labelClass}>Rights note (required to keep full text)<textarea rows={2} maxLength={1000} className={areaClass} value={editing.input.rightsNote ?? ''} onChange={(event) => text('rightsNote', event.target.value)} /></label>
          <label className={labelClass}>Notes<textarea rows={2} maxLength={2000} className={areaClass} value={editing.input.notes ?? ''} onChange={(event) => text('notes', event.target.value)} /></label>
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={editing.input.requiresVerification} onChange={(event) => setInput('requiresVerification', event.target.checked)} />Collected content needs manual verification</label>
          <div className="flex justify-end gap-2"><Button type="button" variant="ghost" onClick={() => setEditing(null)}>Cancel</Button><Button type="submit">Save</Button></div>
        </form>}
      </Dialog>

      <Dialog open={scanTitle !== ''} title={scanTitle} onClose={() => { setScanTitle(''); setScans(null) }}>
        {!scans ? <LoadingState /> : scans.length === 0 ? <EmptyState title="No scans yet" description="Queue a scan to see it here." />
          : <Table headers={['Source', 'Status', 'Requested', 'Result']}>
            {scans.map((scan) => <tr key={scan.id}>
              <td className="px-3 py-2 text-xs">{scan.sourceName}<br /><span className="text-slate-500">{scan.trigger}</span></td>
              <td className="px-3 py-2"><Badge tone={scanTone[scan.status]}>{scan.status}</Badge></td>
              <td className="px-3 py-2 text-xs text-slate-500">{dateTime(scan.requestedAt)}</td>
              <td className="px-3 py-2 text-xs">{scan.discoveredCount} new · {scan.updatedCount} updated · {scan.failedCount} failed{scan.errorMessage && <p className="text-red-700 dark:text-red-400">{scan.errorMessage}</p>}
                {(scan.status === 'Failed' || scan.status === 'PartiallySucceeded' || scan.status === 'Cancelled') && <Button size="sm" variant="ghost" onClick={() => void act(() => contentSourcesApi.retryScan(scan.id, accessToken), 'Retry queued.').then(() => setScanTitle(''))}>Retry</Button>}</td>
            </tr>)}
          </Table>}
      </Dialog>
    </>
  )
}
