import { Circle, Flag } from 'lucide-react'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Badge } from '../../components/ui/Badge'
import { temporaryRoadmap } from '../../mock/roadmap'

export function RoadmapPage() {
  return (
    <>
      <PageHeader title="Preparation Roadmap" description="An illustrative sequence for the three BCS stages. This roadmap is temporary preview content." />
      <Card className="p-5 sm:p-6">
        <div className="space-y-0">{temporaryRoadmap.map((phase, index) => (
          <div key={phase.title} className="grid grid-cols-[40px_minmax(0,1fr)]">
            <div className="relative flex justify-center">
              {index < temporaryRoadmap.length - 1 && <div className="absolute bottom-0 top-9 w-px bg-slate-200 dark:bg-slate-700" />}
              <div className="z-10 grid h-8 w-8 place-items-center rounded-full border border-slate-300 bg-white text-slate-400 dark:border-slate-700 dark:bg-slate-900">{index === 1 ? <Circle className="h-3 w-3 fill-current" /> : <Flag className="h-3.5 w-3.5" />}</div>
            </div>
            <div className="pb-8"><div className="flex flex-wrap items-center justify-between gap-2"><div><h2 className="font-semibold text-slate-950 dark:text-white">{phase.title}</h2><p className="mt-1 text-sm text-slate-500">{phase.detail}</p></div><Badge tone="neutral">Suggested</Badge></div></div>
          </div>
        ))}</div>
      </Card>
    </>
  )
}
