import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { libraryApi, type LibraryResourceSummary } from '../../services/library'

/**
 * Study notes for a syllabus topic. Notes are library resources of kind "Note": shared notes curated by editors in
 * the admin library, plus the learner's own private notes, so there is one note system rather than two.
 */
export function TopicNotesPage() {
  const { accessToken } = useAuth()
  const [params] = useSearchParams()
  const topicId = params.get('topicId')
  const [notes, setNotes] = useState<{ key: string; items: LibraryResourceSummary[] } | null>(null)
  const [error, setError] = useState('')
  const key = topicId ?? 'all'

  useEffect(() => {
    if (!accessToken) return
    let alive = true
    libraryApi.getResources({ kind: 'Note', syllabusItemId: topicId ?? undefined, pageSize: 100, sortBy: 'title' }, accessToken)
      .then((result) => alive && setNotes({ key, items: result.items }))
      .catch((reason: unknown) => alive && setError(reason instanceof Error ? reason.message : 'Unable to load notes.'))
    return () => { alive = false }
  }, [accessToken, topicId, key])

  const items = notes?.key === key ? notes.items : null
  return (
    <>
      <PageHeader title="Study notes" description={topicId ? 'Notes mapped to this syllabus topic or its subtopics.' : 'Shared study notes and your private notes from the library.'} />
      {!accessToken ? <Card><EmptyState title="Sign in to read notes" description="Study notes live in the digital library, which requires an account." /></Card>
        : error ? <ErrorState description={error} />
          : !items ? <LoadingState label="Loading notes..." />
            : items.length === 0 ? <Card><EmptyState title="No notes for this topic yet" description="Editors publish shared notes from the admin library; you can also write your own private note in the library." /></Card>
              : <Card className="divide-y divide-slate-100 dark:divide-slate-800">{items.map((note) => <Link key={note.id} to={`/library/resources/${note.id}`} className="block px-5 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/60"><div className="flex items-center gap-2"><span className="font-medium">{note.title}</span>{note.visibility === 'Private' ? <Badge tone="info">Private</Badge> : <Badge tone="success">Shared</Badge>}</div>{note.description && <p className="mt-1 line-clamp-2 text-sm text-slate-500">{note.description}</p>}<p className="mt-1 text-xs text-slate-500">{note.topics.map((topic) => topic.title).join(', ')}</p></Link>)}</Card>}
      <div className="mt-4 flex gap-4 text-sm"><Link to="/library/new?kind=Note" className="text-emerald-700 hover:underline dark:text-emerald-400">Write a private note</Link><Link to="/written/notes" className="text-emerald-700 hover:underline dark:text-emerald-400">Written study notes</Link><Link to="/syllabus" className="text-slate-500 hover:underline">Back to syllabus</Link></div>
    </>
  )
}
