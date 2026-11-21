import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field, Input } from '../../components/ui/Form'
import { useAuth } from '../../app/useAuth'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      await register(email, password, displayName)
      navigate('/', { replace: true })
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Registration failed. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="mx-auto max-w-md">
      <Card className="p-6 sm:p-8">
        <h1 className="text-xl font-semibold text-slate-950 dark:text-white">Create a candidate account</h1>
        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">An account lets you save topic completion to your profile.</p>
        <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
          <Field label="Display name">
            <Input autoComplete="name" minLength={2} maxLength={120} required value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
          </Field>
          <Field label="Email">
            <Input type="email" autoComplete="email" maxLength={254} required value={email} onChange={(event) => setEmail(event.target.value)} />
          </Field>
          <Field label="Password (12+ characters)">
            <Input type="password" autoComplete="new-password" minLength={12} maxLength={128} required value={password} onChange={(event) => setPassword(event.target.value)} />
          </Field>
          {error && <p role="alert" className="text-sm text-red-700 dark:text-red-300">{error}</p>}
          <Button type="submit" disabled={submitting} className="w-full">{submitting ? 'Creating account…' : 'Create account'}</Button>
        </form>
        <p className="mt-5 text-sm text-slate-600 dark:text-slate-400">
          Already registered? <Link to="/login" className="font-medium text-emerald-700 hover:underline dark:text-emerald-400">Sign in</Link>
        </p>
      </Card>
    </div>
  )
}
