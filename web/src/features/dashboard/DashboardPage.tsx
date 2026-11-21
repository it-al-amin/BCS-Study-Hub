import { CalendarDays, Flame, NotebookPen, RotateCcw, Timer } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { BarList, StatTile } from '../../components/charts/SimpleCharts'
import { plannerApi, type Dashboard } from '../../services/planner'
import { errorMessage, formatDay, formatMinutes, metricLabels, priorityTone, statusLabels } from '../planner/plannerFormat'

const activityLabels: Record<string, string> = {
  StudySession: 'Study', McqAttempt: 'MCQ practice', WrittenSubmission: 'Written answer', VivaSession: 'Viva mock', RevisionReview: 'Revision',
}

export function DashboardPage() {
  const { accessToken, user } = useAuth()
  const [dashboard, setDashboard] = useState<Dashboard | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let active = true
    plannerApi.getDashboard(accessToken)
      .then((result) => active && setDashboard(result))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load your dashboard.')))
    return () => { active = false }
  }, [accessToken])

  if (!accessToken) {
    return (
      <>
        <PageHeader title="BCS Study Hub" description="Plan, practise, and track your BCS Preliminary, Written, and Viva preparation." />
        <Card><EmptyState title="Sign in to see your dashboard" description="Your dashboard is built from your own study sessions, tasks, practice attempts, and revision. Nothing is shown until you start recording activity." /></Card>
        <div className="mt-4 flex gap-2"><Link to="/login"><Button>Sign in</Button></Link><Link to="/register"><Button variant="secondary">Create account</Button></Link></div>
      </>
    )
  }

  if (error) return <ErrorState description={error} />
  if (!dashboard) return <LoadingState label="Loading dashboard..." />

  const practice = dashboard.last30Days
  return (
    <>
      <PageHeader
        title={`Welcome${user?.displayName ? `, ${user.displayName}` : ''}`}
        description={`${formatDay(dashboard.today, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })} · every figure below comes from your recorded activity.`}
        action={<div className="flex gap-2"><Link to="/planner"><Button variant="secondary"><CalendarDays className="h-4 w-4" />Planner</Button></Link><Link to="/revision"><Button><RotateCcw className="h-4 w-4" />Revise ({dashboard.revisionDue + dashboard.currentAffairsDue})</Button></Link></div>}
      />
      {dashboard.activeSession && <Link to="/planner" className="mb-5 flex items-center gap-3 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900 dark:border-emerald-900/60 dark:bg-emerald-950/30 dark:text-emerald-200"><Timer className="h-5 w-5" />Study session running since {new Date(dashboard.activeSession.startedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}{dashboard.activeSession.subjectName ? ` · ${dashboard.activeSession.subjectName}` : ''}. Open the planner to stop it.</Link>}
      <div className="mb-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatTile label="Studied today" value={formatMinutes(dashboard.todayMinutes)} detail={`${formatMinutes(dashboard.weekMinutes)} this week · ${formatMinutes(dashboard.monthMinutes)} this month`} />
        <div className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900"><p className="flex items-center gap-1 text-xs font-medium uppercase tracking-wide text-slate-500"><Flame className="h-3.5 w-3.5 text-amber-500" />Study streak</p><p className="mt-1.5 text-2xl font-semibold tabular-nums">{dashboard.streak.current} day{dashboard.streak.current === 1 ? '' : 's'}</p><p className="mt-0.5 text-xs text-slate-500">Longest {dashboard.streak.longest} · a day counts at {dashboard.streak.minimumMinutes}+ min or any practice/revision</p></div>
        <StatTile label="Revision due" value={dashboard.revisionDue + dashboard.currentAffairsDue} detail={`${dashboard.currentAffairsDue} current affairs · ${dashboard.openMistakes} open mistakes`} />
        <StatTile label="MCQ accuracy (30 days)" value={practice.mcqAccuracyPercent === null ? '—' : `${practice.mcqAccuracyPercent}%`} detail={`${practice.mcqAnswered} answered · ${practice.writtenSubmissions} Written · ${practice.vivaSessionsCompleted} Viva`} />
      </div>
      <div className="grid gap-5 xl:grid-cols-3">
        <Card className="xl:col-span-2">
          <CardHeader title="Today's plan" action={<Link to="/planner" className="text-sm text-emerald-700 hover:underline dark:text-emerald-400">Open planner</Link>} />
          {dashboard.todayTasks.length === 0 ? <EmptyState title="No tasks scheduled today" description="Add tasks or generate them from a recurring study schedule in the planner." />
            : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{dashboard.todayTasks.map((task) => <li key={task.id} className="flex items-center gap-3 px-5 py-3"><span className="w-12 shrink-0 text-xs text-slate-500">{task.startTime?.slice(0, 5) ?? '—'}</span><div className="min-w-0 flex-1"><p className={`truncate text-sm font-medium ${task.status === 'Completed' ? 'text-slate-400 line-through' : ''}`}>{task.title}</p><p className="truncate text-xs text-slate-500">{[task.subjectName, task.topicTitle, `${formatMinutes(task.actualMinutes)} of ${formatMinutes(task.plannedMinutes)}`].filter(Boolean).join(' · ')}</p></div><Badge tone={priorityTone[task.priority]}>{task.priority}</Badge><span className="w-20 text-right text-xs text-slate-500">{statusLabels[task.status]}</span></li>)}</ul>}
        </Card>
        <Card>
          <CardHeader title="Goals in progress" />
          {dashboard.goals.length === 0 ? <p className="px-5 py-6 text-sm text-slate-500">No goals for today, this week, or this month. <Link to="/planner?tab=goals" className="text-emerald-700 hover:underline dark:text-emerald-400">Set one</Link>.</p>
            : <div className="p-5"><BarList max={100} format={(value) => `${value}%`} data={dashboard.goals.map((goal) => ({ key: goal.id, label: goal.title, value: goal.percentComplete, detail: `${goal.currentValue}/${goal.targetValue} ${metricLabels[goal.metric].toLowerCase()} · ${goal.period.toLowerCase()}` }))} /></div>}
        </Card>
        <Card className="xl:col-span-2">
          <CardHeader title="Syllabus progress" subtitle="Completed topics in each published syllabus version" action={<Link to="/progress" className="text-sm text-emerald-700 hover:underline dark:text-emerald-400">Details</Link>} />
          {dashboard.stages.length === 0 ? <p className="px-5 py-6 text-sm text-slate-500">No published syllabus versions yet.</p>
            : <div className="grid gap-5 p-5 md:grid-cols-3">{dashboard.stages.map((stage) => <div key={stage.syllabusVersionId}><p className="text-sm font-medium">{stage.stageName}</p><p className="text-xs text-slate-500">{stage.versionLabel}{stage.trackCode !== 'Common' ? ` · ${stage.trackCode}` : ''}</p><p className="mt-2 text-2xl font-semibold tabular-nums">{stage.totalTopics === 0 ? '—' : `${stage.percent}%`}</p><p className="text-xs text-slate-500">{stage.totalTopics === 0 ? 'Outline only: no detailed topics published' : `${stage.completedTopics} of ${stage.totalTopics} topics`}</p><div className="mt-2 h-1.5 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-1.5 rounded-full bg-emerald-600" style={{ width: `${stage.percent}%` }} /></div></div>)}</div>}
        </Card>
        <Card>
          <CardHeader title="Weak subjects (30 days)" subtitle="Below your accuracy threshold" />
          {dashboard.weakSubjects.length === 0 ? <p className="px-5 py-6 text-sm text-slate-500">None. Weak subjects appear once you have enough MCQ answers in a subject.</p>
            : <div className="p-5"><BarList max={100} format={(value) => `${value}%`} data={dashboard.weakSubjects.map((subject) => ({ key: subject.subjectId, label: subject.subjectName, value: subject.accuracyPercent ?? 0, detail: `${subject.answered} answers` }))} /><Link to="/revision?tab=weak" className="mt-3 inline-block text-sm text-emerald-700 hover:underline dark:text-emerald-400">Revise weak topics</Link></div>}
        </Card>
        <Card className="xl:col-span-3">
          <CardHeader title="Recent activity" action={<Link to="/reports" className="text-sm text-emerald-700 hover:underline dark:text-emerald-400">Reports</Link>} />
          {dashboard.recentActivity.length === 0 ? <EmptyState title="No activity yet" description="Start a study session, take a practice set, submit a Written answer, or review your revision queue." />
            : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{dashboard.recentActivity.map((item, index) => <li key={`${item.type}-${index}`} className="flex items-center gap-3 px-5 py-2.5 text-sm"><Badge tone="neutral">{activityLabels[item.type] ?? item.type}</Badge><span className="min-w-0 flex-1 truncate">{item.title}</span>{item.detail && <span className="text-xs text-slate-500">{item.detail}</span>}<time className="w-32 text-right text-xs text-slate-500">{new Date(item.occurredAt).toLocaleString([], { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}</time></li>)}</ul>}
        </Card>
      </div>
      <p className="mt-5 text-xs text-slate-500"><NotebookPen className="mr-1 inline h-3.5 w-3.5" />Streak, weak-subject, and revision rules use your own settings. Change them on the <Link to="/revision?tab=settings" className="hover:underline">revision settings</Link> page.</p>
    </>
  )
}
