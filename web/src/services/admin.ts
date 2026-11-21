import { ApiClient, type PagedResult } from './api'

export interface PublicSiteSettings {
  siteName: string
  tagline: string | null
  announcement: string | null
  announcementTone: 'Info' | 'Warning' | 'Critical'
  supportEmail: string | null
  registrationOpen: boolean
  footerNotice: string | null
}

export interface SiteSettings {
  siteName: string
  tagline: string | null
  announcementEnabled: boolean
  announcementText: string | null
  announcementTone: 'Info' | 'Warning' | 'Critical'
  announcementStartsAt: string | null
  announcementEndsAt: string | null
  supportEmail: string | null
  registrationOpen: boolean
  footerNotice: string | null
  updatedAt?: string | null
}

export interface AuditEntry {
  id: number
  occurredAt: string
  action: string
  entityType: string | null
  entityId: string | null
  actorId: string | null
  actorName: string | null
  actorEmail: string | null
  correlationId: string | null
}

export interface ModuleOverview {
  module: string
  label: string
  requiredPermission: string
  managePath: string
  draft: number
  published: number
  archived: number
  attention: number
  attentionLabel: string | null
}

export interface AdminOverview {
  modules: ModuleOverview[]
  users: number
  newUsersLast30Days: number
  recentActivity: AuditEntry[]
}

export type TagModule = 'Mcq' | 'Written' | 'Library' | 'CurrentAffairs' | 'OfficialSources'
export interface TagUsage { tag: string; modules: Partial<Record<TagModule, number>>; total: number }

export type BulkModule = 'McqQuestions' | 'McqExamSets' | 'WrittenQuestions' | 'VivaQuestions' | 'LibraryResources' | 'CurrentAffairs' | 'OfficialSources'
export interface BulkResult { module: string; action: string; succeeded: number; failed: number; items: { id: string; succeeded: boolean; error: string | null }[] }

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

class AdminApi extends ApiClient {
  getPublicSettings() { return this.get<PublicSiteSettings>('/site-settings') }
  getSettings(token: string) { return this.get<SiteSettings>('/admin/settings', token) }
  saveSettings(input: SiteSettings, token: string) { return this.put<SiteSettings>('/admin/settings', input, token) }

  getOverview(token: string) { return this.get<AdminOverview>('/admin/overview', token) }

  getAudit(query: { entityType?: string; entityId?: string; actorId?: string; action?: string; from?: string; to?: string; pageNumber?: number; pageSize?: number }, token: string) {
    return this.get<PagedResult<AuditEntry>>(`/admin/audit?${queryString(query)}`, token)
  }
  getAuditEntityTypes(token: string) { return this.get<string[]>('/admin/audit/entity-types', token) }

  getTags(query: { module?: TagModule | ''; search?: string }, token: string) { return this.get<TagUsage[]>(`/admin/tags?${queryString(query)}`, token) }
  renameTag(input: { module: TagModule | null; from: string; to: string }, token: string) {
    return this.post<{ tag: string; updated: number; merged: number; removed: number }>('/admin/tags/rename', input, token)
  }
  deleteTag(input: { module: TagModule | null; tag: string }, token: string) {
    return this.post<{ tag: string; updated: number; merged: number; removed: number }>('/admin/tags/delete', input, token)
  }

  bulkStatus(input: { module: BulkModule; action: 'Publish' | 'Archive'; ids: string[] }, token: string) {
    return this.post<BulkResult>('/admin/bulk/status', input, token)
  }
}

export const adminApi = new AdminApi()
