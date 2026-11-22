import { ApiClient, type PagedResult } from './api'

export const sourceTypes = ['Website', 'RssFeed', 'AtomFeed', 'Sitemap', 'OfficialApi', 'DocumentRepository', 'UserProvided'] as const
export const extractionMethods = ['ManualOnly', 'Feed', 'Sitemap', 'HtmlExtraction', 'DocumentDiscovery', 'Api'] as const
export const retentionPolicies = ['LinkAndMetadataOnly', 'ExcerptAndSummary', 'FullTextPermitted'] as const
export type SourceType = (typeof sourceTypes)[number]
export type ExtractionMethod = (typeof extractionMethods)[number]
export type RetentionPolicy = (typeof retentionPolicies)[number]
export type SourceHealth = 'NeverScanned' | 'Healthy' | 'NeedsAttention' | 'Failing'
export type ScanStatus = 'Queued' | 'Running' | 'Succeeded' | 'PartiallySucceeded' | 'Failed' | 'Cancelled'

export interface ContentSourceInput {
  name: string
  localizedName: string | null
  sourceType: SourceType
  extractionMethod: ExtractionMethod
  baseUrl: string
  feedUrl: string | null
  sitemapUrl: string | null
  category: string
  language: 'bn' | 'en' | 'mixed'
  priority: number
  reliability: number
  scanIntervalMinutes: number
  retentionPolicy: RetentionPolicy
  includePatterns: string | null
  excludePatterns: string | null
  requiresVerification: boolean
  rightsNote: string | null
  notes: string | null
}

export interface ContentSource extends ContentSourceInput {
  id: string
  isEnabled: boolean
  status: 'Active' | 'Archived'
  health: SourceHealth
  lastScanAt: string | null
  lastSuccessAt: string | null
  lastError: string | null
  consecutiveFailures: number
  createdAt: string
  updatedAt: string
}

export interface ContentSourceScan {
  id: string
  sourceId: string
  sourceName: string
  trigger: 'Scheduled' | 'Manual' | 'Retry'
  status: ScanStatus
  requestedAt: string
  startedAt: string | null
  completedAt: string | null
  discoveredCount: number
  updatedCount: number
  unchangedCount: number
  failedCount: number
  errorMessage: string | null
}

export interface ContentSourceSummary {
  total: number
  enabled: number
  disabled: number
  archived: number
  needsAttention: number
  queuedScans: number
  byCategory: { category: string; count: number }[]
}

function queryString(params: object) {
  const search = new URLSearchParams()
  Object.entries(params as Record<string, string | number | boolean | undefined | null>).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') search.set(key, String(value))
  })
  return search.toString()
}

const encode = encodeURIComponent
const base = '/admin/content-sources'

class ContentSourcesApi extends ApiClient {
  list(query: { search?: string; category?: string; status?: string; enabled?: boolean }, token: string) {
    return this.get<PagedResult<ContentSource>>(`${base}?${queryString({ ...query, pageSize: 100 })}`, token)
  }
  summary(token: string) { return this.get<ContentSourceSummary>(`${base}/summary`, token) }
  create(input: ContentSourceInput, token: string) { return this.post<ContentSource>(base, input, token) }
  update(id: string, input: ContentSourceInput, token: string) { return this.put<ContentSource>(`${base}/${encode(id)}`, input, token) }
  action(id: string, action: 'enable' | 'disable' | 'archive' | 'restore', token: string) {
    return this.post<ContentSource>(`${base}/${encode(id)}/${action}`, {}, token)
  }
  remove(id: string, token: string) { return this.delete(`${base}/${encode(id)}`, token) }
  requestScan(id: string, token: string) { return this.post<ContentSourceScan>(`${base}/${encode(id)}/scans`, {}, token) }
  retryScan(scanId: string, token: string) { return this.post<ContentSourceScan>(`${base}/scans/${encode(scanId)}/retry`, {}, token) }
  scans(query: { sourceId?: string; status?: string }, token: string) {
    return this.get<PagedResult<ContentSourceScan>>(`${base}/scans?${queryString({ ...query, pageSize: 50 })}`, token)
  }
}

export interface DiscoveredContent {
  id: string
  sourceId: string
  sourceName: string
  canonicalUrl: string
  title: string
  language: string | null
  kind: string
  publishedAt: string | null
  excerpt: string | null
  hasFullText: boolean
  version: number
  status: string
  visibility: string
  duplicateOfId: string | null
  discoveredAt: string
  lastSeenAt: string
  updatedAt: string
}

export interface DiscoveredContentDetail {
  content: DiscoveredContent
  textContent: string | null
  versions: { version: number; title: string; excerpt: string | null; contentHash: string; capturedAt: string; scanId: string | null }[]
}

class DiscoveredContentApi extends ApiClient {
  list(query: { sourceId?: string; status?: string; search?: string }, token: string) {
    return this.get<PagedResult<DiscoveredContent>>(`/admin/discovered-content?${queryString({ ...query, pageSize: 50 })}`, token)
  }
  get_(id: string, token: string) { return this.get<DiscoveredContentDetail>(`/admin/discovered-content/${encode(id)}`, token) }
}

export const discoveredContentApi = new DiscoveredContentApi()
export const contentSourcesApi = new ContentSourcesApi()
