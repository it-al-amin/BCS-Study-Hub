import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field, Input } from '../../components/ui/Form'
import { useAuth } from '../../app/useAuth'

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const returnTo = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      await login(email, password)
      navigate(returnTo, { replace: true })
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Sign-in failed. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="mx-auto max-w-md">
      <Card className="p-6 sm:p-8">
        <h1 className="text-xl font-semibold text-slate-950 dark:text-white">Sign in to Study Hub</h1>
        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">Track syllabus progress or manage verified content.</p>
        <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
          <Field label="Email">
            <Input type="email" autoComplete="email" required value={email} onChange={(event) => setEmail(event.target.value)} />
          </Field>
          <Field label="Password">
            <Input type="password" autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} />
          </Field>
          {error && <p role="alert" className="text-sm text-red-700 dark:text-red-300">{error}</p>}
          <Button type="submit" disabled={submitting} className="w-full">{submitting ? 'Signing in…' : 'Sign in'}</Button>
        </form>
        <p className="mt-5 text-sm text-slate-600 dark:text-slate-400">
          New to Study Hub? <Link to="/register" className="font-medium text-emerald-700 hover:underline dark:text-emerald-400">Create an account</Link>
        </p>
        <p className="mt-4 text-xs text-slate-500 dark:text-slate-400">This browser session keeps the access token in memory only. Reloading signs you out.</p>
      </Card>
    </div>
  )
}
