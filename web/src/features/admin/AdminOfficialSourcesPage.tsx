import { Archive, ExternalLink, Pencil, Plus, Send, ShieldCheck, Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Dialog } from '../../components/ui/Dialog'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { Table } from '../../components/ui/Table'
import { currentAffairsApi, officialSourceCategories, type OfficialSource, type OfficialSourceCategory, type OfficialSourceInput, type VerificationStatus } from '../../services/currentAffairs'
import { errorMessage, formatDate, isVerificationStale, sourceCategoryLabels, verificationTone } from '../current-affairs/currentAffairsFormat'

const fieldClass = 'mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950'
const labelClass = 'block text-xs font-medium text-slate-600 dark:text-slate-300'
const blank: OfficialSourceInput = { name: '', localizedName: null, category: 'Ministry', url: '', description: null, parentOrganization: null, isInternational: false, usageNotes: null, sortOrder: 0, tags: [] }

export function AdminOfficialSourcesPage() {
  const { accessToken } = useAuth()
  const [sources, setSources] = useState<OfficialSource[] | null>(null)
  const [status, setStatus] = useState('')
  const [verification, setVerification] = useState('')
  const [reload, setReload] = useState(0)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [editing, setEditing] = useState<{ id: string | null; input: OfficialSourceInput; tags: string } | null>(null)
  const [verifying, setVerifying] = useState<{ source: OfficialSource; status: Exclude<VerificationStatus, 'Unverified'>; note: string } | null>(null)

  useEffect(() => {
    if (!accessToken) return
    let active = true
    currentAffairsApi.getAdminSources({ status, verificationStatus: verification }, accessToken)
      .then((result) => active && setSources(result.items))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load the directory.')))
    return () => { active = false }
  }, [accessToken, status, verification, reload])

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

  async function saveSource() {
    if (!accessToken || !editing) return
    const input = { ...editing.input, tags: [...new Set(editing.tags.split(',').map((tag) => tag.trim()).filter(Boolean))] }
    const ok = await act(() => editing.id ? currentAffairsApi.updateSource(editing.id, input, accessToken) : currentAffairsApi.createSource(input, accessToken),
      editing.id ? 'Saved. If the URL changed, the entry was withdrawn until it is re-verified.' : 'Created as an unverified draft.')
    if (ok) setEditing(null)
  }

  async function saveVerification() {
    if (!accessToken || !verifying) return
    const ok = await act(() => currentAffairsApi.recordVerification(verifying.source.id, verifying.status, verifying.note.trim(), accessToken), 'Verification recorded.')
    if (ok) setVerifying(null)
  }

  function setInput<K extends keyof OfficialSourceInput>(key: K, value: OfficialSourceInput[K]) {
    setEditing((current) => current && { ...current, input: { ...current.input, [key]: value } })
  }

  if (!accessToken) return null

  return (
    <>
      <PageHeader
        title="Official resource directory"
        description="Each URL must be opened and checked in a browser, and the outcome recorded, before it can be published. The server never fetches these sites. Changing a URL or recording a failed check withdraws the entry from learners."
        action={<Button onClick={() => setEditing({ id: null, input: blank, tags: '' })}><Plus className="h-4 w-4" />Add source</Button>}
      />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      <Card className="overflow-hidden">
        <div className="flex flex-wrap gap-2 border-b border-slate-100 p-4 dark:border-slate-800">
          <select aria-label="Status" value={status} onChange={(event) => setStatus(event.target.value)} className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">All statuses</option><option value="Draft">Draft</option><option value="Published">Published</option><option value="Archived">Archived</option></select>
          <select aria-label="Verification" value={verification} onChange={(event) => setVerification(event.target.value)} className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">Any verification</option><option value="Verified">Verified</option><option value="Unverified">Unverified</option><option value="NeedsReview">Needs review</option><option value="Unreachable">Unreachable</option></select>
        </div>
        {!sources ? <LoadingState /> : sources.length === 0 ? <EmptyState title="No sources" description="Add an official source and verify its URL." />
          : <Table headers={['Source', 'Category', 'Verification', 'Status', 'Actions']}>
            {sources.map((source) => <tr key={source.id}>
              <td className="px-5 py-3"><p className="font-medium">{source.name}</p><a href={source.url} target="_blank" rel="noopener noreferrer nofollow" className="inline-flex items-center gap-1 break-all text-xs text-emerald-700 hover:underline dark:text-emerald-400"><ExternalLink className="h-3 w-3" />{source.url}</a>{!source.isHttps && <span className="ml-1"><Badge tone="warning">HTTP</Badge></span>}</td>
              <td className="px-5 py-3 text-xs text-slate-500">{sourceCategoryLabels[source.category]}</td>
              <td className="px-5 py-3 text-xs"><Badge tone={verificationTone[source.verificationStatus]}>{source.verificationStatus}</Badge>{source.lastVerifiedOn && <span className={`ml-1 ${isVerificationStale(source.lastVerifiedOn) ? 'text-amber-700' : 'text-slate-500'}`}>{formatDate(source.lastVerifiedOn)}</span>}{source.verificationNote && <p className="mt-1 max-w-xs text-slate-500">{source.verificationNote}</p>}</td>
              <td className="px-5 py-3"><Badge tone={source.status === 'Published' ? 'success' : source.status === 'Draft' ? 'warning' : 'neutral'}>{source.status}</Badge></td>
              <td className="px-5 py-3"><div className="flex gap-1">
                <Button size="sm" variant="ghost" aria-label={`Record verification for ${source.name}`} onClick={() => setVerifying({ source, status: 'Verified', note: '' })}><ShieldCheck className="h-4 w-4" /></Button>
                <Button size="sm" variant="ghost" aria-label={`Edit ${source.name}`} onClick={() => setEditing({ id: source.id, input: { name: source.name, localizedName: source.localizedName, category: source.category, url: source.url, description: source.description, parentOrganization: source.parentOrganization, isInternational: source.isInternational, usageNotes: source.usageNotes, sortOrder: source.sortOrder, tags: source.tags }, tags: source.tags.join(', ') })}><Pencil className="h-4 w-4" /></Button>
                {source.status !== 'Published' && <Button size="sm" variant="ghost" aria-label={`Publish ${source.name}`} onClick={() => void act(() => currentAffairsApi.setSourceStatus(source.id, 'publish', accessToken), `${source.name} published.`)}><Send className="h-4 w-4" /></Button>}
                {source.status !== 'Archived' && <Button size="sm" variant="ghost" aria-label={`Archive ${source.name}`} onClick={() => void act(() => currentAffairsApi.setSourceStatus(source.id, 'archive', accessToken), `${source.name} archived.`)}><Archive className="h-4 w-4" /></Button>}
                {source.status !== 'Published' && <Button size="sm" variant="ghost" aria-label={`Delete ${source.name}`} onClick={() => window.confirm(`Delete ${source.name}?`) && void act(() => currentAffairsApi.deleteSource(source.id, accessToken), 'Deleted.')}><Trash2 className="h-4 w-4" /></Button>}
              </div></td>
            </tr>)}
          </Table>}
      </Card>

      <Dialog open={editing !== null} title={editing?.id ? 'Edit source' : 'Add source'} onClose={() => setEditing(null)}>
        {editing && <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveSource() }}>
          <label className={labelClass}>Name<input required maxLength={200} className={fieldClass} value={editing.input.name} onChange={(event) => setInput('name', event.target.value)} /></label>
          <label className={labelClass}>Bangla name<input maxLength={200} className={fieldClass} value={editing.input.localizedName ?? ''} onChange={(event) => setInput('localizedName', event.target.value || null)} /></label>
          <div className="grid grid-cols-2 gap-2">
            <label className={labelClass}>Category<select className={fieldClass} value={editing.input.category} onChange={(event) => setInput('category', event.target.value as OfficialSourceCategory)}>{officialSourceCategories.map((value) => <option key={value} value={value}>{sourceCategoryLabels[value]}</option>)}</select></label>
            <label className={labelClass}>Sort order<input type="number" min={0} max={10000} className={fieldClass} value={editing.input.sortOrder} onChange={(event) => setInput('sortOrder', Number(event.target.value) || 0)} /></label>
          </div>
          <label className={labelClass}>Official URL<input type="url" required maxLength={512} className={fieldClass} value={editing.input.url} onChange={(event) => setInput('url', event.target.value)} /></label>
          {editing.id && <p className="text-xs text-amber-700 dark:text-amber-400">Changing the URL voids the current verification and unpublishes the entry.</p>}
          <label className={labelClass}>Parent organization<input maxLength={200} className={fieldClass} value={editing.input.parentOrganization ?? ''} onChange={(event) => setInput('parentOrganization', event.target.value || null)} /></label>
          <label className={labelClass}>Description<textarea maxLength={2000} rows={2} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={editing.input.description ?? ''} onChange={(event) => setInput('description', event.target.value || null)} /></label>
          <label className={labelClass}>Usage and terms notes<textarea maxLength={2000} rows={2} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={editing.input.usageNotes ?? ''} onChange={(event) => setInput('usageNotes', event.target.value || null)} /></label>
          <label className={labelClass}>Tags<input className={fieldClass} value={editing.tags} onChange={(event) => setEditing({ ...editing, tags: event.target.value })} /></label>
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={editing.input.isInternational} onChange={(event) => setInput('isInternational', event.target.checked)} />International organization</label>
          <div className="flex justify-end gap-2"><Button type="button" variant="ghost" onClick={() => setEditing(null)}>Cancel</Button><Button type="submit">Save</Button></div>
        </form>}
      </Dialog>

      <Dialog open={verifying !== null} title="Record manual verification" onClose={() => setVerifying(null)}>
        {verifying && <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void saveVerification() }}>
          <p className="text-sm">Open <a href={verifying.source.url} target="_blank" rel="noopener noreferrer nofollow" className="break-all text-emerald-700 hover:underline dark:text-emerald-400">{verifying.source.url}</a> in your browser and confirm that it is the official site of <strong>{verifying.source.name}</strong> (secure connection, organization named on the page, government or organization domain).</p>
          <label className={labelClass}>Outcome<select className={fieldClass} value={verifying.status} onChange={(event) => setVerifying({ ...verifying, status: event.target.value as typeof verifying.status })}><option value="Verified">Verified: official and working</option><option value="NeedsReview">Needs review: moved, unclear, or suspicious</option><option value="Unreachable">Unreachable: error or certificate problem</option></select></label>
          <label className={labelClass}>What you checked<textarea required minLength={3} maxLength={1000} rows={3} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={verifying.note} onChange={(event) => setVerifying({ ...verifying, note: event.target.value })} placeholder="e.g. Page title and footer identify the ministry; HTTPS certificate valid." /></label>
          <div className="flex justify-end gap-2"><Button type="button" variant="ghost" onClick={() => setVerifying(null)}>Cancel</Button><Button type="submit" disabled={verifying.note.trim().length < 3}>Record</Button></div>
        </form>}
      </Dialog>
    </>
  )
}
