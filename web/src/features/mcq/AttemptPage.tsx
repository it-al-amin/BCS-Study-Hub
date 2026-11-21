import { AlertTriangle, ArrowLeft, ArrowRight, Check, Clock3, Flag, Send } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { Dialog } from '../../components/ui/Dialog'
import { PageHeader } from '../../components/ui/PageHeader'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { mcqApi, type McqAttempt, type McqAttemptReview } from '../../services/mcq'

interface LocalAnswer {
  selectedOptionIds: string[]
  isMarkedForReview: boolean
}

function formatTime(seconds: number) {
  const minutes = Math.floor(seconds / 60)
  return `${String(minutes).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`
}

export function AttemptPage() {
  const { attemptId = '' } = useParams()
  const { accessToken } = useAuth()
  const [attempt, setAttempt] = useState<McqAttempt | null>(null)
  const [review, setReview] = useState<McqAttemptReview | null>(null)
  const [answers, setAnswers] = useState<Record<string, LocalAnswer>>({})
  const [currentIndex, setCurrentIndex] = useState(0)
  const [remainingSeconds, setRemainingSeconds] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [savingQuestionId, setSavingQuestionId] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [confirmSubmit, setConfirmSubmit] = useState(false)
  const [error, setError] = useState('')
  const saveQueue = useRef<Promise<void>>(Promise.resolve())
  const pendingSaves = useRef<Promise<void>[]>([])
  const failedSaves = useRef(new Set<string>())
  const timeoutSubmitted = useRef(false)
  const currentQuestion = attempt?.questions[currentIndex] ?? null
  const currentAnswer = currentQuestion ? answers[currentQuestion.id] : undefined

  const loadAttempt = useCallback(async () => {
    if (!accessToken || !attemptId) return
    setLoading(true)
    setError('')
    try {
      const result = await mcqApi.getAttempt(attemptId, accessToken)
      setAttempt(result)
      setAnswers(Object.fromEntries(result.questions.map((question) => [
        question.id,
        { selectedOptionIds: question.selectedOptionIds, isMarkedForReview: question.isMarkedForReview },
      ])))
      if (result.status === 'Submitted') {
        setReview(await mcqApi.getReview(attemptId, accessToken))
      }
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to load this attempt.')
    } finally {
      setLoading(false)
    }
  }, [accessToken, attemptId])

  useEffect(() => { void loadAttempt() }, [loadAttempt])

  const saveAnswer = useCallback((questionId: string, value: LocalAnswer) => {
    if (!accessToken || !attemptId) return Promise.resolve()
    setSavingQuestionId(questionId)
    const operation = saveQueue.current.then(async () => {
      await mcqApi.updateAnswer(attemptId, questionId, value.selectedOptionIds, value.isMarkedForReview, accessToken)
    })
    pendingSaves.current.push(operation)
    saveQueue.current = operation.then(() => undefined, () => undefined)
    void operation.then(
      () => failedSaves.current.delete(questionId),
      () => failedSaves.current.add(questionId),
    )
    void operation
      .catch((reason: unknown) => setError(reason instanceof Error ? reason.message : 'Unable to save the answer.'))
      .finally(() => {
        pendingSaves.current = pendingSaves.current.filter((pending) => pending !== operation)
        setSavingQuestionId((current) => current === questionId ? '' : current)
      })
    return operation
  }, [accessToken, attemptId])

  const submitAttempt = useCallback(async (confirmed: boolean) => {
    if (!accessToken || !attempt || submitting) return
    if (!confirmed) {
      setConfirmSubmit(true)
      return
    }

    setConfirmSubmit(false)
    setSubmitting(true)
    setError('')
    try {
      await Promise.all(pendingSaves.current)
      if (failedSaves.current.size > 0) {
        throw new Error('Some answers did not save. Change or reselect those answers before submitting.')
      }
      const result = await mcqApi.submitAttempt(attempt.id, accessToken)
      setAttempt(result)
      setReview(await mcqApi.getReview(attempt.id, accessToken))
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to submit the attempt.')
      await loadAttempt()
    } finally {
      setSubmitting(false)
    }
  }, [accessToken, attempt, loadAttempt, submitting])

  const submitRef = useRef(submitAttempt)
  useEffect(() => { submitRef.current = submitAttempt }, [submitAttempt])
  useEffect(() => {
    if (!attempt || attempt.status !== 'InProgress' || !attempt.expiresAt) {
      setRemainingSeconds(null)
      return
    }

    const updateRemaining = () => {
      const remaining = Math.max(0, Math.ceil((new Date(attempt.expiresAt!).getTime() - Date.now()) / 1000))
      setRemainingSeconds(remaining)
      if (remaining === 0 && !timeoutSubmitted.current) {
        timeoutSubmitted.current = true
        void submitRef.current(true)
      }
    }
    updateRemaining()
    const interval = window.setInterval(updateRemaining, 1000)
    return () => window.clearInterval(interval)
  }, [attempt])

  function setAnswer(questionId: string, value: LocalAnswer) {
    setAnswers((current) => ({ ...current, [questionId]: value }))
    void saveAnswer(questionId, value)
  }

  function toggleOption(optionId: string) {
    if (!currentQuestion || !currentAnswer) return
    const next = currentQuestion.type === 'SingleAnswer'
      ? (currentAnswer.selectedOptionIds.includes(optionId) ? [] : [optionId])
      : currentAnswer.selectedOptionIds.includes(optionId)
        ? currentAnswer.selectedOptionIds.filter((id) => id !== optionId)
        : [...currentAnswer.selectedOptionIds, optionId]
    setAnswer(currentQuestion.id, { ...currentAnswer, selectedOptionIds: next })
  }

  function toggleReviewFlag() {
    if (!currentQuestion || !currentAnswer) return
    setAnswer(currentQuestion.id, { ...currentAnswer, isMarkedForReview: !currentAnswer.isMarkedForReview })
  }

  const answeredCount = Object.values(answers).filter((answer) => answer.selectedOptionIds.length > 0).length
  const flaggedCount = Object.values(answers).filter((answer) => answer.isMarkedForReview).length

  if (!accessToken) {
    return <Card><EmptyState title="Sign in to continue" description="Practice attempts are private to your account." /><div className="pb-5 text-center"><Link to="/login" className="text-sm font-medium text-emerald-700 hover:underline">Sign in</Link></div></Card>
  }
  if (loading) return <LoadingState label="Loading your attempt..." />
  if (error && !attempt) return <ErrorState description={error} />
  if (!attempt) return <Card><EmptyState title="Attempt not found" description="This attempt may not exist or may belong to another account." /></Card>
  if (attempt.status === 'Submitted') {
    if (!review) return <LoadingState label="Loading answer review..." />
    return <AttemptReviewView review={review} />
  }
  if (!currentQuestion || !currentAnswer) {
    return <Card><EmptyState title="No questions in this attempt" description="The configured set did not produce questions. Contact an administrator." /></Card>
  }

  const isLowTime = remainingSeconds !== null && remainingSeconds <= 60
  return (
    <>
      <PageHeader
        title={attempt.examSetName}
        description={`${attempt.mode === 'MockExam' ? 'Mock examination' : 'Practice'} · ${answeredCount} of ${attempt.questions.length} answered · ${flaggedCount} marked for review`}
        action={remainingSeconds !== null && <div className={`flex items-center gap-2 rounded-lg px-3 py-2 font-mono text-lg font-semibold ${isLowTime ? 'bg-red-100 text-red-700 dark:bg-red-950/40 dark:text-red-300' : 'bg-slate-100 dark:bg-slate-800'}`} aria-live="polite"><Clock3 className="h-4 w-4" />{formatTime(remainingSeconds)}</div>}
      />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_280px]">
        <Card className="overflow-hidden">
          <CardHeader title={`Question ${currentIndex + 1} of ${attempt.questions.length}`} subtitle={currentQuestion.type === 'SingleAnswer' ? 'Choose one answer' : 'Choose all correct answers'} action={currentAnswer.isMarkedForReview && <Badge>Marked for review</Badge>} />
          <div className="space-y-5 p-5 sm:p-7">
            <p className="whitespace-pre-wrap text-base font-medium leading-7">{currentQuestion.prompt}</p>
            {currentQuestion.imageUrl && <img src={currentQuestion.imageUrl} alt="Question illustration" className="max-h-96 rounded-lg border border-slate-200 object-contain dark:border-slate-700" />}
            <div className="space-y-3">
              {currentQuestion.options.map((option, index) => {
                const checked = currentAnswer.selectedOptionIds.includes(option.id)
                return (
                  <label key={option.id} className={`flex cursor-pointer items-start gap-3 rounded-xl border p-4 transition ${checked ? 'border-emerald-600 bg-emerald-50 dark:bg-emerald-500/10' : 'border-slate-200 hover:border-slate-300 dark:border-slate-700 dark:hover:border-slate-600'}`}>
                    <input type={currentQuestion.type === 'SingleAnswer' ? 'radio' : 'checkbox'} name={`question-${currentQuestion.id}`} checked={checked} onChange={() => toggleOption(option.id)} className="mt-1 h-4 w-4 accent-emerald-700" />
                    <span className="text-sm leading-6"><strong className="mr-2 text-slate-400">{String.fromCharCode(65 + index)}.</strong>{option.text}</span>
                  </label>
                )
              })}
            </div>
            <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 pt-4 dark:border-slate-800">
              <Button variant="secondary" onClick={toggleReviewFlag}><Flag className={`h-4 w-4 ${currentAnswer.isMarkedForReview ? 'fill-amber-400 text-amber-500' : ''}`} />{currentAnswer.isMarkedForReview ? 'Unmark review' : 'Mark for review'}</Button>
              <span className="text-xs text-slate-500" role="status">{savingQuestionId === currentQuestion.id ? 'Saving answer...' : 'Answers save automatically'}</span>
            </div>
            <div className="flex items-center justify-between gap-3">
              <Button variant="secondary" disabled={currentIndex === 0} onClick={() => setCurrentIndex((index) => index - 1)}><ArrowLeft className="h-4 w-4" />Previous</Button>
              {currentIndex < attempt.questions.length - 1
                ? <Button onClick={() => setCurrentIndex((index) => index + 1)}>Next<ArrowRight className="h-4 w-4" /></Button>
                : <Button onClick={() => void submitAttempt(false)} disabled={submitting}><Send className="h-4 w-4" />Submit attempt</Button>}
            </div>
          </div>
        </Card>

        <Card className="h-fit p-5">
          <h2 className="font-semibold">Question navigator</h2>
          <p className="mt-1 text-xs text-slate-500">Select a number to move between questions.</p>
          <div className="mt-4 grid grid-cols-5 gap-2">
            {attempt.questions.map((question, index) => {
              const answer = answers[question.id]
              const isAnswered = answer.selectedOptionIds.length > 0
              return (
                <button key={question.id} type="button" onClick={() => setCurrentIndex(index)} aria-label={`Go to question ${index + 1}${answer.isMarkedForReview ? ', marked for review' : ''}`} className={`relative grid h-10 w-10 place-items-center rounded-lg border text-sm font-medium ${index === currentIndex ? 'border-emerald-700 bg-emerald-700 text-white' : isAnswered ? 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-200' : 'border-slate-200 dark:border-slate-700'}`}>
                  {index + 1}{answer.isMarkedForReview && <span className="absolute -right-1 -top-1 h-2.5 w-2.5 rounded-full bg-amber-400" />}
                </button>
              )
            })}
          </div>
          <div className="mt-5 space-y-2 border-t border-slate-100 pt-4 text-xs text-slate-500 dark:border-slate-800">
            <p className="flex items-center gap-2"><span className="h-3 w-3 rounded border border-emerald-300 bg-emerald-50" />Answered</p>
            <p className="flex items-center gap-2"><span className="h-3 w-3 rounded bg-amber-400" />Marked for review</p>
            <p>{attempt.questions.length - answeredCount} unanswered</p>
          </div>
          <Button className="mt-5 w-full" onClick={() => void submitAttempt(false)} disabled={submitting}><Send className="h-4 w-4" />Submit exam</Button>
        </Card>
      </div>
      <Dialog open={confirmSubmit} title="Submit this attempt?" onClose={() => setConfirmSubmit(false)}>
        <div className="space-y-4">
          <p className="text-sm leading-6 text-slate-600 dark:text-slate-300">You have answered {answeredCount} of {attempt.questions.length} questions, with {flaggedCount} marked for review. Unanswered questions use this set's configured scoring rule.</p>
          {attempt.expiresAt && <p className="flex items-center gap-2 text-xs text-amber-700 dark:text-amber-300"><AlertTriangle className="h-4 w-4" />Submitting ends the timed attempt.</p>}
          <div className="flex justify-end gap-2"><Button variant="secondary" onClick={() => setConfirmSubmit(false)}>Continue attempt</Button><Button onClick={() => void submitAttempt(true)} disabled={submitting}><Check className="h-4 w-4" />Confirm submit</Button></div>
        </div>
      </Dialog>
    </>
  )
}

function AttemptReviewView({ review }: { review: McqAttemptReview }) {
  const { attempt, questions } = review
  const accuracy = attempt.correctCount + attempt.incorrectCount === 0
    ? 0
    : Math.round(attempt.correctCount * 100 / (attempt.correctCount + attempt.incorrectCount))
  const elapsed = attempt.submittedAt
    ? Math.max(0, Math.floor((new Date(attempt.submittedAt).getTime() - new Date(attempt.startedAt).getTime()) / 1000))
    : 0
  return (
    <>
      <PageHeader title="Attempt result" description={`${attempt.examSetName}${attempt.isTimedOut ? ' · Time expired' : ''}`} action={<Link to="/practice/history" className="text-sm font-medium text-emerald-700 hover:underline">Attempt history</Link>} />
      <div className="mb-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <SummaryCard label="Score" value={`${attempt.score ?? 0} / ${attempt.maximumScore}`} />
        <SummaryCard label="Correct" value={`${attempt.correctCount} of ${questions.length}`} />
        <SummaryCard label="Accuracy" value={`${accuracy}%`} />
        <SummaryCard label="Time used" value={formatTime(elapsed)} />
      </div>
      <div className="mb-5 flex flex-wrap gap-3 text-sm text-slate-500">
        <span>{attempt.incorrectCount} incorrect</span><span>{attempt.unansweredCount} unanswered</span>
        {attempt.isTimedOut && <Badge>Timed out</Badge>}
      </div>
      <div className="space-y-4">
        {questions.map((question) => (
          <Card key={question.questionId} className="overflow-hidden">
            <CardHeader title={`Question ${question.position} · ${question.isCorrect ? 'Correct' : question.selectedOptionIds.length ? 'Incorrect' : 'Unanswered'}`} subtitle={`${question.topicTitle}${question.parentTopicTitle ? ` · ${question.parentTopicTitle}` : ''} · ${question.marksAwarded} marks`} action={<Badge>{question.isCorrect ? 'Correct' : 'Review'}</Badge>} />
            <div className="space-y-4 p-5">
              <p className="whitespace-pre-wrap text-sm font-medium leading-6">{question.prompt}</p>
              {question.imageUrl && <img src={question.imageUrl} alt="Question illustration" className="max-h-80 rounded-lg object-contain" />}
              <ul className="space-y-2">
                {question.options.map((option) => {
                  const selected = question.selectedOptionIds.includes(option.id)
                  return <li key={option.id} className={`rounded-lg border px-3 py-2 text-sm ${option.isCorrect ? 'border-emerald-300 bg-emerald-50 dark:border-emerald-800 dark:bg-emerald-950/30' : selected ? 'border-red-300 bg-red-50 dark:border-red-900 dark:bg-red-950/30' : 'border-slate-200 dark:border-slate-700'}`}><span className="mr-2">{option.isCorrect ? '✓' : selected ? '×' : '·'}</span>{option.text}{selected && <span className="ml-2 text-xs text-slate-500">(your answer)</span>}</li>
                })}
              </ul>
              {question.explanation && <div className="rounded-lg bg-slate-50 p-4 text-sm leading-6 dark:bg-slate-800/70"><strong>Explanation</strong><p className="mt-1 whitespace-pre-wrap">{question.explanation}</p></div>}
              <div className="flex flex-wrap gap-3 text-xs text-slate-500">{question.sources.map((source) => <a key={source.id} href={source.url} target="_blank" rel="noreferrer" className="hover:text-emerald-700">{source.publisher}: {source.title}{source.citation ? ` (${source.citation})` : ''}</a>)}</div>
            </div>
          </Card>
        ))}
      </div>
    </>
  )
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return <Card className="p-4"><p className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</p><p className="mt-2 text-xl font-semibold">{value}</p></Card>
}
