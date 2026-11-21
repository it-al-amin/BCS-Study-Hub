import type { CurrentAffairsCategory, DigestPeriod, OfficialSourceCategory, VerificationStatus } from '../../services/currentAffairs'

export const categoryLabels: Record<CurrentAffairsCategory, string> = {
  Bangladesh: 'Bangladesh',
  InternationalAffairs: 'International affairs',
  Economy: 'Economy',
  Government: 'Government',
  Politics: 'Politics',
  Constitution: 'Constitution',
  Diplomacy: 'Diplomacy',
  InternationalOrganizations: 'International organizations',
  Science: 'Science',
  Technology: 'Technology',
  Environment: 'Environment',
  ClimateChange: 'Climate change',
  Awards: 'Awards',
  Sports: 'Sports',
  Appointments: 'Important appointments',
  GovernmentPolicies: 'Government policies',
  NationalProjects: 'National projects',
  Reports: 'Reports',
  ImportantEvents: 'Important events',
}

export const sourceCategoryLabels: Record<OfficialSourceCategory, string> = {
  ConstitutionalBody: 'Constitutional bodies and commissions',
  GovernmentPortal: 'Government portals',
  Ministry: 'Ministries and divisions',
  Department: 'Departments and agencies',
  CentralBank: 'Central bank',
  Statistics: 'Statistics',
  Parliament: 'Parliament',
  LegalAndLegislative: 'Constitution, laws and legislation',
  Judiciary: 'Judiciary',
  OfficialNews: 'Official news and press releases',
  InternationalOrganization: 'International organizations',
  ReportsAndPublications: 'Official reports and publications',
}

export const periodLabels: Record<DigestPeriod, string> = { Day: 'Daily', Week: 'Weekly', Month: 'Monthly', Year: 'Yearly' }

export const verificationTone: Record<VerificationStatus, 'success' | 'warning' | 'danger' | 'neutral'> = {
  Verified: 'success',
  NeedsReview: 'warning',
  Unreachable: 'danger',
  Unverified: 'neutral',
}

const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']

export function monthName(month: number) {
  return monthNames[month - 1] ?? String(month)
}

/** Formats an ISO date (yyyy-MM-dd) without timezone shifts. */
export function formatDate(value: string | null | undefined) {
  if (!value) return ''
  const [year, month, day] = value.slice(0, 10).split('-').map(Number)
  return `${day} ${monthName(month).slice(0, 3)} ${year}`
}

export function monthRange(year: number, month: number) {
  const last = new Date(Date.UTC(year, month, 0)).getUTCDate()
  const mm = String(month).padStart(2, '0')
  return { from: `${year}-${mm}-01`, to: `${year}-${mm}-${String(last).padStart(2, '0')}` }
}

export function errorMessage(reason: unknown, fallback: string) {
  return reason instanceof Error ? reason.message : fallback
}

/** A verification older than a year is flagged for re-checking (mirrors the server's publish rule). */
export function isVerificationStale(lastVerifiedOn: string | null) {
  if (!lastVerifiedOn) return true
  const verified = new Date(`${lastVerifiedOn.slice(0, 10)}T00:00:00Z`).getTime()
  return Date.now() - verified > 365 * 24 * 60 * 60 * 1000
}
