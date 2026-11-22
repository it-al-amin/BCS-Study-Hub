import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from '../components/layout/AppLayout'
import { ThemeProvider } from './ThemeProvider'
import { AuthProvider } from './AuthProvider'
import { RequirePermission } from './RequirePermission'
import { DashboardPage } from '../features/dashboard/DashboardPage'
const RoadmapPage = lazy(() => import('../features/roadmap/RoadmapPage').then((module) => ({ default: module.RoadmapPage })))
const SyllabusPage = lazy(() => import('../features/syllabus/SyllabusPage').then((module) => ({ default: module.SyllabusPage })))
const StagePage = lazy(() => import('../features/stages/StagePage').then((module) => ({ default: module.StagePage })))
const SubjectsPage = lazy(() => import('../features/subjects/SubjectsPage').then((module) => ({ default: module.SubjectsPage })))
const PlannerPage = lazy(() => import('../features/planner/PlannerPage').then((module) => ({ default: module.PlannerPage })))
const RevisionPage = lazy(() => import('../features/revision/RevisionPage').then((module) => ({ default: module.RevisionPage })))
const ProgressPage = lazy(() => import('../features/progress/ProgressPage').then((module) => ({ default: module.ProgressPage })))
import { NotFoundPage } from '../features/not-found/NotFoundPage'
import { LoginPage } from '../features/auth/LoginPage'
import { RegisterPage } from '../features/auth/RegisterPage'
const AdminSyllabusPage = lazy(() => import('../features/admin/AdminSyllabusPage').then((module) => ({ default: module.AdminSyllabusPage })))
const TopicNotesPage = lazy(() => import('../features/syllabus/TopicNotesPage').then((module) => ({ default: module.TopicNotesPage })))
const QuestionBankPage = lazy(() => import('../features/mcq/QuestionBankPage').then((module) => ({ default: module.QuestionBankPage })))
const PracticeSetsPage = lazy(() => import('../features/mcq/PracticeSetsPage').then((module) => ({ default: module.PracticeSetsPage })))
const AttemptPage = lazy(() => import('../features/mcq/AttemptPage').then((module) => ({ default: module.AttemptPage })))
const PracticeHistoryPage = lazy(() => import('../features/mcq/PracticeHistoryPage').then((module) => ({ default: module.PracticeHistoryPage })))
const McqAnalyticsPage = lazy(() => import('../features/mcq/McqAnalyticsPage').then((module) => ({ default: module.McqAnalyticsPage })))
const PersonalQuestionsPage = lazy(() => import('../features/mcq/PersonalQuestionsPage').then((module) => ({ default: module.PersonalQuestionsPage })))
const AdminMcqPage = lazy(() => import('../features/admin/AdminMcqPage').then((module) => ({ default: module.AdminMcqPage })))
const WrittenPage = lazy(() => import('../features/written/WrittenPage').then((module) => ({ default: module.WrittenPage })))
const WrittenAnswersPage = lazy(() => import('../features/written/WrittenPage').then((module) => ({ default: module.WrittenAnswersPage })))
const WrittenQuestionPage = lazy(() => import('../features/written/WrittenQuestionPage').then((module) => ({ default: module.WrittenQuestionPage })))
const WrittenAnswerPage = lazy(() => import('../features/written/WrittenAnswerPage').then((module) => ({ default: module.WrittenAnswerPage })))
const AdminWrittenPage = lazy(() => import('../features/admin/AdminWrittenPage').then((module) => ({ default: module.AdminWrittenPage })))
const WrittenNotesPage = lazy(() => import('../features/written/WrittenNotesPage').then((module) => ({ default: module.WrittenNotesPage })))
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
const CurrentAffairsPage = lazy(() => import('../features/current-affairs/CurrentAffairsPage').then((module) => ({ default: module.CurrentAffairsPage })))
const CurrentAffairsDetailPage = lazy(() => import('../features/current-affairs/CurrentAffairsDetailPage').then((module) => ({ default: module.CurrentAffairsDetailPage })))
const DigestPage = lazy(() => import('../features/current-affairs/DigestPage').then((module) => ({ default: module.DigestPage })))
const CurrentAffairsRevisionPage = lazy(() => import('../features/current-affairs/CurrentAffairsRevisionPage').then((module) => ({ default: module.CurrentAffairsRevisionPage })))
const OfficialSourcesPage = lazy(() => import('../features/current-affairs/OfficialSourcesPage').then((module) => ({ default: module.OfficialSourcesPage })))
const AdminCurrentAffairsPage = lazy(() => import('../features/admin/AdminCurrentAffairsPage').then((module) => ({ default: module.AdminCurrentAffairsPage })))
const AdminCurrentAffairsEditorPage = lazy(() => import('../features/admin/AdminCurrentAffairsEditorPage').then((module) => ({ default: module.AdminCurrentAffairsEditorPage })))
const AdminContentSourcesPage = lazy(() => import('../features/admin/AdminContentSourcesPage').then((module) => ({ default: module.AdminContentSourcesPage })))
const AdminDiscoveredContentPage = lazy(() => import('../features/admin/AdminDiscoveredContentPage').then((module) => ({ default: module.AdminDiscoveredContentPage })))
const AdminOfficialSourcesPage = lazy(() => import('../features/admin/AdminOfficialSourcesPage').then((module) => ({ default: module.AdminOfficialSourcesPage })))
const MistakesPage = lazy(() => import('../features/revision/MistakesPage').then((module) => ({ default: module.MistakesPage })))
const ReportsPage = lazy(() => import('../features/progress/ReportsPage').then((module) => ({ default: module.ReportsPage })))
const AdminPage = lazy(() => import('../features/admin/AdminConsolePages').then((module) => ({ default: module.AdminPage })))
const AdminPublishingPage = lazy(() => import('../features/admin/AdminConsolePages').then((module) => ({ default: module.AdminPublishingPage })))
const AdminTagsPage = lazy(() => import('../features/admin/AdminConsolePages').then((module) => ({ default: module.AdminTagsPage })))
const AdminCategoriesPage = lazy(() => import('../features/admin/AdminConsolePages').then((module) => ({ default: module.AdminCategoriesPage })))
const AdminAuditPage = lazy(() => import('../features/admin/AdminConsolePages').then((module) => ({ default: module.AdminAuditPage })))
const AdminSettingsPage = lazy(() => import('../features/admin/AdminConsolePages').then((module) => ({ default: module.AdminSettingsPage })))

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
                <Route path="current-affairs/digest" element={<DigestPage />} />
                <Route path="current-affairs/revision" element={<CurrentAffairsRevisionPage />} />
                <Route path="current-affairs/:entryId" element={<CurrentAffairsDetailPage />} />
                <Route path="official-sources" element={<OfficialSourcesPage />} />
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
                <Route path="reports" element={<ReportsPage />} />
                <Route path="mistakes" element={<MistakesPage />} />
                <Route path="login" element={<LoginPage />} />
                <Route path="register" element={<RegisterPage />} />
                <Route path="admin" element={<RequirePermission permission="content.manage"><AdminPage /></RequirePermission>} />
                <Route path="admin/publishing" element={<RequirePermission permission="content.manage"><AdminPublishingPage /></RequirePermission>} />
                <Route path="admin/tags" element={<RequirePermission permission="content.manage"><AdminTagsPage /></RequirePermission>} />
                <Route path="admin/categories" element={<RequirePermission permission="content.manage"><AdminCategoriesPage /></RequirePermission>} />
                <Route path="admin/audit" element={<RequirePermission permission="audit.read"><AdminAuditPage /></RequirePermission>} />
                <Route path="admin/settings" element={<RequirePermission permission="settings.manage"><AdminSettingsPage /></RequirePermission>} />
                <Route path="admin/syllabus" element={<RequirePermission permission="syllabus.manage"><AdminSyllabusPage /></RequirePermission>} />
                <Route path="admin/mcq" element={<RequirePermission permission="mcq.manage"><AdminMcqPage /></RequirePermission>} />
                <Route path="admin/written" element={<RequirePermission permission="written.manage"><AdminWrittenPage /></RequirePermission>} />
                <Route path="admin/viva" element={<RequirePermission permission="viva.manage"><AdminVivaPage /></RequirePermission>} />
                <Route path="admin/library" element={<RequirePermission permission="library.manage"><AdminLibraryPage /></RequirePermission>} />
                <Route path="admin/current-affairs" element={<RequirePermission permission="currentaffairs.manage"><AdminCurrentAffairsPage /></RequirePermission>} />
                <Route path="admin/current-affairs/new" element={<RequirePermission permission="currentaffairs.manage"><AdminCurrentAffairsEditorPage /></RequirePermission>} />
                <Route path="admin/current-affairs/:entryId/edit" element={<RequirePermission permission="currentaffairs.manage"><AdminCurrentAffairsEditorPage /></RequirePermission>} />
                <Route path="admin/content-sources" element={<RequirePermission permission="sources.manage"><AdminContentSourcesPage /></RequirePermission>} />
                <Route path="admin/discovered-content" element={<RequirePermission permission="sources.manage"><AdminDiscoveredContentPage /></RequirePermission>} />
                <Route path="admin/official-sources" element={<RequirePermission permission="currentaffairs.manage"><AdminOfficialSourcesPage /></RequirePermission>} />
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
