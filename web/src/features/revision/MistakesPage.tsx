import { Download, Pencil, Plus, Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { TopicSelect, type TopicSelection } from '../../components/content/TopicSelect'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Dialog } from '../../components/ui/Dialog'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import type { PagedResult } from '../../services/api'
import { plannerApi, type Mistake, type MistakeCategory, type MistakeInput, type MistakeSourceType } from '../../services/planner'
import { errorMessage, formatDay, mistakeCategoryLabels } from '../planner/plannerFormat'

const field = 'mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950'
const area = 'mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950'
const labelClass = 'block text-xs font-medium text-slate-600 dark:text-slate-300'

export function MistakesPage() {
  const { accessToken } = useAuth()
  const [filters, setFilters] = useState({ search: '', status: 'Open', category: '', sourceType: '' })
  const [applied, setApplied] = useState(filters)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<PagedResult<Mistake> | null>(null)
  const [reload, setReload] = useState(0)
  const [editing, setEditing] = useState<Mistake | 'new' | null>(null)
  const [viewing, setViewing] = useState<Mistake | null>(null)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let alive = true
    plannerApi.getMistakes({ ...applied, search: applied.search || undefined, pageNumber: page }, accessToken)
      .then((value) => alive && setResult(value))
      .catch((reason: unknown) => alive && setError(errorMessage(reason, 'Unable to load mistakes.')))
    return () => { alive = false }
  }, [accessToken, applied, page, reload])

  if (!accessToken) return <><PageHeader title="Mistake notebook" /><Card><EmptyState title="Sign in" description="Your mistake notebook is private." /></Card></>
  const refresh = () => setReload((count) => count + 1)

  async function act(action: () => Promise<unknown>, success?: string) {
    setError('')
    try {
      await action()
      if (success) setMessage(success)
      refresh()
    } catch (reason) {
      setError(errorMessage(reason, 'The action failed.'))
    }
  }

  return (
    <>
      <PageHeader title="Mistake notebook" description="Record what you got wrong, why, and the correct answer. Entries join your revision queue (configurable) and keep their review history."
        action={<div className="flex gap-2"><Button variant="secondary" onClick={() => void act(async () => { const created = await plannerApi.importMcqMistakes(accessToken); setMessage(`${created.created} MCQ mistake${created.created === 1 ? '' : 's'} imported.`) })}><Download className="h-4 w-4" />Import MCQ mistakes</Button><Button onClick={() => setEditing('new')}><Plus className="h-4 w-4" />New entry</Button></div>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      <Card className="overflow-hidden">
        <form className="flex flex-col gap-2 border-b border-slate-100 p-4 sm:flex-row dark:border-slate-800" onSubmit={(event) => { event.preventDefault(); setPage(1); setApplied(filters) }}>
          <input aria-label="Search mistakes" placeholder="Search question, explanation, or answer" className="h-9 flex-1 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800" value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value })} />
          <select aria-label="Status" className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800" value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })}><option value="">Any status</option><option value="Open">Open</option><option value="Resolved">Resolved</option></select>
          <select aria-label="Category" className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800" value={filters.category} onChange={(event) => setFilters({ ...filters, category: event.target.value })}><option value="">Any category</option>{Object.entries(mistakeCategoryLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>
          <select aria-label="Source" className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-800" value={filters.sourceType} onChange={(event) => setFilters({ ...filters, sourceType: event.target.value })}><option value="">Any source</option><option value="Mcq">MCQ</option><option value="Written">Written</option><option value="Viva">Viva</option><option value="Other">Other</option></select>
          <Button type="submit" variant="secondary">Filter</Button>
        </form>
        {!result ? <LoadingState /> : result.items.length === 0 ? <EmptyState title="No mistakes recorded" description="Add an entry, or import your open MCQ mistakes." />
          : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{result.items.map((mistake) => <li key={mistake.id} className="px-5 py-3">
            <div className="flex items-start gap-2"><Badge tone="neutral">{mistake.sourceType}</Badge><Badge tone="warning">{mistakeCategoryLabels[mistake.category]}</Badge>{mistake.status === 'Resolved' && <Badge tone="success">Resolved</Badge>}<span className="flex-1" /><span className="text-xs text-slate-500">{mistake.reviewOn ? `review ${formatDay(mistake.reviewOn)}` : ''}</span></div>
            <button type="button" className="mt-1 block text-left text-sm font-medium hover:underline" onClick={() => void plannerApi.getMistake(mistake.id, accessToken).then(setViewing)}>{mistake.questionText}</button>
            <p className="text-xs text-slate-600 dark:text-slate-300"><strong>Correct:</strong> {mistake.correctAnswerReference}{mistake.topicTitle ? ` · ${mistake.topicTitle}` : ''}</p>
            <div className="mt-1 flex gap-1">
              <Button size="sm" variant="ghost" onClick={() => void act(() => plannerApi.setMistakeStatus(mistake.id, mistake.status === 'Open' ? 'resolve' : 'reopen', accessToken))}>{mistake.status === 'Open' ? 'Mark resolved' : 'Reopen'}</Button>
              <Button size="sm" variant="ghost" aria-label="Edit" onClick={() => setEditing(mistake)}><Pencil className="h-3.5 w-3.5" /></Button>
              <Button size="sm" variant="ghost" aria-label="Delete" onClick={() => window.confirm('Delete this entry and its revision item?') && void act(() => plannerApi.deleteMistake(mistake.id, accessToken))}><Trash2 className="h-3.5 w-3.5" /></Button>
            </div>
          </li>)}</ul>}
        {result && <Pagination page={result.pageNumber} totalPages={Math.max(result.totalPages, 1)} onPageChange={setPage} />}
      </Card>
      <Dialog open={editing !== null} title={editing === 'new' ? 'New mistake' : 'Edit mistake'} onClose={() => setEditing(null)}>
        {editing && <MistakeForm key={editing === 'new' ? 'new' : editing.id} mistake={editing === 'new' ? null : editing} onCancel={() => setEditing(null)} onSave={async (input) => {
          if (editing === 'new') await plannerApi.createMistake(input, accessToken)
          else await plannerApi.updateMistake(editing.id, input, accessToken)
          setEditing(null)
          refresh()
        }} />}
      </Dialog>
      <Dialog open={viewing !== null} title="Mistake" onClose={() => setViewing(null)}>
        {viewing && <div className="space-y-2 text-sm">
          <p className="font-medium">{viewing.questionText}</p>
          {viewing.yourAnswer && <p><strong>My answer:</strong> {viewing.yourAnswer}</p>}
          <p><strong>Correct answer / reference:</strong> {viewing.correctAnswerReference}</p>
          {viewing.explanation && <p className="whitespace-pre-wrap"><strong>Explanation:</strong> {viewing.explanation}</p>}
          <p className="text-xs text-slate-500">{mistakeCategoryLabels[viewing.category]}{viewing.topicTitle ? ` · ${viewing.topicTitle}` : ''} · recorded {formatDay(viewing.createdAt)}</p>
          <h3 className="pt-2 text-xs font-semibold uppercase tracking-wide text-slate-500">Revision history</h3>
          {viewing.history.length === 0 ? <p className="text-xs text-slate-500">Not reviewed yet.</p> : <ul className="space-y-1 text-xs">{viewing.history.map((log) => <li key={log.id}>{new Date(log.reviewedAt).toLocaleString()} · {log.outcome} · next in {log.intervalDays} d</li>)}</ul>}
        </div>}
      </Dialog>
    </>
  )
}

function MistakeForm({ mistake, onSave, onCancel }: { mistake: Mistake | null; onSave: (input: MistakeInput) => Promise<void>; onCancel: () => void }) {
  const [form, setForm] = useState({
    sourceType: (mistake?.sourceType ?? 'Other') as MistakeSourceType,
    questionId: mistake?.questionId ?? '',
    questionText: mistake?.questionText ?? '',
    yourAnswer: mistake?.yourAnswer ?? '',
    correctAnswerReference: mistake?.correctAnswerReference ?? '',
    explanation: mistake?.explanation ?? '',
    category: (mistake?.category ?? 'Concept') as MistakeCategory,
    reviewOn: mistake?.reviewOn ?? '',
  })
  const [topic, setTopic] = useState<TopicSelection | null>(null)
  const [error, setError] = useState('')

  async function submit() {
    setError('')
    try {
      await onSave({
        sourceType: form.sourceType, questionId: form.sourceType === 'Other' ? null : form.questionId.trim() || null, questionText: form.questionText.trim(),
        yourAnswer: form.yourAnswer.trim() || null, correctAnswerReference: form.correctAnswerReference.trim(), explanation: form.explanation.trim() || null,
        category: form.category, syllabusItemId: topic?.syllabusItemId ?? mistake?.syllabusItemId ?? null, reviewOn: form.reviewOn || null,
      })
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to save.'))
    }
  }

  return (
    <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void submit() }}>
      <div className="grid grid-cols-2 gap-2">
        <label className={labelClass}>Source<select className={field} value={form.sourceType} onChange={(event) => setForm({ ...form, sourceType: event.target.value as MistakeSourceType })}><option value="Other">Other / self-study</option><option value="Mcq">MCQ question</option><option value="Written">Written question</option><option value="Viva">Viva question</option></select></label>
        <label className={labelClass}>Category<select className={field} value={form.category} onChange={(event) => setForm({ ...form, category: event.target.value as MistakeCategory })}>{Object.entries(mistakeCategoryLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
      </div>
      {form.sourceType !== 'Other' && <label className={labelClass}>Question ID<input required className={field} value={form.questionId} onChange={(event) => setForm({ ...form, questionId: event.target.value })} placeholder="From the question page URL" /></label>}
      <label className={labelClass}>Question<textarea required maxLength={8000} rows={2} className={area} value={form.questionText} onChange={(event) => setForm({ ...form, questionText: event.target.value })} /></label>
      <label className={labelClass}>My answer<input maxLength={4000} className={field} value={form.yourAnswer} onChange={(event) => setForm({ ...form, yourAnswer: event.target.value })} /></label>
      <label className={labelClass}>Correct answer / reference<input required maxLength={4000} className={field} value={form.correctAnswerReference} onChange={(event) => setForm({ ...form, correctAnswerReference: event.target.value })} placeholder="e.g. Article 7; Constitution of Bangladesh" /></label>
      <label className={labelClass}>Personal explanation<textarea maxLength={8000} rows={3} className={area} value={form.explanation} onChange={(event) => setForm({ ...form, explanation: event.target.value })} /></label>
      <label className={labelClass}>Review on<input type="date" className={field} value={form.reviewOn} onChange={(event) => setForm({ ...form, reviewOn: event.target.value })} /></label>
      {mistake?.topicTitle && !topic && <p className="text-xs text-slate-500">Topic: {mistake.topicTitle}</p>}
      <TopicSelect value={topic} onChange={setTopic} label="Topic" />
      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="flex justify-end gap-2"><Button type="button" variant="ghost" onClick={onCancel}>Cancel</Button><Button type="submit">Save</Button></div>
    </form>
  )
}
