import axios from 'axios'

interface ApiErrorBody {
  detail?: unknown
  message?: unknown
  title?: unknown
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError<ApiErrorBody | string>(error)) {
    return error instanceof Error && error.message ? error.message : fallback
  }

  const body = error.response?.data
  if (typeof body === 'string' && body.trim()) return body
  if (body && typeof body === 'object') {
    for (const value of [body.detail, body.message, body.title]) {
      if (typeof value === 'string' && value.trim()) return value
    }
  }

  return fallback
}
