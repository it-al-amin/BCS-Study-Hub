import { Newspaper, Search } from 'lucide-react'
import { PageHeader } from '../../components/ui/PageHeader'
import { Card } from '../../components/ui/Card'
import { Badge } from '../../components/ui/Badge'
import { Input, Select } from '../../components/ui/Form'
import { temporaryCurrentAffairs } from '../../mock/currentAffairs'
import { useState } from 'react'

export function CurrentAffairsPage() {
  const [searchQuery, setSearchQuery] = useState('')
  const [category, setCategory] = useState('all')
  const query = searchQuery.trim().toLocaleLowerCase()
  const filteredItems = temporaryCurrentAffairs.filter((item) =>
    (category === 'all' || item.category === category) &&
    item.title.toLocaleLowerCase().includes(query))

  return (
    <>
      <PageHeader title="Current Affairs" description="Example entries for layout review; no verified current-affairs feed is connected." />
      <Card className="p-5"><div className="mb-5 grid gap-3 sm:grid-cols-[minmax(0,1fr)_220px]"><div className="relative"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><Input aria-label="Search current affairs examples" placeholder="Search preview entries" value={searchQuery} onChange={(event) => setSearchQuery(event.target.value)} className="mt-0 pl-9" /></div><Select aria-label="Filter current affairs examples" value={category} onChange={(event) => setCategory(event.target.value)} className="mt-0"><option value="all">All categories</option><option value="Economy">Economy</option><option value="International Affairs">International Affairs</option><option value="Governance">Governance</option></Select></div><div className="divide-y divide-slate-100 dark:divide-slate-800">{filteredItems.length === 0 ? <p className="py-4 text-sm text-slate-500">No matching example entries.</p> : filteredItems.map((item) => <article key={item.title} className="flex gap-4 py-4"><div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-sky-50 text-sky-700 dark:bg-sky-500/10"><Newspaper className="h-5 w-5" /></div><div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><h2 className="font-medium">{item.title}</h2><Badge tone="info">{item.category}</Badge></div><p className="mt-1 text-xs text-slate-500">{item.date} · Example date · Source not verified</p></div></article>)}</div></Card>
    </>
  )
}
