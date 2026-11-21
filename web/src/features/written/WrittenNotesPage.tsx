import { FilePlus2, Save, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { syllabusApi, type SyllabusItem, type SyllabusTree, type SyllabusVersion } from '../../services/syllabus'
import { writtenApi, type WrittenStudyNote } from '../../services/written'

function flattenItems(items: SyllabusItem[]): SyllabusItem[] {
  return items.flatMap((item) => [item, ...flattenItems(item.children)])
}

export function WrittenNotesPage() {
  const { accessToken } = useAuth()
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [versionId, setVersionId] = useState('')
  const [tree, setTree] = useState<SyllabusTree | null>(null)
  const [notes, setNotes] = useState<WrittenStudyNote[]>([])
  const [selectedId, setSelectedId] = useState('')
  const [title, setTitle] = useState('')
  const [content, setContent] = useState('')
  const [syllabusItemId, setSyllabusItemId] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  useEffect(() => {
    if (!accessToken) {
      setLoading(false)
      return
    }
    let active = true
    Promise.all([
      syllabusApi.getVersions({ stageCode: 'Written', pageSize: 50 }),
      writtenApi.getStudyNotes({ pageNumber: 1, pageSize: 100 }, accessToken),
    ])
      .then(([versionResult, noteResult]) => {
        if (!active) return
        setVersions(versionResult.items)
        setVersionId(versionResult.items[0]?.id ?? '')
        setNotes(noteResult.items)
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Written notes.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken])

  useEffect(() => {
    if (!versionId) {
      setTree(null)
      return
    }
    let active = true
    syllabusApi.getTree(versionId)
      .then((result) => active && setTree(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Written topics.'))
    return () => { active = false }
  }, [versionId])

  const topics = useMemo(() => tree?.subjects.flatMap((subject) => flattenItems(subject.items)) ?? [], [tree])

  function startNew() {
    setSelectedId('')
    setTitle('')
    setContent('')
    setSyllabusItemId(topics[0]?.id ?? '')
    setMessage('')
  }

  function selectNote(note: WrittenStudyNote) {
    setSelectedId(note.id)
    setVersionId(note.syllabusVersionId)
    setTitle(note.title)
    setContent(note.content)
    setSyllabusItemId(note.syllabusItemId)
    setMessage('')
  }

  async function saveNote() {
    if (!accessToken) return
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const input = { syllabusItemId, title, content }
      const saved = selectedId
        ? await writtenApi.updateStudyNote(selectedId, input, accessToken)
        : await writtenApi.createStudyNote(input, accessToken)
      setSelectedId(saved.id)
      setNotes((current) => [saved, ...current.filter((note) => note.id !== saved.id)])
      setMessage('Private Written note saved.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save this Written note.')
    } finally {
      setSaving(false)
    }
  }

  async function deleteNote() {
    if (!accessToken || !selectedId || !window.confirm('Delete this private note?')) return
    setSaving(true)
    setError('')
    try {
      await writtenApi.deleteStudyNote(selectedId, accessToken)
      setNotes((current) => current.filter((note) => note.id !== selectedId))
      startNew()
      setMessage('Note deleted.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to delete this note.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader title="Written study notes" description="Private notes linked to a topic in a published Written syllabus version." action={<Button variant="secondary" onClick={startNew}><FilePlus2 className="h-4 w-4" />New note</Button>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}
      {!accessToken ? <Card><EmptyState title="Sign in to manage notes" description="Written study notes are private to your account." /></Card>
        : loading ? <LoadingState label="Loading Written notes..." />
          : <div className="grid gap-5 xl:grid-cols-[minmax(16rem,0.65fr)_minmax(0,1.35fr)]">
            <Card className="h-fit overflow-hidden"><CardHeader title="Your notes" subtitle={`${notes.length} loaded`} /><div className="max-h-[70vh] divide-y divide-slate-100 overflow-auto dark:divide-slate-800">{notes.map((note) => <button key={note.id} type="button" onClick={() => selectNote(note)} className={`w-full p-4 text-left ${selectedId === note.id ? 'bg-emerald-50 dark:bg-emerald-500/10' : 'hover:bg-slate-50 dark:hover:bg-slate-800/60'}`}><span className="block truncate text-sm font-medium">{note.title}</span><span className="mt-1 block text-xs text-slate-500">{note.topicTitle} · {new Date(note.updatedAt).toLocaleDateString()}</span></button>)}{notes.length === 0 && <p className="p-5 text-sm text-slate-500">No notes yet. Create one after a verified topic is available.</p>}</div></Card>
            <Card className="p-5">
              <CardHeader title={selectedId ? 'Edit private note' : 'Create private note'} subtitle="This note is not published or shared." />
              {versions.length === 0 ? <EmptyState title="No Written syllabus version" description="A published Written syllabus version is required to link study notes." />
                : topics.length === 0 ? <EmptyState title="No Written topics are available" description="The current syllabus contains only verified subject outlines. Detailed Written topics are not fabricated." />
                  : <div className="mt-4 space-y-4">
                    <label className="block text-xs font-medium text-slate-600 dark:text-slate-300">Syllabus topic<select value={syllabusItemId} onChange={(event) => setSyllabusItemId(event.target.value)} className="mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950">{topics.map((topic) => <option key={topic.id} value={topic.id}>{topic.title}{topic.verificationStatus === 'Verified' ? '' : ' · unverified'}</option>)}</select></label>
                    <label className="block text-xs font-medium text-slate-600 dark:text-slate-300">Title<input value={title} onChange={(event) => setTitle(event.target.value)} maxLength={200} className="mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" /></label>
                    <label className="block text-xs font-medium text-slate-600 dark:text-slate-300">Note<textarea value={content} onChange={(event) => setContent(event.target.value)} maxLength={30000} rows={14} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm leading-6 dark:border-slate-700 dark:bg-slate-950" /></label>
                    <div className="flex justify-end gap-2"><Button variant="secondary" disabled={!selectedId || saving} onClick={() => void deleteNote()}><Trash2 className="h-4 w-4" />Delete</Button><Button disabled={saving || !title.trim() || !content.trim()} onClick={() => void saveNote()}><Save className="h-4 w-4" />{saving ? 'Saving...' : 'Save note'}</Button></div>
                  </div>}
            </Card>
          </div>}
    </>
  )
}
