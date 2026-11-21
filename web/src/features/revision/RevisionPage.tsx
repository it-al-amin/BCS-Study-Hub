import { RotateCcw } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card, CardHeader } from '../../components/ui/Card'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { temporaryRevisions } from '../../mock/revision'

export function RevisionPage() {
  return (
    <>
      <PageHeader title="Revision Queue" description="Temporary review examples; no review progress is saved." />
      <Card><CardHeader title="Example due revisions" subtitle="Temporary preview data" /><div className="divide-y divide-slate-100 dark:divide-slate-800">{temporaryRevisions.map((revision) => <div key={revision.title} className="flex items-center gap-4 px-5 py-4"><div className="grid h-9 w-9 place-items-center rounded-lg bg-amber-50 text-amber-700 dark:bg-amber-500/10"><RotateCcw className="h-4 w-4" /></div><div className="min-w-0 flex-1"><p className="truncate text-sm font-medium">{revision.title}</p><p className="mt-1 text-xs text-slate-500">{revision.type}</p></div><Badge tone="neutral">{revision.due} · Example</Badge><Button disabled title="Revision actions are not connected yet" variant="secondary" size="sm">Review</Button></div>)}</div></Card>
    </>
  )
}
