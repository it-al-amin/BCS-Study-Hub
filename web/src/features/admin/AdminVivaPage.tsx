import { Archive, CheckCircle2, FilePlus2, Plus, Save, Tags } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { ErrorState, LoadingState } from '../../components/ui/States'
import { PageHeader } from '../../components/ui/PageHeader'
import { vivaApi, type VivaAdminQuestion, type VivaCategory, type VivaCategoryInput, type VivaQuestionInput, type VivaQuestionReferenceInput, type VivaQuestionStatus } from '../../services/viva'

const blankCategory: VivaCategoryInput = {
  slug: '',
  name: '',
  description: '',
  displayOrder: 100,
  isActive: true,
}

const blankReference: VivaQuestionReferenceInput = {
  type: 'SuggestedAnswer',
  title: '',
  publisher: '',
  url: '',
  citation: '',
  verificationStatus: 'Unverified',
}

const blankQuestion: VivaQuestionInput = {
  categoryId: '',
  topic: '',
  prompt: '',
  bcsExamYear: null,
  examNumber: null,
  examReference: '',
  references: [],
}

function toQuestionInput(record: VivaAdminQuestion): VivaQuestionInput {
  return {
    categoryId: record.question.categoryId,
    topic: record.question.topic ?? '',
    prompt: record.question.prompt,
    bcsExamYear: record.question.bcsExamYear,
    examNumber: record.question.examNumber,
    examReference: record.question.examReference ?? '',
    references: record.question.references.map(({ id: _id, ...reference }) => reference),
  }
}

export function AdminVivaPage() {
  const { accessToken } = useAuth()
  const [categories, setCategories] = useState<VivaCategory[]>([])
  const [questions, setQuestions] = useState<VivaAdminQuestion[]>([])
  const [categoryInput, setCategoryInput] = useState<VivaCategoryInput>(blankCategory)
  const [selectedCategoryId, setSelectedCategoryId] = useState('')
  const [questionInput, setQuestionInput] = useState<VivaQuestionInput>(blankQuestion)
  const [selectedQuestionId, setSelectedQuestionId] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    Promise.all([
      vivaApi.getAdminCategories(accessToken),
      vivaApi.getAdminQuestions({ pageNumber: 1, pageSize: 100 }, accessToken),
    ])
      .then(([categoryResult, questionResult]) => {
        if (!active) return
        setCategories(categoryResult)
        setQuestions(questionResult.items)
        setQuestionInput((current) => ({ ...current, categoryId: current.categoryId || categoryResult[0]?.id || '' }))
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Viva administration.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken])

  function startNewCategory() {
    setSelectedCategoryId('')
    setCategoryInput(blankCategory)
    setMessage('')
  }

  function selectCategory(category: VivaCategory) {
    setSelectedCategoryId(category.id)
    setCategoryInput({
      slug: category.slug,
      name: category.name,
      description: category.description ?? '',
      displayOrder: category.displayOrder,
      isActive: category.isActive,
    })
    setMessage('')
  }

  async function saveCategory() {
    if (!accessToken) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const saved = selectedCategoryId
        ? await vivaApi.updateCategory(selectedCategoryId, categoryInput, accessToken)
        : await vivaApi.createCategory(categoryInput, accessToken)
      setCategories((current) => [saved, ...current.filter((item) => item.id !== saved.id)].sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name)))
      setSelectedCategoryId(saved.id)
      setMessage('Category saved. Seeded and created categories are editable; deactivation hides mapped questions from learners.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save this Viva category.')
    } finally {
      setSaving(false)
    }
  }

  function startNewQuestion() {
    setSelectedQuestionId('')
    setQuestionInput({ ...blankQuestion, categoryId: categories.find((category) => category.isActive)?.id ?? '' })
    setMessage('')
  }

  async function selectQuestion(id: string) {
    if (!accessToken) return
    setSelectedQuestionId(id)
    setError('')
    setMessage('')
    try {
      const record = await vivaApi.getAdminQuestion(id, accessToken)
      setQuestionInput(toQuestionInput(record))
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to load this Viva question.')
    }
  }

  function updateReference(index: number, patch: Partial<VivaQuestionReferenceInput>) {
    setQuestionInput((current) => ({
      ...current,
      references: current.references.map((reference, referenceIndex) =>
        referenceIndex === index ? { ...reference, ...patch } : reference),
    }))
  }

  async function saveQuestion() {
    if (!accessToken) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const result = selectedQuestionId
        ? await vivaApi.updateQuestion(selectedQuestionId, questionInput, accessToken)
        : await vivaApi.createQuestion(questionInput, accessToken)
      setSelectedQuestionId(result.question.id)
      setQuestionInput(toQuestionInput(result))
      setQuestions((current) => [result, ...current.filter((item) => item.question.id !== result.question.id)])
      setMessage('Viva question saved as a draft. Reference verification labels are editor-entered; no question content is seeded as official.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save this Viva question.')
    } finally {
      setSaving(false)
    }
  }

  async function setQuestionStatus(status: 'publish' | 'archive') {
    if (!accessToken || !selectedQuestionId) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const result = await vivaApi.setQuestionStatus(selectedQuestionId, status, accessToken)
      setQuestions((current) => current.map((item) => item.question.id === selectedQuestionId ? result : item))
      setQuestionInput(toQuestionInput(result))
      setMessage(status === 'publish' ? 'Viva question published.' : 'Viva question archived.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to update question status.')
    } finally {
      setSaving(false)
    }
  }

  if (!accessToken) return <ErrorState title="Sign in required" description="Viva administration requires the viva.manage permission." />

  return (
    <>
      <PageHeader title="Viva content management" description="Edit categories and author questions, topics, source references, and suggested-answer references. No official questions or BPSC topic list are preloaded." action={<Button variant="secondary" onClick={startNewQuestion}><FilePlus2 className="h-4 w-4" />New question</Button>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      {loading ? <LoadingState label="Loading Viva content..." /> : (
        <div className="space-y-5">
          <Card className="p-5">
            <CardHeader title="Editable categories" subtitle={`${categories.length} categories · Category labels are navigation taxonomy, not an official BPSC topic list.`} action={<Button size="sm" variant="secondary" onClick={startNewCategory}><Tags className="h-4 w-4" />New category</Button>} />
            <div className="grid gap-5 pt-4 xl:grid-cols-[minmax(16rem,0.8fr)_minmax(0,1.2fr)]">
              <div className="max-h-72 space-y-2 overflow-auto">{categories.map((category) => <button key={category.id} type="button" onClick={() => selectCategory(category)} className={`w-full rounded-lg border p-3 text-left ${selectedCategoryId === category.id ? 'border-emerald-600 bg-emerald-50 dark:bg-emerald-500/10' : 'border-slate-200 hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800'}`}><div className="flex items-center justify-between gap-2"><span className="text-sm font-medium">{category.name}</span><Badge>{category.isActive ? 'Active' : 'Inactive'}</Badge></div><p className="mt-1 text-xs text-slate-500">{category.questionCount} published · {category.slug}</p></button>)}</div>
              <div className="grid gap-3 sm:grid-cols-2">
                <Field label="Name"><input value={categoryInput.name} onChange={(event) => setCategoryInput((current) => ({ ...current, name: event.target.value }))} maxLength={80} className={inputClass} /></Field>
                <Field label="Slug"><input value={categoryInput.slug} onChange={(event) => setCategoryInput((current) => ({ ...current, slug: event.target.value }))} maxLength={80} className={inputClass} /></Field>
                <Field label="Display order"><input type="number" min={0} max={10000} value={categoryInput.displayOrder} onChange={(event) => setCategoryInput((current) => ({ ...current, displayOrder: Number(event.target.value) }))} className={inputClass} /></Field>
                <label className="flex items-center gap-2 self-end pb-3 text-sm"><input type="checkbox" checked={categoryInput.isActive} onChange={(event) => setCategoryInput((current) => ({ ...current, isActive: event.target.checked }))} className="h-4 w-4 accent-emerald-700" />Active</label>
                <Field label="Description" className="sm:col-span-2"><textarea rows={2} maxLength={500} value={categoryInput.description ?? ''} onChange={(event) => setCategoryInput((current) => ({ ...current, description: event.target.value }))} className={inputClass} /></Field>
                <Button className="sm:col-span-2 sm:w-fit" disabled={saving} onClick={() => void saveCategory()}><Save className="h-4 w-4" />{saving ? 'Saving...' : 'Save category'}</Button>
              </div>
            </div>
          </Card>

          <div className="grid gap-5 xl:grid-cols-[minmax(16rem,0.7fr)_minmax(0,1.3fr)]">
            <Card className="h-fit overflow-hidden">
              <CardHeader title="Question bank" subtitle={`${questions.length} loaded`} />
              <div className="max-h-[70vh] divide-y divide-slate-100 overflow-auto dark:divide-slate-800">
                {questions.map((record) => <button key={record.question.id} type="button" onClick={() => void selectQuestion(record.question.id)} className={`w-full p-4 text-left ${selectedQuestionId === record.question.id ? 'bg-emerald-50 dark:bg-emerald-500/10' : 'hover:bg-slate-50 dark:hover:bg-slate-800/60'}`}><div className="flex items-center justify-between gap-2"><Badge>{record.status}</Badge><span className="text-xs text-slate-500">{record.question.categoryName}</span></div><p className="mt-2 line-clamp-3 text-sm font-medium">{record.question.prompt}</p><p className="mt-1 text-xs text-slate-500">{record.question.topic || 'No topic'}</p></button>)}
                {questions.length === 0 && <p className="p-5 text-sm text-slate-500">No Viva questions have been added.</p>}
              </div>
            </Card>
            <Card className="p-5">
              <CardHeader title={selectedQuestionId ? 'Edit question' : 'Create question'} subtitle="Draft questions can be edited. Published questions may be archived but not returned to draft." />
              <div className="mt-4 grid gap-3 sm:grid-cols-2">
                <Field label="Category"><select value={questionInput.categoryId} onChange={(event) => setQuestionInput((current) => ({ ...current, categoryId: event.target.value }))} className={inputClass}><option value="">Select category</option>{categories.filter((category) => category.isActive).map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></Field>
                <Field label="Topic / subtopic"><input value={questionInput.topic ?? ''} onChange={(event) => setQuestionInput((current) => ({ ...current, topic: event.target.value }))} maxLength={160} placeholder="Optional editor-defined topic" className={inputClass} /></Field>
                <Field label="Question text" className="sm:col-span-2"><textarea rows={4} maxLength={10000} value={questionInput.prompt} onChange={(event) => setQuestionInput((current) => ({ ...current, prompt: event.target.value }))} className={inputClass} /></Field>
                <Field label="BCS exam year (optional)"><input type="number" min={1900} max={2200} value={questionInput.bcsExamYear ?? ''} onChange={(event) => setQuestionInput((current) => ({ ...current, bcsExamYear: event.target.value ? Number(event.target.value) : null }))} className={inputClass} /></Field>
                <Field label="Exam number (optional)"><input type="number" min={1} max={100} value={questionInput.examNumber ?? ''} onChange={(event) => setQuestionInput((current) => ({ ...current, examNumber: event.target.value ? Number(event.target.value) : null }))} className={inputClass} /></Field>
                <Field label="Exam reference (optional)" className="sm:col-span-2"><input maxLength={160} value={questionInput.examReference ?? ''} onChange={(event) => setQuestionInput((current) => ({ ...current, examReference: event.target.value }))} className={inputClass} /></Field>
              </div>

              <div className="mt-5 border-t border-slate-100 pt-4 dark:border-slate-800">
                <div className="flex items-center justify-between gap-2"><div><h3 className="text-sm font-semibold">References</h3><p className="mt-1 text-xs text-slate-500">Optional question sources or suggested-answer reading. Verification status is an editor assertion.</p></div><Button size="sm" variant="secondary" disabled={questionInput.references.length >= 20} onClick={() => setQuestionInput((current) => ({ ...current, references: [...current.references, { ...blankReference }] }))}><Plus className="h-4 w-4" />Add reference</Button></div>
                <div className="mt-3 space-y-3">{questionInput.references.map((reference, index) => <div key={index} className="grid gap-2 rounded-lg border border-slate-200 p-3 sm:grid-cols-2 dark:border-slate-700">
                  <select aria-label="Reference type" value={reference.type} onChange={(event) => updateReference(index, { type: event.target.value as VivaQuestionReferenceInput['type'] })} className={inputClass}><option value="QuestionSource">Question source</option><option value="SuggestedAnswer">Suggested answer</option></select>
                  <select aria-label="Verification status" value={reference.verificationStatus} onChange={(event) => updateReference(index, { verificationStatus: event.target.value as VivaQuestionReferenceInput['verificationStatus'] })} className={inputClass}>{['Unverified', 'Verified', 'NeedsReview'].map((status) => <option key={status}>{status}</option>)}</select>
                  <input aria-label="Reference title" placeholder="Reference title" value={reference.title} onChange={(event) => updateReference(index, { title: event.target.value })} maxLength={300} className={inputClass} />
                  <input aria-label="Publisher" placeholder="Publisher" value={reference.publisher} onChange={(event) => updateReference(index, { publisher: event.target.value })} maxLength={180} className={inputClass} />
                  <input aria-label="HTTPS URL" placeholder="https://..." value={reference.url} onChange={(event) => updateReference(index, { url: event.target.value })} maxLength={512} className={inputClass} />
                  <input aria-label="Citation" placeholder="Citation (optional)" value={reference.citation ?? ''} onChange={(event) => updateReference(index, { citation: event.target.value })} maxLength={500} className={inputClass} />
                  <Button size="sm" variant="ghost" className="w-fit text-red-700" onClick={() => setQuestionInput((current) => ({ ...current, references: current.references.filter((_, referenceIndex) => referenceIndex !== index) }))}>Remove reference</Button>
                </div>)}</div>
              </div>
              <div className="mt-5 flex flex-wrap gap-2 border-t border-slate-100 pt-4 dark:border-slate-800">
                <Button disabled={saving || !categories.some((category) => category.id === questionInput.categoryId && category.isActive)} onClick={() => void saveQuestion()}><Save className="h-4 w-4" />{saving ? 'Saving...' : 'Save draft'}</Button>
                {selectedQuestionId && <Button variant="secondary" disabled={saving || questions.find((item) => item.question.id === selectedQuestionId)?.status === 'Archived'} onClick={() => void setQuestionStatus('publish')}><CheckCircle2 className="h-4 w-4" />Publish</Button>}
                {selectedQuestionId && <Button variant="secondary" disabled={saving || questions.find((item) => item.question.id === selectedQuestionId)?.status === 'Archived'} onClick={() => void setQuestionStatus('archive')}><Archive className="h-4 w-4" />Archive</Button>}
              </div>
              {selectedQuestionId && <p className="mt-3 text-xs text-slate-500">Current status: {questions.find((item) => item.question.id === selectedQuestionId)?.status as VivaQuestionStatus | undefined}</p>}
            </Card>
          </div>
        </div>
      )}
    </>
  )
}

const inputClass = 'mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white'

function Field({ label, children, className = '' }: { label: string; children: React.ReactNode; className?: string }) {
  return <label className={`block text-xs font-medium text-slate-500 ${className}`}>{label}{children}</label>
}
