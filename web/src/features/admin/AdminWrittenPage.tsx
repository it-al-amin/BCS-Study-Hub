import { Archive, CheckCircle2, FilePlus2, Save } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { ErrorState, LoadingState } from '../../components/ui/States'
import { PageHeader } from '../../components/ui/PageHeader'
import { syllabusApi, type SyllabusItem, type SyllabusTree, type SyllabusVersion } from '../../services/syllabus'
import { writtenApi, type WrittenAdminQuestion, type WrittenQuestionInput, type WrittenQuestionType, type WrittenReferenceStatus, type WrittenSource } from '../../services/written'

const blankSource: Omit<WrittenSource, 'id' | 'publishedOn'> = {
  title: '',
  publisher: '',
  url: '',
  citation: '',
  verificationStatus: 'Unverified' as const,
}

const blankInput: WrittenQuestionInput = {
  syllabusItemId: '',
  prompt: '',
  type: 'Descriptive',
  bcsExamYear: null,
  examNumber: null,
  examReference: '',
  marks: null,
  answerGuidance: '',
  sources: [blankSource],
  resources: [],
  tags: [],
  modelAnswerContent: '',
  modelAnswerReferences: [],
}

function flattenItems(items: SyllabusItem[]): SyllabusItem[] {
  return items.flatMap((item) => [item, ...flattenItems(item.children)])
}

function getInputFromRecord(record: WrittenAdminQuestion): WrittenQuestionInput {
  return {
    syllabusItemId: record.question.syllabusItemId,
    prompt: record.question.prompt,
    type: record.question.type,
    bcsExamYear: record.question.bcsExamYear,
    examNumber: record.question.examNumber,
    examReference: record.question.examReference ?? '',
    marks: record.question.marks,
    answerGuidance: record.question.answerGuidance ?? '',
    sources: record.question.sources.map(({ id: _id, ...source }) => source),
    resources: record.question.resources.map(({ id: _id, ...resource }) => resource),
    tags: record.question.tags,
    modelAnswerContent: record.modelAnswerContent ?? '',
    modelAnswerReferences: record.modelAnswerReferences.map(({ id: _id, publishedOn: _publishedOn, ...source }) => source),
  }
}

export function AdminWrittenPage() {
  const { accessToken } = useAuth()
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [versionId, setVersionId] = useState('')
  const [tree, setTree] = useState<SyllabusTree | null>(null)
  const [records, setRecords] = useState<WrittenAdminQuestion[]>([])
  const [selectedId, setSelectedId] = useState('')
  const [input, setInput] = useState<WrittenQuestionInput>(blankInput)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [tagText, setTagText] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    Promise.all([
      syllabusApi.getVersions({ stageCode: 'Written', pageSize: 50 }),
      writtenApi.getAdminQuestions({ pageNumber: 1, pageSize: 100 }, accessToken),
    ])
      .then(([versionResult, questionResult]) => {
        if (!active) return
        setVersions(versionResult.items)
        setVersionId(versionResult.items[0]?.id ?? '')
        setRecords(questionResult.items)
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Written administration.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken])

  useEffect(() => {
    if (!versionId) {
      setTree(null)
      return
    }
    let active = true
    syllabusApi.getTree(versionId, accessToken ?? undefined)
      .then((result) => active && setTree(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load the syllabus topic tree.'))
    return () => { active = false }
  }, [accessToken, versionId])

  const topicOptions = useMemo(() => tree?.subjects.flatMap((subject) => flattenItems(subject.items)) ?? [], [tree])
  const selectedRecord = records.find((record) => record.question.id === selectedId)

  function startNew() {
    setSelectedId('')
    setInput({ ...blankInput, syllabusItemId: topicOptions[0]?.id ?? '' })
    setTagText('')
    setMessage('')
  }

  async function selectRecord(id: string) {
    if (!accessToken) return
    setSelectedId(id)
    setError('')
    try {
      const record = await writtenApi.getAdminQuestion(id, accessToken)
      setInput(getInputFromRecord(record))
      setTagText(record.question.tags.join(', '))
      const version = versions.find((candidate) => candidate.id === record.question.syllabusVersionId)
      if (version) setVersionId(version.id)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to load this question.')
    }
  }

  function updateSource(index: number, patch: Partial<Omit<WrittenSource, 'id' | 'publishedOn'>>) {
    setInput((current) => ({
      ...current,
      sources: current.sources.map((source, sourceIndex) => sourceIndex === index ? { ...source, ...patch } : source),
    }))
  }

  function updateModelReference(index: number, patch: Partial<Omit<WrittenSource, 'id' | 'publishedOn'>>) {
    setInput((current) => ({
      ...current,
      modelAnswerReferences: current.modelAnswerReferences.map((source, sourceIndex) =>
        sourceIndex === index ? { ...source, ...patch } : source),
    }))
  }

  async function saveQuestion() {
    if (!accessToken) return
    setSaving(true)
    setError('')
    setMessage('')
    const body = { ...input, tags: tagText.split(',').map((tag) => tag.trim()).filter(Boolean) }
    try {
      const result = selectedId
        ? await writtenApi.updateQuestion(selectedId, body, accessToken)
        : await writtenApi.createQuestion(body, accessToken)
      setSelectedId(result.question.id)
      setInput(getInputFromRecord(result))
      setTagText(result.question.tags.join(', '))
      setRecords((current) => [result, ...current.filter((record) => record.question.id !== result.question.id)])
      setMessage('Written question saved as a draft. Publishing requires verified topic and source records.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save the Written question.')
    } finally {
      setSaving(false)
    }
  }

  async function setQuestionStatus(status: 'publish' | 'archive') {
    if (!accessToken || !selectedId) return
    setSaving(true)
    setError('')
    try {
      const result = await writtenApi.setQuestionStatus(selectedId, status, accessToken)
      setRecords((current) => current.map((record) => record.question.id === selectedId ? result : record))
      setInput(getInputFromRecord(result))
      setMessage(status === 'publish' ? 'Question published.' : 'Question archived.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to update question status.')
    } finally {
      setSaving(false)
    }
  }

  async function setModelAnswerStatus(status: 'publish' | 'archive') {
    if (!accessToken || !selectedId) return
    setSaving(true)
    setError('')
    try {
      const result = await writtenApi.setModelAnswerStatus(selectedId, status, accessToken)
      setRecords((current) => current.map((record) => record.question.id === selectedId ? result : record))
      setInput(getInputFromRecord(result))
      setMessage(status === 'publish' ? 'Model answer published with verified references.' : 'Model answer archived.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to update model-answer status.')
    } finally {
      setSaving(false)
    }
  }

  if (!accessToken) return <ErrorState title="Sign in required" description="Written content administration requires the written.manage permission." />

  return (
    <>
      <PageHeader title="Written question management" description="Manage sourced questions, model answers, citations, and syllabus mapping. Unverified content cannot be published." action={<Button variant="secondary" onClick={startNew}><FilePlus2 className="h-4 w-4" />New question</Button>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      {loading ? <LoadingState label="Loading Written administration..." /> : (
        <div className="grid gap-5 xl:grid-cols-[minmax(16rem,0.65fr)_minmax(0,1.35fr)]">
          <Card className="h-fit overflow-hidden">
            <CardHeader title="Question bank" subtitle={`${records.length} loaded`} />
            <div className="max-h-[70vh] divide-y divide-slate-100 overflow-auto dark:divide-slate-800">
              {records.map((record) => <button key={record.question.id} type="button" onClick={() => void selectRecord(record.question.id)} className={`w-full p-4 text-left ${record.question.id === selectedId ? 'bg-emerald-50 dark:bg-emerald-500/10' : 'hover:bg-slate-50 dark:hover:bg-slate-800/60'}`}><div className="flex items-center justify-between gap-2"><Badge>{record.question.status}</Badge><span className="text-[11px] text-slate-500">{record.question.type}</span></div><p className="mt-2 line-clamp-3 text-sm font-medium">{record.question.prompt}</p><p className="mt-1 text-xs text-slate-500">{record.question.topicTitle}</p></button>)}
              {records.length === 0 && <p className="p-5 text-sm text-slate-500">No Written question records yet.</p>}
            </div>
          </Card>
          <Card className="p-5">
            <CardHeader title={selectedId ? 'Edit Written question' : 'Create Written question'} subtitle="Content is saved as a draft first. Use the syllabus admin to add only source-verified topics." />
            {versions.length === 0 ? <p className="mt-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-800 dark:bg-amber-950/30 dark:text-amber-300">There is no published Written syllabus version available for mapping.</p> : (
              <div className="mt-4 grid gap-4 md:grid-cols-2">
                <Field label="Syllabus version"><select value={versionId} onChange={(event) => { setVersionId(event.target.value); setInput((current) => ({ ...current, syllabusItemId: '' })) }} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950">{versions.map((version) => <option key={version.id} value={version.id}>{version.versionLabel} · {version.trackCode}</option>)}</select></Field>
                <Field label="Topic / subtopic"><select value={input.syllabusItemId} onChange={(event) => setInput((current) => ({ ...current, syllabusItemId: event.target.value }))} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950"><option value="">Select a verified syllabus item</option>{topicOptions.map((item) => <option key={item.id} value={item.id}>{item.title} · {item.verificationStatus}</option>)}</select></Field>
                {tree && tree.version.coverage === 'OutlineOnly' && <p className="md:col-span-2 rounded-lg bg-amber-50 p-3 text-xs leading-5 text-amber-800 dark:bg-amber-950/30 dark:text-amber-300">This version is marked OutlineOnly. Add verified topic/subtopic detail before publishing questions. Do not invent syllabus content.</p>}
                <Field label="Question type"><select value={input.type} onChange={(event) => setInput((current) => ({ ...current, type: event.target.value as WrittenQuestionType }))} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950">{['Descriptive', 'Analytical', 'Essay', 'Composition', 'Translation'].map((type) => <option key={type}>{type}</option>)}</select></Field>
                <Field label="BCS exam year"><input type="number" min="1900" max="2200" value={input.bcsExamYear ?? ''} onChange={(event) => setInput((current) => ({ ...current, bcsExamYear: event.target.value ? Number(event.target.value) : null }))} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                <Field label="Exam number"><input type="number" min="1" max="100" value={input.examNumber ?? ''} onChange={(event) => setInput((current) => ({ ...current, examNumber: event.target.value ? Number(event.target.value) : null }))} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                <Field label="Marks (optional)"><input type="number" min="0" max="1000" step="0.25" value={input.marks ?? ''} onChange={(event) => setInput((current) => ({ ...current, marks: event.target.value ? Number(event.target.value) : null }))} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                <Field label="Exam/source reference"><input value={input.examReference ?? ''} onChange={(event) => setInput((current) => ({ ...current, examReference: event.target.value }))} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" placeholder="Optional reference identifier" /></Field>
                <div className="md:col-span-2"><Field label="Question text"><textarea rows={4} maxLength={12000} value={input.prompt} onChange={(event) => setInput((current) => ({ ...current, prompt: event.target.value }))} className="w-full rounded-lg border border-slate-200 bg-white p-3 text-sm leading-6 dark:border-slate-700 dark:bg-slate-950" /></Field></div>
                <div className="md:col-span-2"><Field label="Answer-writing guidance"><textarea rows={3} maxLength={8000} value={input.answerGuidance ?? ''} onChange={(event) => setInput((current) => ({ ...current, answerGuidance: event.target.value }))} className="w-full rounded-lg border border-slate-200 bg-white p-3 text-sm leading-6 dark:border-slate-700 dark:bg-slate-950" /></Field></div>
                <div className="md:col-span-2 rounded-lg border border-slate-200 p-4 dark:border-slate-700">
                  <h3 className="font-semibold">Question source reference</h3>
                  {input.sources.map((source, index) => <div key={index} className="mt-3 grid gap-3 rounded-lg bg-slate-50 p-3 md:grid-cols-2 dark:bg-slate-800/40">
                    <div className="md:col-span-2 flex items-center justify-between"><span className="text-xs text-slate-500">Source {index + 1}</span>{input.sources.length > 1 && <button type="button" onClick={() => setInput((current) => ({ ...current, sources: current.sources.filter((_, sourceIndex) => sourceIndex !== index) }))} className="text-xs text-red-700 hover:underline">Remove source</button>}</div>
                    <Field label="Title"><input value={source.title} onChange={(event) => updateSource(index, { title: event.target.value })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                    <Field label="Publisher"><input value={source.publisher} onChange={(event) => updateSource(index, { publisher: event.target.value })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                    <Field label="HTTPS URL"><input type="url" value={source.url} onChange={(event) => updateSource(index, { url: event.target.value })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                    <Field label="Citation"><input value={source.citation ?? ''} onChange={(event) => updateSource(index, { citation: event.target.value })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                    <Field label="Verification"><select value={source.verificationStatus} onChange={(event) => updateSource(index, { verificationStatus: event.target.value as WrittenReferenceStatus })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950"><option>Unverified</option><option>Verified</option><option>NeedsReview</option></select></Field>
                  </div>)}
                  {input.sources.length < 10 && <button type="button" onClick={() => setInput((current) => ({ ...current, sources: [...current.sources, { ...blankSource }] }))} className="mt-3 text-xs font-medium text-emerald-700 hover:underline dark:text-emerald-400">Add another source reference</button>}
                </div>
                <div className="md:col-span-2 rounded-lg border border-slate-200 p-4 dark:border-slate-700">
                  <h3 className="font-semibold">Model answer (optional)</h3>
                  <Field label="Model answer text"><textarea rows={6} maxLength={100000} value={input.modelAnswerContent ?? ''} onChange={(event) => setInput((current) => ({ ...current, modelAnswerContent: event.target.value }))} className="mt-3 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm leading-6 dark:border-slate-700 dark:bg-slate-950" /></Field>
                  {input.modelAnswerContent?.trim() && <>
                    <div className="mt-3 space-y-3">
                      {input.modelAnswerReferences.map((reference, index) => <div key={index} className="grid gap-3 rounded-lg bg-slate-50 p-3 md:grid-cols-2 dark:bg-slate-800/40">
                        <div className="md:col-span-2 flex items-center justify-between"><span className="text-xs text-slate-500">Reference {index + 1}</span>{input.modelAnswerReferences.length > 1 && <button type="button" onClick={() => setInput((current) => ({ ...current, modelAnswerReferences: current.modelAnswerReferences.filter((_, sourceIndex) => sourceIndex !== index) }))} className="text-xs text-red-700 hover:underline">Remove reference</button>}</div>
                        <Field label="Reference title"><input value={reference.title} onChange={(event) => updateModelReference(index, { title: event.target.value })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                        <Field label="Reference publisher"><input value={reference.publisher} onChange={(event) => updateModelReference(index, { publisher: event.target.value })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                        <Field label="Reference HTTPS URL"><input type="url" value={reference.url} onChange={(event) => updateModelReference(index, { url: event.target.value })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                        <Field label="Citation"><input value={reference.citation ?? ''} onChange={(event) => updateModelReference(index, { citation: event.target.value })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                        <Field label="Reference verification"><select value={reference.verificationStatus} onChange={(event) => updateModelReference(index, { verificationStatus: event.target.value as WrittenReferenceStatus })} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950"><option>Unverified</option><option>Verified</option><option>NeedsReview</option></select></Field>
                      </div>)}
                      {input.modelAnswerReferences.length < 10 && <button type="button" onClick={() => setInput((current) => ({ ...current, modelAnswerReferences: [...current.modelAnswerReferences, { ...blankSource }] }))} className="text-xs font-medium text-emerald-700 hover:underline dark:text-emerald-400">Add another model-answer reference</button>}
                    </div>
                    {selectedRecord?.modelAnswerStatus && <p className="mt-3 text-xs text-slate-500">Model answer status: {selectedRecord.modelAnswerStatus}. Editing content returns it to draft and requires re-publication.</p>}
                  </>}
                </div>
                <div className="md:col-span-2 rounded-lg border border-slate-200 p-4 dark:border-slate-700">
                  <h3 className="font-semibold">Related resource links</h3>
                  {input.resources.map((resource, index) => <div key={index} className="mt-3 grid items-end gap-3 md:grid-cols-[1fr_1fr_auto]">
                    <Field label="Resource title"><input value={resource.title} onChange={(event) => setInput((current) => ({ ...current, resources: current.resources.map((item, itemIndex) => itemIndex === index ? { ...item, title: event.target.value } : item) }))} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                    <Field label="HTTPS URL"><input type="url" value={resource.url} onChange={(event) => setInput((current) => ({ ...current, resources: current.resources.map((item, itemIndex) => itemIndex === index ? { ...item, url: event.target.value } : item) }))} className="h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                    <button type="button" onClick={() => setInput((current) => ({ ...current, resources: current.resources.filter((_, itemIndex) => itemIndex !== index) }))} className="mb-2 text-xs text-red-700 hover:underline">Remove</button>
                  </div>)}
                  {input.resources.length < 20 && <button type="button" onClick={() => setInput((current) => ({ ...current, resources: [...current.resources, { title: '', url: '' }] }))} className="mt-3 text-xs font-medium text-emerald-700 hover:underline dark:text-emerald-400">Add a related resource</button>}
                </div>
                <Field label="Tags (comma-separated)"><input value={tagText} onChange={(event) => setTagText(event.target.value)} className="h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" /></Field>
                <div className="md:col-span-2 flex flex-wrap justify-end gap-2">
                  <Button variant="secondary" disabled={saving} onClick={() => void saveQuestion()}><Save className="h-4 w-4" />{saving ? 'Saving...' : 'Save draft'}</Button>
                  {selectedId && selectedRecord?.question.status === 'Draft' && <Button disabled={saving} onClick={() => void setQuestionStatus('publish')}><CheckCircle2 className="h-4 w-4" />Publish question</Button>}
                  {selectedId && selectedRecord?.question.status === 'Published' && <Button variant="secondary" disabled={saving} onClick={() => void setQuestionStatus('archive')}><Archive className="h-4 w-4" />Archive question</Button>}
                  {selectedId && selectedRecord?.question.status === 'Published' && selectedRecord.modelAnswerContent && selectedRecord.modelAnswerStatus === 'Draft' && <Button disabled={saving} onClick={() => void setModelAnswerStatus('publish')}>Publish model answer</Button>}
                  {selectedId && selectedRecord?.modelAnswerStatus === 'Published' && <Button variant="secondary" disabled={saving} onClick={() => void setModelAnswerStatus('archive')}>Archive model answer</Button>}
                </div>
              </div>
            )}
          </Card>
        </div>
      )}
    </>
  )
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return <label className="block text-xs font-medium text-slate-600 dark:text-slate-300">{label}<span className="mt-1 block">{children}</span></label>
}
