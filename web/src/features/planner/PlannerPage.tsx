import { CalendarDays, Plus } from 'lucide-react'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Badge } from '../../components/ui/Badge'
import { temporaryPlanner } from '../../mock/planner'

export function PlannerPage() {
  return (
    <>
      <PageHeader title="Study Planner" description="Temporary schedule examples; study blocks cannot be saved yet." action={<Button disabled title="Planner saving is not connected yet"><Plus className="h-4 w-4" />Add study block</Button>} />
      <div className="grid gap-6 lg:grid-cols-[240px_minmax(0,1fr)]">
        <Card className="p-5"><div className="flex items-center gap-2 font-semibold"><CalendarDays className="h-4 w-4 text-emerald-600" />{temporaryPlanner.month} · Sample</div><div className="mt-5 grid grid-cols-7 gap-1 text-center text-xs text-slate-500">{['M','T','W','T','F','S','S'].map((day, index) => <span key={`${day}-${index}`} className="py-1">{day}</span>)}</div></Card>
        <Card><CardHeader title={temporaryPlanner.dayLabel} subtitle="Example schedule · not saved" /><div className="divide-y divide-slate-100 dark:divide-slate-800">{temporaryPlanner.tasks.map((task, index) => <div key={task} className="flex items-center gap-4 px-5 py-4"><div className="w-16 text-xs text-slate-500">{`${8 + index}:00`}</div><div className="flex-1"><p className="text-sm font-medium">{task}</p><p className="mt-1 text-xs text-slate-500">Example study block</p></div><Badge tone="neutral">Example</Badge></div>)}</div></Card>
      </div>
    </>
  )
}
