import { RotateCcw } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card, CardHeader } from '../../components/ui/Card'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'

const revisions = [
  ['Economic planning', 'Topic', 'Due now'],
  ['International organizations', 'Current affairs', 'Due now'],
  ['Essay structure', 'Written', 'Due tomorrow'],
]

export function RevisionPage() {
  return (
    <>
      <PageHeader title="Revision Queue" description="A spaced-review view based on future study activity." />
      <Card><CardHeader title="Due revisions" subtitle="Temporary preview" /><div className="divide-y divide-slate-100 dark:divide-slate-800">{revisions.map(([title, type, due]) => <div key={title} className="flex items-center gap-4 px-5 py-4"><div className="grid h-9 w-9 place-items-center rounded-lg bg-amber-50 text-amber-700 dark:bg-amber-500/10"><RotateCcw className="h-4 w-4" /></div><div className="min-w-0 flex-1"><p className="truncate text-sm font-medium">{title}</p><p className="mt-1 text-xs text-slate-500">{type}</p></div><Badge tone={due === 'Due now' ? 'danger' : 'neutral'}>{due}</Badge><Button variant="secondary" size="sm">Review</Button></div>)}</div></Card>
    </>
  )
}
