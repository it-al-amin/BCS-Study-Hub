import { Newspaper, Search } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { Badge } from '../../components/ui/Badge'
import { Input, Select } from '../../components/ui/Form'

export function CurrentAffairsPage() {
  const items = [
    { title: 'National budget and economic priorities', category: 'Economy', date: '18 Nov 2026' },
    { title: 'New international organization agreement', category: 'International Affairs', date: '17 Nov 2026' },
    { title: 'Government appointment update', category: 'Governance', date: '16 Nov 2026' },
  ]
  return (
    <>
      <PageHeader title="Current Affairs" description="Curated daily, weekly and monthly updates from reliable sources." />
      <Card className="p-5"><div className="mb-5 grid gap-3 sm:grid-cols-[minmax(0,1fr)_220px]"><div className="relative"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><Input placeholder="Search current affairs" className="mt-0 pl-9" /></div><Select defaultValue="all" className="mt-0"><option value="all">All categories</option><option>Economy</option><option>Governance</option></Select></div><div className="divide-y divide-slate-100 dark:divide-slate-800">{items.map((item) => <article key={item.title} className="flex gap-4 py-4"><div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-sky-50 text-sky-700 dark:bg-sky-500/10"><Newspaper className="h-5 w-5" /></div><div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><h2 className="font-medium">{item.title}</h2><Badge tone="info">{item.category}</Badge></div><p className="mt-1 text-xs text-slate-500">{item.date} · Source attribution pending</p></div></article>)}</div></Card>
    </>
  )
}
