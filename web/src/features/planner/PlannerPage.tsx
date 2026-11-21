import { ChevronLeft, ChevronRight, Pencil, Play, Plus, Square, Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { TopicSelect, type TopicSelection } from '../../components/content/TopicSelect'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { Dialog } from '../../components/ui/Dialog'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import {
  plannerApi,
  type CalendarDay,
  type GoalMetric,
  type PlannerPeriod,
  type StudyActivity,
  type StudyGoal,
  type StudySchedule,
  type StudySession,
  type StudyStage,
  type StudyTask,
  type TaskPriority,
  type TaskStatus,
} from '../../services/planner'
import { activityLabels, addDays, errorMessage, formatDay, formatMinutes, metricLabels, monthGrid, priorityTone, statusLabels, todayIso } from './plannerFormat'

const field = 'mt-1 h-9 w-full rounded-lg border border-slate-200 bg-white px-2 text-sm dark:border-slate-700 dark:bg-slate-950'
const labelClass = 'block text-xs font-medium text-slate-600 dark:text-slate-300'
const stages: StudyStage[] = ['Preliminary', 'Written', 'Viva', 'General']
const priorities: TaskPriority[] = ['Low', 'Medium', 'High', 'Urgent']
const weekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
type Tab = 'calendar' | 'goals' | 'schedules' | 'sessions'

export function PlannerPage() {
  const { accessToken } = useAuth()
  const [params, setParams] = useSearchParams()
  const tab = (params.get('tab') as Tab | null) ?? 'calendar'
  const [reload, setReload] = useState(0)
  const [error, setError] = useState('')
  const refresh = () => setReload((count) => count + 1)

  if (!accessToken) return <><PageHeader title="Study Planner" /><Card><EmptyState title="Sign in to plan your study" description="Tasks, schedules, sessions, and goals are private to your account." /></Card></>

  const tabs: [Tab, string][] = [['calendar', 'Calendar'], ['goals', 'Goals'], ['schedules', 'Schedules'], ['sessions', 'Sessions']]
  return (
    <>
      <PageHeader title="Study Planner" description="Plan daily tasks and recurring schedules, time your study sessions, and set daily, weekly and monthly goals. Progress is calculated from what you actually record." />
      <SessionTimer token={accessToken} onChanged={refresh} reload={reload} />
      <div className="mb-4 flex flex-wrap gap-1">{tabs.map(([value, label]) => <button key={value} type="button" onClick={() => setParams(value === 'calendar' ? {} : { tab: value })} className={`rounded-lg px-3 py-1.5 text-sm ${tab === value ? 'bg-emerald-700 text-white' : 'border border-slate-200 bg-white hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:hover:bg-slate-800'}`}>{label}</button>)}</div>
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {tab === 'calendar' && <CalendarTab token={accessToken} reload={reload} onChanged={refresh} onError={setError} />}
      {tab === 'goals' && <GoalsTab token={accessToken} onError={setError} />}
      {tab === 'schedules' && <SchedulesTab token={accessToken} onError={setError} />}
      {tab === 'sessions' && <SessionsTab token={accessToken} reload={reload} onChanged={refresh} onError={setError} />}
    </>
  )
}

function SessionTimer({ token, reload, onChanged }: { token: string; reload: number; onChanged: () => void }) {
  const [active, setActive] = useState<StudySession | null | undefined>(undefined)
  const [activity, setActivity] = useState<StudyActivity>('Reading')
  const [scope, setScope] = useState<TopicSelection | null>(null)
  const [now, setNow] = useState(() => Date.now())
  const [error, setError] = useState('')

  useEffect(() => {
    let alive = true
    plannerApi.getActiveSession(token).then((session) => alive && setActive(session ?? null)).catch(() => alive && setActive(null))
    return () => { alive = false }
  }, [token, reload])

  useEffect(() => {
    if (!active) return
    const timer = window.setInterval(() => setNow(Date.now()), 15000)
    return () => window.clearInterval(timer)
  }, [active])

  async function start() {
    setError('')
    try {
      setActive(await plannerApi.startSession({ activity, taskId: null, subjectId: scope?.subjectId ?? null, syllabusItemId: scope?.syllabusItemId ?? null, stage: null }, token))
      setNow(Date.now())
      onChanged()
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to start the session.'))
    }
  }

  async function stop() {
    if (!active) return
    try {
      await plannerApi.stopSession(active.id, null, token)
      setActive(null)
      onChanged()
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to stop the session.'))
    }
  }

  if (active === undefined) return null
  return (
    <Card className="mb-5 p-4">
      {active ? <div className="flex flex-wrap items-center gap-3">
        <span className="relative flex h-3 w-3"><span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75" /><span className="relative inline-flex h-3 w-3 rounded-full bg-emerald-600" /></span>
        <p className="flex-1 text-sm"><strong>{activityLabels[active.activity]}</strong>{active.subjectName ? ` · ${active.subjectName}` : ''}{active.taskTitle ? ` · ${active.taskTitle}` : ''} · started {new Date(active.startedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} · <span className="tabular-nums">{formatMinutes(Math.max(0, Math.floor((now - new Date(active.startedAt).getTime()) / 60000)))}</span></p>
        <Button onClick={() => void stop()}><Square className="h-4 w-4" />Stop session</Button>
      </div> : <div className="space-y-3">
        <div className="flex flex-wrap items-end gap-2">
          <label className={labelClass}>Activity<select className={`${field} w-40`} value={activity} onChange={(event) => setActivity(event.target.value as StudyActivity)}>{Object.entries(activityLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
          <Button onClick={() => void start()}><Play className="h-4 w-4" />Start study timer</Button>
        </div>
        <TopicSelect value={scope} onChange={setScope} />
      </div>}
      {error && <p className="mt-2 text-xs text-red-600">{error}</p>}
    </Card>
  )
}

function CalendarTab({ token, reload, onChanged, onError }: { token: string; reload: number; onChanged: () => void; onError: (value: string) => void }) {
  const [month, setMonth] = useState(todayIso().slice(0, 7))
  const [selected, setSelected] = useState(todayIso())
  const [days, setDays] = useState<CalendarDay[] | null>(null)
  const [editing, setEditing] = useState<StudyTask | 'new' | null>(null)
  const grid = monthGrid(`${month}-01`)
  const gridStart = grid[0]
  const gridEnd = grid[grid.length - 1]
  const today = todayIso()

  useEffect(() => {
    let alive = true
    plannerApi.getCalendar(gridStart, gridEnd, token)
      .then((result) => alive && setDays(result.days))
      .catch((reason: unknown) => alive && onError(errorMessage(reason, 'Unable to load the calendar.')))
    return () => { alive = false }
  }, [gridStart, gridEnd, token, reload, onError])

  const byDate = new Map((days ?? []).map((day) => [day.date, day]))
  const day = byDate.get(selected)

  function shiftMonth(delta: number) {
    const [year, value] = month.split('-').map(Number)
    const next = new Date(Date.UTC(year, value - 1 + delta, 1))
    setMonth(next.toISOString().slice(0, 7))
  }

  async function setStatus(task: StudyTask, status: TaskStatus) {
    try {
      const minutes = status === 'Completed' && !task.hasSessions && task.actualMinutes === 0
        ? Number(window.prompt('Minutes actually studied (leave blank if none):', String(task.plannedMinutes)) || 0) || null
        : null
      await plannerApi.setTaskStatus(task.id, status, minutes, token)
      onChanged()
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to update the task.'))
    }
  }

  return (
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)]">
      <Card className="p-4">
        <div className="mb-3 flex items-center justify-between"><Button size="sm" variant="ghost" aria-label="Previous month" onClick={() => shiftMonth(-1)}><ChevronLeft className="h-4 w-4" /></Button><h2 className="font-semibold">{formatDay(`${month}-01`, { month: 'long', year: 'numeric' })}</h2><Button size="sm" variant="ghost" aria-label="Next month" onClick={() => shiftMonth(1)}><ChevronRight className="h-4 w-4" /></Button></div>
        {!days ? <LoadingState /> : <div className="grid grid-cols-7 gap-1 text-xs">
          {weekdays.map((name) => <div key={name} className="py-1 text-center font-medium text-slate-500">{name.slice(0, 3)}</div>)}
          {grid.map((date) => {
            const entry = byDate.get(date)
            const inMonth = date.startsWith(month)
            const done = entry?.tasks.filter((task) => task.status === 'Completed').length ?? 0
            return <button key={date} type="button" onClick={() => setSelected(date)} aria-label={formatDay(date)} className={`min-h-20 rounded-lg border p-1.5 text-left align-top ${selected === date ? 'border-emerald-600 ring-1 ring-emerald-600' : 'border-slate-200 dark:border-slate-800'} ${inMonth ? '' : 'opacity-40'} hover:bg-slate-50 dark:hover:bg-slate-800/60`}>
              <span className={`inline-grid h-6 w-6 place-items-center rounded-full ${date === today ? 'bg-emerald-700 text-white' : ''}`}>{Number(date.slice(8))}</span>
              {entry && entry.tasks.length > 0 && <span className="block truncate text-[11px] text-slate-600 dark:text-slate-300">{done}/{entry.tasks.length} tasks</span>}
              {entry && entry.studyMinutes > 0 && <span className="block text-[11px] text-emerald-700 dark:text-emerald-400">{formatMinutes(entry.studyMinutes)}</span>}
              {entry && entry.revisionDue > 0 && <span className="block text-[11px] text-amber-700 dark:text-amber-400">{entry.revisionDue} to revise</span>}
            </button>
          })}
        </div>}
      </Card>
      <Card>
        <CardHeader title={formatDay(selected, { weekday: 'long', day: 'numeric', month: 'long' })} subtitle={day ? `${formatMinutes(day.studyMinutes)} studied${day.isActiveDay ? ' · counts toward your streak' : ''}` : undefined} action={<Button size="sm" onClick={() => setEditing('new')}><Plus className="h-4 w-4" />Task</Button>} />
        {!day || day.tasks.length === 0 ? <EmptyState title="No tasks" description="Add a task for this day, or generate tasks from a schedule." />
          : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{day.tasks.map((task) => <li key={task.id} className="px-5 py-3">
            <div className="flex items-start gap-2">
              <input type="checkbox" aria-label={`Mark ${task.title} complete`} className="mt-1" checked={task.status === 'Completed'} onChange={(event) => void setStatus(task, event.target.checked ? 'Completed' : 'Planned')} />
              <div className="min-w-0 flex-1"><p className={`text-sm font-medium ${task.status === 'Completed' ? 'text-slate-400 line-through' : ''}`}>{task.startTime ? `${task.startTime.slice(0, 5)} · ` : ''}{task.title}</p><p className="text-xs text-slate-500">{[task.stage, task.subjectName, task.topicTitle].filter(Boolean).join(' · ')}</p><p className="text-xs text-slate-500">Planned {formatMinutes(task.plannedMinutes)} · actual {formatMinutes(task.actualMinutes)}{task.hasSessions ? ' (from sessions)' : ''}</p></div>
              <Badge tone={priorityTone[task.priority]}>{task.priority}</Badge>
            </div>
            <div className="mt-2 flex flex-wrap gap-1 pl-6">
              <select aria-label="Task status" className="h-7 rounded border border-slate-200 bg-white px-1 text-xs dark:border-slate-700 dark:bg-slate-900" value={task.status} onChange={(event) => void setStatus(task, event.target.value as TaskStatus)}>{Object.entries(statusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>
              <Button size="sm" variant="ghost" aria-label="Edit task" onClick={() => setEditing(task)}><Pencil className="h-3.5 w-3.5" /></Button>
              <Button size="sm" variant="ghost" aria-label="Delete task" onClick={() => window.confirm('Delete this task?') && void plannerApi.deleteTask(task.id, token).then(onChanged)}><Trash2 className="h-3.5 w-3.5" /></Button>
            </div>
          </li>)}</ul>}
      </Card>
      <TaskDialog token={token} task={editing} date={selected} onClose={() => setEditing(null)} onSaved={() => { setEditing(null); onChanged() }} />
    </div>
  )
}

function TaskDialog({ token, task, date, onClose, onSaved }: { token: string; task: StudyTask | 'new' | null; date: string; onClose: () => void; onSaved: () => void }) {
  return <Dialog open={task !== null} title={task === 'new' ? 'New study task' : 'Edit task'} onClose={onClose}>{task && <TaskForm key={task === 'new' ? `new-${date}` : task.id} token={token} task={task === 'new' ? null : task} date={date} onSaved={onSaved} onCancel={onClose} />}</Dialog>
}

function TaskForm({ token, task, date, onSaved, onCancel }: { token: string; task: StudyTask | null; date: string; onSaved: () => void; onCancel: () => void }) {
  const [title, setTitle] = useState(task?.title ?? '')
  const [description, setDescription] = useState(task?.description ?? '')
  const [priority, setPriority] = useState<TaskPriority>(task?.priority ?? 'Medium')
  const [scheduledDate, setScheduledDate] = useState(task?.scheduledDate ?? date)
  const [startTime, setStartTime] = useState(task?.startTime?.slice(0, 5) ?? '')
  const [planned, setPlanned] = useState(String(task?.plannedMinutes ?? 60))
  const [stage, setStage] = useState<StudyStage | ''>(task?.stage ?? '')
  const [scope, setScope] = useState<TopicSelection | null>(null)
  const [error, setError] = useState('')

  async function save() {
    setError('')
    const input = {
      title: title.trim(), description: description.trim() || null, priority, scheduledDate, startTime: startTime ? `${startTime}:00` : null,
      plannedMinutes: Number(planned) || 0,
      subjectId: scope ? scope.subjectId : task?.subjectId ?? null,
      syllabusItemId: scope ? scope.syllabusItemId : task?.syllabusItemId ?? null,
      stage: stage || null,
    }
    try {
      if (task) await plannerApi.updateTask(task.id, input, token)
      else await plannerApi.createTask(input, token)
      onSaved()
    } catch (reason) {
      setError(errorMessage(reason, 'Unable to save the task.'))
    }
  }

  return (
    <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); void save() }}>
      <label className={labelClass}>Title<input required maxLength={200} className={field} value={title} onChange={(event) => setTitle(event.target.value)} /></label>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <label className={labelClass}>Date<input type="date" required className={field} value={scheduledDate} onChange={(event) => setScheduledDate(event.target.value)} /></label>
        <label className={labelClass}>Start<input type="time" className={field} value={startTime} onChange={(event) => setStartTime(event.target.value)} /></label>
        <label className={labelClass}>Planned (min)<input type="number" min={0} max={1440} className={field} value={planned} onChange={(event) => setPlanned(event.target.value)} /></label>
        <label className={labelClass}>Priority<select className={field} value={priority} onChange={(event) => setPriority(event.target.value as TaskPriority)}>{priorities.map((value) => <option key={value}>{value}</option>)}</select></label>
      </div>
      <label className={labelClass}>Stage<select className={field} value={stage} onChange={(event) => setStage(event.target.value as StudyStage | '')}><option value="">From topic / none</option>{stages.map((value) => <option key={value}>{value}</option>)}</select></label>
      {task?.subjectName && !scope && <p className="text-xs text-slate-500">Currently: {[task.subjectName, task.topicTitle].filter(Boolean).join(' · ')}. Choose below to change.</p>}
      <TopicSelect value={scope} onChange={setScope} />
      <label className={labelClass}>Notes<textarea maxLength={4000} rows={3} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-950" value={description} onChange={(event) => setDescription(event.target.value)} /></label>
      {error && <p className="text-sm text-red-600">{error}</p>}
      <div className="flex justify-end gap-2"><Button type="button" variant="ghost" onClick={onCancel}>Cancel</Button><Button type="submit" disabled={!title.trim()}>Save task</Button></div>
    </form>
  )
}

function GoalsTab({ token, onError }: { token: string; onError: (value: string) => void }) {
  const [goals, setGoals] = useState<StudyGoal[] | null>(null)
  const [reload, setReload] = useState(0)
  const [form, setForm] = useState({ period: 'Weekly' as PlannerPeriod, date: todayIso(), title: '', metric: 'StudyMinutes' as GoalMetric, target: '300', stage: '' as StudyStage | '' })
  const [scope, setScope] = useState<TopicSelection | null>(null)

  useEffect(() => {
    let alive = true
    plannerApi.getGoals({ date: todayIso() }, token)
      .then((result) => alive && setGoals(result))
      .catch((reason: unknown) => alive && onError(errorMessage(reason, 'Unable to load goals.')))
    return () => { alive = false }
  }, [token, reload, onError])

  async function create() {
    try {
      await plannerApi.createGoal({ period: form.period, date: form.date, title: form.title.trim(), metric: form.metric, targetValue: Number(form.target), subjectId: scope?.subjectId ?? null, stage: form.stage || null, notes: null }, token)
      setForm({ ...form, title: '' })
      setReload((count) => count + 1)
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to create the goal.'))
    }
  }

  return (
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
      <Card>
        <CardHeader title="Current goals" subtitle="Today, this week, and this month" />
        {!goals ? <LoadingState /> : goals.length === 0 ? <EmptyState title="No current goals" description="Create a daily, weekly, or monthly goal." />
          : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{goals.map((goal) => <li key={goal.id} className="px-5 py-3">
            <div className="flex items-start justify-between gap-2"><div><p className="text-sm font-medium">{goal.title}</p><p className="text-xs text-slate-500">{goal.period} · {formatDay(goal.periodStart)}{goal.periodEnd !== goal.periodStart ? ` – ${formatDay(goal.periodEnd)}` : ''} · {metricLabels[goal.metric]}{goal.subjectName ? ` · ${goal.subjectName}` : ''}{goal.stage ? ` · ${goal.stage}` : ''}</p></div><button type="button" aria-label="Delete goal" className="text-slate-400 hover:text-red-600" onClick={() => void plannerApi.deleteGoal(goal.id, token).then(() => setReload((count) => count + 1))}><Trash2 className="h-4 w-4" /></button></div>
            <div className="mt-2 h-2 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-2 rounded-full bg-emerald-600" style={{ width: `${goal.percentComplete}%` }} /></div>
            <p className="mt-1 text-xs tabular-nums text-slate-500">{goal.currentValue} / {goal.targetValue} ({goal.percentComplete}%)</p>
          </li>)}</ul>}
      </Card>
      <Card className="space-y-3 p-5">
        <h2 className="font-semibold">New goal</h2>
        <label className={labelClass}>Title<input maxLength={200} className={field} value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} placeholder="e.g. 10 hours of Bangladesh affairs" /></label>
        <div className="grid grid-cols-2 gap-2">
          <label className={labelClass}>Period<select className={field} value={form.period} onChange={(event) => setForm({ ...form, period: event.target.value as PlannerPeriod })}><option>Daily</option><option>Weekly</option><option>Monthly</option></select></label>
          <label className={labelClass}>Any date in the period<input type="date" className={field} value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} /></label>
          <label className={labelClass}>Measure<select className={field} value={form.metric} onChange={(event) => setForm({ ...form, metric: event.target.value as GoalMetric })}>{Object.entries(metricLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
          <label className={labelClass}>Target<input type="number" min={1} className={field} value={form.target} onChange={(event) => setForm({ ...form, target: event.target.value })} /></label>
          <label className={labelClass}>Stage (optional)<select className={field} value={form.stage} onChange={(event) => setForm({ ...form, stage: event.target.value as StudyStage | '' })}><option value="">Any</option>{stages.map((value) => <option key={value}>{value}</option>)}</select></label>
        </div>
        <TopicSelect value={scope} onChange={(value) => setScope(value && { ...value, syllabusItemId: null })} label="Subject filter" />
        <Button disabled={!form.title.trim() || Number(form.target) < 1} onClick={() => void create()}><Plus className="h-4 w-4" />Create goal</Button>
      </Card>
    </div>
  )
}

function SchedulesTab({ token, onError }: { token: string; onError: (value: string) => void }) {
  const [schedules, setSchedules] = useState<StudySchedule[] | null>(null)
  const [reload, setReload] = useState(0)
  const [form, setForm] = useState({ title: '', days: ['Monday', 'Wednesday', 'Friday'], startTime: '07:00', minutes: '60', priority: 'Medium' as TaskPriority, startDate: todayIso(), endDate: '' })
  const [scope, setScope] = useState<TopicSelection | null>(null)
  const [message, setMessage] = useState('')

  useEffect(() => {
    let alive = true
    plannerApi.getSchedules(token).then((result) => alive && setSchedules(result)).catch((reason: unknown) => alive && onError(errorMessage(reason, 'Unable to load schedules.')))
    return () => { alive = false }
  }, [token, reload, onError])

  async function create() {
    try {
      await plannerApi.createSchedule({
        title: form.title.trim(), days: form.days, startTime: form.startTime ? `${form.startTime}:00` : null, plannedMinutes: Number(form.minutes), priority: form.priority,
        subjectId: scope?.subjectId ?? null, syllabusItemId: scope?.syllabusItemId ?? null, stage: null, startDate: form.startDate, endDate: form.endDate || null, isActive: true,
      }, token)
      setForm({ ...form, title: '' })
      setReload((count) => count + 1)
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to create the schedule.'))
    }
  }

  async function generate(schedule: StudySchedule) {
    const from = todayIso()
    const result = await plannerApi.generateTasks(schedule.id, from, addDays(from, 27), token).catch((reason: unknown) => { onError(errorMessage(reason, 'Unable to generate tasks.')); return null })
    if (result) setMessage(`${result.created} task${result.created === 1 ? '' : 's'} added for the next four weeks from "${schedule.title}".`)
  }

  return (
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
      <Card>
        <CardHeader title="Recurring schedules" subtitle="Generate dated tasks for the next four weeks; existing days are skipped." />
        {message && <p className="mx-5 mt-3 rounded bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300">{message}</p>}
        {!schedules ? <LoadingState /> : schedules.length === 0 ? <EmptyState title="No schedules" description="Create a weekly study block, for example English grammar every Sunday and Tuesday." />
          : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{schedules.map((schedule) => <li key={schedule.id} className="flex items-start gap-3 px-5 py-3">
            <div className="min-w-0 flex-1"><p className="text-sm font-medium">{schedule.title} {!schedule.isActive && <Badge tone="neutral">Paused</Badge>}</p><p className="text-xs text-slate-500">{schedule.days.map((day) => day.slice(0, 3)).join(', ')}{schedule.startTime ? ` at ${schedule.startTime.slice(0, 5)}` : ''} · {formatMinutes(schedule.plannedMinutes)}{schedule.subjectName ? ` · ${schedule.subjectName}` : ''}</p><p className="text-xs text-slate-500">From {formatDay(schedule.startDate)}{schedule.endDate ? ` to ${formatDay(schedule.endDate)}` : ''}</p></div>
            <Button size="sm" variant="secondary" disabled={!schedule.isActive} onClick={() => void generate(schedule)}>Generate</Button>
            <Button size="sm" variant="ghost" aria-label="Delete schedule" onClick={() => window.confirm('Delete this schedule? Generated tasks are kept.') && void plannerApi.deleteSchedule(schedule.id, token).then(() => setReload((count) => count + 1))}><Trash2 className="h-4 w-4" /></Button>
          </li>)}</ul>}
      </Card>
      <Card className="space-y-3 p-5">
        <h2 className="font-semibold">New schedule</h2>
        <label className={labelClass}>Title<input maxLength={200} className={field} value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} /></label>
        <fieldset><legend className={labelClass}>Days</legend><div className="mt-1 flex flex-wrap gap-1">{weekdays.map((day) => <label key={day} className={`cursor-pointer rounded-lg border px-2 py-1 text-xs ${form.days.includes(day) ? 'border-emerald-600 bg-emerald-50 dark:bg-emerald-500/10' : 'border-slate-200 dark:border-slate-700'}`}><input type="checkbox" className="sr-only" checked={form.days.includes(day)} onChange={() => setForm({ ...form, days: form.days.includes(day) ? form.days.filter((value) => value !== day) : [...form.days, day] })} />{day.slice(0, 3)}</label>)}</div></fieldset>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <label className={labelClass}>Start time<input type="time" className={field} value={form.startTime} onChange={(event) => setForm({ ...form, startTime: event.target.value })} /></label>
          <label className={labelClass}>Minutes<input type="number" min={5} max={1440} className={field} value={form.minutes} onChange={(event) => setForm({ ...form, minutes: event.target.value })} /></label>
          <label className={labelClass}>From<input type="date" className={field} value={form.startDate} onChange={(event) => setForm({ ...form, startDate: event.target.value })} /></label>
          <label className={labelClass}>Until (optional)<input type="date" className={field} value={form.endDate} onChange={(event) => setForm({ ...form, endDate: event.target.value })} /></label>
        </div>
        <label className={labelClass}>Priority<select className={field} value={form.priority} onChange={(event) => setForm({ ...form, priority: event.target.value as TaskPriority })}>{priorities.map((value) => <option key={value}>{value}</option>)}</select></label>
        <TopicSelect value={scope} onChange={setScope} />
        <Button disabled={!form.title.trim() || form.days.length === 0} onClick={() => void create()}><Plus className="h-4 w-4" />Create schedule</Button>
      </Card>
    </div>
  )
}

function SessionsTab({ token, reload, onChanged, onError }: { token: string; reload: number; onChanged: () => void; onError: (value: string) => void }) {
  const [sessions, setSessions] = useState<StudySession[] | null>(null)
  const [form, setForm] = useState({ date: todayIso(), time: '08:00', minutes: '45', activity: 'Reading' as StudyActivity, notes: '' })
  const [scope, setScope] = useState<TopicSelection | null>(null)

  useEffect(() => {
    let alive = true
    plannerApi.getSessions({ from: addDays(todayIso(), -30), pageSize: 100 }, token)
      .then((result) => alive && setSessions(result.items))
      .catch((reason: unknown) => alive && onError(errorMessage(reason, 'Unable to load sessions.')))
    return () => { alive = false }
  }, [token, reload, onError])

  async function log() {
    try {
      await plannerApi.logSession({
        startedAt: `${form.date}T${form.time}:00+06:00`, durationMinutes: Number(form.minutes), activity: form.activity, taskId: null,
        subjectId: scope?.subjectId ?? null, syllabusItemId: scope?.syllabusItemId ?? null, stage: null, notes: form.notes.trim() || null,
      }, token)
      setForm({ ...form, notes: '' })
      onChanged()
    } catch (reason) {
      onError(errorMessage(reason, 'Unable to log the session.'))
    }
  }

  return (
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)]">
      <Card>
        <CardHeader title="Sessions in the last 30 days" />
        {!sessions ? <LoadingState /> : sessions.length === 0 ? <EmptyState title="No sessions" description="Use the timer above or log a past session." />
          : <ul className="divide-y divide-slate-100 dark:divide-slate-800">{sessions.map((session) => <li key={session.id} className="flex items-center gap-3 px-5 py-2.5 text-sm">
            <span className="w-28 text-xs text-slate-500">{new Date(session.startedAt).toLocaleString([], { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}</span>
            <span className="min-w-0 flex-1 truncate">{activityLabels[session.activity]}{session.subjectName ? ` · ${session.subjectName}` : ''}{session.topicTitle ? ` · ${session.topicTitle}` : ''}{session.taskTitle ? ` · ${session.taskTitle}` : ''}</span>
            <span className="tabular-nums">{session.isRunning ? 'running' : formatMinutes(session.durationMinutes)}</span>
            {!session.isRunning && <button type="button" aria-label="Delete session" className="text-slate-400 hover:text-red-600" onClick={() => window.confirm('Delete this session?') && void plannerApi.deleteSession(session.id, token).then(onChanged)}><Trash2 className="h-4 w-4" /></button>}
          </li>)}</ul>}
      </Card>
      <Card className="space-y-3 p-5">
        <h2 className="font-semibold">Log a past session</h2>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <label className={labelClass}>Date<input type="date" max={todayIso()} className={field} value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} /></label>
          <label className={labelClass}>Start<input type="time" className={field} value={form.time} onChange={(event) => setForm({ ...form, time: event.target.value })} /></label>
          <label className={labelClass}>Minutes<input type="number" min={1} max={1440} className={field} value={form.minutes} onChange={(event) => setForm({ ...form, minutes: event.target.value })} /></label>
          <label className={labelClass}>Activity<select className={field} value={form.activity} onChange={(event) => setForm({ ...form, activity: event.target.value as StudyActivity })}>{Object.entries(activityLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        </div>
        <TopicSelect value={scope} onChange={setScope} />
        <label className={labelClass}>Notes<input maxLength={2000} className={field} value={form.notes} onChange={(event) => setForm({ ...form, notes: event.target.value })} /></label>
        <Button disabled={!Number(form.minutes)} onClick={() => void log()}><Plus className="h-4 w-4" />Log session</Button>
      </Card>
    </div>
  )
}
