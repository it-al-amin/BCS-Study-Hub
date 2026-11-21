import { Clock3, Play, Target } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { mcqApi, type McqExamSet } from '../../services/mcq'

function formatDuration(seconds: number | null) {
  if (seconds === null) return 'Untimed'
  const minutes = Math.floor(seconds / 60)
  return `${minutes} min${seconds % 60 ? ` ${seconds % 60} sec` : ''}`
}

export function PracticeSetsPage() {
  const { accessToken } = useAuth()
  const navigate = useNavigate()
  const [sets, setSets] = useState<McqExamSet[]>([])
  const [loading, setLoading] = useState(true)
  const [starting, setStarting] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    mcqApi.getExamSets({ pageNumber: 1, pageSize: 100 })
      .then((result) => active && setSets(result.items))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load practice sets.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [])

  async function start(set: McqExamSet) {
    if (!accessToken) return
    setError('')
    setStarting(set.id)
    try {
      const attempt = await mcqApi.createAttempt(set.id, accessToken)
      navigate(`/practice/attempts/${attempt.id}`)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to start this practice set.')
    } finally {
      setStarting('')
    }
  }

  return (
    <>
      <PageHeader title="MCQ practice" description="Choose a published, dynamically configured Preliminary practice set or mock exam." action={<Link to="/questions" className="text-sm font-medium text-emerald-700 hover:underline">Browse question bank</Link>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {loading ? <LoadingState label="Loading practice sets..." /> : sets.length === 0 ? (
        <Card><EmptyState title="No practice sets are published" description="An authorized editor must publish an exam configuration and enough verified, source-attributed questions before a practice set can be started." /></Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {sets.map((set) => (
            <Card key={set.id} className="flex flex-col p-5">
              <div className="flex items-start justify-between gap-3">
                <div><h2 className="font-semibold">{set.name}</h2><p className="mt-1 text-xs text-slate-500">{set.syllabusVersionLabel}{set.subjectName ? ` · ${set.subjectName}` : ''}{set.syllabusItemTitle ? ` · ${set.syllabusItemTitle}` : ''}</p></div>
                <Badge>{set.mode === 'MockExam' ? 'Mock exam' : `${set.mode} practice`}</Badge>
              </div>
              {set.instructions && <p className="mt-4 text-sm leading-6 text-slate-600 dark:text-slate-300">{set.instructions}</p>}
              <div className="mt-5 grid grid-cols-2 gap-3 text-sm">
                <div className="flex items-center gap-2 text-slate-600 dark:text-slate-300"><Target className="h-4 w-4 text-emerald-700" />{set.questionCount} questions</div>
                <div className="flex items-center gap-2 text-slate-600 dark:text-slate-300"><Clock3 className="h-4 w-4 text-emerald-700" />{formatDuration(set.durationSeconds)}</div>
              </div>
              <p className="mt-3 text-xs text-slate-500">Scoring configured for this set: correct {set.marksCorrect}, incorrect {set.marksIncorrect}, unanswered {set.marksUnanswered}.</p>
              {set.difficulty && <p className="mt-1 text-xs text-slate-500">Difficulty: {set.difficulty}</p>}
              <div className="mt-auto pt-5">
                {accessToken
                  ? <Button className="w-full" disabled={starting !== ''} onClick={() => void start(set)}><Play className="h-4 w-4" />{starting === set.id ? 'Starting...' : 'Start set'}</Button>
                  : <Link to="/login" className="block rounded-lg bg-emerald-700 px-4 py-2.5 text-center text-sm font-medium text-white hover:bg-emerald-800">Sign in to practice</Link>}
              </div>
            </Card>
          ))}
        </div>
      )}
      <div className="mt-6 flex flex-wrap gap-4 text-sm">
        <Link to="/practice/history" className="text-emerald-700 hover:underline">Attempt history</Link>
        <Link to="/practice/analytics" className="text-emerald-700 hover:underline">Performance analysis</Link>
      </div>
    </>
  )
}
