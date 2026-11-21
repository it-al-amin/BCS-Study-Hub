import { useEffect, useMemo, useState } from 'react'
import { syllabusApi, type SyllabusItem, type SyllabusTree, type SyllabusVersion } from '../../services/syllabus'

export interface TopicSelection {
  versionId: string
  subjectId: string | null
  syllabusItemId: string | null
}

function flatten(items: SyllabusItem[], depth = 0): { item: SyllabusItem; depth: number }[] {
  return items.flatMap((item) => [{ item, depth }, ...flatten(item.children, depth + 1)])
}

const selectClass = 'mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950'

/**
 * Hierarchical single selection: syllabus version → subject → topic/subtopic (indented).
 * Used wherever one subject or topic is chosen (planner tasks, sessions, mistakes, admin forms).
 */
export function TopicSelect({
  value,
  onChange,
  stageCode,
  adminTree,
  token,
  allowSubjectOnly = true,
  label = 'Syllabus',
}: {
  value: TopicSelection | null
  onChange: (value: TopicSelection | null) => void
  stageCode?: string
  adminTree?: boolean
  token?: string | null
  allowSubjectOnly?: boolean
  label?: string
}) {
  const [versions, setVersions] = useState<SyllabusVersion[]>([])
  const [tree, setTree] = useState<{ versionId: string; tree: SyllabusTree } | null>(null)
  const versionId = value?.versionId ?? ''

  useEffect(() => {
    let active = true
    const request = adminTree && token
      ? syllabusApi.getAdminVersions({ stageCode, pageSize: 100 }, token)
      : syllabusApi.getVersions({ stageCode, pageSize: 50 })
    request.then((result) => active && setVersions(result.items)).catch(() => undefined)
    return () => { active = false }
  }, [stageCode, adminTree, token])

  useEffect(() => {
    if (!versionId) return
    let active = true
    const request = adminTree && token ? syllabusApi.getAdminTree(versionId, token) : syllabusApi.getTree(versionId)
    request.then((result) => active && setTree({ versionId, tree: result })).catch(() => undefined)
    return () => { active = false }
  }, [versionId, adminTree, token])

  const currentTree = tree?.versionId === versionId ? tree.tree : null
  const subject = currentTree?.subjects.find((item) => item.subjectId === value?.subjectId) ?? null
  const topics = useMemo(() => (subject ? flatten(subject.items) : []), [subject])

  return (
    <fieldset className="grid gap-2 sm:grid-cols-3">
      <legend className="sr-only">{label}</legend>
      <label className="text-xs font-medium text-slate-600 dark:text-slate-300">Syllabus version
        <select className={selectClass} value={versionId} onChange={(event) => onChange(event.target.value ? { versionId: event.target.value, subjectId: null, syllabusItemId: null } : null)}>
          <option value="">— None —</option>
          {versions.map((version) => <option key={version.id} value={version.id}>{version.stageName} · {version.versionLabel}{version.trackCode !== 'Common' ? ` · ${version.trackCode}` : ''}{version.status !== 'Published' ? ` (${version.status})` : ''}</option>)}
        </select>
      </label>
      <label className="text-xs font-medium text-slate-600 dark:text-slate-300">Subject
        <select className={selectClass} disabled={!currentTree} value={value?.subjectId ?? ''} onChange={(event) => value && onChange({ ...value, subjectId: event.target.value || null, syllabusItemId: null })}>
          <option value="">{allowSubjectOnly ? '— Any subject —' : '— Choose —'}</option>
          {currentTree?.subjects.map((item) => <option key={item.id} value={item.subjectId}>{item.subjectName}</option>)}
        </select>
      </label>
      <label className="text-xs font-medium text-slate-600 dark:text-slate-300">Topic
        <select className={selectClass} disabled={!subject || topics.length === 0} value={value?.syllabusItemId ?? ''} onChange={(event) => value && onChange({ ...value, syllabusItemId: event.target.value || null })}>
          <option value="">{subject && topics.length === 0 ? 'No detailed topics' : '— Whole subject —'}</option>
          {topics.map(({ item, depth }) => <option key={item.id} value={item.id}>{`${'  '.repeat(depth)}${depth > 0 ? '↳ ' : ''}${item.title}`}</option>)}
        </select>
      </label>
    </fieldset>
  )
}
