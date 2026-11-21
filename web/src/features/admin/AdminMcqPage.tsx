import { Archive, Check, Plus, Save, Tags } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { Field, Input, Select, Textarea } from '../../components/ui/Form'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import {
  mcqApi,
  type McqAdminQuestion,
  type McqDifficulty,
  type McqExamSet,
  type McqExamSetInput,
  type McqOptionInput,
  type McqPracticeMode,
  type McqQuestionInput,
  type McqQuestionSource,
  type McqQuestionType,
} from '../../services/mcq'
import { syllabusApi, type SyllabusItem, type SyllabusTree, type SyllabusVersion } from '../../services/syllabus'

type AdminTab = 'questions' | 'sets'
type QuestionSourceInput = Omit<McqQuestionSource, 'id'>
type QuestionForm = {
  id: string
  syllabusVersionId: string
  syllabusItemId: string
  prompt: string
  explanation: string
  imageUrl: string
  type: McqQuestionType
  difficulty: McqDifficulty
  bcsExamYear: string
  examNumber: string
  examReference: string
  options: McqOptionInput[]
  sources: QuestionSourceInput[]
  tags: string
}
type SetForm = {
  id: string
  syllabusVersionId: string
  subjectId: string
  syllabusItemId: string
  name: string
  instructions: string
  mode: McqPracticeMode
  questionCount: string
  durationSeconds: string
  bcsExamYear: string
  difficulty: string
  marksCorrect: string
  marksIncorrect: string
  marksUnanswered: string
  showFeedbackImmediately: boolean
}

const blankQuestion: QuestionForm = {
  id: '',
  syllabusVersionId: '',
  syllabusItemId: '',
  prompt: '',
  explanation: '',
  imageUrl: '',
  type: 'SingleAnswer',
  difficulty: 'Medium',
  bcsExamYear: '',
  examNumber: '',
  examReference: '',
  options: [{ text: '', isCorrect: false }, { text: '', isCorrect: false }],
  sources: [{ title: '', publisher: 'Bangladesh Public Service Commission', url: '', citation: '', publishedOn: '', verificationStatus: 'Unverified' }],
  tags: '',
}

const blankSet: SetForm = {
  id: '',
  syllabusVersionId: '',
  subjectId: '',
  syllabusItemId: '',
  name: '',
  instructions: '',
  mode: 'Mixed',
  questionCount: '20',
  durationSeconds: '',
  bcsExamYear: '',
  difficulty: '',
  marksCorrect: '',
  marksIncorrect: '',
  marksUnanswered: '',
  showFeedbackImmediately: false,
}

function flattenItems(items: SyllabusItem[]): SyllabusItem[] {
  return items.flatMap((item) => [item, ...flattenItems(item.children)])
}

function toQuestionForm(question: McqAdminQuestion): QuestionForm {
  return {
    id: question.id,
    syllabusVersionId: question.syllabusVersionId,
    syllabusItemId: question.syllabusItemId,
    prompt: question.prompt,
    explanation: question.explanation ?? '',
    imageUrl: question.imageUrl ?? '',
    type: question.type,
    difficulty: question.difficulty,
    bcsExamYear: question.bcsExamYear?.toString() ?? '',
    examNumber: question.examNumber?.toString() ?? '',
    examReference: question.examReference ?? '',
    options: question.options.map((option) => ({ text: option.text, isCorrect: option.isCorrect === true })),
    sources: question.sources.map(({ id: _id, ...source }) => ({ ...source, citation: source.citation ?? '', publishedOn: source.publishedOn ?? '' })),
    tags: question.tags.join(', '),
  }
}

function toSetForm(set: McqExamSet): SetForm {
  return {
    id: set.id,
    syllabusVersionId: set.syllabusVersionId,
    subjectId: set.subjectId ?? '',
    syllabusItemId: set.syllabusItemId ?? '',
    name: set.name,
    instructions: set.instructions,
    mode: set.mode,
    questionCount: set.questionCount.toString(),
    durationSeconds: set.durationSeconds?.toString() ?? '',
    bcsExamYear: set.bcsExamYear?.toString() ?? '',
    difficulty: set.difficulty ?? '',
    marksCorrect: set.marksCorrect.toString(),
    marksIncorrect: set.marksIncorrect.toString(),
    marksUnanswered: set.marksUnanswered.toString(),
    showFeedbackImmediately: set.showFeedbackImmediately,
  }
}

export function AdminMcqPage() {
  const { accessToken } = useAuth()
  const [tab, setTab] = useState<AdminTab>('questions')
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [questionTree, setQuestionTree] = useState<SyllabusTree | null>(null)
  const [setTree, setSetTree] = useState<SyllabusTree | null>(null)
  const [questions, setQuestions] = useState<McqAdminQuestion[]>([])
  const [sets, setSets] = useState<McqExamSet[]>([])
  const [questionForm, setQuestionForm] = useState<QuestionForm>(blankQuestion)
  const [setForm, setSetForm] = useState<SetForm>(blankSet)
  const [questionSearch, setQuestionSearch] = useState('')
  const [saving, setSaving] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const questionItems = useMemo(
    () => questionTree?.subjects.flatMap((subject) => flattenItems(subject.items).map((item) => ({
      ...item,
      subjectId: subject.subjectId,
      subjectName: subject.subjectName,
    }))) ?? [],
    [questionTree],
  )
  const setTopics = useMemo(
    () => setTree?.subjects.flatMap((subject) => flattenItems(subject.items).filter((item) => item.verificationStatus === 'Verified').map((item) => ({
      ...item,
      subjectId: subject.subjectId,
      subjectName: subject.subjectName,
    }))) ?? [],
    [setTree],
  )

  const loadAdminData = useCallback(async () => {
    if (!accessToken) return
    setLoading(true)
    setError('')
    try {
      const [versionPage, questionPage, setPage] = await Promise.all([
        syllabusApi.getVersions({ stageCode: 'Preliminary', pageSize: 100 }),
        mcqApi.getAdminQuestions({ pageNumber: 1, pageSize: 50, search: questionSearch || undefined }, accessToken),
        mcqApi.getAdminExamSets({ pageNumber: 1, pageSize: 100 }, accessToken),
      ])
      setVersions(versionPage.items)
      setQuestions(questionPage.items)
      setSets(setPage.items)
      setQuestionForm((current) => ({
        ...current,
        syllabusVersionId: current.syllabusVersionId || versionPage.items[0]?.id || '',
      }))
      setSetForm((current) => ({
        ...current,
        syllabusVersionId: current.syllabusVersionId || versionPage.items[0]?.id || '',
      }))
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to load MCQ administration data.')
    } finally {
      setLoading(false)
    }
  }, [accessToken, questionSearch])

  useEffect(() => { void loadAdminData() }, [loadAdminData])

  useEffect(() => {
    let active = true
    if (!questionForm.syllabusVersionId) {
      setQuestionTree(null)
      return () => { active = false }
    }
    syllabusApi.getTree(questionForm.syllabusVersionId)
      .then((result) => active && setQuestionTree(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load syllabus topics.'))
    return () => { active = false }
  }, [questionForm.syllabusVersionId])

  useEffect(() => {
    let active = true
    if (!setForm.syllabusVersionId) {
      setSetTree(null)
      return () => { active = false }
    }
    syllabusApi.getTree(setForm.syllabusVersionId)
      .then((result) => active && setSetTree(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load set topics.'))
    return () => { active = false }
  }, [setForm.syllabusVersionId])

  function changeQuestionForm<K extends keyof QuestionForm>(key: K, value: QuestionForm[K]) {
    setQuestionForm((current) => ({ ...current, [key]: value }))
  }

  function changeSetForm<K extends keyof SetForm>(key: K, value: SetForm[K]) {
    setSetForm((current) => ({ ...current, [key]: value }))
  }

  function editQuestion(question: McqAdminQuestion) {
    setQuestionForm(toQuestionForm(question))
  }

  async function saveQuestion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken) return
    setError('')
    setNotice('')
    setSaving(true)
    const input: McqQuestionInput = {
      syllabusItemId: questionForm.syllabusItemId,
      prompt: questionForm.prompt,
      explanation: questionForm.explanation || null,
      imageUrl: questionForm.imageUrl || null,
      type: questionForm.type,
      difficulty: questionForm.difficulty,
      bcsExamYear: questionForm.bcsExamYear ? Number(questionForm.bcsExamYear) : null,
      examNumber: questionForm.examNumber ? Number(questionForm.examNumber) : null,
      examReference: questionForm.examReference || null,
      options: questionForm.options,
      sources: questionForm.sources.map((source) => ({
        ...source,
        citation: source.citation || null,
        publishedOn: source.publishedOn || null,
      })),
      tags: questionForm.tags.split(',').map((tag) => tag.trim()).filter(Boolean),
    }
    try {
      const saved = questionForm.id
        ? await mcqApi.updateQuestion(questionForm.id, input, accessToken)
        : await mcqApi.createQuestion(input, accessToken)
      setQuestionForm(toQuestionForm(saved))
      setNotice('Question draft saved.')
      await loadAdminData()
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save question.')
    } finally {
      setSaving(false)
    }
  }

  async function setQuestionStatus(question: McqAdminQuestion, status: 'publish' | 'archive') {
    if (!accessToken) return
    setError('')
    try {
      const updated = await mcqApi.setQuestionStatus(question.id, status, accessToken)
      setQuestions((current) => current.map((item) => item.id === updated.id ? updated : item))
      setNotice(status === 'publish' ? 'Question published.' : 'Question archived.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to change question status.')
    }
  }

  async function saveSet(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken) return
    setError('')
    setNotice('')
    setSaving(true)
    const input: McqExamSetInput = {
      syllabusVersionId: setForm.syllabusVersionId,
      subjectId: setForm.subjectId || null,
      syllabusItemId: setForm.syllabusItemId || null,
      name: setForm.name,
      instructions: setForm.instructions,
      mode: setForm.mode,
      questionCount: Number(setForm.questionCount),
      durationSeconds: setForm.durationSeconds ? Number(setForm.durationSeconds) : null,
      bcsExamYear: setForm.bcsExamYear ? Number(setForm.bcsExamYear) : null,
      difficulty: setForm.difficulty ? setForm.difficulty as McqDifficulty : null,
      marksCorrect: Number(setForm.marksCorrect),
      marksIncorrect: Number(setForm.marksIncorrect),
      marksUnanswered: Number(setForm.marksUnanswered),
      showFeedbackImmediately: setForm.showFeedbackImmediately,
    }
    try {
      const saved = setForm.id
        ? await mcqApi.updateExamSet(setForm.id, input, accessToken)
        : await mcqApi.createExamSet(input, accessToken)
      setSetForm(toSetForm(saved))
      setNotice('Practice set configuration saved.')
      await loadAdminData()
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save practice set.')
    } finally {
      setSaving(false)
    }
  }

  async function setSetStatus(set: McqExamSet, status: 'publish' | 'archive') {
    if (!accessToken) return
    setError('')
    try {
      const updated = await mcqApi.setExamSetStatus(set.id, status, accessToken)
      setSets((current) => current.map((item) => item.id === updated.id ? updated : item))
      setNotice(status === 'publish' ? 'Practice set published.' : 'Practice set archived.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to change practice set status.')
    }
  }

  function editSet(set: McqExamSet) {
    setSetForm(toSetForm(set))
  }

  function addOption() {
    if (questionForm.options.length < 8) changeQuestionForm('options', [...questionForm.options, { text: '', isCorrect: false }])
  }

  function addSource() {
    if (questionForm.sources.length < 10) changeQuestionForm('sources', [...questionForm.sources, { title: '', publisher: '', url: '', citation: '', publishedOn: '', verificationStatus: 'Unverified' }])
  }

  const displayedQuestions = questions
  if (loading) return <LoadingState label="Loading MCQ administration..." />

  return (
    <>
      <PageHeader title="MCQ administration" description="Manage source-attributed Preliminary questions and dynamic practice or mock-exam configurations." />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {notice && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-200" role="status">{notice}</p>}
      <div className="mb-5 flex gap-2 border-b border-slate-200 dark:border-slate-800">
        <button type="button" onClick={() => setTab('questions')} className={`border-b-2 px-3 py-2 text-sm font-medium ${tab === 'questions' ? 'border-emerald-700 text-emerald-800' : 'border-transparent text-slate-500'}`}>Questions</button>
        <button type="button" onClick={() => setTab('sets')} className={`border-b-2 px-3 py-2 text-sm font-medium ${tab === 'sets' ? 'border-emerald-700 text-emerald-800' : 'border-transparent text-slate-500'}`}>Practice sets</button>
      </div>

      {tab === 'questions' ? (
        <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1.2fr)_minmax(360px,0.8fr)]">
          <Card className="overflow-hidden">
            <CardHeader title="Question bank" subtitle="Draft and published Preliminary MCQs" action={<Button variant="secondary" size="sm" onClick={() => setQuestionForm({ ...blankQuestion, syllabusVersionId: versions[0]?.id ?? '' })}><Plus className="h-4 w-4" />New question</Button>} />
            <form onSubmit={(event) => { event.preventDefault(); void loadAdminData() }} className="flex gap-2 border-b border-slate-100 p-4 dark:border-slate-800">
              <Input value={questionSearch} onChange={(event) => setQuestionSearch(event.target.value)} placeholder="Search question text..." />
              <Button variant="secondary" type="submit">Search</Button>
            </form>
            {displayedQuestions.length === 0 ? <EmptyState title="No questions found" description="Create a source-attributed question draft to begin building the bank." /> : <div className="divide-y divide-slate-100 dark:divide-slate-800">
              {displayedQuestions.map((question) => <div key={question.id} className="p-4">
                <div className="flex flex-wrap items-center gap-2"><Badge>{question.status}</Badge><Badge>{question.difficulty}</Badge><span className="text-xs text-slate-500">{question.bcsExamYear ?? 'No exam year'}{question.examNumber ? ` · ${question.examNumber}th BCS` : ''}</span></div>
                <p className="mt-2 line-clamp-2 text-sm font-medium">{question.prompt}</p>
                <p className="mt-1 text-xs text-slate-500">{question.sources.length} source(s) · {question.options.length} options</p>
                <div className="mt-3 flex flex-wrap gap-2">
                  <Button size="sm" variant="secondary" onClick={() => editQuestion(question)}>Edit</Button>
                  {question.status === 'Draft' && <Button size="sm" onClick={() => void setQuestionStatus(question, 'publish')}><Check className="h-3.5 w-3.5" />Publish</Button>}
                  {question.status !== 'Archived' && <Button size="sm" variant="ghost" onClick={() => void setQuestionStatus(question, 'archive')}><Archive className="h-3.5 w-3.5" />Archive</Button>}
                </div>
              </div>)}
            </div>}
          </Card>

          <Card className="overflow-hidden">
            <CardHeader title={questionForm.id ? 'Edit question' : 'Create question'} subtitle="Published questions require a verified Preliminary topic and verified source attribution." />
            <form onSubmit={(event) => void saveQuestion(event)} className="space-y-4 p-5">
              <Field label="Preliminary syllabus version">
                <Select required value={questionForm.syllabusVersionId} onChange={(event) => {
                  changeQuestionForm('syllabusVersionId', event.target.value)
                  changeQuestionForm('syllabusItemId', '')
                }}>
                  {versions.map((version) => <option key={version.id} value={version.id}>{version.versionLabel}</option>)}
                </Select>
              </Field>
              <Field label="Verified Preliminary topic or subtopic">
                <Select required value={questionForm.syllabusItemId} onChange={(event) => changeQuestionForm('syllabusItemId', event.target.value)}>
                  <option value="">Select a topic</option>
                  {questionItems.filter((item) => item.verificationStatus === 'Verified').map((item) => <option key={item.id} value={item.id}>{item.subjectName} · {item.title}</option>)}
                </Select>
              </Field>
              {questionItems.length === 0 && <p className="text-xs text-amber-700 dark:text-amber-300">Add verified topics to a published Preliminary syllabus before authoring questions.</p>}
              <Field label="Question text"><Textarea required minLength={1} maxLength={5000} value={questionForm.prompt} onChange={(event) => changeQuestionForm('prompt', event.target.value)} /></Field>
              <div className="grid gap-3 sm:grid-cols-2">
                <Field label="Answer type"><Select value={questionForm.type} onChange={(event) => changeQuestionForm('type', event.target.value as McqQuestionType)}><option value="SingleAnswer">Single answer</option><option value="MultipleAnswer">Multiple answer</option></Select></Field>
                <Field label="Difficulty"><Select value={questionForm.difficulty} onChange={(event) => changeQuestionForm('difficulty', event.target.value as McqDifficulty)}><option value="Easy">Easy</option><option value="Medium">Medium</option><option value="Hard">Hard</option></Select></Field>
              </div>
              <div className="space-y-2">
                <div className="flex items-center justify-between"><h3 className="text-sm font-medium">Options and correct answers</h3><Button type="button" size="sm" variant="secondary" disabled={questionForm.options.length >= 8} onClick={addOption}><Plus className="h-3.5 w-3.5" />Add option</Button></div>
                {questionForm.options.map((option, index) => <div key={index} className="flex items-center gap-2">
                  <Input required maxLength={2000} aria-label={`Option ${index + 1}`} value={option.text} onChange={(event) => changeQuestionForm('options', questionForm.options.map((item, itemIndex) => itemIndex === index ? { ...item, text: event.target.value } : item))} />
                  <label className="flex shrink-0 items-center gap-1 text-xs"><input type={questionForm.type === 'SingleAnswer' ? 'radio' : 'checkbox'} name="correct-option" checked={option.isCorrect} onChange={() => changeQuestionForm('options', questionForm.options.map((item, itemIndex) => ({ ...item, isCorrect: questionForm.type === 'SingleAnswer' ? itemIndex === index : itemIndex === index ? !item.isCorrect : item.isCorrect })))} />Correct</label>
                  <button type="button" disabled={questionForm.options.length <= 2} onClick={() => changeQuestionForm('options', questionForm.options.filter((_, itemIndex) => itemIndex !== index))} className="text-xs text-red-600 disabled:opacity-30">Remove</button>
                </div>)}
                <p className="text-xs text-slate-500">Multiple-answer scoring requires selecting the exact correct option set; partial credit is not assumed.</p>
              </div>
              <Field label="Explanation"><Textarea maxLength={8000} value={questionForm.explanation} onChange={(event) => changeQuestionForm('explanation', event.target.value)} /></Field>
              <Field label="HTTPS image URL (optional)"><Input type="url" value={questionForm.imageUrl} onChange={(event) => changeQuestionForm('imageUrl', event.target.value)} placeholder="https://..." /></Field>
              <div className="grid gap-3 sm:grid-cols-3">
                <Field label="BCS exam year"><Input type="number" min={1900} max={2200} value={questionForm.bcsExamYear} onChange={(event) => changeQuestionForm('bcsExamYear', event.target.value)} /></Field>
                <Field label="Exam number"><Input type="number" min={1} max={100} value={questionForm.examNumber} onChange={(event) => changeQuestionForm('examNumber', event.target.value)} /></Field>
                <Field label="Exam reference"><Input maxLength={160} value={questionForm.examReference} onChange={(event) => changeQuestionForm('examReference', event.target.value)} placeholder="Paper / session" /></Field>
              </div>
              <Field label="Tags (comma-separated)"><Input value={questionForm.tags} onChange={(event) => changeQuestionForm('tags', event.target.value)} placeholder="e.g. constitution, geography" /></Field>
              <div className="space-y-3">
                <div className="flex items-center justify-between"><h3 className="text-sm font-medium">Source references</h3><Button type="button" size="sm" variant="secondary" disabled={questionForm.sources.length >= 10} onClick={addSource}><Plus className="h-3.5 w-3.5" />Add source</Button></div>
                {questionForm.sources.map((source, index) => <div key={index} className="space-y-2 rounded-lg border border-slate-200 p-3 dark:border-slate-700">
                  <Field label="Title"><Input required maxLength={300} value={source.title} onChange={(event) => changeQuestionForm('sources', questionForm.sources.map((item, itemIndex) => itemIndex === index ? { ...item, title: event.target.value } : item))} /></Field>
                  <div className="grid gap-2 sm:grid-cols-2">
                    <Field label="Publisher"><Input required maxLength={180} value={source.publisher} onChange={(event) => changeQuestionForm('sources', questionForm.sources.map((item, itemIndex) => itemIndex === index ? { ...item, publisher: event.target.value } : item))} /></Field>
                    <Field label="Verification"><Select value={source.verificationStatus} onChange={(event) => changeQuestionForm('sources', questionForm.sources.map((item, itemIndex) => itemIndex === index ? { ...item, verificationStatus: event.target.value as QuestionSourceInput['verificationStatus'] } : item))}><option value="Unverified">Unverified</option><option value="Verified">Verified</option><option value="NeedsReview">Needs review</option></Select></Field>
                  </div>
                  <Field label="HTTPS source URL"><Input type="url" required maxLength={512} value={source.url} onChange={(event) => changeQuestionForm('sources', questionForm.sources.map((item, itemIndex) => itemIndex === index ? { ...item, url: event.target.value } : item))} /></Field>
                  <div className="grid gap-2 sm:grid-cols-2">
                    <Field label="Citation"><Input maxLength={500} value={source.citation ?? ''} onChange={(event) => changeQuestionForm('sources', questionForm.sources.map((item, itemIndex) => itemIndex === index ? { ...item, citation: event.target.value } : item))} placeholder="Page, section, or question number" /></Field>
                    <Field label="Publication date"><Input type="date" value={source.publishedOn ?? ''} onChange={(event) => changeQuestionForm('sources', questionForm.sources.map((item, itemIndex) => itemIndex === index ? { ...item, publishedOn: event.target.value } : item))} /></Field>
                  </div>
                  {questionForm.sources.length > 1 && <button type="button" onClick={() => changeQuestionForm('sources', questionForm.sources.filter((_, itemIndex) => itemIndex !== index))} className="text-xs text-red-600">Remove source</button>}
                </div>)}
              </div>
              <div className="flex flex-wrap justify-end gap-2 border-t border-slate-100 pt-4 dark:border-slate-800">
                {questionForm.id && <Button type="button" variant="ghost" onClick={() => setQuestionForm({ ...blankQuestion, syllabusVersionId: versions[0]?.id ?? '' })}>New draft</Button>}
                <Button type="submit" disabled={saving}><Save className="h-4 w-4" />{saving ? 'Saving...' : 'Save draft'}</Button>
              </div>
            </form>
          </Card>
        </div>
      ) : (
        <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(360px,0.8fr)]">
          <Card className="overflow-hidden">
            <CardHeader title="Dynamic practice sets" subtitle="Publishing validates the question count against currently published questions." action={<Button variant="secondary" size="sm" onClick={() => setSetForm({ ...blankSet, syllabusVersionId: versions[0]?.id ?? '' })}><Plus className="h-4 w-4" />New configuration</Button>} />
            {sets.length === 0 ? <EmptyState title="No practice sets configured" description="Create a topic, subject, mixed practice, or mock exam configuration." /> : <div className="divide-y divide-slate-100 dark:divide-slate-800">
              {sets.map((set) => <div key={set.id} className="p-4">
                <div className="flex flex-wrap items-center gap-2"><Badge>{set.status}</Badge><Badge>{set.mode}</Badge><h3 className="font-medium">{set.name}</h3></div>
                <p className="mt-1 text-xs text-slate-500">{set.questionCount} questions · {set.durationSeconds ? `${Math.floor(set.durationSeconds / 60)} min` : 'Untimed'} · +{set.marksCorrect} / {set.marksIncorrect} / {set.marksUnanswered}</p>
                <div className="mt-3 flex gap-2"><Button size="sm" variant="secondary" onClick={() => editSet(set)}>Edit configuration</Button>{set.status === 'Draft' && <Button size="sm" onClick={() => void setSetStatus(set, 'publish')}><Check className="h-3.5 w-3.5" />Publish</Button>}{set.status !== 'Archived' && <Button size="sm" variant="ghost" onClick={() => void setSetStatus(set, 'archive')}><Archive className="h-3.5 w-3.5" />Archive</Button>}</div>
              </div>)}
            </div>}
          </Card>
          <Card className="overflow-hidden">
            <CardHeader title={setForm.id ? 'Edit configuration' : 'Create configuration'} subtitle="Scoring values are editable settings; no official BCS scoring rule is assumed." />
            <form onSubmit={(event) => void saveSet(event)} className="space-y-4 p-5">
              <Field label="Preliminary syllabus version"><Select required value={setForm.syllabusVersionId} onChange={(event) => changeSetForm('syllabusVersionId', event.target.value)}><option value="">Select version</option>{versions.map((version) => <option key={version.id} value={version.id}>{version.versionLabel}</option>)}</Select></Field>
              <Field label="Set name"><Input required minLength={2} maxLength={180} value={setForm.name} onChange={(event) => changeSetForm('name', event.target.value)} /></Field>
              <Field label="Instructions"><Textarea maxLength={3000} value={setForm.instructions} onChange={(event) => changeSetForm('instructions', event.target.value)} /></Field>
              <div className="grid gap-3 sm:grid-cols-2">
                <Field label="Practice mode"><Select value={setForm.mode} onChange={(event) => changeSetForm('mode', event.target.value as McqPracticeMode)}><option value="Topic">Topic practice</option><option value="Subject">Subject practice</option><option value="Mixed">Mixed practice</option><option value="MockExam">Mock examination</option></Select></Field>
                <Field label="Question count"><Input required type="number" min={1} max={200} value={setForm.questionCount} onChange={(event) => changeSetForm('questionCount', event.target.value)} /></Field>
              </div>
              {setForm.mode === 'Subject' && <Field label="Subject"><Select required value={setForm.subjectId} onChange={(event) => changeSetForm('subjectId', event.target.value)}><option value="">Select subject</option>{setTree?.subjects.map((subject) => <option key={subject.subjectId} value={subject.subjectId}>{subject.subjectName}</option>)}</Select></Field>}
              {setForm.mode === 'Topic' && <Field label="Verified topic or subtopic"><Select required value={setForm.syllabusItemId} onChange={(event) => changeSetForm('syllabusItemId', event.target.value)}><option value="">Select topic</option>{setTopics.map((item) => <option key={item.id} value={item.id}>{item.subjectName} · {item.title}</option>)}</Select></Field>}
              <div className="grid gap-3 sm:grid-cols-3">
                <Field label="Time limit (seconds)"><Input type="number" min={1} max={86400} value={setForm.durationSeconds} onChange={(event) => changeSetForm('durationSeconds', event.target.value)} placeholder={setForm.mode === 'MockExam' ? 'Required for mock exam' : 'Optional'} /></Field>
                <Field label="BCS exam year filter"><Input type="number" min={1900} max={2200} value={setForm.bcsExamYear} onChange={(event) => changeSetForm('bcsExamYear', event.target.value)} /></Field>
                <Field label="Difficulty filter"><Select value={setForm.difficulty} onChange={(event) => changeSetForm('difficulty', event.target.value)}><option value="">Any difficulty</option><option value="Easy">Easy</option><option value="Medium">Medium</option><option value="Hard">Hard</option></Select></Field>
              </div>
              <div className="rounded-lg border border-slate-200 p-4 dark:border-slate-700">
                <div className="mb-3 flex items-center gap-2"><Tags className="h-4 w-4 text-emerald-700" /><h3 className="text-sm font-medium">Scoring configuration</h3></div>
                <div className="grid gap-3 sm:grid-cols-3">
                  <Field label="Marks per correct"><Input required type="number" min="0.01" max="100" step="0.01" value={setForm.marksCorrect} onChange={(event) => changeSetForm('marksCorrect', event.target.value)} /></Field>
                  <Field label="Marks per incorrect"><Input required type="number" min="-100" max="100" step="0.01" value={setForm.marksIncorrect} onChange={(event) => changeSetForm('marksIncorrect', event.target.value)} /></Field>
                  <Field label="Marks unanswered"><Input required type="number" min="-100" max="100" step="0.01" value={setForm.marksUnanswered} onChange={(event) => changeSetForm('marksUnanswered', event.target.value)} /></Field>
                </div>
                <label className="mt-3 flex items-center gap-2 text-xs text-slate-600 dark:text-slate-300"><input type="checkbox" checked={setForm.showFeedbackImmediately} onChange={(event) => changeSetForm('showFeedbackImmediately', event.target.checked)} />Show correctness and explanation immediately after answering</label>
              </div>
              <div className="flex justify-end gap-2 border-t border-slate-100 pt-4 dark:border-slate-800">
                {setForm.id && <Button type="button" variant="ghost" onClick={() => setSetForm({ ...blankSet, syllabusVersionId: versions[0]?.id ?? '' })}>New configuration</Button>}
                <Button type="submit" disabled={saving || !setForm.syllabusVersionId}><Save className="h-4 w-4" />{saving ? 'Saving...' : 'Save configuration'}</Button>
              </div>
            </form>
          </Card>
        </div>
      )}
      {!versions.length && tab === 'questions' && <p className="mt-3 text-xs text-slate-500">There are no published Preliminary syllabus versions available yet.</p>}
    </>
  )
}
