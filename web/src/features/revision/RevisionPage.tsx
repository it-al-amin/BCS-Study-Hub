import { Pause, Play, Plus, Save, Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { TopicSelect, type TopicSelection } from '../../components/content/TopicSelect'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import type { PagedResult } from '../../services/api'
import { plannerApi, type ImportSource, type PlannerSettings, type RevisionLog, type RevisionOutcome, type RevisionQueue, type WeakTopic } from '../../services/planner'
import { errorMessage, formatDay, sourceLabels, sourceLink } from '../planner/plannerFormat'

type Tab = 'queue' | 'weak' | 'history' | 'settings'
const outcomes: RevisionOutcome[] = ['Again', 'Hard', 'Good', 'Easy']
const imports: [ImportSource, string][] = [
  ['McqMistakes', 'MCQ mistakes'], ['MistakeNotebook', 'Mistake notebook'], ['WeakTopics', 'Weak topics'], ['McqBookmarks', 'MCQ bookmarks'],
  ['WrittenBookmarks', 'Written bookmarks'], ['VivaBookmarks', 'Viva bookmarks'], ['WrittenNotes', 'Written notes'], ['LibraryNotes', 'Library notes'], ['VivaNotes', 'Viva notes'],
]

export function RevisionPage() {
  const { accessToken } = useAuth()
  const [params, setParams] = useSearchParams()
  const tab = (params.get('tab') as Tab | null) ?? 'queue'
  const [error, setError] = useState('')
  if (!accessToken) return <><PageHeader title="Revision" /><Card><EmptyState title="Sign in to revise" description="Your revision queue, history, and settings are private." /></Card></>
  const tabs: [Tab, string][] = [['queue', 'Queue'], ['weak', 'Weak topics'], ['history', 'History'], ['settings', 'Settings']]
  return (
    <>
      <PageHeader title="Revision" description="Spaced revision of topics, questions, notes, and mistakes. The intervals and how each answer moves an item are your own settings, not fixed rules." action={<Link to="/mistakes"><Button variant="secondary">Mistake notebook</Button></Link>} />
      <div className="mb-4 flex flex-wrap gap-1">{tabs.map(([value, label]) => <button key={value} type="button" onClick={() => setParams(value === 'queue' ? {} : { tab: value })} className={`rounded-lg px-3 py-1.5 text-sm ${tab === value ? 'bg-emerald-700 text-white' : 'border border-slate-200 bg-white hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:hover:bg-slate-800'}`}>{label}</button>)}</div>
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {tab === 'queue' && <QueueTab token={accessToken} onError={setError} />}
      {tab === 'weak' && <WeakTab token={accessToken} onError={setError} />}
      {tab === 'history' && <HistoryTab token={accessToken} onError={setError} />}
      {tab === 'settings' && <SettingsTab token={accessToken} onError={setError} />}
    </>
  )
}

function QueueTab({ token, onError }: { token: string; onError: (value: string) => void }) {
  const [view, setView] = useState<'due' | 'all' | 'Suspended' | 'Retired'>('due')
  const [page, setPage] = useState(1)
  const [queue, setQueue] = useState<RevisionQueue | null>(null)
  const [reload, setReload] = useState(0)
  const [message, setMessage] = useState('')
  const [customTitle, setCustomTitle] = useState('')
  const [topic, setTopic] = useState<TopicSelection | null>(null)
  const refresh = () => setReload((count) => count + 1)

  useEffect(() => {
    let alive = true
    plannerApi.getQueue({ dueOnly: view === 'due', status: view === 'Suspended' || view === 'Retired' ? view : undefined, pageNumber: page }, token)
      .then((result) => alive && setQueue(result))
      .catch((reason: unknown) => alive && onError(errorMessage(reason, 'Unable to load the queue.')))
    return () => { alive = false }
  }, [token, view, page, reload, onError])

  async function act(action: () => Promise<unknown>, success?: string) {
    try {
      await action()
      if (success) setMessage(success)
      refresh()
    } catch (reason) {
      onError(errorMessage(reason, 'The action failed.'))
    }
  }

  return (
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1.5fr)_minmax(0,1fr)]">
      <Card className="overflow-hidden">
        <CardHeader title={view === 'due' ? 'Due now' : view === 'all' ? 'All active items' : view} subtitle={queue ? `${queue.dueCount} due (${queue.overdueCount} overdue) · showing up to ${queue.shownLimit} per session${queue.currentAffairsDue ? ` · ${queue.currentAffairsDue} current-affairs entries due` : ''}` : undefined}
          action={<select aria-label="Queue view" value={view} onChange={(event) => { setView(event.target.value as typeof view); setPage(1) }} className="h-8 rounded-lg border border-slate-200 bg-white px-2 text-xs dark:border-slate-700 dark:bg-slate-900"><option value="due">Due now</option><option value="all">All active</option><option value="Suspended">Suspended</option><option value="Retired">Retired</option></select>} />
        {queue?.currentAffairsDue ? <p className="mx-5 mt-3 rounded bg-amber-50 px-3 py-2 text-xs text-amber-900 dark:bg-amber-950/30 dark:text-amber-200">{queue.currentAffairsDue} current-affairs entr{queue.currentAffairsDue === 1 ? 'y is' : 'ies are'} due. <Link to="/current-affairs/revision" className="underline">Review them</Link> (they use the same settings).</p> : null}
        {!queue ? <LoadingState /> : queue.items.length === 0 ? <EmptyState title={view === 'due' ? 'Nothing due' : 'Nothing here'} description="Import from your bookmarks, mistakes, weak topics, or notes, or add a custom item." />
          : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{queue.items.map((item) => {
            const link = sourceLink(item.sourceType, item.sourceId, item.syllabusItemId)
            return <li key={item.id} className="px-5 py-3">
              <div className="flex items-start gap-2"><Badge tone="neutral">{sourceLabels[item.sourceType]}</Badge><div className="min-w-0 flex-1">{link ? <Link to={link} className="text-sm font-medium hover:underline">{item.title}</Link> : <p className="text-sm font-medium">{item.title}</p>}<p className="text-xs text-slate-500">{[item.topicTitle, item.reason !== 'Manual' ? `from ${item.reason.toLowerCase()}` : null, item.isDue ? 'due' : `next ${formatDay(item.dueOn)}`, `step ${item.step}`, `reviewed ${item.reviewCount}×`, item.lapseCount ? `${item.lapseCount} lapse${item.lapseCount === 1 ? '' : 's'}` : null].filter(Boolean).join(' · ')}</p></div></div>
              <div className="mt-2 flex flex-wrap items-center gap-1 pl-1">
                {item.status === 'Active' && outcomes.map((outcome) => <Button key={outcome} size="sm" variant={outcome === 'Good' ? 'primary' : 'secondary'} onClick={() => void act(() => plannerApi.review(item.id, outcome, token))}>{outcome}</Button>)}
                {item.status === 'Active'
                  ? <Button size="sm" variant="ghost" aria-label="Suspend" onClick={() => void act(() => plannerApi.setRevisionStatus(item.id, 'suspend', token))}><Pause className="h-3.5 w-3.5" /></Button>
                  : <Button size="sm" variant="ghost" onClick={() => void act(() => plannerApi.setRevisionStatus(item.id, 'resume', token))}><Play className="h-3.5 w-3.5" />Resume</Button>}
                <Button size="sm" variant="ghost" aria-label="Remove from revision" onClick={() => window.confirm('Remove this item? Its history is kept.') && void act(() => plannerApi.deleteRevisionItem(item.id, token))}><Trash2 className="h-3.5 w-3.5" /></Button>
              </div>
            </li>
          })}</ul>}
        {queue && view !== 'due' && <Pagination page={queue.pageNumber} totalPages={Math.max(1, Math.ceil(queue.totalCount / queue.pageSize))} onPageChange={setPage} />}
      </Card>
      <div className="space-y-5">
        <Card className="p-5">
          <h2 className="font-semibold">Add to revision</h2>
          {message && <p className="mt-2 rounded bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300">{message}</p>}
          <p className="mt-2 text-xs text-slate-500">Import everything from a source; items already queued are skipped.</p>
          <div className="mt-2 flex flex-wrap gap-1">{imports.map(([source, label]) => <Button key={source} size="sm" variant="secondary" onClick={() => void act(async () => { const result = await plannerApi.importRevision(source, token); setMessage(`${label}: ${result.added} added, ${result.alreadyQueued} already queued.`) })}>{label}</Button>)}</div>
          <h3 className="mt-4 text-sm font-medium">A syllabus topic</h3>
          <TopicSelect value={topic} onChange={setTopic} />
          <Button className="mt-2" size="sm" disabled={!topic?.syllabusItemId} onClick={() => topic?.syllabusItemId && void act(() => plannerApi.addRevisionItem({ sourceType: 'Topic', sourceId: topic.syllabusItemId, title: null, dueOn: null }, token), 'Topic added.')}><Plus className="h-4 w-4" />Add topic</Button>
          <h3 className="mt-4 text-sm font-medium">Something else</h3>
          <div className="mt-1 flex gap-2"><input aria-label="Custom item" maxLength={300} placeholder="e.g. Constitution articles 1–20" value={customTitle} onChange={(event) => setCustomTitle(event.target.value)} className="h-9 min-w-0 flex-1 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /><Button size="sm" disabled={!customTitle.trim()} onClick={() => void act(async () => { await plannerApi.addRevisionItem({ sourceType: 'Custom', sourceId: null, title: customTitle.trim(), dueOn: null }, token); setCustomTitle('') }, 'Custom item added.')}><Plus className="h-4 w-4" /></Button></div>
        </Card>
      </div>
    </div>
  )
}

function WeakTab({ token, onError }: { token: string; onError: (value: string) => void }) {
  const [topics, setTopics] = useState<WeakTopic[] | null>(null)
  useEffect(() => {
    let alive = true
    plannerApi.getWeakTopics(token).then((result) => alive && setTopics(result)).catch((reason: unknown) => alive && onError(errorMessage(reason, 'Unable to load weak topics.')))
    return () => { alive = false }
  }, [token, onError])

  return (
    <Card className="overflow-hidden">
      <CardHeader title="Weak topics" subtitle="Low MCQ accuracy (your threshold and minimum answers) or self-rated mastery of 1–2" action={<Button size="sm" onClick={() => void plannerApi.importRevision('WeakTopics', token).then((result) => window.alert(`${result.added} topics added to revision.`)).catch((reason: unknown) => onError(errorMessage(reason, 'Import failed.')))}>Queue all</Button>} />
      {!topics ? <LoadingState /> : topics.length === 0 ? <EmptyState title="No weak topics detected" description="Answer more MCQs or rate your mastery on the Progress page to surface weak topics." />
        : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{topics.map((topic) => <li key={topic.syllabusItemId} className="px-5 py-3"><div className="flex items-center justify-between gap-3"><p className="text-sm font-medium">{topic.title}</p><span className="text-sm tabular-nums">{topic.accuracyPercent === null ? '—' : `${topic.accuracyPercent}%`}</span></div><p className="text-xs text-slate-500">{topic.subjectName} · {topic.stageCode}</p><ul className="mt-1 list-inside list-disc text-xs text-slate-600 dark:text-slate-300">{topic.reasons.map((reason) => <li key={reason}>{reason}</li>)}</ul></li>)}</ul>}
    </Card>
  )
}

function HistoryTab({ token, onError }: { token: string; onError: (value: string) => void }) {
  const [page, setPage] = useState(1)
  const [history, setHistory] = useState<PagedResult<RevisionLog> | null>(null)
  useEffect(() => {
    let alive = true
    plannerApi.getHistory(page, token).then((result) => alive && setHistory(result)).catch((reason: unknown) => alive && onError(errorMessage(reason, 'Unable to load history.')))
    return () => { alive = false }
  }, [token, page, onError])

  return (
    <Card className="overflow-hidden">
      <CardHeader title="Revision history" subtitle="Every review, including current affairs" />
      {!history ? <LoadingState /> : history.items.length === 0 ? <EmptyState title="No reviews yet" description="Reviews you make appear here." />
        : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{history.items.map((log) => <li key={log.id} className="flex items-center gap-3 px-5 py-2.5 text-sm"><time className="w-32 text-xs text-slate-500">{new Date(log.reviewedAt).toLocaleString([], { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}</time><Badge tone="neutral">{sourceLabels[log.sourceType]}</Badge><span className="min-w-0 flex-1 truncate">{log.title}</span><Badge tone={log.outcome === 'Again' ? 'danger' : log.outcome === 'Hard' ? 'warning' : 'success'}>{log.outcome}</Badge><span className="w-28 text-right text-xs text-slate-500">step {log.stepBefore}→{log.stepAfter} · {log.intervalDays} d</span></li>)}</ul>}
      {history && <Pagination page={history.pageNumber} totalPages={Math.max(history.totalPages, 1)} onPageChange={setPage} />}
    </Card>
  )
}

function SettingsTab({ token, onError }: { token: string; onError: (value: string) => void }) {
  const [settings, setSettings] = useState<PlannerSettings | null>(null)
  const [intervals, setIntervals] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    let alive = true
    plannerApi.getSettings(token).then((result) => {
      if (!alive) return
      setSettings(result)
      setIntervals(result.revisionIntervalsDays.join(', '))
    }).catch((reason: unknown) => alive && onError(errorMessage(reason, 'Unable to load settings.')))
    return () => { alive = false }
  }, [token, onError])

  if (!settings) return <LoadingState />
  const set = <K extends keyof PlannerSettings>(key: K, value: PlannerSettings[K]) => setSettings({ ...settings, [key]: value })
  const ladder = intervals.split(',').map((value) => Number(value.trim())).filter((value) => Number.isFinite(value) && value > 0)
  const preview = (step: number, delta: number) => ladder[Math.min(ladder.length, Math.max(1, step + delta)) - 1]
  const number = (key: keyof PlannerSettings, label: string, min: number, max: number, help?: string) => (
    <label className="block text-xs font-medium text-slate-600 dark:text-slate-300">{label}
      <input type="number" min={min} max={max} className="mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={(settings[key] as number | null) ?? ''} onChange={(event) => set(key, (event.target.value === '' ? null : Number(event.target.value)) as never)} />
      {help && <span className="mt-0.5 block font-normal text-slate-500">{help}</span>}
    </label>
  )

  async function save() {
    if (!settings) return
    setMessage('')
    try {
      const saved = await plannerApi.saveSettings({ ...settings, revisionIntervalsDays: ladder }, token)
      setSettings(saved)
      setIntervals(saved.revisionIntervalsDays.join(', '))
      setMessage('Settings saved. New reviews use them immediately.')
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to save settings.'))
    }
  }

  return (
    <div className="grid gap-5 xl:grid-cols-2">
      <Card className="space-y-3 p-5">
        <h2 className="font-semibold">Revision schedule</h2>
        {settings.isDefault && <p className="text-xs text-slate-500">You are using the starting defaults. Adjust them to suit your preparation.</p>}
        <label className="block text-xs font-medium text-slate-600 dark:text-slate-300">Interval ladder (days, increasing)
          <input className="mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={intervals} onChange={(event) => setIntervals(event.target.value)} />
          <span className="mt-0.5 block font-normal text-slate-500">Each successful review moves an item along this ladder. Example: 1, 3, 7, 14, 30.</span>
        </label>
        <div className="grid grid-cols-2 gap-3">
          {number('againStep', '"Again" restarts at step', 1, 20)}
          {number('hardStepDelta', '"Hard" moves by (steps)', -20, 20)}
          {number('goodStepDelta', '"Good" moves by (steps)', -20, 20)}
          {number('easyStepDelta', '"Easy" moves by (steps)', 1, 20)}
          {number('retireAfterStep', 'Retire at step (optional)', 1, 20, 'Leave empty to keep items forever.')}
          {number('dailyRevisionLimit', 'Items per session', 1, 500)}
        </div>
        {ladder.length > 0 && <p className="rounded bg-slate-50 p-2 text-xs text-slate-600 dark:bg-slate-800 dark:text-slate-300">A new item answered Good is next due in {preview(0, settings.goodStepDelta)} day(s); answered Easy, in {preview(0, settings.easyStepDelta)}. "Again" always returns it to {ladder[Math.min(ladder.length, settings.againStep) - 1]} day(s).</p>}
      </Card>
      <Card className="space-y-3 p-5">
        <h2 className="font-semibold">Tracking</h2>
        {number('streakMinimumMinutes', 'Minimum study minutes for a streak day', 1, 600, 'Any MCQ, Written, Viva, or revision activity also counts.')}
        {number('weakTopicAccuracyPercent', 'Weak-topic accuracy threshold (%)', 1, 100)}
        {number('weakTopicMinimumAnswers', 'Minimum MCQ answers before judging a topic', 1, 1000)}
        <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={settings.autoQueueMistakes} onChange={(event) => set('autoQueueMistakes', event.target.checked)} />Add new mistake-notebook entries to revision automatically</label>
        {message && <p className="rounded bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300">{message}</p>}
        <Button onClick={() => void save()}><Save className="h-4 w-4" />Save settings</Button>
      </Card>
    </div>
  )
}
