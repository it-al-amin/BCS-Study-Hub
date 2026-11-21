import { useEffect, useState } from 'react'
import {
  libraryApi,
  type LibraryFile,
  type LibraryMode,
  type LibraryReadingStatus,
  type LibraryResourceCategory,
  type LibraryResourceKind,
  type LibraryResourceStatus,
  type LibraryRightsBasis,
  type LibrarySourceType,
} from '../../services/library'

export const kindLabels: Record<LibraryResourceKind, string> = {
  Book: 'Book',
  File: 'File',
  ExternalLink: 'External link',
  Note: 'Study note',
}

export const categoryLabels: Record<LibraryResourceCategory, string> = {
  General: 'General',
  Textbook: 'Textbook',
  StudyNote: 'Study note',
  GovernmentPublication: 'Government publication',
  Report: 'Report',
  NewspaperArticle: 'Newspaper article',
  Reference: 'Reference',
  ChapterMaterial: 'Chapter material',
  Other: 'Other',
}

export const sourceTypeLabels: Record<LibrarySourceType, string> = {
  Unspecified: 'Not specified',
  OfficialGovernment: 'Official / government',
  Academic: 'Academic',
  NewsMedia: 'News media',
  ReferenceWork: 'Reference work',
  Commercial: 'Commercial publisher',
  Personal: 'Personal',
  Other: 'Other',
}

export const rightsLabels: Record<LibraryRightsBasis, { label: string; help: string }> = {
  NotApplicable: { label: 'No file / metadata only', help: 'Catalogue entry or link without an uploaded file.' },
  PersonalCopy: { label: 'Personal copy', help: 'A copy you own for private study. It can never be shared.' },
  OwnWork: { label: 'My own work', help: 'Notes or material you created yourself.' },
  PublicDomain: { label: 'Public domain', help: 'Copyright has expired or never applied.' },
  GovernmentPublication: { label: 'Government publication', help: 'Released for public distribution. Record where and on what terms.' },
  OpenLicense: { label: 'Open licence', help: 'For example Creative Commons. Record the licence.' },
  PermissionGranted: { label: 'Permission granted', help: 'The rights holder allowed distribution. Record the permission.' },
}

export const readingStatusLabels: Record<LibraryReadingStatus, string> = {
  NotStarted: 'Not started',
  Reading: 'Reading',
  OnHold: 'On hold',
  Completed: 'Completed',
}

export function statusTone(status: LibraryResourceStatus) {
  return status === 'Published' ? 'success' : status === 'Draft' ? 'warning' : 'neutral'
}

export function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`
}

export function errorMessage(reason: unknown, fallback: string) {
  return reason instanceof Error ? reason.message : fallback
}

export async function downloadLibraryFile(resourceId: string, file: LibraryFile, token: string, mode: LibraryMode = 'personal') {
  const blob = await libraryApi.downloadFileBlob(resourceId, file.id, token, mode)
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = file.originalFileName
  anchor.rel = 'noopener'
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}

interface LoadedFile { key: string; url: string | null; blob: Blob | null; error: string }

/**
 * Loads a protected file into an object URL using the in-memory bearer token.
 * State is keyed by file so switching files never shows a stale preview.
 */
export function useFileObjectUrl(resourceId: string, fileId: string | null, token: string | null, mode: LibraryMode = 'personal') {
  const key = fileId && token ? `${mode}:${resourceId}:${fileId}` : ''
  const [loaded, setLoaded] = useState<LoadedFile | null>(null)
  useEffect(() => {
    if (!fileId || !token) return
    let active = true
    let url: string | null = null
    libraryApi.getFileBlob(resourceId, fileId, token, mode)
      .then((blob) => {
        if (!active) return
        url = URL.createObjectURL(blob)
        setLoaded({ key, url, blob, error: '' })
      })
      .catch((reason: unknown) => active && setLoaded({ key, url: null, blob: null, error: errorMessage(reason, 'Unable to load the file.') }))
    return () => {
      active = false
      if (url) URL.revokeObjectURL(url)
    }
  }, [resourceId, fileId, token, mode, key])
  const current = loaded?.key === key ? loaded : null
  return { url: current?.url ?? null, blob: current?.blob ?? null, error: current?.error ?? '', loading: Boolean(key) && !current }
}
