import { AlertCircle, BookOpen, CheckCircle2, ChevronDown, ChevronRight, ExternalLink, FileText, Search, ShieldCheck } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { Field, Input, Select } from '../../components/ui/Form'
import { PageHeader } from '../../components/ui/PageHeader'
import { syllabusApi, type ExamStage, type SyllabusItem, type SyllabusSearchHit, type SyllabusTree, type SyllabusVersion } from '../../services/syllabus'

function updateCompletion(items: SyllabusItem[], id: string, value: boolean): SyllabusItem[] {
  return items.map((item) => item.id === id
    ? { ...item, isCompleted: value }
    : { ...item, children: updateCompletion(item.children, id, value) })
}

function CompletionIndicator({
  item,
  token,
  onChange,
  pending,
}: {
  item: SyllabusItem
  token: string | null
  onChange: (item: SyllabusItem, completed: boolean) => void
  pending: boolean
}) {
  if (!token) {
    return <span className="shrink-0 text-[11px] text-slate-400" title="Sign in to track completion">Sign in to track</span>
  }
  return (
    <input
      type="checkbox"
      aria-label={`Mark ${item.title} complete`}
      checked={item.isCompleted === true}
      disabled={pending}
      onChange={(event) => onChange(item, event.target.checked)}
      className="h-4 w-4 shrink-0 rounded border-slate-300 text-emerald-700 focus:ring-emerald-600"
    />
  )
}

function SyllabusTreeItem({
  item,
  expanded,
  onToggle,
  token,
  onProgress,
  pending,
  onSelect,
  selected,
}: {
  item: SyllabusItem
  expanded: Record<string, boolean>
  onToggle: (id: string) => void
  token: string | null
  onProgress: (item: SyllabusItem, value: boolean) => void
  pending: Set<string>
  onSelect: (item: SyllabusItem) => void
  selected: string | null
}) {
  const hasChildren = item.children.length > 0
  const isExpanded = expanded[item.id] ?? item.kind === 'Topic'
  return (
    <div>
      <div className={`group flex items-center gap-2 rounded-lg py-2 pl-2 pr-3 ${selected === item.id ? 'bg-emerald-50 dark:bg-emerald-500/10' : 'hover:bg-slate-50 dark:hover:bg-slate-800/70'}`}>
        <button
          type="button"
          aria-label={`${isExpanded ? 'Collapse' : 'Expand'} ${item.title}`}
          onClick={() => hasChildren && onToggle(item.id)}
          className="grid h-7 w-7 shrink-0 place-items-center rounded text-slate-500 disabled:opacity-30"
          disabled={!hasChildren}
        >
          {hasChildren ? (isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />) : <span className="h-1 w-1 rounded-full bg-slate-400" />}
        </button>
        <button type="button" onClick={() => onSelect(item)} className="min-w-0 flex-1 text-left">
          <span className="block truncate text-sm font-medium text-slate-800 dark:text-slate-200">{item.title}</span>
          <span className="text-[11px] text-slate-500">{item.kind === 'Topic' ? 'Topic' : 'Subtopic'}{item.verificationStatus !== 'Verified' ? ' · Not verified' : ' · Source verified'}</span>
        </button>
        <CompletionIndicator item={item} token={token} pending={pending.has(item.id)} onChange={onProgress} />
      </div>
      {hasChildren && isExpanded && (
        <div className="ml-5 border-l border-slate-200 py-1 pl-2 dark:border-slate-700">
          {item.children.map((child) => (
            <SyllabusTreeItem
              key={child.id}
              item={child}
              expanded={expanded}
              onToggle={onToggle}
              token={token}
              onProgress={onProgress}
              pending={pending}
              onSelect={onSelect}
              selected={selected}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function collectItems(items: SyllabusItem[]): SyllabusItem[] {
  return items.flatMap((item) => [item, ...collectItems(item.children)])
}

export function SyllabusPage() {
  const { accessToken } = useAuth()
  const [stages, setStages] = useState<ExamStage[]>([])
  const [stageCode, setStageCode] = useState('Preliminary')
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [versionId, setVersionId] = useState('')
  const [tree, setTree] = useState<SyllabusTree | null>(null)
  const [loadingStages, setLoadingStages] = useState(true)
  const [loadingVersions, setLoadingVersions] = useState(false)
  const [loadingTree, setLoadingTree] = useState(false)
  const [error, setError] = useState('')
  const [expanded, setExpanded] = useState<Record<string, boolean>>({})
  const [selectedItem, setSelectedItem] = useState<SyllabusItem | SyllabusSearchHit | null>(null)
  const [selectedSources, setSelectedSources] = useState<SyllabusTree['sources']>([])
  const [detailError, setDetailError] = useState('')
  const [pendingProgress, setPendingProgress] = useState<Set<string>>(new Set())
  const [query, setQuery] = useState('')
  const [searchResults, setSearchResults] = useState<SyllabusSearchHit[]>([])
  const [searching, setSearching] = useState(false)
  const [searchError, setSearchError] = useState('')

  useEffect(() => {
    let active = true
    setLoadingStages(true)
    syllabusApi.getStages()
      .then((result) => {
        if (active) {
          setStages(result)
          if (result.length > 0 && !result.some((stage) => stage.code === stageCode)) setStageCode(result[0].code)
        }
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load exam stages.'))
      .finally(() => active && setLoadingStages(false))
    return () => { active = false }
  }, [])

  const loadVersions = useCallback(async () => {
    setLoadingVersions(true)
    setError('')
    setVersionId('')
    setTree(null)
    setSearchResults([])
    try {
      const result = await syllabusApi.getVersions({ stageCode, pageSize: 100, sortBy: 'stage' })
      setVersions(result.items)
      setVersionId(result.items[0]?.id ?? '')
    } catch (reason) {
      setVersions([])
      setError(reason instanceof Error ? reason.message : 'Unable to load published syllabus versions.')
    } finally {
      setLoadingVersions(false)
    }
  }, [stageCode])

  useEffect(() => {
    if (stages.length > 0) void loadVersions()
  }, [stages.length, loadVersions])

  const loadTree = useCallback(async () => {
    if (!versionId) {
      setTree(null)
      return
    }
    setLoadingTree(true)
    setError('')
    try {
      setTree(await syllabusApi.getTree(versionId, accessToken ?? undefined))
      setSelectedItem(null)
    } catch (reason) {
      setTree(null)
      setError(reason instanceof Error ? reason.message : 'Unable to load this syllabus.')
    } finally {
      setLoadingTree(false)
    }
  }, [accessToken, versionId])

  useEffect(() => { void loadTree() }, [loadTree])

  useEffect(() => {
    let active = true
    setSelectedSources([])
    setDetailError('')
    if (!selectedItem) return () => { active = false }
    syllabusApi.getItemSources(selectedItem.id)
      .then((sources) => active && setSelectedSources(sources))
      .catch((reason: unknown) => active && setDetailError(reason instanceof Error ? reason.message : 'Unable to load item sources.'))
    return () => { active = false }
  }, [selectedItem?.id])

  const allItems = useMemo(() => tree ? tree.subjects.flatMap((subject) => collectItems(subject.items)) : [], [tree])
  const completedCount = allItems.filter((item) => item.isCompleted === true).length
  const selectedSubject = selectedItem && tree?.subjects.find((subject) => subject.subjectId === selectedItem.subjectId)

  async function handleSearch(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSearchError('')
    if (query.trim().length < 2) {
      setSearchResults([])
      setSearchError('Enter at least two characters to search the published syllabus.')
      return
    }
    setSearching(true)
    try {
      const result = await syllabusApi.search({ q: query.trim(), stageCode, versionId, pageSize: 30, sortBy: 'title' })
      setSearchResults(result.items)
    } catch (reason) {
      setSearchError(reason instanceof Error ? reason.message : 'Search failed.')
      setSearchResults([])
    } finally {
      setSearching(false)
    }
  }

  async function handleProgress(item: SyllabusItem, completed: boolean) {
    if (!accessToken) return
    setPendingProgress((current) => new Set(current).add(item.id))
    setError('')
    try {
      await syllabusApi.setProgress(item.id, completed, accessToken)
      setTree((current) => current ? {
        ...current,
        subjects: current.subjects.map((subject) => ({
          ...subject,
          items: updateCompletion(subject.items, item.id, completed),
        })),
      } : current)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Could not save topic progress.')
    } finally {
      setPendingProgress((current) => {
        const next = new Set(current)
        next.delete(item.id)
        return next
      })
    }
  }

  function getTreeItem(id: string) {
    return allItems.find((item) => item.id === id)
  }

  function selectSearchHit(hit: SyllabusSearchHit) {
    setSelectedItem(getTreeItem(hit.id) ?? hit)
    const item = getTreeItem(hit.id)
    if (item?.parentId) setExpanded((current) => ({ ...current, [item.parentId!]: true }))
  }

  return (
    <>
      <PageHeader
        title="Examination Syllabus"
        description="Explore the published, source-linked BPSC syllabus outline and track your own topic progress."
      />

      <Card className="mb-5 p-4 sm:p-5">
        <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
          <Field label="Examination stage">
            <Select value={stageCode} disabled={loadingStages} onChange={(event) => setStageCode(event.target.value)}>
              {stages.map((stage) => <option key={stage.id} value={stage.code}>{stage.name} · {stage.localizedName}</option>)}
            </Select>
          </Field>
          <Field label="Published syllabus version">
            <Select value={versionId} disabled={loadingVersions || versions.length === 0} onChange={(event) => setVersionId(event.target.value)}>
              {versions.length === 0 && <option value="">No published version</option>}
              {versions.map((version) => (
                <option key={version.id} value={version.id}>{version.versionLabel} · {version.trackCode}</option>
              ))}
            </Select>
          </Field>
        </div>
        {tree && (
          <div className="mt-4 flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 pt-4 dark:border-slate-800">
            <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
              <span className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-800 dark:bg-emerald-500/10 dark:text-emerald-300">{tree.version.status}</span>
              <span>{tree.subjects.length} subject groups</span>
              {allItems.length > 0 && <span>· {completedCount}/{allItems.length} topics complete</span>}
            </div>
            <span className="text-xs text-slate-500">Track: {tree.version.trackCode}</span>
          </div>
        )}
      </Card>

      {error && (
        <div role="alert" className="mb-5 flex items-start gap-3 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-900/60 dark:bg-red-950/30 dark:text-red-200">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <div className="flex-1">{error}</div>
          <Button size="sm" variant="secondary" onClick={() => void (versionId ? loadTree() : loadVersions())}>Retry</Button>
        </div>
      )}

      <Card className="mb-5 p-4 sm:p-5">
        <form onSubmit={handleSearch} className="flex flex-col gap-3 sm:flex-row">
          <div className="relative min-w-0 flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <Input aria-label="Search syllabus topics" placeholder="Search published topics and subtopics" value={query} onChange={(event) => setQuery(event.target.value)} className="mt-0 pl-9" />
          </div>
          <Button type="submit" disabled={searching || !versionId}>{searching ? 'Searching…' : 'Search syllabus'}</Button>
        </form>
        {searchError && <p role="alert" className="mt-3 text-sm text-red-700 dark:text-red-300">{searchError}</p>}
        {searchResults.length > 0 && (
          <div className="mt-4 border-t border-slate-100 pt-3 dark:border-slate-800">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">{searchResults.length} matching syllabus items</p>
            <ul className="grid gap-1 sm:grid-cols-2">
              {searchResults.map((hit) => (
                <li key={hit.id}>
                  <button type="button" onClick={() => selectSearchHit(hit)} className="w-full rounded-lg px-3 py-2 text-left hover:bg-slate-50 dark:hover:bg-slate-800">
                    <span className="block text-sm font-medium text-slate-800 dark:text-slate-200">{hit.title}</span>
                    <span className="text-xs text-slate-500">{hit.subjectName} · {hit.kind}</span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}
      </Card>

      {loadingStages || loadingVersions || loadingTree ? (
        <Card className="space-y-4 p-5" aria-label="Loading syllabus">
          <div className="h-4 w-1/3 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
          <div className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800" />
          <div className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800" />
          <div className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800" />
        </Card>
      ) : !versionId ? (
        <Card className="p-8 text-center">
          <BookOpen className="mx-auto h-8 w-8 text-slate-400" />
          <h2 className="mt-3 font-semibold text-slate-900 dark:text-white">No published version for this stage</h2>
          <p className="mt-1 text-sm text-slate-500">A syllabus version will appear here after it has been reviewed and published.</p>
        </Card>
      ) : tree ? (
        <div className="grid gap-5 xl:grid-cols-[minmax(0,1.6fr)_minmax(300px,0.8fr)]">
          <Card className="min-w-0">
            <CardHeader
              title={`${tree.version.stageName} syllabus`}
              subtitle={tree.version.versionLabel}
              action={<span className="text-xs text-slate-500">{tree.version.coverage === 'OutlineOnly' ? 'Verified outline' : 'Topic coverage'}</span>}
            />
            <div className="px-4 pb-5 sm:px-5">
              <div className="mb-4 rounded-lg border border-sky-200 bg-sky-50 px-3 py-2.5 text-sm text-sky-900 dark:border-sky-900/60 dark:bg-sky-950/30 dark:text-sky-200">
                {tree.version.summary}
              </div>
              {tree.version.coverage === 'OutlineOnly' && (
                <div className="mb-4 flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2.5 text-xs text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-200">
                  <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
                  <p>This release contains only the source-verified outline. Detailed topics not shown here have not been verified or filled in with sample content.</p>
                </div>
              )}
              {tree.subjects.length === 0 ? (
                <div className="rounded-lg border border-dashed border-slate-300 p-6 text-center dark:border-slate-700">
                  <p className="text-sm font-medium text-slate-700 dark:text-slate-200">No official topic tree is published for this stage.</p>
                  <p className="mt-1 text-xs text-slate-500">See the source notes for the verified examination structure.</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {tree.subjects.map((subject) => (
                    <section key={subject.id} className="rounded-xl border border-slate-200 p-3 dark:border-slate-800 sm:p-4">
                      <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                        <h2 className="text-sm font-semibold text-slate-900 dark:text-white">{subject.subjectName}</h2>
                        <div className="flex items-center gap-2 text-xs text-slate-500">
                          {subject.marks !== null && <span>{subject.marks} marks</span>}
                          {subject.isRequired && <span className="rounded-full bg-slate-100 px-2 py-0.5 dark:bg-slate-800">Required</span>}
                        </div>
                      </div>
                      {subject.papers.map((paper) => (
                        <div key={paper.id} className="mb-2 text-xs text-slate-500">{paper.name}{paper.maxMarks !== null ? ` · ${paper.maxMarks} marks` : ''}</div>
                      ))}
                      {subject.items.length === 0 ? (
                        <p className="rounded-md bg-slate-50 px-3 py-2 text-xs text-slate-500 dark:bg-slate-800/70">Detailed topic entries are not yet source-verified.</p>
                      ) : (
                        <div className="space-y-0.5">
                          {subject.items.map((item) => (
                            <SyllabusTreeItem
                              key={item.id}
                              item={item}
                              expanded={expanded}
                              onToggle={(id) => setExpanded((current) => ({ ...current, [id]: !(current[id] ?? true) }))}
                              token={accessToken}
                              onProgress={handleProgress}
                              pending={pendingProgress}
                              onSelect={setSelectedItem}
                              selected={selectedItem?.id ?? null}
                            />
                          ))}
                        </div>
                      )}
                    </section>
                  ))}
                </div>
              )}
            </div>
          </Card>

          <aside className="space-y-5">
            <Card>
              <CardHeader title="Topic details" subtitle="Select an item from the syllabus tree" />
              <div className="px-4 pb-5 sm:px-5">
                {selectedItem ? (
                  <>
                    <div className="flex items-start gap-2">
                      {selectedItem.verificationStatus === 'Verified' ? <ShieldCheck className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600" /> : <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />}
                      <div>
                        <h3 className="font-semibold text-slate-900 dark:text-white">{selectedItem.title}</h3>
                        <p className="mt-1 text-xs text-slate-500">{'kind' in selectedItem ? selectedItem.kind : 'Syllabus item'} · {selectedItem.verificationStatus}</p>
                      </div>
                    </div>
                    <p className="mt-4 whitespace-pre-wrap text-sm leading-6 text-slate-600 dark:text-slate-300">{selectedItem.description || 'No description is available for this item.'}</p>
                    {detailError && <p role="alert" className="mt-2 text-xs text-red-700 dark:text-red-300">{detailError}</p>}
                    {selectedSources.length > 0 && (
                      <div className="mt-4 space-y-2 border-t border-slate-100 pt-3 dark:border-slate-800">
                        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Item references</p>
                        {selectedSources.map((source) => (
                          <a key={source.id} href={source.url} target="_blank" rel="noreferrer" className="block text-sm text-emerald-800 hover:underline dark:text-emerald-300">
                            {source.title} · {source.verificationStatus}
                          </a>
                        ))}
                      </div>
                    )}
                    <div className="mt-4 grid gap-2">
                      <Link to={`/notes?topicId=${selectedItem.id}`} className="flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800">
                        <FileText className="h-4 w-4" /> Open topic notes
                      </Link>
                      <Link to={`/library?topicId=${selectedItem.id}`} className="flex items-center gap-2 rounded-lg border border-slate-200 px-3 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800">
                        <BookOpen className="h-4 w-4" /> Browse related resources
                      </Link>
                    </div>
                    {selectedSubject && <p className="mt-3 text-xs text-slate-500">Subject: {selectedSubject.subjectName}</p>}
                  </>
                ) : (
                  <p className="text-sm text-slate-500">Choose a topic or subtopic to see its verification status and related navigation.</p>
                )}
              </div>
            </Card>

            <Card>
              <CardHeader title="Official references" subtitle="Source links and coverage notes" />
              <div className="space-y-3 px-4 pb-5 sm:px-5">
                {tree.sources.map((source) => (
                  <a key={source.id} href={source.url} target="_blank" rel="noreferrer" className="group flex gap-3 rounded-lg border border-slate-200 p-3 hover:border-emerald-300 dark:border-slate-800 dark:hover:border-emerald-800">
                    <ExternalLink className="mt-0.5 h-4 w-4 shrink-0 text-slate-400 group-hover:text-emerald-700" />
                    <span className="min-w-0">
                      <span className="block text-sm font-medium text-slate-800 dark:text-slate-200">{source.title}</span>
                      <span className="mt-1 block text-xs text-slate-500">{source.publisher} · {source.verificationStatus}</span>
                      {source.notes && <span className="mt-1 block text-xs leading-5 text-slate-500">{source.notes}</span>}
                    </span>
                  </a>
                ))}
                {tree.sources.length === 0 && <p className="text-sm text-slate-500">No source references have been attached to this version.</p>}
              </div>
            </Card>

            {accessToken ? (
              <Card className="p-4">
                <div className="flex items-center gap-2 text-sm font-medium text-slate-800 dark:text-slate-200"><CheckCircle2 className="h-4 w-4 text-emerald-600" /> Progress is linked to your account</div>
                <p className="mt-1 text-xs text-slate-500">Only your completion state is stored; syllabus content remains shared.</p>
              </Card>
            ) : (
              <Card className="p-4">
                <p className="text-sm font-medium text-slate-800 dark:text-slate-200">Keep your progress</p>
                <p className="mt-1 text-xs text-slate-500">Sign in to record completed topics to your account.</p>
                <Link to="/login" className="mt-3 inline-flex text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">Sign in to track progress</Link>
              </Card>
            )}
          </aside>
        </div>
      ) : null}
    </>
  )
}
