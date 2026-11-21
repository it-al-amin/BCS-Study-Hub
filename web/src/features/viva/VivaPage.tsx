import { ArrowDown, ArrowUp, Bookmark, CircleUserRound, Clock3, Play, Search, X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/useAuth'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { Card, CardHeader } from '../../components/ui/Card'
import { PageHeader } from '../../components/ui/PageHeader'
import { Pagination } from '../../components/ui/Pagination'
import { EmptyState, ErrorState, LoadingState } from '../../components/ui/States'
import { vivaApi, type VivaCategory, type VivaMockSessionListItem, type VivaProfile, type VivaQuestion, type VivaQuestionHistory, type VivaWeakTopic } from '../../services/viva'

const blankProfile: VivaProfile = {
  selfIntroduction: '',
  academicBackground: '',
  universityAndSubject: '',
  homeDistrict: '',
  professionalExperience: '',
  cadrePreferences: '',
  interestsAndHobbies: '',
  strengthsAndDevelopmentAreas: '',
  updatedAt: null,
}

const profileFields: Array<{ key: keyof Omit<VivaProfile, 'updatedAt'>; label: string; placeholder: string }> = [
  { key: 'selfIntroduction', label: 'Self-introduction', placeholder: 'Outline a concise personal introduction for practice.' },
  { key: 'academicBackground', label: 'Academic background', placeholder: 'Prepare notes about your academic path and key learning.' },
  { key: 'universityAndSubject', label: 'University and subject', placeholder: 'Record likely questions about your institution and subject.' },
  { key: 'homeDistrict', label: 'Home district', placeholder: 'Collect facts you have independently verified about your district.' },
  { key: 'professionalExperience', label: 'Professional experience', placeholder: 'Note experience, responsibilities, and lessons learned.' },
  { key: 'cadrePreferences', label: 'Cadre-specific research', placeholder: 'Prepare reasons and research for your preferred cadre(s).' },
  { key: 'interestsAndHobbies', label: 'Interests and hobbies', placeholder: 'Prepare authentic examples related to your interests.' },
  { key: 'strengthsAndDevelopmentAreas', label: 'Strengths and development areas', placeholder: 'Reflect on examples, growth areas, and improvement actions.' },
]

export function VivaPage() {
  const { accessToken } = useAuth()
  const navigate = useNavigate()
  const [categories, setCategories] = useState<VivaCategory[]>([])
  const [topics, setTopics] = useState<string[]>([])
  const [questions, setQuestions] = useState<VivaQuestion[]>([])
  const [history, setHistory] = useState<VivaQuestionHistory[]>([])
  const [sessions, setSessions] = useState<VivaMockSessionListItem[]>([])
  const [weakTopics, setWeakTopics] = useState<VivaWeakTopic[]>([])
  const [profile, setProfile] = useState<VivaProfile>(blankProfile)
  const [categoryId, setCategoryId] = useState('')
  const [topic, setTopic] = useState('')
  const [searchText, setSearchText] = useState('')
  const [search, setSearch] = useState('')
  const [bookmarkedOnly, setBookmarkedOnly] = useState(false)
  const [selectedQuestions, setSelectedQuestions] = useState<VivaQuestion[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [title, setTitle] = useState('Practice Viva')
  const [loading, setLoading] = useState(true)
  const [questionLoading, setQuestionLoading] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [savingProfile, setSavingProfile] = useState(false)
  const [startingSession, setStartingSession] = useState(false)

  useEffect(() => {
    let active = true
    vivaApi.getCategories()
      .then((result) => active && setCategories(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Viva categories.'))
      .finally(() => active && setLoading(false))
    return () => { active = false }
  }, [])

  useEffect(() => {
    let active = true
    vivaApi.getTopics(categoryId || undefined)
      .then((result) => active && setTopics(result))
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Viva topics.'))
    return () => { active = false }
  }, [categoryId])

  useEffect(() => {
    let active = true
    vivaApi.getQuestions({
      search: search || undefined,
      categoryId: categoryId || undefined,
      topic: topic || undefined,
      bookmarkedOnly: Boolean(accessToken && bookmarkedOnly),
      pageNumber: page,
      pageSize: 15,
    }, accessToken ?? undefined)
      .then((result) => {
        if (!active) return
        setQuestions(result.items)
        setTotalPages(result.totalPages)
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load Viva questions.'))
      .finally(() => active && setQuestionLoading(false))
    return () => { active = false }
  }, [accessToken, bookmarkedOnly, categoryId, page, search, topic])

  useEffect(() => {
    if (!accessToken) return
    let active = true
    Promise.all([
      vivaApi.getQuestionHistory({ pageNumber: 1, pageSize: 5 }, accessToken),
      vivaApi.getSessions({ pageNumber: 1, pageSize: 5 }, accessToken),
      vivaApi.getWeakTopics(accessToken),
      vivaApi.getProfile(accessToken),
    ])
      .then(([historyResult, sessionResult, weakResult, profileResult]) => {
        if (!active) return
        setHistory(historyResult.items)
        setSessions(sessionResult.items)
        setWeakTopics(weakResult)
        setProfile(profileResult)
      })
      .catch((reason: unknown) => active && setError(reason instanceof Error ? reason.message : 'Unable to load your Viva preparation data.'))
    return () => { active = false }
  }, [accessToken])

  function toggleQuestion(question: VivaQuestion) {
    setSelectedQuestions((current) => current.some((item) => item.id === question.id)
      ? current.filter((item) => item.id !== question.id)
      : [...current, question])
  }

  function moveSelectedQuestion(index: number, direction: -1 | 1) {
    const targetIndex = index + direction
    if (targetIndex < 0 || targetIndex >= selectedQuestions.length) return
    setSelectedQuestions((current) => {
      const next = [...current]
      const selected = next[index]
      const adjacent = next[targetIndex]
      if (!selected || !adjacent) return current
      next[index] = adjacent
      next[targetIndex] = selected
      return next
    })
  }

  async function toggleBookmark(question: VivaQuestion) {
    if (!accessToken) {
      setError('Sign in to save Viva bookmarks.')
      return
    }

    try {
      if (question.isBookmarked) await vivaApi.removeBookmark(question.id, accessToken)
      else await vivaApi.addBookmark(question.id, accessToken)
      setQuestions((current) => current.map((item) => item.id === question.id
        ? { ...item, isBookmarked: !item.isBookmarked }
        : item))
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to update this bookmark.')
    }
  }

  async function startSession() {
    if (!accessToken || selectedQuestions.length === 0) return
    setStartingSession(true)
    setError('')
    try {
      const session = await vivaApi.createSession(title.trim() || 'Practice Viva', selectedQuestions.map((question) => question.id), accessToken)
      navigate(`/viva/sessions/${session.id}`)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to start a mock Viva.')
    } finally {
      setStartingSession(false)
    }
  }

  async function saveProfile() {
    if (!accessToken) return
    setSavingProfile(true)
    setMessage('')
    setError('')
    try {
      const saved = await vivaApi.saveProfile(profile, accessToken)
      setProfile(saved)
      setMessage('Your private profile preparation was saved.')
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Unable to save profile preparation.')
    } finally {
      setSavingProfile(false)
    }
  }

  return (
    <>
      <PageHeader
        title="Viva preparation"
        description="Build a personal practice set, keep private answer notes, and review mock-session feedback. No official Viva topic list or AI evaluation is implied."
        action={<Link to="/admin/viva" className="text-sm font-medium text-emerald-700 hover:underline dark:text-emerald-400">Viva administration</Link>}
      />
      {error && <div className="mb-4"><ErrorState description={error} /></div>}
      {message && <p className="mb-4 rounded-lg bg-emerald-50 px-4 py-3 text-sm text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300" role="status">{message}</p>}

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.55fr)_minmax(18rem,0.8fr)]">
        <div className="space-y-5">
          <Card className="p-5">
            <CardHeader title="Viva question bank" subtitle="Filter by editable category, topic, or text. Content is administrator-authored and references display their verification status." />
            <form className="grid gap-3 pt-4 md:grid-cols-2" onSubmit={(event) => { event.preventDefault(); setSearch(searchText.trim()); setPage(1) }}>
              <label className="text-xs font-medium text-slate-500">Category
                <select value={categoryId} onChange={(event) => { setCategoryId(event.target.value); setTopic(''); setPage(1) }} className="mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white">
                  <option value="">All categories</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
                </select>
              </label>
              <label className="text-xs font-medium text-slate-500">Topic
                <select value={topic} onChange={(event) => { setTopic(event.target.value); setPage(1) }} className="mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white">
                  <option value="">All topics</option>{topics.map((item) => <option key={item}>{item}</option>)}
                </select>
              </label>
              <input aria-label="Search Viva questions" minLength={2} maxLength={100} value={searchText} onChange={(event) => setSearchText(event.target.value)} placeholder="Search question or reference" className="h-10 rounded-lg border border-slate-200 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-950" />
              <div className="flex flex-wrap items-center gap-2">
                <Button type="submit"><Search className="h-4 w-4" />Search</Button>
                <Button type="button" variant={bookmarkedOnly ? 'primary' : 'secondary'} disabled={!accessToken} onClick={() => { setBookmarkedOnly((current) => !current); setPage(1) }}><Bookmark className="h-4 w-4" />{bookmarkedOnly ? 'Bookmarks only' : 'Bookmarks'}</Button>
              </div>
            </form>

            {loading || questionLoading ? <LoadingState label="Loading Viva questions..." /> : questions.length === 0 ? (
              <EmptyState title="No published Viva questions yet" description="Categories are ready for editing. A content manager must author and publish questions; no BPSC questions are fabricated or seeded." />
            ) : (
              <div className="mt-4 space-y-3">
                {questions.map((question) => (
                  <div key={question.id} className="rounded-xl border border-slate-200 p-4 dark:border-slate-800">
                    <div className="flex flex-wrap items-start justify-between gap-3">
                      <div className="flex min-w-0 flex-1 items-start gap-3">
                        {accessToken && <input aria-label={`Select question ${question.prompt}`} type="checkbox" checked={selectedQuestions.some((item) => item.id === question.id)} onChange={() => toggleQuestion(question)} className="mt-1 h-4 w-4 accent-emerald-700" />}
                        <div className="min-w-0">
                          <div className="flex flex-wrap gap-2"><Badge>{question.categoryName}</Badge>{question.topic && <Badge>{question.topic}</Badge>}</div>
                          <Link to={`/viva/questions/${question.id}`} className="mt-2 block text-sm font-medium leading-6 text-slate-900 hover:text-emerald-700 dark:text-white dark:hover:text-emerald-400">{question.prompt}</Link>
                          <p className="mt-1 text-xs text-slate-500">{question.references.length} reference{question.references.length === 1 ? '' : 's'} · {question.bcsExamYear ? `BCS ${question.bcsExamYear}` : 'General practice'}</p>
                        </div>
                      </div>
                      <Button size="sm" variant="ghost" aria-label={question.isBookmarked ? 'Remove bookmark' : 'Bookmark question'} onClick={() => void toggleBookmark(question)}><Bookmark className={`h-4 w-4 ${question.isBookmarked ? 'fill-emerald-600 text-emerald-600' : ''}`} /></Button>
                    </div>
                  </div>
                ))}
                {totalPages > 1 && <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />}
              </div>
            )}
          </Card>

          {accessToken && (
            <Card className="p-5">
              <CardHeader title="Profile preparation" subtitle="Private prompts to help organize your own background. Not shared publicly." />
              <div className="grid gap-3 pt-4 md:grid-cols-2">
                {profileFields.map((field) => <label key={field.key} className="text-xs font-medium text-slate-500">{field.label}<textarea rows={3} value={profile[field.key]} onChange={(event) => setProfile((current) => ({ ...current, [field.key]: event.target.value }))} placeholder={field.placeholder} className="mt-1 w-full rounded-lg border border-slate-200 bg-white p-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white" /></label>)}
              </div>
              <div className="mt-3 flex items-center justify-between gap-2"><Button disabled={savingProfile} onClick={() => void saveProfile()}>{savingProfile ? 'Saving...' : 'Save profile preparation'}</Button>{profile.updatedAt && <span className="text-xs text-slate-500">Updated {new Date(profile.updatedAt).toLocaleString()}</span>}</div>
            </Card>
          )}
        </div>

        <aside className="space-y-5">
          {accessToken ? (
            <>
              <Card className="p-5">
                <CardHeader title="Mock Viva session" subtitle="Selected questions keep their selection order. Notes and evaluation are personal; there is no voice recording or automated scoring." />
                <label className="mt-4 block text-xs font-medium text-slate-500">Session name<input value={title} onChange={(event) => setTitle(event.target.value)} maxLength={160} className="mt-1 h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-white" /></label>
                <p className="mt-3 text-sm text-slate-600 dark:text-slate-300">{selectedQuestions.length} question{selectedQuestions.length === 1 ? '' : 's'} selected</p>
                {selectedQuestions.length > 0 && <ol className="mt-3 space-y-2">{selectedQuestions.map((question, index) => <li key={question.id} className="flex items-center gap-1 rounded-lg bg-slate-50 p-2 dark:bg-slate-800"><span className="mr-auto line-clamp-2 pr-1 text-xs"><span className="mr-1 font-semibold text-emerald-700">{index + 1}.</span>{question.prompt}</span><Button size="sm" variant="ghost" aria-label="Move question up" disabled={index === 0} onClick={() => moveSelectedQuestion(index, -1)}><ArrowUp className="h-3.5 w-3.5" /></Button><Button size="sm" variant="ghost" aria-label="Move question down" disabled={index === selectedQuestions.length - 1} onClick={() => moveSelectedQuestion(index, 1)}><ArrowDown className="h-3.5 w-3.5" /></Button><Button size="sm" variant="ghost" aria-label="Remove question from selection" onClick={() => toggleQuestion(question)}><X className="h-3.5 w-3.5" /></Button></li>)}</ol>}
                <Button className="mt-3 w-full" disabled={selectedQuestions.length === 0 || startingSession} onClick={() => void startSession()}><Play className="h-4 w-4" />{startingSession ? 'Starting...' : 'Start mock Viva'}</Button>
                {selectedQuestions.length === 0 && <p className="mt-2 text-xs text-slate-500">Select questions in the bank to create a session.</p>}
              </Card>

              <Card>
                <CardHeader title="Session history" action={<Link to="/viva/history" className="text-xs font-medium text-emerald-700 hover:underline dark:text-emerald-400">Question history</Link>} />
                {sessions.length === 0 ? <EmptyState title="No sessions yet" description="Create a mock session from selected published questions." /> : (
                  <div className="divide-y divide-slate-100 dark:divide-slate-800">
                    {sessions.map((session) => <Link key={session.id} to={`/viva/sessions/${session.id}`} className="block p-4 hover:bg-slate-50 dark:hover:bg-slate-800/60">
                      <div className="flex items-center justify-between gap-2"><span className="line-clamp-1 text-sm font-medium">{session.title}</span><Badge>{session.status === 'Completed' ? 'Complete' : 'In progress'}</Badge></div>
                      <p className="mt-1 flex items-center gap-1 text-xs text-slate-500"><Clock3 className="h-3 w-3" />{session.questionCount} questions · {new Date(session.startedAt).toLocaleDateString()}{session.overallRating ? ` · self-rating ${session.overallRating}/5` : ''}</p>
                    </Link>)}
                  </div>
                )}
              </Card>

              <Card>
                <CardHeader title="Topics to revisit" subtitle="Based on your own revision flags after completed sessions." />
                {weakTopics.length === 0 ? <EmptyState title="No revision topics flagged" description="Mark question notes for revision during a mock Viva to build this list." /> : <div className="space-y-2 p-4">{weakTopics.slice(0, 8).map((item) => <div key={`${item.categoryName}-${item.topic}`} className="flex items-center justify-between gap-3 rounded-lg bg-slate-50 px-3 py-2 text-sm dark:bg-slate-800"><span>{item.topic || item.categoryName}<span className="ml-2 text-xs text-slate-500">{item.categoryName}</span></span><Badge>{item.revisionCount}</Badge></div>)}</div>}
              </Card>
            </>
          ) : (
            <Card className="p-5"><div className="flex items-center gap-3"><CircleUserRound className="h-5 w-5 text-emerald-700" /><p className="text-sm text-slate-600 dark:text-slate-300">Sign in to bookmark questions, keep private answer notes, prepare your profile, and save mock Viva sessions.</p></div></Card>
          )}
          {accessToken && history.length > 0 && (
            <Card>
              <CardHeader title="Recently viewed" />
              <div className="divide-y divide-slate-100 dark:divide-slate-800">{history.map((item) => <Link key={item.questionId} to={`/viva/questions/${item.questionId}`} className="block p-4 hover:bg-slate-50 dark:hover:bg-slate-800/60"><p className="line-clamp-2 text-sm font-medium">{item.prompt}</p><p className="mt-1 text-xs text-slate-500">{item.categoryName}{item.topic ? ` · ${item.topic}` : ''} · viewed {item.viewCount} times</p></Link>)}</div>
            </Card>
          )}
        </aside>
      </div>
    </>
  )
}
