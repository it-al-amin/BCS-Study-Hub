import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { vivaApi, type VivaMockSessionListItem, type VivaQuestionHistory } from '../../services/viva'

export function VivaHistoryPage() {
  const { accessToken } = useAuth()
  const [history, setHistory] = useState<VivaQuestionHistory[]>([])
  const [sessions, setSessions] = useState<VivaMockSessionListItem[]>([])
  const [historyPage, setHistoryPage] = useState(1)
  const [sessionPage, setSessionPage] = useState(1)
  const [historyPages, setHistoryPages] = useState(1)
  const [sessionPages, setSessionPages] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    Promise.all([
      vivaApi.getQuestionHistory({ pageNumber: historyPage, pageSize: 15 }, accessToken),
      vivaApi.getSessions({ pageNumber: sessionPage, pageSize: 15 }, accessToken),
    ])
      .then(([historyResult, sessionResult]) => {
        if (!active) return
        setHistory(historyResult.items)
        setHistoryPages(historyResult.totalPages)
        setSessions(sessionResult.items)
        setSessionPages(sessionResult.totalPages)
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Viva history.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [accessToken, historyPage, sessionPage])

  if (!accessToken) {
    return <><PageHeader title="Viva history" description="Your question and mock-session history is private." /><EmptyState title="Sign in required" description="Sign in to view your Viva practice history." /></>
  }

  return (
    <>
      <PageHeader title="Viva history" description="Recently viewed questions and your private mock Viva sessions." action={<Link to="/viva" className="text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">Back to preparation</Link>} />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {loading ? <LoadingState label="Loading Viva history..." /> : (
        <div className="grid gap-5 xl:grid-cols-2">
          <Card className="overflow-hidden">
            <div className="border-b border-slate-100 px-5 py-4 dark:border-slate-800"><h2 className="font-semibold">Viewed questions</h2><p className="mt-1 text-sm text-slate-500">A question is recorded when you open its detail page.</p></div>
            {history.length === 0 ? <EmptyState title="No question history" description="Open a published question to start your history." /> : <div className="divide-y divide-slate-100 dark:divide-slate-800">{history.map((item) => <Link key={item.questionId} to={`/viva/questions/${item.questionId}`} className="block p-4 hover:bg-slate-50 dark:hover:bg-slate-800/60"><p className="text-sm font-medium">{item.prompt}</p><p className="mt-1 text-xs text-slate-500">{item.categoryName}{item.topic ? ` · ${item.topic}` : ''} · viewed {item.viewCount} times · {new Date(item.lastViewedAt).toLocaleString()}</p></Link>)}</div>}
            {historyPages > 1 && <div className="p-4"><Pagination page={historyPage} totalPages={historyPages} onPageChange={setHistoryPage} /></div>}
          </Card>
          <Card className="overflow-hidden">
            <div className="border-b border-slate-100 px-5 py-4 dark:border-slate-800"><h2 className="font-semibold">Mock Viva sessions</h2><p className="mt-1 text-sm text-slate-500">Your session notes and self-evaluations are visible only to you.</p></div>
            {sessions.length === 0 ? <EmptyState title="No mock sessions" description="Create a session by selecting questions from the Viva bank." /> : <div className="divide-y divide-slate-100 dark:divide-slate-800">{sessions.map((session) => <Link key={session.id} to={`/viva/sessions/${session.id}`} className="block p-4 hover:bg-slate-50 dark:hover:bg-slate-800/60"><div className="flex items-center justify-between gap-2"><p className="text-sm font-medium">{session.title}</p><Badge>{session.status === 'Completed' ? 'Completed' : 'In progress'}</Badge></div><p className="mt-1 text-xs text-slate-500">{session.questionCount} questions · {new Date(session.startedAt).toLocaleString()}{session.overallRating ? ` · self-rating ${session.overallRating}/5` : ''}</p></Link>)}</div>}
            {sessionPages > 1 && <div className="p-4"><Pagination page={sessionPage} totalPages={sessionPages} onPageChange={setSessionPage} /></div>}
          </Card>
        </div>
      )}
    </>
  )
}
