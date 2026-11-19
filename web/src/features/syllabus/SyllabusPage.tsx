import { ChevronDown, ChevronRight, Search } from 'lucide-react'
import { useState } from 'react'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { Field, Input, Select } from '../../components/ui/Form'
import { PageHeader } from '../../components/ui/PageHeader'

const tree = [
  { title: 'Bangladesh Affairs', children: ['National History', 'Constitution', 'Government and Administration'] },
  { title: 'International Affairs', children: ['Organizations', 'Global politics', 'Foreign relations'] },
  { title: 'Bangla', children: ['Literature', 'Grammar', 'Composition'] },
  { title: 'English', children: ['Grammar', 'Vocabulary', 'Comprehension'] },
]

export function SyllabusPage() {
  const [open, setOpen] = useState<Record<string, boolean>>({ 'Bangladesh Affairs': true })
  return (
    <>
      <PageHeader title="Examination Syllabus" description="Version-aware syllabus structure prepared for official BPSC publications." action={<Button variant="secondary">Change syllabus version</Button>} />
      <div className="grid gap-6 lg:grid-cols-[260px_minmax(0,1fr)]">
        <Card className="p-4"><Field label="Syllabus version"><Select defaultValue="latest"><option value="latest">Latest draft — 2026</option></Select></Field><Field label="Stage"><Select defaultValue="preliminary" className="mt-3"><option value="preliminary">Preliminary</option><option value="written">Written</option><option value="viva">Viva</option></Select></Field></Card>
        <Card><CardHeader title="Syllabus tree" subtitle="Temporary hierarchy for UI review" action={<div className="relative hidden sm:block"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><Input aria-label="Filter syllabus" placeholder="Filter topics" className="mt-0 w-52 pl-9" /></div>} /><div className="p-3">{tree.map((node) => <div key={node.title}><button className="flex w-full items-center gap-2 rounded-lg px-3 py-2.5 text-left text-sm font-medium hover:bg-slate-50 dark:hover:bg-slate-800" onClick={() => setOpen((value) => ({ ...value, [node.title]: !value[node.title] }))}>{open[node.title] ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}{node.title}</button>{open[node.title] && <div className="ml-9 border-l border-slate-200 py-1 pl-4 dark:border-slate-700">{node.children.map((child) => <button key={child} className="block w-full rounded-md px-3 py-2 text-left text-sm text-slate-600 hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-slate-800">{child}</button>)}</div>}</div>)}</div></Card>
      </div>
    </>
  )
}
