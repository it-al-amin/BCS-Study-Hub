import { Check, Circle, Flag } from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Badge } from '../../components/ui/Badge'

const phases = [
  { title: 'Foundation', detail: 'Understand BCS structure and build a daily study routine', status: 'Complete', progress: 100 },
  { title: 'Preliminary preparation', detail: 'Subject coverage, MCQ practice and timed mock exams', status: 'In progress', progress: 48 },
  { title: 'Written preparation', detail: 'Descriptive answers, essays and structured evaluation', status: 'Upcoming', progress: 12 },
  { title: 'Viva preparation', detail: 'Profile, current affairs and cadre-related practice', status: 'Upcoming', progress: 0 },
]

export function RoadmapPage() {
  return (
    <>
      <PageHeader title="Preparation Roadmap" description="A long-term view of the three BCS examination stages." />
      <Card className="p-5 sm:p-6">
        <div className="space-y-0">{phases.map((phase, index) => (
          <div key={phase.title} className="grid grid-cols-[40px_minmax(0,1fr)]">
            <div className="relative flex justify-center">
              {index < phases.length - 1 && <div className="absolute bottom-0 top-9 w-px bg-slate-200 dark:bg-slate-700" />}
              <div className={`z-10 grid h-8 w-8 place-items-center rounded-full border ${phase.progress === 100 ? 'border-emerald-600 bg-emerald-600 text-white' : 'border-slate-300 bg-white text-slate-400 dark:border-slate-700 dark:bg-slate-900'}`}>{phase.progress === 100 ? <Check className="h-4 w-4" /> : index === 1 ? <Circle className="h-3 w-3 fill-current" /> : <Flag className="h-3.5 w-3.5" />}</div>
            </div>
            <div className="pb-8"><div className="flex flex-wrap items-center justify-between gap-2"><div><h2 className="font-semibold text-slate-950 dark:text-white">{phase.title}</h2><p className="mt-1 text-sm text-slate-500">{phase.detail}</p></div><Badge tone={phase.progress === 100 ? 'success' : index === 1 ? 'warning' : 'neutral'}>{phase.status}</Badge></div><div className="mt-4 h-1.5 max-w-xl overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-full rounded-full bg-emerald-600" style={{ width: `${phase.progress}%` }} /></div></div>
          </div>
        ))}</div>
      </Card>
    </>
  )
}
