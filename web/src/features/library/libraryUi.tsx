import { BookOpen, Download, FileText, Image as ImageIcon, Link2, NotebookPen } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { ErrorState, LoadingState } from '../../components/ui/States'
import type { LibraryFile, LibraryMode, LibraryResourceSummary } from '../../services/library'
import { syllabusApi, type SyllabusItem, type SyllabusTree, type SyllabusVersion } from '../../services/syllabus'
import { downloadLibraryFile, errorMessage, statusTone, useFileObjectUrl } from './libraryFormat'

export function KindIcon({ resource, className = 'h-4 w-4' }: { resource: Pick<LibraryResourceSummary, 'kind' | 'primaryFormat'>; className?: string }) {
  if (resource.kind === 'Book') return <BookOpen className={`${className} text-emerald-600`} />
  if (resource.kind === 'ExternalLink') return <Link2 className={`${className} text-sky-500`} />
  if (resource.kind === 'Note') return <NotebookPen className={`${className} text-amber-500`} />
  if (resource.primaryFormat === 'Image') return <ImageIcon className={`${className} text-violet-500`} />
  return <FileText className={`${className} text-red-500`} />
}

export function VisibilityBadge({ resource }: { resource: Pick<LibraryResourceSummary, 'visibility' | 'status'> }) {
  if (resource.visibility === 'Private') return <Badge tone="info">Private</Badge>
  return <Badge tone={statusTone(resource.status)}>{resource.status === 'Published' ? 'Shared' : `Shared · ${resource.status}`}</Badge>
}

export function ProtectedImage({ resourceId, fileId, token, alt, className = '', mode = 'personal' }: { resourceId: string; fileId: string | null; token: string | null; alt: string; className?: string; mode?: LibraryMode }) {
  const { url } = useFileObjectUrl(resourceId, fileId, token, mode)
  if (!url) return <div className={`grid place-items-center bg-slate-100 text-slate-400 dark:bg-slate-800 ${className}`}><BookOpen className="h-8 w-8" /></div>
  return <img src={url} alt={alt} className={`object-cover ${className}`} />
}

function TextPreview({ blob }: { blob: Blob }) {
  const [loaded, setLoaded] = useState<{ blob: Blob; text: string } | null>(null)
  useEffect(() => {
    let active = true
    blob.text().then((text) => active && setLoaded({ blob, text }))
    return () => { active = false }
  }, [blob])
  return <pre className="max-h-[75vh] overflow-auto whitespace-pre-wrap rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm leading-6 dark:border-slate-700 dark:bg-slate-950">{loaded?.blob === blob ? loaded.text : 'Loading...'}</pre>
}

/** Inline viewer for PDFs, images and UTF-8 text. Office documents are download-only. */
export function FileViewer({ resourceId, file, token, page, mode = 'personal' }: { resourceId: string; file: LibraryFile; token: string; page?: number | null; mode?: LibraryMode }) {
  const { url, blob, error, loading } = useFileObjectUrl(resourceId, file.canPreviewInline ? file.id : null, token, mode)

  if (!file.canPreviewInline) {
    return <div className="rounded-lg border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500 dark:border-slate-700"><p>Preview is not available for Office documents. Download the file to open it.</p><Button className="mt-3" variant="secondary" onClick={() => void downloadLibraryFile(resourceId, file, token, mode)}><Download className="h-4 w-4" />Download {file.originalFileName}</Button></div>
  }
  if (loading) return <LoadingState label="Loading preview..." />
  if (error) return <ErrorState title="Preview unavailable" description={error} />
  if (!url || !blob) return null
  if (file.format === 'Pdf') {
    // The browser's built-in viewer supports #page=N on blob URLs. The iframe is not sandboxed because browsers
    // refuse to run their PDF viewer in sandboxed frames; the server only accepts files with a verified PDF signature.
    return <iframe key={`${url}-${page ?? 1}`} title={file.originalFileName} src={`${url}#page=${page ?? 1}`} className="h-[75vh] w-full rounded-lg border border-slate-200 bg-white dark:border-slate-700" />
  }
  if (file.format === 'Image') return <img src={url} alt={file.originalFileName} className="mx-auto max-h-[75vh] rounded-lg border border-slate-200 dark:border-slate-700" />
  return <TextPreview blob={blob} />
}

function flatten(items: SyllabusItem[]): SyllabusItem[] {
  return items.flatMap((item) => [item, ...flatten(item.children)])
}

export interface PickedRef { id: string; label: string }

/**
 * Picks subjects and topics from published syllabus versions. Selections from other versions are kept
 * as labelled chips so a resource can map to topics across Preliminary, Written and Viva.
 */
export function SyllabusMappingPicker({
  subjects,
  topics,
  onSubjectsChange,
  onTopicsChange,
  showSubjects = true,
}: {
  subjects: PickedRef[]
  topics: PickedRef[]
  onSubjectsChange?: (value: PickedRef[]) => void
  onTopicsChange: (value: PickedRef[]) => void
  showSubjects?: boolean
}) {
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [versionId, setVersionId] = useState('')
  const [tree, setTree] = useState<SyllabusTree | null>(null)
  const [filter, setFilter] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    syllabusApi.getVersions({ pageSize: 50 })
      .then((result) => {
        if (!active) return
        setVersions(result.items)
        setVersionId(result.items[0]?.id ?? '')
      })
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load syllabus versions.')))
    return () => { active = false }
  }, [])

  useEffect(() => {
    if (!versionId) return
    let active = true
    syllabusApi.getTree(versionId)
      .then((result) => active && setTree(result))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load syllabus topics.')))
    return () => { active = false }
  }, [versionId])

  const allTopics = useMemo(() => tree?.subjects.flatMap((subject) => flatten(subject.items).map((item) => ({ item, subject: subject.subjectName }))) ?? [], [tree])
  const visibleTopics = allTopics.filter(({ item }) => item.title.toLocaleLowerCase().includes(filter.trim().toLocaleLowerCase()))
  const version = versions.find((candidate) => candidate.id === versionId)

  function toggle(list: PickedRef[], entry: PickedRef, onChange: (value: PickedRef[]) => void) {
    onChange(list.some((item) => item.id === entry.id) ? list.filter((item) => item.id !== entry.id) : [...list, entry])
  }

  return (
    <div className="space-y-3 rounded-lg border border-slate-200 p-4 dark:border-slate-700">
      {error && <ErrorState description={error} />}
      <div className="flex flex-wrap gap-2">
        {subjects.map((subject) => <button type="button" key={subject.id} onClick={() => onSubjectsChange?.(subjects.filter((item) => item.id !== subject.id))} className="rounded-full bg-emerald-50 px-2.5 py-1 text-xs text-emerald-800 hover:line-through dark:bg-emerald-500/10 dark:text-emerald-300">Subject: {subject.label} ×</button>)}
        {topics.map((topic) => <button type="button" key={topic.id} onClick={() => onTopicsChange(topics.filter((item) => item.id !== topic.id))} className="rounded-full bg-sky-50 px-2.5 py-1 text-xs text-sky-800 hover:line-through dark:bg-sky-500/10 dark:text-sky-300">{topic.label} ×</button>)}
        {subjects.length === 0 && topics.length === 0 && <span className="text-xs text-slate-500">No subjects or topics mapped yet.</span>}
      </div>
      {versions.length === 0 ? <p className="text-xs text-slate-500">No published syllabus version is available to map against.</p> : <>
        <div className="grid gap-2 sm:grid-cols-2">
          <label className="text-xs font-medium text-slate-600 dark:text-slate-300">Syllabus version<select value={versionId} onChange={(event) => setVersionId(event.target.value)} className="mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950">{versions.map((item) => <option key={item.id} value={item.id}>{item.stageName} · {item.versionLabel}</option>)}</select></label>
          <label className="text-xs font-medium text-slate-600 dark:text-slate-300">Filter topics<input value={filter} onChange={(event) => setFilter(event.target.value)} className="mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950" /></label>
        </div>
        {showSubjects && onSubjectsChange && <div className="flex flex-wrap gap-2">{tree?.subjects.map((subject) => {
          const checked = subjects.some((item) => item.id === subject.subjectId)
          return <label key={subject.id} className={`cursor-pointer rounded-lg border px-2.5 py-1 text-xs ${checked ? 'border-emerald-600 bg-emerald-50 dark:bg-emerald-500/10' : 'border-slate-200 dark:border-slate-700'}`}><input type="checkbox" className="mr-1.5" checked={checked} onChange={() => toggle(subjects, { id: subject.subjectId, label: subject.subjectName }, onSubjectsChange)} />{subject.subjectName}</label>
        })}</div>}
        <div className="max-h-56 space-y-1 overflow-auto">
          {visibleTopics.map(({ item, subject }) => {
            const checked = topics.some((topic) => topic.id === item.id)
            const label = `${item.title} (${subject} · ${version?.stageCode ?? ''} ${version?.versionLabel ?? ''})`
            return <label key={item.id} className="flex cursor-pointer items-start gap-2 rounded px-1 py-0.5 text-sm hover:bg-slate-50 dark:hover:bg-slate-800"><input type="checkbox" className="mt-1" checked={checked} onChange={() => toggle(topics, { id: item.id, label }, onTopicsChange)} /><span className={item.kind === 'Subtopic' ? 'pl-4' : ''}>{item.title} <span className="text-xs text-slate-500">· {subject}{item.verificationStatus === 'Verified' ? '' : ' · unverified'}</span></span></label>
          })}
          {tree && visibleTopics.length === 0 && <p className="text-xs text-slate-500">This version has no detailed topics yet. Map the resource to a subject instead.</p>}
        </div>
      </>}
    </div>
  )
}
