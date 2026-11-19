import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import {
  Bell, BookMarked, BookOpen, CalendarDays, ChartNoAxesCombined, ChevronRight,
  CircleCheck, LayoutDashboard, Library, Map, Menu, Mic2, Moon, Network,
  Newspaper, PenTool, RotateCcw, Search, Settings, Sun, X,
} from 'lucide-react'
import { useTheme } from '../../app/ThemeProvider'
import { primaryNavigation, studyNavigation } from './navigation'

const icons = { LayoutDashboard, Map, Network, BookOpen, CircleCheck, PenTool, Mic2, CalendarDays, RotateCcw, ChartNoAxesCombined, Library, BookMarked, Newspaper }

function NavigationGroup({ items, onNavigate }: { items: readonly { label: string; href: string; icon: keyof typeof icons }[]; onNavigate?: () => void }) {
  return (
    <nav className="space-y-1">
      {items.map((item) => {
        const Icon = icons[item.icon]
        return (
          <NavLink
            key={item.href}
            to={item.href}
            end={item.href === '/'}
            onClick={onNavigate}
            className={({ isActive }) => `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition ${isActive ? 'bg-emerald-50 text-emerald-800 dark:bg-emerald-500/10 dark:text-emerald-300' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-950 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-slate-100'}`}
          >
            <Icon className="h-4 w-4" />
            {item.label}
          </NavLink>
        )
      })}
    </nav>
  )
}

export function AppLayout() {
  const [mobileOpen, setMobileOpen] = useState(false)
  const [searchOpen, setSearchOpen] = useState(false)
  const { theme, toggleTheme } = useTheme()
  const location = useLocation()
  const segments = location.pathname.split('/').filter(Boolean)

  const sidebar = (
    <div className="flex h-full flex-col">
      <NavLink to="/" className="flex h-16 items-center gap-3 px-5">
        <div className="grid h-9 w-9 place-items-center rounded-lg bg-emerald-700 text-sm font-bold text-white">BCS</div>
        <div>
          <div className="font-semibold text-slate-950 dark:text-white">Study Hub</div>
          <div className="text-xs text-slate-500 dark:text-slate-400">Preparation workspace</div>
        </div>
      </NavLink>
      <div className="flex-1 overflow-y-auto px-3 py-4">
        <NavigationGroup items={primaryNavigation} onNavigate={() => setMobileOpen(false)} />
        <div className="mb-2 mt-6 px-3 text-[11px] font-semibold uppercase tracking-[0.16em] text-slate-400">Study tools</div>
        <NavigationGroup items={studyNavigation} onNavigate={() => setMobileOpen(false)} />
      </div>
      <div className="border-t border-slate-200 p-3 dark:border-slate-800">
        <NavLink to="/admin" className="flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium text-slate-600 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800">
          <Settings className="h-4 w-4" /> Administration
        </NavLink>
      </div>
    </div>
  )

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-64 border-r border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900 lg:block">{sidebar}</aside>
      {mobileOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <button aria-label="Close navigation" className="absolute inset-0 bg-slate-950/40" onClick={() => setMobileOpen(false)} />
          <aside className="relative h-full w-[86%] max-w-72 bg-white shadow-2xl dark:bg-slate-900">
            <button aria-label="Close menu" className="absolute right-3 top-4 z-10 rounded-md p-2 text-slate-500" onClick={() => setMobileOpen(false)}><X className="h-5 w-5" /></button>
            {sidebar}
          </aside>
        </div>
      )}
      <div className="lg:pl-64">
        <header className="sticky top-0 z-20 flex h-16 items-center gap-3 border-b border-slate-200 bg-white/95 px-4 backdrop-blur dark:border-slate-800 dark:bg-slate-900/95 lg:px-6">
          <button aria-label="Open navigation" className="rounded-md p-2 text-slate-600 hover:bg-slate-100 lg:hidden dark:text-slate-300 dark:hover:bg-slate-800" onClick={() => setMobileOpen(true)}><Menu className="h-5 w-5" /></button>
          <div className="hidden items-center gap-1 text-sm text-slate-500 sm:flex">
            <NavLink to="/" className="hover:text-slate-900 dark:hover:text-white">Workspace</NavLink>
            {segments.map((segment, index) => (
              <span key={`${segment}-${index}`} className="flex items-center gap-1">
                <ChevronRight className="h-3.5 w-3.5" />
                <span className="max-w-32 truncate capitalize">{segment.replaceAll('-', ' ')}</span>
              </span>
            ))}
          </div>
          <div className="ml-auto flex items-center gap-1">
            <button aria-label="Search" onClick={() => setSearchOpen((value) => !value)} className="hidden items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-sm text-slate-500 hover:border-slate-300 md:flex dark:border-slate-700 dark:hover:border-slate-600"><Search className="h-4 w-4" /><span>Search</span><kbd className="rounded border border-slate-200 px-1.5 py-0.5 text-xs dark:border-slate-700">/</kbd></button>
            <button aria-label="Toggle theme" onClick={toggleTheme} className="rounded-md p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800">{theme === 'light' ? <Moon className="h-5 w-5" /> : <Sun className="h-5 w-5" />}</button>
            <button aria-label="Notifications" className="relative rounded-md p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"><Bell className="h-5 w-5" /><span className="absolute right-1.5 top-1.5 h-1.5 w-1.5 rounded-full bg-amber-500" /></button>
          </div>
        </header>
        {searchOpen && (
          <div className="border-b border-slate-200 bg-white px-4 py-3 dark:border-slate-800 dark:bg-slate-900 lg:px-6">
            <div className="relative mx-auto max-w-2xl">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input autoFocus aria-label="Global search" placeholder="Search topics, resources, notes and questions..." className="h-10 w-full rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-3 text-sm outline-none focus:border-emerald-600 focus:ring-2 focus:ring-emerald-600/10 dark:border-slate-700 dark:bg-slate-800" />
            </div>
          </div>
        )}
        <main className="mx-auto max-w-[1440px] p-4 sm:p-6 lg:p-8"><Outlet /></main>
      </div>
    </div>
  )
}
