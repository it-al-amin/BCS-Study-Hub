import { ArrowRight, Clock3, Flame, Target } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { temporaryDashboard } from '../../mock/dashboard'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'

export function DashboardPage() {
  return (
    <>
      <PageHeader title="Good evening, Candidate" description="Here is your preparation snapshot for today." action={<Button><NavLink to="/planner">Open study planner</NavLink></Button>} />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {[
          { label: 'Overall progress', value: `${temporaryDashboard.overallProgress}%`, icon: Target, detail: 'Across all stages' },
          { label: 'Study time', value: temporaryDashboard.studyTime, icon: Clock3, detail: 'This week' },
          { label: 'Study streak', value: `${temporaryDashboard.streak} days`, icon: Flame, detail: 'Personal best: 14' },
          { label: 'Weekly goal', value: `${temporaryDashboard.weeklyGoal}%`, icon: Target, detail: '34 of 50 study hours' },
        ].map(({ label, value, icon: Icon, detail }) => (
          <Card key={label} className="p-5"><div className="flex items-start justify-between"><div><p className="text-sm text-slate-500">{label}</p><p className="mt-2 text-2xl font-semibold text-slate-950 dark:text-white">{value}</p><p className="mt-1 text-xs text-slate-400">{detail}</p></div><div className="rounded-lg bg-emerald-50 p-2 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300"><Icon className="h-5 w-5" /></div></div></Card>
        ))}
      </div>
      <div className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,1.5fr)_minmax(300px,.7fr)]">
        <Card><CardHeader title="Subject progress" subtitle="Live from study activity" action={<NavLink to="/progress" className="text-sm font-medium text-emerald-700">View report</NavLink>} /><div className="space-y-5 p-5">{temporaryDashboard.subjects.map((subject) => <div key={subject.name}><div className="mb-2 flex justify-between text-sm"><span className="font-medium">{subject.name}</span><span className="text-slate-500">{subject.progress}%</span></div><div className="h-2 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800"><div className={`h-full rounded-full ${subject.color}`} style={{ width: `${subject.progress}%` }} /></div></div>)}</div></Card>
        <Card><CardHeader title="Upcoming tasks" subtitle="Your next study blocks" /><div className="divide-y divide-slate-100 px-5 dark:divide-slate-800">{temporaryDashboard.tasks.map((task) => <div key={task.title} className="flex items-center justify-between gap-3 py-4"><div><p className="text-sm font-medium">{task.title}</p><p className="mt-1 text-xs text-slate-500">{task.meta}</p></div><Badge tone={task.tone}>{task.tone === 'warning' ? 'Due' : 'Planned'}</Badge></div>)}</div></Card>
      </div>
      <div className="mt-6 grid gap-6 md:grid-cols-2">
        <Card><CardHeader title="Revision queue" subtitle="Topics most ready for review" action={<NavLink to="/revision" className="text-sm text-emerald-700">Open queue</NavLink>} /><div className="divide-y divide-slate-100 px-5 dark:divide-slate-800">{temporaryDashboard.revisions.map((item) => <div key={item.title} className="flex items-center justify-between py-4"><div><p className="text-sm font-medium">{item.title}</p><p className="mt-1 text-xs text-slate-500">{item.type}</p></div><span className="flex items-center gap-1 text-sm text-slate-500">{item.count}<ArrowRight className="h-4 w-4" /></span></div>)}</div></Card>
        <Card><CardHeader title="Recent activity" subtitle="Temporary UI preview" /><div className="grid min-h-48 place-items-center p-5 text-center"><div><p className="text-sm font-medium">No backend activity connected</p><p className="mt-1 text-sm text-slate-500">Real attempts and study sessions will appear here.</p></div></div></Card>
      </div>
    </>
  )
}
