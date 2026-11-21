import { ExternalLink, Search, ShieldAlert, ShieldCheck } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { currentAffairsApi, officialSourceCategories, type OfficialSource, type OfficialSourceCategory } from '../../services/currentAffairs'
import { errorMessage, formatDate, isVerificationStale, sourceCategoryLabels } from './currentAffairsFormat'

export function OfficialSourcesPage() {
  const [sources, setSources] = useState<OfficialSource[] | null>(null)
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState<OfficialSourceCategory | ''>('')
  const [scope, setScope] = useState<'all' | 'national' | 'international'>('all')

  useEffect(() => {
    let active = true
    currentAffairsApi.getOfficialSources({})
      .then((result) => active && setSources(result))
      .catch((reason: unknown) => active && setError(errorMessage(reason, 'Unable to load the directory.')))
    return () => { active = false }
  }, [])

  // The directory is small, so filtering happens client-side for instant results.
  const filtered = useMemo(() => {
    const term = search.trim().toLocaleLowerCase()
    return (sources ?? []).filter((source) =>
      (!category || source.category === category) &&
      (scope === 'all' || (scope === 'international') === source.isInternational) &&
      (!term || [source.name, source.localizedName, source.description, source.parentOrganization, ...source.tags]
        .some((value) => value?.toLocaleLowerCase().includes(term))))
  }, [sources, search, category, scope])

  const groups = officialSourceCategories
    .map((value) => ({ category: value, items: filtered.filter((source) => source.category === value) }))
    .filter((group) => group.items.length > 0)

  return (
    <>
      <PageHeader
        title="Official Resource Directory"
        description="Curated official websites for BCS preparation. Every listed URL was opened and checked by an editor; the verification date is shown. Content belongs to each publisher; link to it rather than copying it."
      />
      <Card className="mb-5 flex flex-col gap-2 p-4 sm:flex-row">
        <div className="relative flex-1"><Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" /><input aria-label="Search the directory" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search organizations (English or বাংলা)" className="h-10 w-full rounded-lg border border-slate-200 bg-white pl-9 pr-3 text-sm dark:border-slate-700 dark:bg-slate-800" /></div>
        <select aria-label="Category" value={category} onChange={(event) => setCategory(event.target.value as OfficialSourceCategory | '')} className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="">All categories</option>{officialSourceCategories.map((value) => <option key={value} value={value}>{sourceCategoryLabels[value]}</option>)}</select>
        <select aria-label="Scope" value={scope} onChange={(event) => setScope(event.target.value as typeof scope)} className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-800"><option value="all">Bangladesh and international</option><option value="national">Bangladesh</option><option value="international">International</option></select>
      </Card>
      {error && <ErrorState description={error} />}
      {!sources && !error ? <LoadingState label="Loading directory..." />
        : groups.length === 0 ? <Card><EmptyState title="No matching sources" description="Try another search or category." /></Card>
          : <div className="space-y-6">{groups.map((group) => <section key={group.category}>
            <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-slate-500">{sourceCategoryLabels[group.category]}</h2>
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{group.items.map((source) => {
              const stale = isVerificationStale(source.lastVerifiedOn)
              return <Card key={source.id} className="flex flex-col p-4">
                <div className="flex items-start justify-between gap-2"><div className="min-w-0"><h3 className="font-medium">{source.name}</h3>{source.localizedName && <p className="text-sm text-slate-600 dark:text-slate-300">{source.localizedName}</p>}</div>{source.isInternational && <Badge tone="neutral">International</Badge>}</div>
                {source.parentOrganization && <p className="mt-1 text-xs text-slate-500">{source.parentOrganization}</p>}
                {source.description && <p className="mt-2 text-sm text-slate-600 dark:text-slate-300">{source.description}</p>}
                <div className="mt-3 flex flex-wrap items-center gap-2 text-xs">
                  {stale ? <span className="inline-flex items-center gap-1 text-amber-700 dark:text-amber-400"><ShieldAlert className="h-3.5 w-3.5" />Verification due</span> : <span className="inline-flex items-center gap-1 text-emerald-700 dark:text-emerald-400"><ShieldCheck className="h-3.5 w-3.5" />Verified {formatDate(source.lastVerifiedOn)}</span>}
                  {!source.isHttps && <Badge tone="warning">No HTTPS</Badge>}
                </div>
                {source.usageNotes && <p className="mt-2 text-xs text-slate-500">{source.usageNotes}</p>}
                <a href={source.url} target="_blank" rel="noopener noreferrer nofollow" className="mt-auto pt-3"><Button size="sm" variant="secondary" className="w-full"><ExternalLink className="h-4 w-4" /><span className="truncate">{source.url.replace(/^https?:\/\//, '').replace(/\/$/, '')}</span></Button></a>
              </Card>
            })}</div>
          </section>)}</div>}
    </>
  )
}
