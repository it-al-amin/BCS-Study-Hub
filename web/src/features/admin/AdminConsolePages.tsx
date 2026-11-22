import { AlertTriangle, Archive, ArrowRight, BookOpen, CircleHelp, FileClock, Globe, Landmark, Library, Mic2, Newspaper, PenTool, Save, Send, Settings, ShieldCheck, Tags, Users } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { Table } from '../../components/ui/Table'
import type { PagedResult } from '../../services/api'
import { adminApi, type AdminOverview, type AuditEntry, type BulkModule, type BulkResult, type SiteSettings, type TagModule, type TagUsage } from '../../services/admin'
import { currentAffairsApi, currentAffairsCategories } from '../../services/currentAffairs'
import { libraryApi } from '../../services/library'
import { mcqApi } from '../../services/mcq'
import { vivaApi } from '../../services/viva'
import { writtenApi } from '../../services/written'
import { categoryLabels as caCategoryLabels, sourceCategoryLabels } from '../current-affairs/currentAffairsFormat'
import { categoryLabels as libraryCategoryLabels } from '../library/libraryFormat'

const message = (reason: unknown, fallback: string) => (reason instanceof Error ? reason.message : fallback)

function usePermissions() {
  const { user } = useAuth()
  const permissions = new Set(user?.permissions ?? [])
  return (code: string) => permissions.has(code)
}

// ---------- Dashboard ----------

const moduleCards = [
  { title: 'Syllabus', detail: 'Exam stages, versions, subjects, papers, topics, subtopics, and sources', href: '/admin/syllabus', permission: 'syllabus.manage', icon: BookOpen },
  { title: 'MCQ', detail: 'Questions, options, explanations, sources, and exam sets', href: '/admin/mcq', permission: 'mcq.manage', icon: CircleHelp },
  { title: 'Written', detail: 'Questions, model answers, and references', href: '/admin/written', permission: 'written.manage', icon: PenTool },
  { title: 'Viva', detail: 'Categories and questions', href: '/admin/viva', permission: 'viva.manage', icon: Mic2 },
  { title: 'Library', detail: 'Books, chapters, study notes, PDFs, images, files, and links', href: '/admin/library', permission: 'library.manage', icon: Library },
  { title: 'Current affairs', detail: 'Curated entries, references, and topic mapping', href: '/admin/current-affairs', permission: 'currentaffairs.manage', icon: Newspaper },
  { title: 'Content sources', detail: 'Monitored websites, feeds, retention rules, and scan history', href: '/admin/content-sources', permission: 'sources.manage', icon: Globe },
  { title: 'Discovered content', detail: 'Items found by scans, private until reviewed', href: '/admin/discovered-content', permission: 'sources.manage', icon: Newspaper },
  { title: 'Official sources', detail: 'Verified official website directory', href: '/admin/official-sources', permission: 'currentaffairs.manage', icon: Landmark },
  { title: 'Publishing', detail: 'Bulk publish or archive drafts across modules', href: '/admin/publishing', permission: 'content.manage', icon: Send },
  { title: 'Tags', detail: 'Rename, merge, and remove tags across modules', href: '/admin/tags', permission: 'content.manage', icon: Tags },
  { title: 'Categories', detail: 'All category systems and where they are managed', href: '/admin/categories', permission: 'content.manage', icon: BookOpen },
  { title: 'Audit history', detail: 'Who changed what, and when', href: '/admin/audit', permission: 'audit.read', icon: FileClock },
  { title: 'Site settings', detail: 'Name, announcement banner, registration, and footer', href: '/admin/settings', permission: 'settings.manage', icon: Settings },
] as const

export function AdminPage() {
  const { accessToken } = useAuth()
  const can = usePermissions()
  const [overview, setOverview] = useState<AdminOverview | null>(null)
  const [error, setError] = useState('')
  const contentAllowed = can('content.manage')

  useEffect(() => {
    if (!accessToken || !contentAllowed) return
    let alive = true
    adminApi.getOverview(accessToken).then((result) => alive && setOverview(result)).catch((reason: unknown) => alive && setError(message(reason, 'Unable to load the overview.')))
    return () => { alive = false }
  }, [accessToken, contentAllowed])

  const attention = overview?.modules.filter((module) => module.attention > 0) ?? []
  return (
    <>
      <PageHeader title="Administration" description="Manage all study content from one place. Every action here is authorized and audited on the server; you only see the areas your permissions cover." />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {overview && <div className="mb-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Card className="p-4"><p className="text-xs uppercase tracking-wide text-slate-500">Items in draft</p><p className="mt-1 text-2xl font-semibold tabular-nums">{overview.modules.reduce((sum, module) => sum + module.draft, 0)}</p></Card>
        <Card className="p-4"><p className="text-xs uppercase tracking-wide text-slate-500">Published</p><p className="mt-1 text-2xl font-semibold tabular-nums">{overview.modules.reduce((sum, module) => sum + module.published, 0)}</p></Card>
        <Card className="p-4"><p className="text-xs uppercase tracking-wide text-slate-500">Needs attention</p><p className="mt-1 text-2xl font-semibold tabular-nums">{attention.reduce((sum, module) => sum + module.attention, 0)}</p></Card>
        <Card className="p-4"><p className="flex items-center gap-1 text-xs uppercase tracking-wide text-slate-500"><Users className="h-3.5 w-3.5" />Accounts</p><p className="mt-1 text-2xl font-semibold tabular-nums">{overview.users}</p><p className="text-xs text-slate-500">{overview.newUsersLast30Days} new in 30 days</p></Card>
      </div>}
      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
        <div className="space-y-5">
          {overview && <Card className="overflow-hidden">
            <CardHeader title="Content status" />
            <Table headers={['Module', 'Draft', 'Published', 'Archived', 'Attention']}>{overview.modules.map((module) => <tr key={module.module}>
              <td className="px-5 py-2.5"><Link to={module.managePath} className="font-medium hover:underline">{module.label}</Link></td>
              <td className="px-5 py-2.5 tabular-nums">{module.draft}</td><td className="px-5 py-2.5 tabular-nums">{module.published}</td><td className="px-5 py-2.5 tabular-nums">{module.archived}</td>
              <td className="px-5 py-2.5 text-xs">{module.attention > 0 ? <span className="inline-flex items-center gap-1 text-amber-700 dark:text-amber-400"><AlertTriangle className="h-3.5 w-3.5" />{module.attention} {module.attentionLabel}</span> : <span className="text-slate-400">—</span>}</td>
            </tr>)}</Table>
          </Card>}
          <div className="grid gap-4 sm:grid-cols-2">{moduleCards.filter((card) => can(card.permission)).map((card) => <Link key={card.title} to={card.href} className="group"><Card className="h-full p-4 transition group-hover:border-emerald-600"><div className="flex items-start gap-3"><div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-slate-100 dark:bg-slate-800"><card.icon className="h-4 w-4" /></div><div className="min-w-0"><p className="flex items-center gap-1 font-medium">{card.title}<ArrowRight className="h-3.5 w-3.5 opacity-0 transition group-hover:opacity-100" /></p><p className="text-xs text-slate-500">{card.detail}</p></div></div></Card></Link>)}</div>
        </div>
        <Card className="h-fit overflow-hidden">
          <CardHeader title="Recent administrative activity" action={can('audit.read') ? <Link to="/admin/audit" className="text-sm text-emerald-700 hover:underline dark:text-emerald-400">All history</Link> : undefined} />
          {!can('audit.read') ? <p className="px-5 py-6 text-sm text-slate-500">Audit history requires the audit permission.</p>
            : !overview ? <LoadingState /> : overview.recentActivity.length === 0 ? <p className="px-5 py-6 text-sm text-slate-500">No activity yet.</p>
              : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{overview.recentActivity.map((entry) => <li key={entry.id} className="px-5 py-2.5 text-xs"><p className="font-medium text-slate-700 dark:text-slate-200">{entry.action}</p><p className="text-slate-500">{entry.actorName ?? entry.actorEmail ?? 'System'} · {new Date(entry.occurredAt).toLocaleString()}</p></li>)}</ul>}
        </Card>
      </div>
    </>
  )
}

// ---------- Publishing (bulk) ----------

interface Row { id: string; title: string; status: string }
const bulkModules: { module: BulkModule; label: string; permission: string; load: (token: string) => Promise<Row[]> }[] = [
  { module: 'McqQuestions', label: 'MCQ questions', permission: 'mcq.manage', load: async (token) => (await mcqApi.getAdminQuestions({ status: 'Draft', pageSize: 100 }, token)).items.map((item) => ({ id: item.id, title: item.prompt, status: item.status })) },
  { module: 'McqExamSets', label: 'MCQ exam sets', permission: 'mcq.manage', load: async (token) => (await mcqApi.getAdminExamSets({ pageSize: 100 }, token)).items.filter((item) => item.status === 'Draft').map((item) => ({ id: item.id, title: item.name, status: item.status })) },
  { module: 'WrittenQuestions', label: 'Written questions', permission: 'written.manage', load: async (token) => (await writtenApi.getAdminQuestions({ status: 'Draft', pageSize: 100 }, token)).items.map((item) => ({ id: item.question.id, title: item.question.prompt, status: item.question.status })) },
  { module: 'VivaQuestions', label: 'Viva questions', permission: 'viva.manage', load: async (token) => (await vivaApi.getAdminQuestions({ status: 'Draft', pageSize: 100 }, token)).items.map((item) => ({ id: item.question.id, title: item.question.prompt, status: item.status })) },
  { module: 'LibraryResources', label: 'Shared library', permission: 'library.manage', load: async (token) => (await libraryApi.getAdminResources({ status: 'Draft', pageSize: 100 }, token)).items.map((item) => ({ id: item.id, title: item.title, status: item.status })) },
  { module: 'CurrentAffairs', label: 'Current affairs', permission: 'currentaffairs.manage', load: async (token) => (await currentAffairsApi.getAdminEntries({ status: 'Draft' }, token)).items.map((item) => ({ id: item.id, title: item.title, status: item.status })) },
  { module: 'OfficialSources', label: 'Official sources', permission: 'currentaffairs.manage', load: async (token) => (await currentAffairsApi.getAdminSources({ status: 'Draft' }, token)).items.map((item) => ({ id: item.id, title: item.name, status: item.status })) },
]

export function AdminPublishingPage() {
  const { accessToken } = useAuth()
  const can = usePermissions()
  const available = bulkModules.filter((module) => can(module.permission))
  const [module, setModule] = useState<BulkModule | ''>(available[0]?.module ?? '')
  const [rows, setRows] = useState<{ module: string; items: Row[] } | null>(null)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [result, setResult] = useState<BulkResult | null>(null)
  const [reload, setReload] = useState(0)
  const [error, setError] = useState('')
  const config = bulkModules.find((item) => item.module === module)

  useEffect(() => {
    if (!accessToken || !config) return
    let alive = true
    config.load(accessToken)
      .then((items) => { if (alive) { setRows({ module: config.module, items }); setSelected(new Set()) } })
      .catch((reason: unknown) => alive && setError(message(reason, 'Unable to load drafts.')))
    return () => { alive = false }
  }, [accessToken, config, reload])

  async function run(action: 'Publish' | 'Archive') {
    if (!accessToken || !module || selected.size === 0) return
    if (!window.confirm(`${action} ${selected.size} item(s)? Each item is validated individually.`)) return
    setError('')
    try {
      setResult(await adminApi.bulkStatus({ module, action, ids: [...selected] }, accessToken))
      setReload((count) => count + 1)
    } catch (reason) {
      setError(message(reason, 'Bulk action failed.'))
    }
  }

  const items = rows?.module === module ? rows.items : null
  return (
    <>
      <PageHeader title="Publishing" description="Review drafts and publish or archive them in bulk (up to 100 at a time). Each item goes through the same validation as publishing it individually, such as verified sources and rights checks; failures are listed and do not block the others." />
      {available.length === 0 ? <Card><EmptyState title="No content permissions" description="You do not manage any publishable module." /></Card> : <>
        <div className="mb-4 flex flex-wrap items-center gap-2">
          <select aria-label="Module" value={module} onChange={(event) => { setModule(event.target.value as BulkModule); setResult(null) }} className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-900">{available.map((item) => <option key={item.module} value={item.module}>{item.label}</option>)}</select>
          <Button size="sm" disabled={selected.size === 0} onClick={() => void run('Publish')}><Send className="h-4 w-4" />Publish {selected.size || ''}</Button>
          <Button size="sm" variant="secondary" disabled={selected.size === 0} onClick={() => void run('Archive')}><Archive className="h-4 w-4" />Archive {selected.size || ''}</Button>
        </div>
        {error && <div className="mb-4"><ErrorState description={error} /></div>}
        {result && <Card className="mb-4 p-4 text-sm"><p><strong>{result.succeeded}</strong> {result.action === 'Published' ? 'published' : 'archived'}, <strong>{result.failed}</strong> failed.</p>{result.items.filter((item) => !item.succeeded).length > 0 && <ul className="mt-2 space-y-1 text-xs text-red-700 dark:text-red-400">{result.items.filter((item) => !item.succeeded).map((item) => <li key={item.id}>{rows?.items.find((row) => row.id === item.id)?.title.slice(0, 80) ?? item.id}: {item.error}</li>)}</ul>}</Card>}
        <Card className="overflow-hidden">
          {!items ? <LoadingState /> : items.length === 0 ? <EmptyState title="No drafts" description="Everything in this module is published or archived." />
            : <Table headers={['', 'Title', 'Status']}>
              <tr><td className="px-5 py-2"><input type="checkbox" aria-label="Select all" checked={selected.size === items.length} onChange={(event) => setSelected(event.target.checked ? new Set(items.slice(0, 100).map((item) => item.id)) : new Set())} /></td><td className="px-5 py-2 text-xs text-slate-500" colSpan={2}>{items.length} draft(s)</td></tr>
              {items.map((item) => <tr key={item.id}><td className="px-5 py-2"><input type="checkbox" aria-label={`Select ${item.title}`} checked={selected.has(item.id)} onChange={(event) => setSelected((current) => { const next = new Set(current); if (event.target.checked) next.add(item.id); else next.delete(item.id); return next })} /></td><td className="max-w-xl truncate px-5 py-2">{item.title}</td><td className="px-5 py-2"><Badge tone="warning">{item.status}</Badge></td></tr>)}
            </Table>}
        </Card>
      </>}
    </>
  )
}

// ---------- Tags ----------

const tagModuleLabels: Record<TagModule, string> = { Mcq: 'MCQ', Written: 'Written', Library: 'Library', CurrentAffairs: 'Current affairs', OfficialSources: 'Official sources' }

export function AdminTagsPage() {
  const { accessToken } = useAuth()
  const [module, setModule] = useState<TagModule | ''>('')
  const [search, setSearch] = useState('')
  const [tags, setTags] = useState<TagUsage[] | null>(null)
  const [reload, setReload] = useState(0)
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let alive = true
    adminApi.getTags({ module, search: search.trim() || undefined }, accessToken).then((result) => alive && setTags(result)).catch((reason: unknown) => alive && setError(message(reason, 'Unable to load tags.')))
    return () => { alive = false }
  }, [accessToken, module, search, reload])

  async function rename(tag: string) {
    if (!accessToken) return
    const to = window.prompt(`Rename "${tag}" to (an existing tag merges):`, tag)?.trim()
    if (!to || to === tag) return
    try {
      const result = await adminApi.renameTag({ module: module || null, from: tag, to }, accessToken)
      setNotice(`Renamed on ${result.updated} item(s); merged into an existing tag on ${result.merged}.`)
      setReload((count) => count + 1)
    } catch (reason) {
      setError(message(reason, 'Rename failed.'))
    }
  }

  async function remove(tag: string) {
    if (!accessToken || !window.confirm(`Remove "${tag}" from every item${module ? ` in ${tagModuleLabels[module]}` : ''} you manage?`)) return
    try {
      const result = await adminApi.deleteTag({ module: module || null, tag }, accessToken)
      setNotice(`Removed from ${result.removed} item(s).`)
      setReload((count) => count + 1)
    } catch (reason) {
      setError(message(reason, 'Delete failed.'))
    }
  }

  return (
    <>
      <PageHeader title="Tags" description="Tags across MCQ, Written, Library, Current affairs, and the official directory. Renaming to an existing tag merges them. Changes apply only to modules you manage and are audited." />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {notice && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{notice}</p>}
      <Card className="overflow-hidden">
        <div className="flex flex-col gap-2 border-b border-slate-100 p-4 sm:flex-row dark:border-slate-800">
          <input aria-label="Search tags" placeholder="Filter tags" value={search} onChange={(event) => setSearch(event.target.value)} className="h-9 flex-1 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800" />
          <select aria-label="Module" value={module} onChange={(event) => setModule(event.target.value as TagModule | '')} className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">All my modules</option>{Object.entries(tagModuleLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>
        </div>
        {!tags ? <LoadingState /> : tags.length === 0 ? <EmptyState title="No tags" description="Tags appear here once content is tagged." />
          : <Table headers={['Tag', 'Used in', 'Total', '']}>{tags.map((tag) => <tr key={tag.tag}>
            <td className="px-5 py-2.5 font-medium">{tag.tag}</td>
            <td className="px-5 py-2.5 text-xs text-slate-500">{Object.entries(tag.modules).map(([name, count]) => `${tagModuleLabels[name as TagModule]} ${count}`).join(' · ')}</td>
            <td className="px-5 py-2.5 tabular-nums">{tag.total}</td>
            <td className="px-5 py-2.5"><div className="flex gap-1"><Button size="sm" variant="ghost" onClick={() => void rename(tag.tag)}>Rename / merge</Button><Button size="sm" variant="ghost" onClick={() => void remove(tag.tag)}>Remove</Button></div></td>
          </tr>)}</Table>}
      </Card>
    </>
  )
}

// ---------- Categories ----------

export function AdminCategoriesPage() {
  const can = usePermissions()
  const [caCounts, setCaCounts] = useState<Record<string, number>>({})
  useEffect(() => {
    let alive = true
    currentAffairsApi.getCategories().then((rows) => alive && setCaCounts(Object.fromEntries(rows.map((row) => [row.category, row.count])))).catch(() => undefined)
    return () => { alive = false }
  }, [])

  return (
    <>
      <PageHeader title="Categories" description="Every category system in the platform and where it is managed. Fixed taxonomies come from the project requirements and are changed in code with a migration, so stored data is never orphaned." />
      <div className="grid gap-5 lg:grid-cols-2">
        <Card className="p-5"><h2 className="font-semibold">Viva categories <Badge tone="success">Editable</Badge></h2><p className="mt-1 text-sm text-slate-500">Create, rename, reorder, and deactivate categories.</p>{can('viva.manage') && <Link to="/admin/viva" className="mt-3 inline-block text-sm text-emerald-700 hover:underline dark:text-emerald-400">Manage Viva categories</Link>}</Card>
        <Card className="p-5"><h2 className="font-semibold">Syllabus subjects <Badge tone="success">Editable</Badge></h2><p className="mt-1 text-sm text-slate-500">Subjects, papers, topics, and subtopics act as the subject taxonomy across all modules.</p>{can('syllabus.manage') && <Link to="/admin/syllabus" className="mt-3 inline-block text-sm text-emerald-700 hover:underline dark:text-emerald-400">Manage syllabus</Link>}</Card>
        <Card className="p-5"><h2 className="font-semibold">Current-affairs categories <Badge tone="neutral">Fixed</Badge></h2><ul className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-sm">{currentAffairsCategories.map((value) => <li key={value} className="flex justify-between gap-2"><span>{caCategoryLabels[value]}</span><span className="tabular-nums text-slate-500">{caCounts[value] ?? 0}</span></li>)}</ul><p className="mt-2 text-xs text-slate-500">Counts are published entries.</p></Card>
        <Card className="p-5"><h2 className="font-semibold">Library categories <Badge tone="neutral">Fixed</Badge></h2><ul className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-sm">{Object.values(libraryCategoryLabels).map((label) => <li key={label}>{label}</li>)}</ul></Card>
        <Card className="p-5 lg:col-span-2"><h2 className="font-semibold">Official directory categories <Badge tone="neutral">Fixed</Badge></h2><ul className="mt-2 grid gap-x-4 gap-y-1 text-sm sm:grid-cols-3">{Object.values(sourceCategoryLabels).map((label) => <li key={label}>{label}</li>)}</ul><p className="mt-3 text-xs text-slate-500">For finer, editable grouping use <Link to="/admin/tags" className="underline">tags</Link>.</p></Card>
      </div>
    </>
  )
}

// ---------- Audit ----------

export function AdminAuditPage() {
  const { accessToken } = useAuth()
  const [filters, setFilters] = useState({ entityType: '', entityId: '', action: '', from: '', to: '' })
  const [applied, setApplied] = useState(filters)
  const [page, setPage] = useState(1)
  const [types, setTypes] = useState<string[]>([])
  const [result, setResult] = useState<PagedResult<AuditEntry> | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let alive = true
    adminApi.getAuditEntityTypes(accessToken).then((value) => alive && setTypes(value)).catch(() => undefined)
    return () => { alive = false }
  }, [accessToken])

  useEffect(() => {
    if (!accessToken) return
    let alive = true
    adminApi.getAudit({ ...applied, pageNumber: page, pageSize: 50 }, accessToken).then((value) => alive && setResult(value)).catch((reason: unknown) => alive && setError(message(reason, 'Unable to load audit history.')))
    return () => { alive = false }
  }, [accessToken, applied, page])

  const input = 'h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800'
  return (
    <>
      <PageHeader title="Audit history" description="Append-only log of administrative and security-relevant actions, with the acting account and request correlation ID." />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      <Card className="overflow-hidden">
        <form className="grid gap-2 border-b border-slate-100 p-4 sm:grid-cols-3 lg:grid-cols-6 dark:border-slate-800" onSubmit={(event) => { event.preventDefault(); setPage(1); setApplied(filters) }}>
          <select aria-label="Entity type" className={input} value={filters.entityType} onChange={(event) => setFilters({ ...filters, entityType: event.target.value })}><option value="">Any record type</option>{types.map((type) => <option key={type}>{type}</option>)}</select>
          <input aria-label="Record ID" placeholder="Record ID" className={input} value={filters.entityId} onChange={(event) => setFilters({ ...filters, entityId: event.target.value })} />
          <input aria-label="Action prefix" placeholder="Action, e.g. mcq." className={input} value={filters.action} onChange={(event) => setFilters({ ...filters, action: event.target.value })} />
          <input aria-label="From" type="date" className={input} value={filters.from} onChange={(event) => setFilters({ ...filters, from: event.target.value })} />
          <input aria-label="To" type="date" className={input} value={filters.to} onChange={(event) => setFilters({ ...filters, to: event.target.value })} />
          <Button type="submit" variant="secondary">Filter</Button>
        </form>
        {!result ? <LoadingState /> : result.items.length === 0 ? <EmptyState title="No entries" description="No audit entries match these filters." />
          : <Table headers={['When', 'Action', 'Record', 'By', 'Correlation']}>{result.items.map((entry) => <tr key={entry.id}>
            <td className="whitespace-nowrap px-5 py-2 text-xs text-slate-500">{new Date(entry.occurredAt).toLocaleString()}</td>
            <td className="px-5 py-2 font-mono text-xs">{entry.action}</td>
            <td className="px-5 py-2 text-xs">{entry.entityType ?? '—'}<span className="block max-w-48 truncate font-mono text-slate-500">{entry.entityId}</span></td>
            <td className="px-5 py-2 text-xs">{entry.actorName ?? 'System'}<span className="block text-slate-500">{entry.actorEmail}</span></td>
            <td className="max-w-32 truncate px-5 py-2 font-mono text-[11px] text-slate-500">{entry.correlationId}</td>
          </tr>)}</Table>}
        {result && <Pagination page={result.pageNumber} totalPages={Math.max(result.totalPages, 1)} onPageChange={setPage} />}
      </Card>
    </>
  )
}

// ---------- Site settings ----------

export function AdminSettingsPage() {
  const { accessToken } = useAuth()
  const [settings, setSettings] = useState<SiteSettings | null>(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let alive = true
    adminApi.getSettings(accessToken).then((value) => alive && setSettings(value)).catch((reason: unknown) => alive && setError(message(reason, 'Unable to load settings.')))
    return () => { alive = false }
  }, [accessToken])

  if (error && !settings) return <ErrorState description={error} />
  if (!settings || !accessToken) return <LoadingState />
  const set = <K extends keyof SiteSettings>(key: K, value: SiteSettings[K]) => setSettings({ ...settings, [key]: value })
  const field = 'mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950'
  const label = 'block text-xs font-medium text-slate-600 dark:text-slate-300'
  const local = (value: string | null) => (value ? new Date(new Date(value).getTime() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '')

  async function save() {
    if (!settings || !accessToken) return
    setError('')
    setNotice('')
    try {
      setSettings(await adminApi.saveSettings(settings, accessToken))
      setNotice('Saved. Visitors see the change on their next page load.')
    } catch (reason) {
      setError(message(reason, 'Unable to save settings.'))
    }
  }

  return (
    <>
      <PageHeader title="Site settings" description="Global configuration. Registration is enforced by the API, so closing it blocks sign-ups even from direct API calls." />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {notice && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{notice}</p>}
      <form className="grid gap-5 xl:grid-cols-2" onSubmit={(event) => { event.preventDefault(); void save() }}>
        <Card className="space-y-3 p-5">
          <h2 className="font-semibold">Identity</h2>
          <label className={label}>Site name<input required maxLength={80} className={field} value={settings.siteName} onChange={(event) => set('siteName', event.target.value)} /></label>
          <label className={label}>Tagline<input maxLength={200} className={field} value={settings.tagline ?? ''} onChange={(event) => set('tagline', event.target.value || null)} /></label>
          <label className={label}>Support email<input type="email" maxLength={254} className={field} value={settings.supportEmail ?? ''} onChange={(event) => set('supportEmail', event.target.value || null)} /></label>
          <label className={label}>Footer notice<textarea maxLength={500} rows={2} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={settings.footerNotice ?? ''} onChange={(event) => set('footerNotice', event.target.value || null)} placeholder="e.g. Independent study aid; not affiliated with BPSC." /></label>
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={settings.registrationOpen} onChange={(event) => set('registrationOpen', event.target.checked)} />Allow new registrations</label>
        </Card>
        <Card className="space-y-3 p-5">
          <h2 className="font-semibold">Announcement banner</h2>
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={settings.announcementEnabled} onChange={(event) => set('announcementEnabled', event.target.checked)} />Show announcement</label>
          <label className={label}>Text<textarea maxLength={500} rows={3} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={settings.announcementText ?? ''} onChange={(event) => set('announcementText', event.target.value || null)} /></label>
          <div className="grid grid-cols-3 gap-2">
            <label className={label}>Tone<select className={field} value={settings.announcementTone} onChange={(event) => set('announcementTone', event.target.value as SiteSettings['announcementTone'])}><option>Info</option><option>Warning</option><option>Critical</option></select></label>
            <label className={label}>Starts<input type="datetime-local" className={field} value={local(settings.announcementStartsAt)} onChange={(event) => set('announcementStartsAt', event.target.value ? new Date(event.target.value).toISOString() : null)} /></label>
            <label className={label}>Ends<input type="datetime-local" className={field} value={local(settings.announcementEndsAt)} onChange={(event) => set('announcementEndsAt', event.target.value ? new Date(event.target.value).toISOString() : null)} /></label>
          </div>
          {settings.announcementText && <div className={`rounded-lg px-3 py-2 text-sm ${settings.announcementTone === 'Critical' ? 'bg-red-50 text-red-900 dark:bg-red-950/40 dark:text-red-200' : settings.announcementTone === 'Warning' ? 'bg-amber-50 text-amber-900 dark:bg-amber-950/40 dark:text-amber-200' : 'bg-sky-50 text-sky-900 dark:bg-sky-950/40 dark:text-sky-200'}`}><span className="text-xs font-semibold uppercase">Preview: </span>{settings.announcementText}</div>}
        </Card>
        <div className="flex justify-end xl:col-span-2"><Button type="submit"><Save className="h-4 w-4" />Save settings</Button></div>
      </form>
      <p className="mt-4 flex items-center gap-1 text-xs text-slate-500"><ShieldCheck className="h-3.5 w-3.5" />Changes are recorded in the audit history.</p>
    </>
  )
}
