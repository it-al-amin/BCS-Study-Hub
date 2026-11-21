import { Link, useSearchParams } from 'react-router-dom'
import { Card } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'

export function TopicNotesPage() {
  const [searchParams] = useSearchParams()
  const topicId = searchParams.get('topicId')
  return (
    <>
      <PageHeader title="Topic notes" description="Topic-specific note storage is not part of the syllabus phase." />
      <Card className="p-6">
        <h2 className="font-semibold text-slate-900 dark:text-white">Notes are not available yet</h2>
        <p className="mt-2 text-sm leading-6 text-slate-600 dark:text-slate-300">
          This destination is provided for syllabus navigation only. No notes are saved or simulated in this phase.
          {topicId && <span className="block break-all pt-2 text-xs text-slate-500">Selected syllabus item: {topicId}</span>}
        </p>
        <Link to="/syllabus" className="mt-4 inline-flex text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">Return to syllabus</Link>
      </Card>
    </>
  )
}
