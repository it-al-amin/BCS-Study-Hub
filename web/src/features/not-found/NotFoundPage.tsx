import { NavLink } from 'react-router-dom'
import { Button } from '../../components/ui/Button'

export function NotFoundPage() {
  return <div className="grid min-h-[60vh] place-items-center text-center"><div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-emerald-700">404</p><h1 className="mt-3 text-3xl font-semibold">Page not found</h1><p className="mt-2 text-sm text-slate-500">The requested workspace page does not exist.</p><NavLink to="/" className="mt-6 inline-flex"><Button>Return to dashboard</Button></NavLink></div></div>
}
