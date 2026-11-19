import { PageHeader } from '../../components/ui/PageHeader'
import { Card, CardHeader } from '../../components/ui/Card'
import { Table } from '../../components/ui/Table'

const progress = [
  ['Preliminary', 42, '12h 30m'],
  ['Written', 18, '3h 10m'],
  ['Viva', 8, '45m'],
]

export function ProgressPage() {
  return (
    <>
      <PageHeader title="Study Progress" description="Overall stage and subject performance reports." />
      <div className="grid gap-6 md:grid-cols-3">{progress.map(([stage, value, time]) => <Card key={String(stage)} className="p-5"><p className="text-sm text-slate-500">{stage}</p><p className="mt-2 text-2xl font-semibold">{value}%</p><div className="mt-4 h-2 rounded-full bg-slate-100 dark:bg-slate-800"><div className="h-full rounded-full bg-emerald-600" style={{ width: `${value}%` }} /></div><p className="mt-3 text-xs text-slate-500">{time} studied</p></Card>)}</div>
      <Card className="mt-6"><CardHeader title="Subject summary" subtitle="Temporary presentation data" /><Table headers={['Subject', 'Completed topics', 'Avg. quiz score']} >{[['Bangladesh Affairs','18 / 32','72%'],['English','12 / 28','68%'],['Mathematics','9 / 26','61%']].map((row) => <tr key={row[0]}>{row.map((cell) => <td key={cell} className="px-5 py-4">{cell}</td>)}</tr>)}</Table></Card>
    </>
  )
}
