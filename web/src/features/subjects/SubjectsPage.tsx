import { ArrowRight, BookOpen } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { temporarySubjects } from '../../mock/subjects'

export function SubjectsPage() {
  return (
    <>
      <PageHeader title="Subjects" description="Temporary subject examples for layout review; this is not the official syllabus." />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{temporarySubjects.map((subject) => <Card key={subject.name} className="group p-5"><div className="flex items-start justify-between"><div className="flex items-center gap-3"><div className="grid h-10 w-10 place-items-center rounded-lg bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300"><BookOpen className="h-5 w-5" /></div><div><h2 className="font-medium">{subject.name}</h2><p className="mt-1 text-xs text-slate-500">Example: {subject.topicCount} topics</p></div></div><ArrowRight className="h-4 w-4 text-slate-300 transition group-hover:text-emerald-600" /></div></Card>)}</div>
    </>
  )
}
