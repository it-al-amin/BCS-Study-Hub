import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { StatTile } from '../../components/charts/SimpleCharts'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { Table } from '../../components/ui/Table'
import { plannerApi, type ProgressOverview, type TopicStatus } from '../../services/planner'
import { errorMessage, formatMinutes } from '../planner/plannerFormat'

export function ProgressPage() {
  const { accessToken } = useAuth()
  const [overview, setOverview] = useState<ProgressOverview | null>(null)
  const [versionId, setVersionId] = useState('')
  const [filter, setFilter] = useState<'all' | 'pending' | 'completed'>('all')
  const [topics, setTopics] = useState<{ key: string; items: TopicStatus[] } | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!accessToken) return
    let alive = true
    plannerApi.getProgress(accessToken).then((result) => {
      if (!alive) return
      setOverview(result)
      setVersionId((current) => current || (result.stages.find((stage) => stage.totalTopics > 0) ?? result.stages[0])?.syllabusVersionId || '')
    }).catch((reason: unknown) => alive && setError(errorMessage(reason, 'Unable to load progress.')))
    return () => { alive = false }
  }, [accessToken])

  const key = `${versionId}:${filter}`
  useEffect(() => {
    if (!accessToken || !versionId) return
    let alive = true
    plannerApi.getTopics(versionId, { status: filter }, accessToken)
      .then((items) => alive && setTopics({ key, items }))
      .catch((reason: unknown) => alive && setError(errorMessage(reason, 'Unable to load topics.')))
    return () => { alive = false }
  }, [accessToken, versionId, filter, key])

  if (!accessToken) return <><PageHeader title="Progress" /><Card><EmptyState title="Sign in to track progress" description="Progress is calculated from your own topic completion, study sessions, and practice." /></Card></>
  if (error) return <ErrorState description={error} />
  if (!overview) return <LoadingState label="Loading progress..." />

  async function rate(topic: TopicStatus, mastery: number | null) {
    if (!accessToken) return
    try {
      const updated = await plannerApi.setMastery(topic.syllabusItemId, mastery, accessToken)
      setTopics((current) => current && { ...current, items: current.items.map((item) => item.syllabusItemId === updated.syllabusItemId ? updated : item) })
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to save mastery.'))
    }
  }

  const practice = overview.practice
  const currentTopics = topics?.key === key ? topics.items : null
  return (
    <>
      <PageHeader title="Progress" description="Topic completion, self-rated mastery, study time, and practice performance across Preliminary, Written, and Viva." action={<Link to="/reports" className="text-sm text-emerald-700 hover:underline dark:text-emerald-400">Study reports</Link>} />
      <div className="mb-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatTile label="Total study time" value={formatMinutes(overview.totalStudyMinutes)} detail={`${overview.totalSessions} sessions`} />
        <StatTile label="Streak" value={`${overview.streak.current} days`} detail={`Longest ${overview.streak.longest} days`} />
        <StatTile label="MCQ accuracy" value={practice.mcqAccuracyPercent === null ? '—' : `${practice.mcqAccuracyPercent}%`} detail={`${practice.mcqCorrect} of ${practice.mcqAnswered} correct in ${practice.mcqAttempts} attempts`} />
        <StatTile label="Written & Viva" value={`${practice.writtenSubmissions} / ${practice.vivaSessionsCompleted}`} detail={`${practice.writtenWords.toLocaleString()} words submitted · Viva avg ${practice.vivaAverageRating ?? '—'}/5`} />
      </div>
      <div className="mb-5 grid gap-5 lg:grid-cols-3">{overview.stages.map((stage) => <Card key={stage.syllabusVersionId} className={`cursor-pointer p-5 ${stage.syllabusVersionId === versionId ? 'ring-2 ring-emerald-600' : ''}`}>
        <button type="button" className="w-full text-left" onClick={() => setVersionId(stage.syllabusVersionId)}>
          <p className="text-sm font-semibold">{stage.stageName} <span className="font-normal text-slate-500">· {stage.versionLabel}{stage.trackCode !== 'Common' ? ` · ${stage.trackCode}` : ''}</span></p>
          <p className="mt-1 text-2xl font-semibold tabular-nums">{stage.totalTopics === 0 ? '—' : `${stage.percent}%`}</p>
          <p className="text-xs text-slate-500">{stage.totalTopics === 0 ? 'No detailed topics published yet' : `${stage.completedTopics} of ${stage.totalTopics} topics completed`}</p>
        </button>
        <ul className="mt-3 space-y-2">{stage.subjects.map((subject) => <li key={subject.subjectId} className="text-xs"><div className="flex justify-between gap-2"><span className="truncate">{subject.subjectName}</span><span className="shrink-0 tabular-nums text-slate-500">{subject.totalTopics ? `${subject.completedTopics}/${subject.totalTopics}` : '—'} · {formatMinutes(subject.studyMinutes)}{subject.mcqAccuracyPercent !== null ? ` · ${subject.mcqAccuracyPercent}%` : ''}</span></div><div className="mt-1 h-1.5 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-1.5 rounded-full bg-emerald-600" style={{ width: `${subject.percent}%` }} /></div></li>)}</ul>
      </Card>)}</div>
      <Card className="overflow-hidden">
        <CardHeader title="Topics" subtitle="Mark topics complete on the syllabus page; rate your mastery here (1 = weak, 5 = strong)." action={<select aria-label="Topic filter" value={filter} onChange={(event) => setFilter(event.target.value as typeof filter)} className="h-8 rounded-lg border border-slate-200 bg-white px-2 text-xs dark:border-slate-700 dark:bg-slate-900"><option value="all">All topics</option><option value="pending">Pending</option><option value="completed">Completed</option></select>} />
        {!currentTopics ? <LoadingState /> : currentTopics.length === 0 ? <EmptyState title="No topics" description="This syllabus version has no detailed topics, or none match the filter." />
          : <Table headers={['Topic', 'Subject', 'Status', 'Mastery', 'MCQ', 'Study time']}>{currentTopics.map((topic) => <tr key={topic.syllabusItemId}>
            <td className={`px-5 py-2.5 ${topic.kind === 'Subtopic' ? 'pl-10' : 'font-medium'}`}>{topic.title}</td>
            <td className="px-5 py-2.5 text-xs text-slate-500">{topic.subjectName}</td>
            <td className="px-5 py-2.5 text-xs">{topic.isCompleted ? <span className="text-emerald-700 dark:text-emerald-400">Completed</span> : <span className="text-slate-500">Pending</span>}</td>
            <td className="px-5 py-2.5"><div className="flex gap-0.5" role="radiogroup" aria-label={`Mastery for ${topic.title}`}>{[1, 2, 3, 4, 5].map((level) => <button key={level} type="button" role="radio" aria-checked={topic.mastery === level} aria-label={`${level} of 5`} onClick={() => void rate(topic, topic.mastery === level ? null : level)} className={`h-6 w-6 rounded text-xs ${topic.mastery !== null && level <= topic.mastery ? 'bg-emerald-600 text-white' : 'bg-slate-100 text-slate-500 dark:bg-slate-800'}`}>{level}</button>)}</div></td>
            <td className="px-5 py-2.5 text-xs tabular-nums text-slate-500">{topic.mcqAnswered ? `${topic.mcqAccuracyPercent}% of ${topic.mcqAnswered}` : '—'}</td>
            <td className="px-5 py-2.5 text-xs tabular-nums text-slate-500">{formatMinutes(topic.studyMinutes)}</td>
          </tr>)}</Table>}
      </Card>
    </>
  )
}
