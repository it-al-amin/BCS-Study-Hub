// Temporary presentation data. Replace with typed API responses when backend endpoints exist.
export const temporaryDashboard = {
  overallProgress: 42,
  studyTime: '12h 30m',
  streak: 9,
  weeklyGoal: 68,
  subjects: [
    { name: 'Bangla', progress: 58, color: 'bg-emerald-500' },
    { name: 'English', progress: 46, color: 'bg-sky-500' },
    { name: 'Bangladesh Affairs', progress: 64, color: 'bg-amber-500' },
    { name: 'General Science', progress: 31, color: 'bg-violet-500' },
  ],
  tasks: [
    { title: 'Revise Bangladesh Constitution', meta: 'Today, 20:00', tone: 'warning' as const },
    { title: 'Solve 30 mathematics MCQs', meta: 'Today, 21:00', tone: 'success' as const },
    { title: 'Review weekly current affairs', meta: 'Tomorrow', tone: 'info' as const },
  ],
  revisions: [
    { title: 'Economic planning', type: 'Topic', count: 12 },
    { title: 'International organizations', type: 'Current affairs', count: 8 },
    { title: 'Essay structure', type: 'Written', count: 5 },
  ],
}
