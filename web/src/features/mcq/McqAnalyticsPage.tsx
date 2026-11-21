import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { mcqApi, type McqAnalytics } from '../../services/mcq'

export function McqAnalyticsPage() {
  const { accessToken } = useAuth()
  const [analytics, setAnalytics] = useState<McqAnalytics | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    if (!accessToken) {
      setLoading(false)
      return () => { active = false }
    }

    mcqApi.getAnalytics(accessToken)
      .then((result) => active && setAnalytics(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load performance analysis.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken])

  return (
    <>
      <PageHeader title="MCQ performance" description="Results are computed from your submitted attempts and server-validated answers." action={<Link to="/practice/history" className="text-sm font-medium text-emerald-700 hover:underline">Attempt history</Link>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {!accessToken ? <Card><EmptyState title="Sign in to view analytics" description="Your performance data is private to your account." /></Card>
        : loading ? <LoadingState label="Calculating performance..." />
          : !analytics || analytics.attemptCount === 0 ? <Card><EmptyState title="No completed attempts yet" description="Submit a practice set to see your accuracy, score trends, and topic performance." /></Card>
            : <>
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <Metric label="Completed attempts" value={analytics.attemptCount} />
                <Metric label="Answered questions" value={analytics.answerCount} />
                <Metric label="Accuracy" value={`${analytics.accuracyPercent}%`} />
                <Metric label="Average score" value={analytics.averageScore ?? '—'} />
              </div>
              <div className="mt-5 grid gap-5 xl:grid-cols-2">
                <Card className="overflow-hidden">
                  <CardHeader title="Topic performance" subtitle="Based on submitted answers; unanswered items are excluded." />
                  {analytics.topicPerformance.length === 0 ? <EmptyState title="No topic results" description="Topic analysis appears after you answer questions." /> : <div className="divide-y divide-slate-100 dark:divide-slate-800">
                    {analytics.topicPerformance.map((topic) => (
                      <div key={topic.syllabusItemId} className="p-4">
                        <div className="flex items-center justify-between gap-3"><h3 className="text-sm font-medium">{topic.topicTitle}</h3><span className="text-sm font-semibold">{topic.accuracyPercent}%</span></div>
                        <div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-full rounded-full bg-emerald-600" style={{ width: `${Math.min(topic.accuracyPercent, 100)}%` }} /></div>
                        <p className="mt-1 text-xs text-slate-500">{topic.correctCount} correct of {topic.answerCount} answered</p>
                      </div>
                    ))}
                  </div>}
                </Card>
                <Card className="overflow-hidden">
                  <CardHeader title="Recent results" action={<Link to="/practice/history" className="text-xs font-medium text-emerald-700 hover:underline">View history</Link>} />
                  <div className="divide-y divide-slate-100 dark:divide-slate-800">
                    {analytics.recentAttempts.map((attempt) => <Link key={attempt.id} to={`/practice/attempts/${attempt.id}`} className="flex items-center justify-between gap-4 p-4 hover:bg-slate-50 dark:hover:bg-slate-800/60"><span className="min-w-0"><span className="block truncate text-sm font-medium">{attempt.examSetName}</span><span className="text-xs text-slate-500">{new Date(attempt.submittedAt ?? attempt.startedAt).toLocaleDateString()}</span></span><strong className="text-sm">{attempt.score ?? 0} / {attempt.maximumScore}</strong></Link>)}
                  </div>
                </Card>
              </div>
            </>}
    </>
  )
}

function Metric({ label, value }: { label: string; value: string | number }) {
  return <Card className="p-4"><p className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</p><p className="mt-2 text-2xl font-semibold">{value}</p></Card>
}
