import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { mcqApi, type McqAttemptListItem } from '../../services/mcq'

export function PracticeHistoryPage() {
  const { accessToken } = useAuth()
  const [attempts, setAttempts] = useState<McqAttemptListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    if (!accessToken) {
      setLoading(false)
      return () => { active = false }
    }

    mcqApi.getAttemptHistory({ pageNumber: 1, pageSize: 50 }, accessToken)
      .then((result) => active && setAttempts(result.items))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load attempt history.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken])

  return (
    <>
      <PageHeader title="Attempt history" description="Review results or continue an in-progress attempt." />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {!accessToken ? <Card><EmptyState title="Sign in to view your history" description="Your practice attempts are private to your account." /></Card>
        : loading ? <LoadingState label="Loading attempt history..." />
          : attempts.length === 0 ? <Card><EmptyState title="No attempts yet" description="Start a published practice set to build your personal attempt history." /></Card>
            : <Card className="overflow-hidden">
              <CardHeader title="Recent attempts" subtitle={`${attempts.length} recent attempts`} />
              <div className="divide-y divide-slate-100 dark:divide-slate-800">
                {attempts.map((attempt) => (
                  <div key={attempt.id} className="flex flex-col gap-3 p-5 sm:flex-row sm:items-center sm:justify-between">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2"><h2 className="font-medium">{attempt.examSetName}</h2><Badge>{attempt.mode}</Badge>{attempt.isTimedOut && <Badge>Timed out</Badge>}</div>
                      <p className="mt-1 text-xs text-slate-500">{new Date(attempt.startedAt).toLocaleString()} · {attempt.status === 'Submitted' ? `Score ${attempt.score ?? 0} / ${attempt.maximumScore}` : 'In progress'}</p>
                      {attempt.status === 'Submitted' && <p className="mt-1 text-xs text-slate-500">{attempt.correctCount} correct · {attempt.incorrectCount} incorrect · {attempt.unansweredCount} unanswered</p>}
                    </div>
                    <Link to={`/practice/attempts/${attempt.id}`}><Button variant="secondary" size="sm">{attempt.status === 'Submitted' ? 'Review answers' : 'Continue attempt'}</Button></Link>
                  </div>
                ))}
              </div>
            </Card>}
    </>
  )
}
