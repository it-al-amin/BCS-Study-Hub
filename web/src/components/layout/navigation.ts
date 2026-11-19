export const primaryNavigation = [
  { label: 'Dashboard', href: '/', icon: 'LayoutDashboard' },
  { label: 'Preparation Roadmap', href: '/roadmap', icon: 'Map' },
  { label: 'Syllabus', href: '/syllabus', icon: 'Network' },
  { label: 'Subjects', href: '/subjects', icon: 'BookOpen' },
  { label: 'Preliminary', href: '/preliminary', icon: 'CircleCheck' },
  { label: 'Written', href: '/written', icon: 'PenTool' },
  { label: 'Viva', href: '/viva', icon: 'Mic2' },
] as const

export const studyNavigation = [
  { label: 'Study Planner', href: '/planner', icon: 'CalendarDays' },
  { label: 'Revision', href: '/revision', icon: 'RotateCcw' },
  { label: 'Progress', href: '/progress', icon: 'ChartNoAxesCombined' },
  { label: 'Library', href: '/library', icon: 'Library' },
  { label: 'Books', href: '/books', icon: 'BookMarked' },
  { label: 'Current Affairs', href: '/current-affairs', icon: 'Newspaper' },
] as const
