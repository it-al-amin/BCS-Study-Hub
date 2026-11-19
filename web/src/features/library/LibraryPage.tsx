import { FileText, Image, Link, Search } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { Badge } from '../../components/ui/Badge'
import { Input } from '../../components/ui/Form'
import { Pagination } from '../../components/ui/Pagination'
import { Table } from '../../components/ui/Table'

const resources = [
  { title: 'BPSC Written Examination Syllabus', type: 'PDF', subject: 'All subjects', status: 'Published' },
  { title: 'Constitution reference diagram', type: 'Image', subject: 'Bangladesh Affairs', status: 'Draft' },
  { title: 'Bangladesh Bank Monthly Report', type: 'External link', subject: 'Economy', status: 'Published' },
]

export function LibraryPage() {
  return (
    <>
      <PageHeader title="Resource Library" description="A searchable workspace for PDFs, documents, images and official links." />
      <Card><div className="flex flex-col gap-3 border-b border-slate-100 p-5 sm:flex-row sm:items-center dark:border-slate-800"><div className="relative flex-1"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><Input placeholder="Search resources" className="mt-0 pl-9" /></div><select aria-label="Filter resource type" className="h-10 rounded-lg border border-slate-200 px-3 text-sm dark:border-slate-700 dark:bg-slate-800"><option>All resource types</option></select></div>
      <Table headers={['Resource', 'Type', 'Subject', 'Status']}>{resources.map((resource) => <tr key={resource.title}><td className="px-5 py-4"><div className="flex items-center gap-3">{resource.type === 'PDF' ? <FileText className="h-4 w-4 text-red-500" /> : resource.type === 'Image' ? <Image className="h-4 w-4 text-violet-500" /> : <Link className="h-4 w-4 text-sky-500" />}<span className="font-medium">{resource.title}</span></div></td><td className="px-5 py-4 text-slate-500">{resource.type}</td><td className="px-5 py-4 text-slate-500">{resource.subject}</td><td className="px-5 py-4"><Badge tone={resource.status === 'Published' ? 'success' : 'warning'}>{resource.status}</Badge></td></tr>)}</Table>
      <Pagination page={1} totalPages={1} /></Card>
    </>
  )
}
