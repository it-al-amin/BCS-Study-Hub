import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from './Button'

export function Pagination({ page = 1, totalPages = 1 }: { page?: number; totalPages?: number }) {
  return (
    <div className="flex items-center justify-between px-5 py-3">
      <span className="text-xs text-slate-500">Page {page} of {totalPages}</span>
      <div className="flex gap-1"><Button variant="secondary" size="sm" aria-label="Previous page"><ChevronLeft className="h-4 w-4" /></Button><Button variant="secondary" size="sm" aria-label="Next page"><ChevronRight className="h-4 w-4" /></Button></div>
    </div>
  )
}
