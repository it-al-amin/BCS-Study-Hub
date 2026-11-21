import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from '../components/layout/AppLayout'
import { ThemeProvider } from './ThemeProvider'
import { AuthProvider } from './AuthProvider'
import { RequirePermission } from './RequirePermission'
import { DashboardPage } from '../features/dashboard/DashboardPage'
import { RoadmapPage } from '../features/roadmap/RoadmapPage'
import { SyllabusPage } from '../features/syllabus/SyllabusPage'
import { StagePage } from '../features/stages/StagePage'
import { SubjectsPage } from '../features/subjects/SubjectsPage'
import { PlannerPage } from '../features/planner/PlannerPage'
import { RevisionPage } from '../features/revision/RevisionPage'
import { ProgressPage } from '../features/progress/ProgressPage'
import { CurrentAffairsPage } from '../features/current-affairs/CurrentAffairsPage'
import { AdminPage } from '../features/admin/AdminPage'
import { NotFoundPage } from '../features/not-found/NotFoundPage'
import { LoginPage } from '../features/auth/LoginPage'
import { RegisterPage } from '../features/auth/RegisterPage'
import { AdminSyllabusPage } from '../features/admin/AdminSyllabusPage'
import { TopicNotesPage } from '../features/syllabus/TopicNotesPage'
import { QuestionBankPage } from '../features/mcq/QuestionBankPage'
import { PracticeSetsPage } from '../features/mcq/PracticeSetsPage'
import { AttemptPage } from '../features/mcq/AttemptPage'
import { PracticeHistoryPage } from '../features/mcq/PracticeHistoryPage'
import { McqAnalyticsPage } from '../features/mcq/McqAnalyticsPage'
import { PersonalQuestionsPage } from '../features/mcq/PersonalQuestionsPage'
import { AdminMcqPage } from '../features/admin/AdminMcqPage'
import { WrittenPage, WrittenAnswersPage } from '../features/written/WrittenPage'
import { WrittenQuestionPage } from '../features/written/WrittenQuestionPage'
import { WrittenAnswerPage } from '../features/written/WrittenAnswerPage'
import { AdminWrittenPage } from '../features/admin/AdminWrittenPage'
import { WrittenNotesPage } from '../features/written/WrittenNotesPage'
import { LoadingState } from '../components/ui/States'

const VivaPage = lazy(() => import('../features/viva/VivaPage').then((module) => ({ default: module.VivaPage })))
const VivaQuestionPage = lazy(() => import('../features/viva/VivaQuestionPage').then((module) => ({ default: module.VivaQuestionPage })))
const VivaSessionPage = lazy(() => import('../features/viva/VivaSessionPage').then((module) => ({ default: module.VivaSessionPage })))
const VivaHistoryPage = lazy(() => import('../features/viva/VivaHistoryPage').then((module) => ({ default: module.VivaHistoryPage })))
const AdminVivaPage = lazy(() => import('../features/admin/AdminVivaPage').then((module) => ({ default: module.AdminVivaPage })))
const LibraryPage = lazy(() => import('../features/library/LibraryPage').then((module) => ({ default: module.LibraryPage })))
const BooksPage = lazy(() => import('../features/library/BooksPage').then((module) => ({ default: module.BooksPage })))
const ResourceCatalogPage = lazy(() => import('../features/library/ResourceCatalogPage').then((module) => ({ default: module.ResourceCatalogPage })))
const ResourceDetailPage = lazy(() => import('../features/library/ResourceDetailPage').then((module) => ({ default: module.ResourceDetailPage })))
const ResourceEditorPage = lazy(() => import('../features/library/ResourceEditorPage').then((module) => ({ default: module.ResourceEditorPage })))
const CollectionsPage = lazy(() => import('../features/library/CollectionsPage').then((module) => ({ default: module.CollectionsPage })))
const ReadingPage = lazy(() => import('../features/library/ReadingPage').then((module) => ({ default: module.ReadingPage })))
const AdminLibraryPage = lazy(() => import('../features/admin/AdminLibraryPage').then((module) => ({ default: module.AdminLibraryPage })))

export function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <BrowserRouter>
          <Suspense fallback={<LoadingState label="Loading..." />}>
            <Routes>
              <Route element={<AppLayout />}>
                <Route index element={<DashboardPage />} />
                <Route path="roadmap" element={<RoadmapPage />} />
                <Route path="syllabus" element={<SyllabusPage />} />
                <Route path="notes" element={<TopicNotesPage />} />
                <Route path="questions" element={<QuestionBankPage />} />
                <Route path="questions/bookmarks" element={<PersonalQuestionsPage kind="bookmarks" />} />
                <Route path="questions/mistakes" element={<PersonalQuestionsPage kind="mistakes" />} />
                <Route path="practice" element={<PracticeSetsPage />} />
                <Route path="practice/history" element={<PracticeHistoryPage />} />
                <Route path="practice/analytics" element={<McqAnalyticsPage />} />
                <Route path="practice/attempts/:attemptId" element={<AttemptPage />} />
                <Route path="preliminary" element={<StagePage stage="Preliminary" />} />
                <Route path="written" element={<WrittenPage />} />
                <Route path="written/notes" element={<WrittenNotesPage />} />
                <Route path="written/questions/:questionId" element={<WrittenQuestionPage />} />
                <Route path="written/answers" element={<WrittenAnswersPage />} />
                <Route path="written/bookmarks" element={<WrittenAnswersPage bookmarks />} />
                <Route path="written/answers/:answerId" element={<WrittenAnswerPage />} />
                <Route path="viva" element={<VivaPage />} />
                <Route path="viva/history" element={<VivaHistoryPage />} />
                <Route path="viva/questions/:questionId" element={<VivaQuestionPage />} />
                <Route path="viva/sessions/:sessionId" element={<VivaSessionPage />} />
                <Route path="subjects" element={<SubjectsPage />} />
                <Route path="current-affairs" element={<CurrentAffairsPage />} />
                <Route path="library" element={<LibraryPage />} />
                <Route path="library/resources" element={<ResourceCatalogPage />} />
                <Route path="library/resources/:resourceId" element={<ResourceDetailPage />} />
                <Route path="library/resources/:resourceId/edit" element={<ResourceEditorPage mode="personal" />} />
                <Route path="library/new" element={<ResourceEditorPage mode="personal" />} />
                <Route path="library/books" element={<BooksPage />} />
                <Route path="library/books/:resourceId" element={<ResourceDetailPage />} />
                <Route path="library/collections" element={<CollectionsPage />} />
                <Route path="library/reading" element={<ReadingPage />} />
                <Route path="books" element={<BooksPage />} />
                <Route path="planner" element={<PlannerPage />} />
                <Route path="revision" element={<RevisionPage />} />
                <Route path="progress" element={<ProgressPage />} />
                <Route path="login" element={<LoginPage />} />
                <Route path="register" element={<RegisterPage />} />
                <Route path="admin" element={<RequirePermission permission="admin.access"><AdminPage /></RequirePermission>} />
                <Route path="admin/syllabus" element={<RequirePermission permission="syllabus.manage"><AdminSyllabusPage /></RequirePermission>} />
                <Route path="admin/mcq" element={<RequirePermission permission="mcq.manage"><AdminMcqPage /></RequirePermission>} />
                <Route path="admin/written" element={<RequirePermission permission="written.manage"><AdminWrittenPage /></RequirePermission>} />
                <Route path="admin/viva" element={<RequirePermission permission="viva.manage"><AdminVivaPage /></RequirePermission>} />
                <Route path="admin/library" element={<RequirePermission permission="library.manage"><AdminLibraryPage /></RequirePermission>} />
                <Route path="admin/library/new" element={<RequirePermission permission="library.manage"><ResourceEditorPage mode="admin" /></RequirePermission>} />
                <Route path="admin/library/:resourceId/edit" element={<RequirePermission permission="library.manage"><ResourceEditorPage mode="admin" /></RequirePermission>} />
                <Route path="home" element={<Navigate to="/" replace />} />
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </Routes>
          </Suspense>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  )
}
