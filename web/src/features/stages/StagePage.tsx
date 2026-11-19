import { ArrowRight, ClipboardCheck, Clock3, Target } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'

export function StagePage({ stage }: { stage: 'Preliminary' | 'Written' | 'Viva' }) {
  const tools = stage === 'Preliminary'
    ? ['MCQ practice', 'Timed mock exams', 'Previous questions', 'Weak-topic analysis']
    : stage === 'Written'
      ? ['Written syllabus', 'Answer practice', 'Model answers', 'Personal evaluation']
      : ['Viva question bank', 'Mock viva', 'Personal profile', 'Cadre research']
  return (
    <>
      <PageHeader title={`${stage} Preparation`} description={`Study, practice and progress tools for the ${stage.toLowerCase()} stage.`} action={<Button variant="secondary">View syllabus</Button>} />
      <div className="grid gap-4 md:grid-cols-3">
        {[
          { label: 'Stage progress', value: stage === 'Preliminary' ? '42%' : stage === 'Written' ? '18%' : '8%', icon: Target },
          { label: 'Study time', value: stage === 'Preliminary' ? '12h 30m' : '—', icon: Clock3 },
          { label: 'Practice activity', value: 'Ready', icon: ClipboardCheck },
        ].map(({ label, value, icon: Icon }) => <Card key={label} className="p-5"><div className="flex items-center justify-between"><div><p className="text-sm text-slate-500">{label}</p><p className="mt-2 text-xl font-semibold">{value}</p></div><Icon className="h-5 w-5 text-emerald-600" /></div></Card>)}
      </div>
      <div className="mt-6 grid gap-4 sm:grid-cols-2">{tools.map((tool) => <Card key={tool} className="group p-5"><div className="flex items-center justify-between"><div><h2 className="font-medium">{tool}</h2><p className="mt-1 text-sm text-slate-500">Backend-powered workspace preview</p></div><ArrowRight className="h-4 w-4 text-slate-400 transition group-hover:translate-x-1 group-hover:text-emerald-600" /></div></Card>)}</div>
    </>
  )
}
