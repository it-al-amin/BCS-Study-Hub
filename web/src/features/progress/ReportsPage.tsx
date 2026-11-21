import { ChevronLeft, ChevronRight, Printer } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useAuth } from '../../app/useAuth'
import { BarList, ColumnChart, StatTile } from '../../components/charts/SimpleCharts'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { plannerApi, type StudyReport } from '../../services/planner'
import { activityLabels, addDays, errorMessage, formatDay, formatMinutes, metricLabels, todayIso } from '../planner/plannerFormat'

type Period = 'daily' | 'weekly' | 'monthly'

function shift(period: Period, date: string, direction: number) {
  if (period === 'daily') return addDays(date, direction)
  if (period === 'weekly') return addDays(date, 7 * direction)
  const [year, month] = date.split('-').map(Number)
  return new Date(Date.UTC(year, month - 1 + direction, 1)).toISOString().slice(0, 10)
}

export function ReportsPage() {
  const { accessToken } = useAuth()
  const [period, setPeriod] = useState<Period>('weekly')
  const [date, setDate] = useState(todayIso())
  const [loaded, setLoaded] = useState<{ key: string; report: StudyReport | null; error: string } | null>(null)
  const key = `${period}:${date}`

  useEffect(() => {
    if (!accessToken) return
    let alive = true
    plannerApi.getReport(period, date, accessToken)
      .then((report) => alive && setLoaded({ key, report, error: '' }))
      .catch((reason: unknown) => alive && setLoaded({ key, report: null, error: errorMessage(reason, 'Unable to load the report.') }))
    return () => { alive = false }
  }, [accessToken, period, date, key])

  if (!accessToken) return <><PageHeader title="Study reports" /><Card><EmptyState title="Sign in" description="Reports are built from your own activity." /></Card></>
  const current = loaded?.key === key ? loaded : null
  const report = current?.report

  return (
    <>
      <div className="mb-5 flex flex-col gap-3 print:hidden sm:flex-row sm:items-center sm:justify-between">
        <div className="flex rounded-lg border border-slate-200 bg-white p-1 dark:border-slate-700 dark:bg-slate-900">{(['daily', 'weekly', 'monthly'] as Period[]).map((value) => <button key={value} type="button" onClick={() => setPeriod(value)} className={`rounded-md px-3 py-1.5 text-sm capitalize ${value === period ? 'bg-emerald-700 text-white' : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'}`}>{value}</button>)}</div>
        <div className="flex items-center gap-2"><Button size="sm" variant="ghost" aria-label="Previous period" onClick={() => setDate(shift(period, date, -1))}><ChevronLeft className="h-4 w-4" /></Button><input type="date" aria-label="Report date" className="h-9 rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-900" value={date} onChange={(event) => event.target.value && setDate(event.target.value)} /><Button size="sm" variant="ghost" aria-label="Next period" onClick={() => setDate(shift(period, date, 1))}><ChevronRight className="h-4 w-4" /></Button><Button size="sm" variant="secondary" onClick={() => window.print()}><Printer className="h-4 w-4" />Print</Button></div>
      </div>
      {!current ? <LoadingState label="Building report..." /> : current.error ? <ErrorState description={current.error} /> : report && <>
        <PageHeader title={`${report.period} study report`} description={`${report.label} · ${formatDay(report.from)}${report.to !== report.from ? ` – ${formatDay(report.to)}` : ''}`} />
        <div className="mb-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatTile label="Study time" value={formatMinutes(report.studyMinutes)} detail={`${report.sessionCount} sessions · ${report.activeDays} active day${report.activeDays === 1 ? '' : 's'}`} />
          <StatTile label="Tasks" value={`${report.tasks.completed}/${report.tasks.planned}`} detail={`${report.tasks.skipped} skipped · planned ${formatMinutes(report.tasks.plannedMinutes)}, actual ${formatMinutes(report.tasks.actualMinutes)}`} />
          <StatTile label="MCQ accuracy" value={report.practice.mcqAccuracyPercent === null ? '—' : `${report.practice.mcqAccuracyPercent}%`} detail={`${report.practice.mcqCorrect}/${report.practice.mcqAnswered} correct · ${report.practice.mcqAttempts} attempts`} />
          <StatTile label="Revision" value={report.revision.reviews} detail={`${report.revision.retentionPercent ?? '—'}% recalled (Good/Easy) · ${report.revision.dueNow} due now`} />
        </div>
        <div className="grid gap-5 xl:grid-cols-2">
          {report.minutesByDay.length > 1 && <Card className="xl:col-span-2"><CardHeader title="Study minutes per day" /><div className="p-5"><ColumnChart label="Study minutes per day" format={formatMinutes} data={report.minutesByDay.map((day) => ({ key: day.date, label: report.period === 'Monthly' ? day.date.slice(8) : formatDay(day.date, { weekday: 'short' }), value: day.minutes }))} /></div></Card>}
          <Card><CardHeader title="Time by subject" />{report.minutesBySubject.length === 0 ? <p className="px-5 py-6 text-sm text-slate-500">No sessions recorded.</p> : <div className="p-5"><BarList format={formatMinutes} data={report.minutesBySubject.map((item) => ({ key: item.key, label: item.label, value: item.minutes }))} /></div>}</Card>
          <Card><CardHeader title="Time by stage and activity" />{report.minutesByStage.length === 0 ? <p className="px-5 py-6 text-sm text-slate-500">No sessions recorded.</p> : <div className="space-y-5 p-5"><BarList format={formatMinutes} data={report.minutesByStage.map((item) => ({ key: item.key, label: item.label, value: item.minutes }))} /><BarList format={formatMinutes} data={report.minutesByActivity.map((item) => ({ key: item.key, label: activityLabels[item.key as keyof typeof activityLabels] ?? item.label, value: item.minutes }))} /></div>}</Card>
          <Card><CardHeader title="MCQ accuracy by subject" subtitle={report.weakSubjects.length ? `Weak: ${report.weakSubjects.map((subject) => subject.subjectName).join(', ')}` : 'No weak subjects in this period'} />{report.mcqBySubject.length === 0 ? <p className="px-5 py-6 text-sm text-slate-500">No MCQ practice in this period.</p> : <div className="p-5"><BarList max={100} format={(value) => `${value}%`} data={report.mcqBySubject.map((item) => ({ key: item.subjectId, label: item.subjectName, value: item.accuracyPercent ?? 0, detail: `${item.correct}/${item.answered}` }))} /></div>}</Card>
          <Card className="p-5"><h2 className="font-semibold">Written & Viva practice</h2><dl className="mt-3 grid grid-cols-2 gap-3 text-sm"><div><dt className="text-xs text-slate-500">Written submissions</dt><dd className="text-lg font-semibold tabular-nums">{report.practice.writtenSubmissions}</dd></div><div><dt className="text-xs text-slate-500">Words submitted</dt><dd className="text-lg font-semibold tabular-nums">{report.practice.writtenWords.toLocaleString()}</dd></div><div><dt className="text-xs text-slate-500">Drafts in progress</dt><dd className="text-lg font-semibold tabular-nums">{report.practice.writtenDrafts}</dd></div><div><dt className="text-xs text-slate-500">Viva mocks completed</dt><dd className="text-lg font-semibold tabular-nums">{report.practice.vivaSessionsCompleted}{report.practice.vivaAverageRating !== null ? <span className="ml-1 text-xs font-normal text-slate-500">avg {report.practice.vivaAverageRating}/5</span> : null}</dd></div></dl></Card>
          <Card className="p-5"><h2 className="font-semibold">Topics completed ({report.topicsCompleted})</h2>{report.completedTopicTitles.length === 0 ? <p className="mt-2 text-sm text-slate-500">None in this period.</p> : <ul className="mt-2 list-inside list-disc text-sm">{report.completedTopicTitles.map((title, index) => <li key={`${title}-${index}`}>{title}</li>)}</ul>}</Card>
          <Card className="p-5"><h2 className="font-semibold">Revision</h2><dl className="mt-3 grid grid-cols-4 gap-2 text-center text-sm">{Object.entries(report.revision.byOutcome).map(([outcome, count]) => <div key={outcome}><dt className="text-xs text-slate-500">{outcome}</dt><dd className="text-lg font-semibold tabular-nums">{count}</dd></div>)}</dl><p className="mt-3 text-xs text-slate-500">{report.revision.activeItems} active items · {report.revision.overdue} overdue · {report.revision.retiredItems} retired</p></Card>
          {report.goals.length > 0 && <Card className="p-5 xl:col-span-2"><h2 className="font-semibold">Goals for this period</h2><div className="mt-3"><BarList max={100} format={(value) => `${value}%`} data={report.goals.map((goal) => ({ key: goal.id, label: goal.title, value: goal.percentComplete, detail: `${goal.currentValue}/${goal.targetValue} ${metricLabels[goal.metric].toLowerCase()}` }))} /></div></Card>}
        </div>
      </>}
    </>
  )
}
