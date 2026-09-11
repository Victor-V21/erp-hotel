import axios from 'axios'
import type { AuthResponse } from '@/types'

const csrfCookieName = 'hotel_erp_csrf'
const csrfHeaderName = 'X-CSRF-Token'
const refreshLockName = 'hotel-erp-session-refresh'

function readCookie(name: string): string | null {
  const prefix = `${encodeURIComponent(name)}=`
  const entry = document.cookie.split('; ').find((cookie) => cookie.startsWith(prefix))
  if (!entry) return null

  try {
    return decodeURIComponent(entry.slice(prefix.length))
  } catch {
    return null
  }
}

async function requestRefresh(): Promise<AuthResponse> {
  const csrfToken = readCookie(csrfCookieName)
  const response = await axios.post<AuthResponse>(
    '/api/auth/refresh',
    {},
    {
      withCredentials: true,
      headers: csrfToken ? { [csrfHeaderName]: csrfToken } : {},
    },
  )
  return response.data
}

export async function refreshBrowserSession(): Promise<AuthResponse> {
  if (navigator.locks) {
    return navigator.locks.request(refreshLockName, requestRefresh)
  }

  return requestRefresh()
}
