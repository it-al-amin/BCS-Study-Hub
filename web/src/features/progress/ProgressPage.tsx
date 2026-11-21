import { PageHeader } from '../../components/ui/PageHeader'
import { Card, CardHeader } from '../../components/ui/Card'
import { Table } from '../../components/ui/Table'
import { temporaryProgress } from '../../mock/progress'

export function ProgressPage() {
  return (
    <>
      <PageHeader title="Study Progress" description="Temporary example metrics; no study history is connected." />
      <div className="grid gap-6 md:grid-cols-3">{temporaryProgress.stages.map((stage) => <Card key={stage.name} className="p-5"><p className="text-sm text-slate-500">{stage.name}</p><p className="mt-2 text-2xl font-semibold">{stage.progress}%</p><div className="mt-4 h-2 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-full rounded-full bg-emerald-600" style={{ width: `${stage.progress}%` }} /></div><p className="mt-3 text-xs text-slate-500">Example: {stage.studyTime}</p></Card>)}</div>
      <Card className="mt-6"><CardHeader title="Example subject summary" subtitle="Temporary preview data" /><Table headers={['Subject', 'Completed topics', 'Avg. quiz score']} >{temporaryProgress.subjects.map((subject) => <tr key={subject.name}><td className="px-5 py-4">{subject.name}</td><td className="px-5 py-4">{subject.completedTopics}</td><td className="px-5 py-4">{subject.averageQuizScore}</td></tr>)}</Table></Card>
    </>
  )
}
