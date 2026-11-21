import { useEffect, useState } from 'react'
import { adminApi, type PublicSiteSettings } from '../services/admin'

let cached: Promise<PublicSiteSettings> | null = null

/** Public site settings, loaded once per page load. Failures fall back to defaults so the app never blocks on them. */
export function useSiteSettings() {
  const [settings, setSettings] = useState<PublicSiteSettings | null>(null)
  useEffect(() => {
    let alive = true
    cached ??= adminApi.getPublicSettings().catch(() => {
      cached = null
      return { siteName: 'BCS Study Hub', tagline: null, announcement: null, announcementTone: 'Info', supportEmail: null, registrationOpen: true, footerNotice: null } as PublicSiteSettings
    })
    cached.then((value) => alive && setSettings(value))
    return () => { alive = false }
  }, [])
  return settings
}
