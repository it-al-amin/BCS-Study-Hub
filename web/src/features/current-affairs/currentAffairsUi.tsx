import { Bookmark, NotebookPen, RotateCcw } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Badge } from '../../components/ui/Badge'
import type { CurrentAffairsSummary } from '../../services/currentAffairs'
import { categoryLabels, formatDate } from './currentAffairsFormat'

export function EntryCard({ entry, compact = false }: { entry: CurrentAffairsSummary; compact?: boolean }) {
  return (
    <article className="py-4 print:break-inside-avoid">
      <div className="flex flex-wrap items-center gap-2 text-xs text-slate-500">
        <time dateTime={entry.eventDate} className="font-medium text-slate-700 dark:text-slate-300">{formatDate(entry.eventDate)}</time>
        <Badge tone="info">{categoryLabels[entry.category]}</Badge>
        {entry.isBookmarked && <Bookmark className="h-3.5 w-3.5 text-emerald-600 print:hidden" aria-label="Bookmarked" />}
        {entry.inRevision && <RotateCcw className="h-3.5 w-3.5 text-amber-600 print:hidden" aria-label="In revision" />}
        {entry.hasNote && <NotebookPen className="h-3.5 w-3.5 text-sky-600 print:hidden" aria-label="Has a study note" />}
      </div>
      <h3 className="mt-1.5 font-medium"><Link to={`/current-affairs/${entry.id}`} className="hover:underline">{entry.title}</Link></h3>
      <p className={`mt-1 text-sm text-slate-600 dark:text-slate-300 ${compact ? 'line-clamp-2 print:line-clamp-none' : ''}`}>{entry.summary}</p>
      <p className="mt-1.5 text-xs text-slate-500">
        Source: <a href={entry.sourceUrl} target="_blank" rel="noopener noreferrer nofollow" className="hover:underline">{entry.sourceName}</a>
        {entry.topics.length > 0 && <> · Topics: {entry.topics.map((topic) => topic.title).join(', ')}</>}
      </p>
      {!compact && entry.tags.length > 0 && <div className="mt-2 flex flex-wrap gap-1 print:hidden">{entry.tags.map((tag) => <Link key={tag} to={`/current-affairs?tag=${encodeURIComponent(tag)}`} className="rounded bg-slate-100 px-1.5 py-0.5 text-[11px] text-slate-600 hover:underline dark:bg-slate-800 dark:text-slate-300">{tag}</Link>)}</div>}
    </article>
  )
}
