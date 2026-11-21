import { FileText, Image, Link, Search } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { Badge } from '../../components/ui/Badge'
import { Input, Select } from '../../components/ui/Form'
import { Pagination } from '../../components/ui/Pagination'
import { Table } from '../../components/ui/Table'
import { temporaryResources } from '../../mock/library'
import { useState } from 'react'

export function LibraryPage() {
  const [searchQuery, setSearchQuery] = useState('')
  const [resourceType, setResourceType] = useState('all')
  const query = searchQuery.trim().toLocaleLowerCase()
  const filteredResources = temporaryResources.filter((resource) =>
    (resourceType === 'all' || resource.type === resourceType) &&
    resource.title.toLocaleLowerCase().includes(query))

  return (
    <>
      <PageHeader title="Resource Library" description="Temporary resource examples for UI review. No files are uploaded or saved." />
      <Card><div className="flex flex-col gap-3 border-b border-slate-100 p-5 sm:flex-row sm:items-center dark:border-slate-800"><div className="relative flex-1"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><Input aria-label="Search resource examples" placeholder="Search example resources" value={searchQuery} onChange={(event) => setSearchQuery(event.target.value)} className="mt-0 pl-9" /></div><Select aria-label="Filter example resource type" value={resourceType} onChange={(event) => setResourceType(event.target.value)} className="mt-0"><option value="all">All resource types</option><option value="PDF">PDF</option><option value="Image">Image</option><option value="External link">External link</option></Select></div>
      {filteredResources.length > 0 ? <Table headers={['Resource', 'Type', 'Subject', 'Status']}>{filteredResources.map((resource) => <tr key={resource.title}><td className="px-5 py-4"><div className="flex items-center gap-3">{resource.type === 'PDF' ? <FileText className="h-4 w-4 text-red-500" /> : resource.type === 'Image' ? <Image className="h-4 w-4 text-violet-500" /> : <Link className="h-4 w-4 text-sky-500" />}<span className="font-medium">{resource.title}</span></div></td><td className="px-5 py-4 text-slate-500">{resource.type}</td><td className="px-5 py-4 text-slate-500">{resource.subject}</td><td className="px-5 py-4"><Badge tone="neutral">Example</Badge></td></tr>)}</Table> : <p className="px-5 py-8 text-sm text-slate-500">No matching example resources.</p>}
      <Pagination page={1} totalPages={1} /></Card>
    </>
  )
}
