import { BookOpen } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { EmptyState } from '../../components/ui/States'

export function BooksPage() {
  return (
    <>
      <PageHeader title="Book Library" description="Manage books, cover images and chapter-level resources." />
      <Card><div className="grid gap-4 p-5 md:grid-cols-3"><div className="rounded-lg border border-dashed border-slate-300 p-5 text-center dark:border-slate-700"><BookOpen className="mx-auto h-6 w-6 text-slate-400" /><p className="mt-2 text-sm font-medium">Book catalog</p></div><div className="md:col-span-2"><EmptyState title="No books added" description="Books and chapters will appear here after the library API is connected." /></div></div></Card>
    </>
  )
}
