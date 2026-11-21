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
import { LibraryPage } from '../features/library/LibraryPage'
import { BooksPage } from '../features/library/BooksPage'
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

export function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route element={<AppLayout />}>
              <Route index element={<DashboardPage />} />
              <Route path="roadmap" element={<RoadmapPage />} />
              <Route path="syllabus" element={<SyllabusPage />} />
              <Route path="notes" element={<TopicNotesPage />} />
              <Route path="preliminary" element={<StagePage stage="Preliminary" />} />
              <Route path="written" element={<StagePage stage="Written" />} />
              <Route path="viva" element={<StagePage stage="Viva" />} />
              <Route path="subjects" element={<SubjectsPage />} />
              <Route path="current-affairs" element={<CurrentAffairsPage />} />
              <Route path="library" element={<LibraryPage />} />
              <Route path="books" element={<BooksPage />} />
              <Route path="planner" element={<PlannerPage />} />
              <Route path="revision" element={<RevisionPage />} />
              <Route path="progress" element={<ProgressPage />} />
              <Route path="login" element={<LoginPage />} />
              <Route path="register" element={<RegisterPage />} />
              <Route path="admin" element={<RequirePermission permission="admin.access"><AdminPage /></RequirePermission>} />
              <Route path="admin/syllabus" element={<RequirePermission permission="syllabus.manage"><AdminSyllabusPage /></RequirePermission>} />
              <Route path="home" element={<Navigate to="/" replace />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  )
}
