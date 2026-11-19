import { BookOpen, FileText, ShieldCheck, Tags, Users } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'

export function AdminPage() {
  const modules = [
    ['Syllabus', 'Versions, subjects, topics and subtopics', BookOpen],
    ['Content', 'Notes, questions and publications', FileText],
    ['Library', 'Books, resources and collections', Tags],
    ['Users', 'Accounts, roles and permissions', Users],
    ['Audit', 'Critical administration history', ShieldCheck],
  ] as const
  return (
    <>
      <PageHeader title="Administration" description="Manage syllabus and published content through permission-protected tools." />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{modules.map(([title, detail, Icon]) => <Card key={title} className="p-5"><div className="flex items-center gap-3"><div className="grid h-10 w-10 place-items-center rounded-lg bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200"><Icon className="h-5 w-5" /></div><div><h2 className="font-medium">{title}</h2><p className="mt-1 text-xs text-slate-500">{detail}</p></div></div></Card>)}</div>
    </>
  )
}
