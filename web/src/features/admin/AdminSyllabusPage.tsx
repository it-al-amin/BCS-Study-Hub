import { AlertCircle, Archive, Check, Plus, Save, Trash2 } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { Field, Input, Select, Textarea } from '../../components/ui/Form'
import { PageHeader } from '../../components/ui/PageHeader'
import { useAuth } from '../../app/useAuth'
import {
  syllabusApi,
  type ExamStage,
  type SyllabusItem,
  type SyllabusItemInput,
  type SyllabusPaper,
  type SyllabusSource,
  type SyllabusSourceInput,
  type SyllabusSubject,
  type SyllabusVersion,
  type VersionInput,
  type SubjectRecord,
} from '../../services/syllabus'

type AdminTab = 'overview' | 'structure' | 'sources' | 'stages'
type FormVersion = VersionInput
type SourceStatus = SyllabusSourceInput['verificationStatus']
type ItemStatus = SyllabusItemInput['verificationStatus']

const emptyVersion: FormVersion = {
  versionLabel: '',
  trackCode: 'Common',
  summary: '',
  coverage: 'OutlineOnly',
  effectiveFrom: null,
  effectiveTo: null,
}

const emptySource: SyllabusSourceInput = {
  title: '',
  publisher: 'Bangladesh Public Service Commission',
  url: '',
  sourceType: 'OfficialPage',
  publishedOn: null,
  lastVerifiedOn: null,
  verificationStatus: 'Unverified',
  notes: '',
}

function flattenItems(items: SyllabusItem[]): SyllabusItem[] {
  return items.flatMap((item) => [item, ...flattenItems(item.children)])
}

function emptyItem(mappingId: string): SyllabusItemInput {
  return {
    syllabusSubjectId: mappingId,
    paperId: null,
    parentId: null,
    title: '',
    description: '',
    sortOrder: 1,
    verificationStatus: 'Unverified',
  }
}

export function AdminSyllabusPage() {
  const { accessToken } = useAuth()
  const [stages, setStages] = useState<ExamStage[]>([])
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [subjects, setSubjects] = useState<SubjectRecord[]>([])
  const [tree, setTree] = useState<{ version: SyllabusVersion; subjects: SyllabusSubject[]; sources: SyllabusSource[] } | null>(null)
  const [selectedVersionId, setSelectedVersionId] = useState('')
  const [selectedStageId, setSelectedStageId] = useState('')
  const [selectedSubjectId, setSelectedSubjectId] = useState('')
  const [selectedMappingId, setSelectedMappingId] = useState('')
  const [selectedParentId, setSelectedParentId] = useState('')
  const [tab, setTab] = useState<AdminTab>('overview')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [versionForm, setVersionForm] = useState<FormVersion>(emptyVersion)
  const [createVersionForm, setCreateVersionForm] = useState<FormVersion>(emptyVersion)
  const [subjectForm, setSubjectForm] = useState({ code: '', name: '', description: '' })
  const [mappingForm, setMappingForm] = useState({ sortOrder: 1, marks: '', isRequired: true })
  const [paperForm, setPaperForm] = useState({ code: '', name: '', sortOrder: 1, maxMarks: '' })
  const [editingPaperId, setEditingPaperId] = useState('')
  const [papers, setPapers] = useState<SyllabusPaper[]>([])
  const [itemKind, setItemKind] = useState<'Topic' | 'Subtopic'>('Topic')
  const [itemForm, setItemForm] = useState<SyllabusItemInput>(emptyItem(''))
  const [editingItemId, setEditingItemId] = useState('')
  const [sourceForm, setSourceForm] = useState<SyllabusSourceInput>(emptySource)
  const [editingSourceId, setEditingSourceId] = useState('')
  const [linkSourceId, setLinkSourceId] = useState('')
  const [itemSourceIds, setItemSourceIds] = useState<Record<string, string>>({})
  const [stageForm, setStageForm] = useState({ name: '', localizedName: '', sortOrder: 1, isActive: true })

  const selectedVersion = versions.find((version) => version.id === selectedVersionId) ?? null
  const selectedStage = stages.find((stage) => stage.id === selectedStageId) ?? null
  const selectedMapping = tree?.subjects.find((subject) => subject.id === selectedMappingId) ?? null
  const topicOptions = useMemo(
    () => tree?.subjects.flatMap((subject) => flattenItems(subject.items).filter((item) => item.kind === 'Topic')) ?? [],
    [tree],
  )

  const loadAdminData = useCallback(async () => {
    if (!accessToken) return
    setLoading(true)
    setError('')
    try {
      const [stageRows, versionPage, subjectPage] = await Promise.all([
        syllabusApi.getAdminStages(accessToken),
        syllabusApi.getAdminVersions({ pageSize: 100, sortBy: 'updated' }, accessToken),
        syllabusApi.getSubjects(accessToken),
      ])
      setStages(stageRows)
      setVersions(versionPage.items)
      setSubjects(subjectPage.items)
      setSelectedVersionId((current) => current && versionPage.items.some((version) => version.id === current)
        ? current
        : versionPage.items[0]?.id ?? '')
      setSelectedStageId((current) => current && stageRows.some((stage) => stage.id === current)
        ? current
        : stageRows[0]?.id ?? '')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to load syllabus administration data.')
    } finally {
      setLoading(false)
    }
  }, [accessToken])

  const refreshTree = useCallback(async (versionId = selectedVersionId) => {
    if (!accessToken || !versionId) {
      setTree(null)
      return
    }
    setError('')
    try {
      const result = await syllabusApi.getAdminTree(versionId, accessToken)
      setTree(result)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to load this syllabus version.')
    }
  }, [accessToken, selectedVersionId])

  useEffect(() => { void loadAdminData() }, [loadAdminData])
  useEffect(() => { void refreshTree() }, [refreshTree])
  useEffect(() => {
    if (selectedVersion) {
      setVersionForm({
        versionLabel: selectedVersion.versionLabel,
        trackCode: selectedVersion.trackCode,
        summary: selectedVersion.summary,
        coverage: selectedVersion.coverage,
        effectiveFrom: selectedVersion.effectiveFrom,
        effectiveTo: selectedVersion.effectiveTo,
      })
    }
  }, [selectedVersion])
  useEffect(() => {
    if (selectedMapping) {
      setItemForm(emptyItem(selectedMapping.id))
      setMappingForm({
        sortOrder: selectedMapping.sortOrder,
        marks: selectedMapping.marks === null ? '' : String(selectedMapping.marks),
        isRequired: selectedMapping.isRequired,
      })
    }
  }, [selectedMapping])
  useEffect(() => {
    if (selectedStage) {
      setStageForm({
        name: selectedStage.name,
        localizedName: selectedStage.localizedName,
        sortOrder: selectedStage.sortOrder,
        isActive: selectedStage.isActive,
      })
    }
  }, [selectedStage])
  useEffect(() => {
    let active = true
    if (!accessToken || !selectedMappingId) {
      setPapers([])
      return () => { active = false }
    }
    syllabusApi.getAdminPapers(selectedMappingId, accessToken)
      .then((result) => active && setPapers(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load papers.'))
    return () => { active = false }
  }, [accessToken, selectedMappingId])

  async function runAction(action: () => Promise<unknown>, successMessage: string, refresh = true) {
    setError('')
    setNotice('')
    setSaving(true)
    try {
      await action()
      setNotice(successMessage)
      if (refresh) {
        await loadAdminData()
        await refreshTree()
      }
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'The requested change could not be completed.')
    } finally {
      setSaving(false)
    }
  }

  function toVersionInput(form: FormVersion): FormVersion {
    return {
      ...form,
      effectiveFrom: form.effectiveFrom || null,
      effectiveTo: form.effectiveTo || null,
    }
  }

  async function handleCreateVersion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken || !selectedStageId) return
    await runAction(
      async () => {
        const created = await syllabusApi.createVersion(selectedStageId, toVersionInput(createVersionForm), accessToken)
        setSelectedVersionId(created.id)
        setCreateVersionForm(emptyVersion)
      },
      'Draft syllabus version created.',
    )
  }

  async function handleSaveVersion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken || !selectedVersionId) return
    await runAction(
      () => syllabusApi.updateVersion(selectedVersionId, toVersionInput(versionForm), accessToken),
      'Syllabus version updated.',
    )
  }

  async function handleAssignSubject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken || !selectedVersionId || !selectedSubjectId) return
    await runAction(
      async () => {
        const mapping = await syllabusApi.assignSubject(selectedVersionId, {
          subjectId: selectedSubjectId,
          sortOrder: mappingForm.sortOrder,
          marks: mappingForm.marks === '' ? null : Number(mappingForm.marks),
          isRequired: mappingForm.isRequired,
        }, accessToken)
        setSelectedMappingId(mapping.id)
      },
      'Subject added to the draft version.',
    )
  }

  async function handleCreateSubject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken) return
    await runAction(
      async () => {
        const result = await syllabusApi.createSubject({
          code: subjectForm.code,
          name: subjectForm.name,
          description: subjectForm.description || null,
        }, accessToken)
        setSelectedSubjectId(result.id)
        setSubjectForm({ code: '', name: '', description: '' })
      },
      'Subject catalog entry created.',
    )
  }

  async function handleSaveMapping(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken || !selectedMapping) return
    await runAction(
      () => syllabusApi.updateSyllabusSubject(selectedMapping.id, {
        sortOrder: mappingForm.sortOrder,
        marks: mappingForm.marks === '' ? null : Number(mappingForm.marks),
        isRequired: mappingForm.isRequired,
      }, accessToken),
      'Subject placement updated.',
    )
  }

  async function handleSavePaper(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken || !selectedMappingId) return
    const input = {
      code: paperForm.code,
      name: paperForm.name,
      sortOrder: paperForm.sortOrder,
      maxMarks: paperForm.maxMarks === '' ? null : Number(paperForm.maxMarks),
    }
    await runAction(
      async () => {
        if (editingPaperId) {
          await syllabusApi.updatePaper(editingPaperId, {
            name: input.name,
            sortOrder: input.sortOrder,
            maxMarks: input.maxMarks,
          }, accessToken)
        } else {
          await syllabusApi.createPaper(selectedMappingId, input, accessToken)
        }
        setEditingPaperId('')
        setPaperForm({ code: '', name: '', sortOrder: papers.length + 1, maxMarks: '' })
        setPapers(await syllabusApi.getAdminPapers(selectedMappingId, accessToken))
      },
      editingPaperId ? 'Paper updated.' : 'Paper created.',
      false,
    )
  }

  async function handleCreateItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken || !selectedMappingId) return
    const item = {
      ...itemForm,
      syllabusSubjectId: selectedMappingId,
      parentId: itemKind === 'Subtopic' ? selectedParentId || null : null,
    }
    await runAction(
      async () => {
        await syllabusApi.createItem(itemKind === 'Topic' ? 'topics' : 'subtopics', item, accessToken)
        setItemForm(emptyItem(selectedMappingId))
        setSelectedParentId('')
      },
      `${itemKind} created in the draft version.`,
    )
  }

  function startEditItem(item: SyllabusItem) {
    setEditingItemId(item.id)
    setItemKind(item.kind)
    setSelectedMappingId(item.syllabusSubjectId)
    setSelectedParentId(item.parentId ?? '')
    setItemForm({
      syllabusSubjectId: item.syllabusSubjectId,
      paperId: item.paperId,
      parentId: item.parentId,
      title: item.title,
      description: item.description ?? '',
      sortOrder: item.sortOrder,
      verificationStatus: item.verificationStatus,
    })
  }

  async function handleSaveItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken || !editingItemId) return
    await runAction(
      async () => {
        await syllabusApi.updateItem(editingItemId, {
          paperId: itemForm.paperId,
          parentId: itemKind === 'Subtopic' ? selectedParentId || null : null,
          title: itemForm.title,
          description: itemForm.description,
          sortOrder: itemForm.sortOrder,
          verificationStatus: itemForm.verificationStatus,
        }, accessToken)
        setEditingItemId('')
        setItemForm(emptyItem(selectedMappingId))
        setSelectedParentId('')
      },
      'Syllabus item updated.',
    )
  }

  async function handleSaveSource(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken) return
    await runAction(
      async () => {
        let source: SyllabusSource
        if (editingSourceId) {
          source = await syllabusApi.updateSource(editingSourceId, sourceForm, accessToken)
        } else {
          source = await syllabusApi.createSource(sourceForm, accessToken)
          if (selectedVersionId) await syllabusApi.linkVersionSource(selectedVersionId, source.id, sourceForm.notes ?? '', accessToken)
        }
        setEditingSourceId('')
        setSourceForm(emptySource)
        if (selectedVersionId) await refreshTree(selectedVersionId)
      },
      editingSourceId ? 'Source reference updated.' : 'Source reference created and linked to this version.',
      false,
    )
  }

  async function handleLinkSource(sourceId = linkSourceId) {
    if (!accessToken || !selectedVersionId || !sourceId) return
    await runAction(
      () => syllabusApi.linkVersionSource(selectedVersionId, sourceId, '', accessToken),
      'Source linked to this syllabus version.',
    )
  }

  async function handleUpdateStage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!accessToken || !selectedStageId) return
    await runAction(
      () => syllabusApi.updateStage(selectedStageId, stageForm, accessToken),
      'Exam stage updated.',
    )
  }

  async function handleItemSource(itemId: string) {
    const sourceId = itemSourceIds[itemId]
    if (!accessToken || !sourceId) return
    await runAction(
      () => syllabusApi.linkItemSource(itemId, sourceId, accessToken),
      'Source linked to syllabus item.',
    )
  }

  async function handlePublish() {
    if (!accessToken || !selectedVersionId) return
    await runAction(
      () => syllabusApi.publishVersion(selectedVersionId, accessToken),
      'Syllabus version published.',
    )
  }

  async function handleArchive() {
    if (!accessToken || !selectedVersionId) return
    await runAction(
      () => syllabusApi.archiveVersion(selectedVersionId, accessToken),
      'Syllabus version archived.',
    )
  }

  const editable = selectedVersion?.status === 'Draft'

  return (
    <>
      <PageHeader title="Syllabus administration" description="Manage versioned BPSC syllabus outlines, content hierarchy and source verification." />
      {error && <div role="alert" className="mb-4 flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800 dark:border-red-900/60 dark:bg-red-950/30 dark:text-red-200"><AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />{error}</div>}
      {notice && <div role="status" className="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800 dark:border-emerald-900/60 dark:bg-emerald-950/30 dark:text-emerald-200">{notice}</div>}

      <Card className="mb-5 p-4 sm:p-5">
        <div className="grid gap-4 md:grid-cols-2">
          <Field label="Syllabus version">
            <Select value={selectedVersionId} disabled={loading} onChange={(event) => setSelectedVersionId(event.target.value)}>
              {versions.length === 0 && <option value="">No versions</option>}
              {versions.map((version) => <option key={version.id} value={version.id}>{version.stageName} · {version.versionLabel} · {version.trackCode} · {version.status}</option>)}
            </Select>
          </Field>
          <Field label="Stage for new version">
            <Select value={selectedStageId} onChange={(event) => setSelectedStageId(event.target.value)}>
              {stages.map((stage) => <option key={stage.id} value={stage.id}>{stage.name} · {stage.localizedName}</option>)}
            </Select>
          </Field>
        </div>
        <details className="mt-4 border-t border-slate-100 pt-4 dark:border-slate-800">
          <summary className="cursor-pointer text-sm font-medium text-emerald-800 dark:text-emerald-300"><Plus className="mr-1 inline h-4 w-4" /> Create a draft version</summary>
          <form onSubmit={handleCreateVersion} className="mt-4 grid gap-3 md:grid-cols-2">
            <Field label="Version label"><Input required minLength={2} maxLength={120} value={createVersionForm.versionLabel} onChange={(event) => setCreateVersionForm({ ...createVersionForm, versionLabel: event.target.value })} /></Field>
            <Field label="Track code"><Input required maxLength={40} value={createVersionForm.trackCode} onChange={(event) => setCreateVersionForm({ ...createVersionForm, trackCode: event.target.value })} /></Field>
            <Field label="Coverage">
              <Select value={createVersionForm.coverage} onChange={(event) => setCreateVersionForm({ ...createVersionForm, coverage: event.target.value as FormVersion['coverage'] })}>
                <option value="OutlineOnly">Verified outline only</option><option value="FullTopics">Full topic coverage</option>
              </Select>
            </Field>
            <Field label="Effective from"><Input type="date" value={createVersionForm.effectiveFrom ?? ''} onChange={(event) => setCreateVersionForm({ ...createVersionForm, effectiveFrom: event.target.value || null })} /></Field>
            <Field label="Effective to"><Input type="date" value={createVersionForm.effectiveTo ?? ''} onChange={(event) => setCreateVersionForm({ ...createVersionForm, effectiveTo: event.target.value || null })} /></Field>
            <Field label="Source and coverage summary"><Textarea required minLength={1} maxLength={2000} value={createVersionForm.summary} onChange={(event) => setCreateVersionForm({ ...createVersionForm, summary: event.target.value })} /></Field>
            <div className="md:col-span-2"><Button type="submit" disabled={saving || !selectedStageId}>Create draft</Button></div>
          </form>
        </details>
      </Card>

      {selectedVersion && (
        <>
          <div className="mb-4 flex flex-wrap gap-2 border-b border-slate-200 pb-2 dark:border-slate-800" role="tablist" aria-label="Syllabus administration sections">
            {(['overview', 'structure', 'sources', 'stages'] as const).map((key) => (
              <button key={key} type="button" role="tab" aria-selected={tab === key} onClick={() => setTab(key)} className={`rounded-lg px-3 py-2 text-sm font-medium capitalize ${tab === key ? 'bg-emerald-50 text-emerald-800 dark:bg-emerald-500/10 dark:text-emerald-300' : 'text-slate-600 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800'}`}>{key}</button>
            ))}
          </div>

          {tab === 'overview' && (
            <div className="grid gap-5 lg:grid-cols-[minmax(0,1.3fr)_minmax(280px,0.7fr)]">
              <Card>
                <CardHeader title="Version metadata" subtitle={`${selectedVersion.stageName} · ${selectedVersion.trackCode} · ${selectedVersion.status}`} />
                <form onSubmit={handleSaveVersion} className="grid gap-3 p-4 sm:grid-cols-2 sm:p-5">
                  <Field label="Version label"><Input disabled={!editable} required value={versionForm.versionLabel} onChange={(event) => setVersionForm({ ...versionForm, versionLabel: event.target.value })} /></Field>
                  <Field label="Track code"><Input disabled={!editable} required value={versionForm.trackCode} onChange={(event) => setVersionForm({ ...versionForm, trackCode: event.target.value })} /></Field>
                  <Field label="Coverage">
                    <Select disabled={!editable} value={versionForm.coverage} onChange={(event) => setVersionForm({ ...versionForm, coverage: event.target.value as FormVersion['coverage'] })}>
                      <option value="OutlineOnly">Verified outline only</option><option value="FullTopics">Full topic coverage</option>
                    </Select>
                  </Field>
                  <Field label="Effective from"><Input disabled={!editable} type="date" value={versionForm.effectiveFrom ?? ''} onChange={(event) => setVersionForm({ ...versionForm, effectiveFrom: event.target.value || null })} /></Field>
                  <Field label="Effective to"><Input disabled={!editable} type="date" value={versionForm.effectiveTo ?? ''} onChange={(event) => setVersionForm({ ...versionForm, effectiveTo: event.target.value || null })} /></Field>
                  <Field label="Summary"><Textarea disabled={!editable} required value={versionForm.summary} onChange={(event) => setVersionForm({ ...versionForm, summary: event.target.value })} /></Field>
                  {editable && <div className="sm:col-span-2"><Button type="submit" disabled={saving}><Save className="h-4 w-4" /> Save version</Button></div>}
                </form>
              </Card>
              <Card className="p-5">
                <h2 className="font-semibold text-slate-900 dark:text-white">Review and publish</h2>
                <p className="mt-2 text-sm leading-6 text-slate-600 dark:text-slate-300">Published versions are immutable. Attach at least one source marked verified before publishing. Changes after publication require a new draft version.</p>
                <div className="mt-4 flex flex-wrap gap-2">
                  {editable && <Button disabled={saving} onClick={() => void handlePublish()}><Check className="h-4 w-4" /> Publish version</Button>}
                  {selectedVersion.status !== 'Archived' && <Button variant="secondary" disabled={saving} onClick={() => void handleArchive()}><Archive className="h-4 w-4" /> Archive</Button>}
                </div>
                <dl className="mt-5 grid grid-cols-2 gap-3 border-t border-slate-100 pt-4 text-sm dark:border-slate-800">
                  <div><dt className="text-xs text-slate-500">Subjects</dt><dd className="mt-1 font-medium">{selectedVersion.subjectCount}</dd></div>
                  <div><dt className="text-xs text-slate-500">Sources</dt><dd className="mt-1 font-medium">{selectedVersion.sourceCount}</dd></div>
                </dl>
              </Card>
            </div>
          )}

          {tab === 'structure' && (
            <div className="grid gap-5 xl:grid-cols-2">
              <div className="space-y-5">
                <Card>
                  <CardHeader title="Subject catalog" subtitle="Create or attach a subject to this version." />
                  <form onSubmit={handleCreateSubject} className="grid gap-3 p-4 sm:grid-cols-2">
                    <Field label="Subject code"><Input required disabled={!editable} value={subjectForm.code} onChange={(event) => setSubjectForm({ ...subjectForm, code: event.target.value })} /></Field>
                    <Field label="Subject name"><Input required disabled={!editable} value={subjectForm.name} onChange={(event) => setSubjectForm({ ...subjectForm, name: event.target.value })} /></Field>
                    <Field label="Description"><Input disabled={!editable} value={subjectForm.description} onChange={(event) => setSubjectForm({ ...subjectForm, description: event.target.value })} /></Field>
                    {editable && <div className="self-end"><Button type="submit" disabled={saving}><Plus className="h-4 w-4" /> Add to catalog</Button></div>}
                  </form>
                  <form onSubmit={handleAssignSubject} className="grid gap-3 border-t border-slate-100 p-4 dark:border-slate-800 sm:grid-cols-2">
                    <Field label="Catalog subject">
                      <Select disabled={!editable} required value={selectedSubjectId} onChange={(event) => setSelectedSubjectId(event.target.value)}>
                        <option value="">Select a subject</option>
                        {subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name} · {subject.code}</option>)}
                      </Select>
                    </Field>
                    <Field label="Sort order"><Input disabled={!editable} type="number" min={0} value={mappingForm.sortOrder} onChange={(event) => setMappingForm({ ...mappingForm, sortOrder: Number(event.target.value) })} /></Field>
                    <Field label="Marks (optional)"><Input disabled={!editable} type="number" min={0} value={mappingForm.marks} onChange={(event) => setMappingForm({ ...mappingForm, marks: event.target.value })} /></Field>
                    <label className="flex items-center gap-2 self-end pb-2 text-sm text-slate-600 dark:text-slate-300"><input type="checkbox" checked={mappingForm.isRequired} disabled={!editable} onChange={(event) => setMappingForm({ ...mappingForm, isRequired: event.target.checked })} /> Required subject</label>
                    {editable && <div className="sm:col-span-2"><Button type="submit" disabled={saving || !selectedSubjectId}><Plus className="h-4 w-4" /> Assign subject</Button></div>}
                  </form>
                </Card>
                <Card>
                  <CardHeader title="Papers" subtitle="Papers belong to a subject in the selected version." />
                  <div className="space-y-2 p-4">
                    <Field label="Selected syllabus subject">
                      <Select disabled={tree?.subjects.length === 0} value={selectedMappingId} onChange={(event) => setSelectedMappingId(event.target.value)}>
                        <option value="">Select a syllabus subject</option>
                        {tree?.subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.subjectName}</option>)}
                      </Select>
                    </Field>
                    {selectedMapping && editable && (
                      <form onSubmit={handleSaveMapping} className="grid gap-3 border-t border-slate-100 pt-3 dark:border-slate-800 sm:grid-cols-2">
                        <Field label="Subject order"><Input type="number" min={0} value={mappingForm.sortOrder} onChange={(event) => setMappingForm({ ...mappingForm, sortOrder: Number(event.target.value) })} /></Field>
                        <Field label="Subject marks"><Input type="number" min={0} value={mappingForm.marks} onChange={(event) => setMappingForm({ ...mappingForm, marks: event.target.value })} /></Field>
                        <label className="flex items-center gap-2 self-end pb-2 text-sm"><input type="checkbox" checked={mappingForm.isRequired} onChange={(event) => setMappingForm({ ...mappingForm, isRequired: event.target.checked })} /> Required subject</label>
                        <div className="self-end"><Button type="submit" disabled={saving}><Save className="h-4 w-4" /> Save placement</Button></div>
                      </form>
                    )}
                    {papers.map((paper) => (
                      <div key={paper.id} className="flex items-center justify-between gap-3 rounded-lg border border-slate-200 p-3 text-sm dark:border-slate-800">
                        <span>{paper.name}{paper.maxMarks !== null ? ` · ${paper.maxMarks} marks` : ''}</span>
                        {editable && <span className="flex gap-2"><Button size="sm" variant="secondary" onClick={() => { setEditingPaperId(paper.id); setPaperForm({ code: paper.code, name: paper.name, sortOrder: paper.sortOrder, maxMarks: paper.maxMarks === null ? '' : String(paper.maxMarks) }) }}>Edit</Button><Button size="sm" variant="ghost" aria-label={`Delete ${paper.name}`} onClick={() => void runAction(async () => { await syllabusApi.deletePaper(paper.id, accessToken!); setPapers(await syllabusApi.getAdminPapers(selectedMappingId, accessToken!)) }, 'Paper deleted.', false)}><Trash2 className="h-4 w-4" /></Button></span>}
                      </div>
                    ))}
                    <form onSubmit={handleSavePaper} className="grid gap-3 border-t border-slate-100 pt-4 dark:border-slate-800 sm:grid-cols-2">
                      <Field label="Paper code"><Input required disabled={!editable || !selectedMappingId || !!editingPaperId} value={paperForm.code} onChange={(event) => setPaperForm({ ...paperForm, code: event.target.value })} /></Field>
                      <Field label="Paper name"><Input required disabled={!editable || !selectedMappingId} value={paperForm.name} onChange={(event) => setPaperForm({ ...paperForm, name: event.target.value })} /></Field>
                      <Field label="Sort order"><Input type="number" min={0} disabled={!editable || !selectedMappingId} value={paperForm.sortOrder} onChange={(event) => setPaperForm({ ...paperForm, sortOrder: Number(event.target.value) })} /></Field>
                      <Field label="Maximum marks"><Input type="number" min={0} disabled={!editable || !selectedMappingId} value={paperForm.maxMarks} onChange={(event) => setPaperForm({ ...paperForm, maxMarks: event.target.value })} /></Field>
                      {editable && <div className="sm:col-span-2"><Button type="submit" disabled={saving || !selectedMappingId}><Save className="h-4 w-4" /> {editingPaperId ? 'Save paper' : 'Add paper'}</Button></div>}
                    </form>
                  </div>
                </Card>
              </div>

              <div className="space-y-5">
                <Card>
                  <CardHeader title="Topics and subtopics" subtitle="Only source-verified items should be marked verified." />
                  <form onSubmit={editingItemId ? handleSaveItem : handleCreateItem} className="grid gap-3 p-4 sm:grid-cols-2">
                    <Field label="Item type">
                      <Select disabled={!editable || !!editingItemId} value={itemKind} onChange={(event) => { setItemKind(event.target.value as 'Topic' | 'Subtopic'); setSelectedParentId('') }}>
                        <option value="Topic">Topic</option><option value="Subtopic">Subtopic</option>
                      </Select>
                    </Field>
                    <Field label="Subject">
                      <Select disabled={!editable || !!editingItemId} required value={selectedMappingId} onChange={(event) => { setSelectedMappingId(event.target.value); setItemForm(emptyItem(event.target.value)) }}>
                        <option value="">Select a syllabus subject</option>
                        {tree?.subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.subjectName}</option>)}
                      </Select>
                    </Field>
                    {itemKind === 'Subtopic' && (
                      <Field label="Parent topic">
                        <Select disabled={!editable} required value={selectedParentId} onChange={(event) => setSelectedParentId(event.target.value)}>
                          <option value="">Select parent topic</option>
                          {topicOptions.filter((topic) => topic.syllabusSubjectId === selectedMappingId).map((topic) => <option key={topic.id} value={topic.id}>{topic.title}</option>)}
                        </Select>
                      </Field>
                    )}
                    <Field label="Paper (optional)">
                      <Select disabled={!editable || !selectedMappingId} value={itemForm.paperId ?? ''} onChange={(event) => setItemForm({ ...itemForm, paperId: event.target.value || null })}>
                        <option value="">No paper</option>
                        {papers.map((paper) => <option key={paper.id} value={paper.id}>{paper.name}</option>)}
                      </Select>
                    </Field>
                    <Field label="Title"><Input required disabled={!editable || !selectedMappingId} value={itemForm.title} onChange={(event) => setItemForm({ ...itemForm, title: event.target.value })} /></Field>
                    <Field label="Sort order"><Input type="number" min={0} disabled={!editable} value={itemForm.sortOrder} onChange={(event) => setItemForm({ ...itemForm, sortOrder: Number(event.target.value) })} /></Field>
                    <Field label="Verification status">
                      <Select disabled={!editable} value={itemForm.verificationStatus} onChange={(event) => setItemForm({ ...itemForm, verificationStatus: event.target.value as ItemStatus })}>
                        <option value="Unverified">Unverified</option><option value="NeedsReview">Needs review</option><option value="Verified">Verified</option>
                      </Select>
                    </Field>
                    <Field label="Description"><Textarea disabled={!editable} value={itemForm.description ?? ''} onChange={(event) => setItemForm({ ...itemForm, description: event.target.value })} /></Field>
                    {editable && <div className="sm:col-span-2"><Button type="submit" disabled={saving || !selectedMappingId}><Plus className="h-4 w-4" /> {editingItemId ? 'Save item' : `Add ${itemKind.toLowerCase()}`}</Button>{editingItemId && <Button type="button" variant="ghost" className="ml-2" onClick={() => { setEditingItemId(''); setItemForm(emptyItem(selectedMappingId)) }}>Cancel edit</Button>}</div>}
                  </form>
                  <div className="space-y-2 border-t border-slate-100 p-4 dark:border-slate-800">
                    {tree?.subjects.flatMap((subject) => flattenItems(subject.items)).map((item) => (
                      <div key={item.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-200 p-3 text-sm dark:border-slate-800">
                        <span className="min-w-0"><span className="font-medium">{item.title}</span><span className="ml-2 text-xs text-slate-500">{item.kind} · {item.verificationStatus}</span></span>
                        <div className="flex items-center gap-2">
                          {editable && <Select aria-label={`Attach a source to ${item.title}`} className="w-44" value={itemSourceIds[item.id] ?? ''} onChange={(event) => setItemSourceIds({ ...itemSourceIds, [item.id]: event.target.value })}>
                            <option value="">Select source</option>{tree?.sources.map((source) => <option key={source.id} value={source.id}>{source.title}</option>)}
                          </Select>}
                          {editable && <Button size="sm" variant="secondary" disabled={!itemSourceIds[item.id]} onClick={() => void handleItemSource(item.id)}>Link source</Button>}
                          {editable && <Button size="sm" variant="secondary" onClick={() => startEditItem(item)}>Edit</Button>}
                          {editable && <Button size="sm" variant="ghost" aria-label={`Delete ${item.title}`} onClick={() => void runAction(() => syllabusApi.deleteItem(item.id, accessToken!), 'Syllabus item deleted.')}><Trash2 className="h-4 w-4" /></Button>}
                        </div>
                      </div>
                    ))}
                    {(!tree || tree.subjects.every((subject) => subject.items.length === 0)) && <p className="text-sm text-slate-500">No topic items have been added to this version.</p>}
                  </div>
                </Card>
              </div>
            </div>
          )}

          {tab === 'sources' && (
            <div className="grid gap-5 lg:grid-cols-2">
              <Card>
                <CardHeader title="Source references" subtitle="HTTPS URLs are required. Verified status should only be used after checking the source." />
                <div className="space-y-3 p-4">
                  {tree?.sources.map((source) => (
                    <div key={source.id} className="rounded-lg border border-slate-200 p-3 dark:border-slate-800">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0"><p className="font-medium text-slate-800 dark:text-slate-200">{source.title}</p><a className="mt-1 block break-all text-xs text-emerald-700 hover:underline dark:text-emerald-400" href={source.url} target="_blank" rel="noreferrer">{source.url}</a><p className="mt-1 text-xs text-slate-500">{source.publisher} · {source.verificationStatus}</p>{source.notes && <p className="mt-2 text-xs text-slate-500">{source.notes}</p>}</div>
                        <div className="flex shrink-0 gap-2">{editable && <Button size="sm" variant="secondary" onClick={() => { setEditingSourceId(source.id); setSourceForm({ title: source.title, publisher: source.publisher, url: source.url, sourceType: source.sourceType, publishedOn: source.publishedOn, lastVerifiedOn: source.lastVerifiedOn, verificationStatus: source.verificationStatus, notes: source.notes ?? '' }) }}>Edit</Button>}{editable && <Button size="sm" variant="ghost" onClick={() => void runAction(() => syllabusApi.unlinkVersionSource(selectedVersion.id, source.id, accessToken!), 'Source unlinked.')}>Unlink</Button>}</div>
                      </div>
                    </div>
                  ))}
                  {(!tree || tree.sources.length === 0) && <p className="text-sm text-slate-500">No sources are linked to this version yet.</p>}
                </div>
              </Card>
              <div className="space-y-5">
                {editable && (
                  <Card>
                    <CardHeader title={editingSourceId ? 'Edit source reference' : 'Add source reference'} subtitle="Only record content that can be traced to the linked source." />
                    <form onSubmit={handleSaveSource} className="grid gap-3 p-4">
                      <Field label="Title"><Input required value={sourceForm.title} onChange={(event) => setSourceForm({ ...sourceForm, title: event.target.value })} /></Field>
                      <Field label="Publisher"><Input required value={sourceForm.publisher} onChange={(event) => setSourceForm({ ...sourceForm, publisher: event.target.value })} /></Field>
                      <Field label="HTTPS URL"><Input type="url" required placeholder="https://" value={sourceForm.url} onChange={(event) => setSourceForm({ ...sourceForm, url: event.target.value })} /></Field>
                      <div className="grid gap-3 sm:grid-cols-2">
                        <Field label="Source type"><Input required value={sourceForm.sourceType} onChange={(event) => setSourceForm({ ...sourceForm, sourceType: event.target.value })} /></Field>
                        <Field label="Verification status">
                          <Select value={sourceForm.verificationStatus} onChange={(event) => setSourceForm({ ...sourceForm, verificationStatus: event.target.value as SourceStatus })}>
                            <option value="Unverified">Unverified</option><option value="Verified">Verified</option><option value="Superseded">Superseded</option>
                          </Select>
                        </Field>
                        <Field label="Published on"><Input type="date" value={sourceForm.publishedOn ?? ''} onChange={(event) => setSourceForm({ ...sourceForm, publishedOn: event.target.value || null })} /></Field>
                        <Field label="Last verified on"><Input type="date" value={sourceForm.lastVerifiedOn ?? ''} onChange={(event) => setSourceForm({ ...sourceForm, lastVerifiedOn: event.target.value || null })} /></Field>
                      </div>
                      <Field label="Notes"><Textarea value={sourceForm.notes ?? ''} onChange={(event) => setSourceForm({ ...sourceForm, notes: event.target.value })} /></Field>
                      <div className="flex gap-2"><Button type="submit" disabled={saving}><Save className="h-4 w-4" /> {editingSourceId ? 'Save source' : 'Create and link source'}</Button>{editingSourceId && <Button type="button" variant="ghost" onClick={() => { setEditingSourceId(''); setSourceForm(emptySource) }}>Cancel</Button>}</div>
                    </form>
                  </Card>
                )}
                {editable && (
                  <Card className="p-4">
                    <h3 className="font-medium text-slate-900 dark:text-white">Link an existing source</h3>
                    <p className="mt-1 text-xs text-slate-500">Enter a source reference ID created by an administrator.</p>
                    <div className="mt-3 flex gap-2"><Input aria-label="Source reference ID" value={linkSourceId} onChange={(event) => setLinkSourceId(event.target.value)} /><Button disabled={saving || !linkSourceId} onClick={() => void handleLinkSource()}><Plus className="h-4 w-4" /> Link</Button></div>
                  </Card>
                )}
              </div>
            </div>
          )}

          {tab === 'stages' && (
            <div className="grid gap-5 lg:grid-cols-[minmax(0,0.7fr)_minmax(0,1.3fr)]">
              <Card className="p-4">
                <Field label="Exam stage">
                  <Select value={selectedStageId} onChange={(event) => setSelectedStageId(event.target.value)}>
                    {stages.map((stage) => <option key={stage.id} value={stage.id}>{stage.name} · {stage.localizedName}{stage.isActive ? '' : ' · inactive'}</option>)}
                  </Select>
                </Field>
                <p className="mt-3 text-xs text-slate-500">Stage codes are stable identifiers used by syllabus version APIs and cannot be changed here.</p>
              </Card>
              {selectedStage && (
                <Card>
                  <CardHeader title="Exam stage metadata" subtitle={selectedStage.code} />
                  <form onSubmit={handleUpdateStage} className="grid gap-3 p-4 sm:grid-cols-2">
                    <Field label="Display name"><Input required value={stageForm.name} onChange={(event) => setStageForm({ ...stageForm, name: event.target.value })} /></Field>
                    <Field label="Localized name"><Input required value={stageForm.localizedName} onChange={(event) => setStageForm({ ...stageForm, localizedName: event.target.value })} /></Field>
                    <Field label="Sort order"><Input type="number" min={0} value={stageForm.sortOrder} onChange={(event) => setStageForm({ ...stageForm, sortOrder: Number(event.target.value) })} /></Field>
                    <label className="flex items-center gap-2 self-end pb-2 text-sm"><input type="checkbox" checked={stageForm.isActive} onChange={(event) => setStageForm({ ...stageForm, isActive: event.target.checked })} /> Active</label>
                    <div className="sm:col-span-2"><Button type="submit" disabled={saving}><Save className="h-4 w-4" /> Save stage</Button></div>
                  </form>
                </Card>
              )}
            </div>
          )}
        </>
      )}
    </>
  )
}
