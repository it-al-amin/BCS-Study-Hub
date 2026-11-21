import { ArrowRight, ClipboardCheck, Clock3, Target } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { NavLink } from 'react-router-dom'
import { temporaryStageProgress } from '../../mock/stages'

export function StagePage({ stage }: { stage: 'Preliminary' | 'Written' | 'Viva' }) {
  const progress = temporaryStageProgress[stage]
  const tools = stage === 'Preliminary'
    ? ['MCQ practice', 'Timed mock exams', 'Previous questions', 'Weak-topic analysis']
    : stage === 'Written'
      ? ['Written syllabus', 'Answer practice', 'Model answers', 'Personal evaluation']
      : ['Viva question bank', 'Mock viva', 'Personal profile', 'Cadre research']
  return (
    <>
      <PageHeader title={`${stage} Preparation`} description={`Temporary ${stage.toLowerCase()} preview data; no study history is connected.`} action={<NavLink to="/syllabus" className="inline-flex h-10 items-center rounded-lg border border-slate-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800">View syllabus</NavLink>} />
      <div className="grid gap-4 md:grid-cols-3">
        {[
          { label: 'Example progress', value: `${progress.progress}%`, icon: Target },
          { label: 'Example study time', value: progress.studyTime, icon: Clock3 },
          { label: 'Practice activity', value: progress.practiceActivity, icon: ClipboardCheck },
        ].map(({ label, value, icon: Icon }) => <Card key={label} className="p-5"><div className="flex items-center justify-between"><div><p className="text-sm text-slate-500">{label}</p><p className="mt-2 text-xl font-semibold">{value}</p></div><Icon className="h-5 w-5 text-emerald-600" /></div></Card>)}
      </div>
      <div className="mt-6 grid gap-4 sm:grid-cols-2">{tools.map((tool) => <Card key={tool} className="group p-5"><div className="flex items-center justify-between"><div><h2 className="font-medium">{tool}</h2><p className="mt-1 text-sm text-slate-500">Planned workspace; not connected yet</p></div><ArrowRight className="h-4 w-4 text-slate-400 transition group-hover:translate-x-1 group-hover:text-emerald-600" /></div></Card>)}</div>
    </>
  )
}
