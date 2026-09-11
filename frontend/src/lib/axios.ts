import axios, { type InternalAxiosRequestConfig } from 'axios'
import { getAccessToken } from '@/lib/authSession'
import { refreshBrowserSession } from '@/lib/sessionRefresh'
import { useAuthStore } from '@/store/authStore'

const authPaths = ['/auth/login', '/auth/refresh']

const isAuthRequest = (url?: string) =>
  !!url && authPaths.some((path) => url.includes(path))

const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

let isRefreshing = false
let failedQueue: Array<{
  resolve: (value?: unknown) => void
  reject: (reason?: unknown) => void
  config: InternalAxiosRequestConfig & { _retry?: boolean }
}> = []

const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach((pending) => {
    if (error) {
      pending.reject(error)
    } else if (token) {
      pending.config.headers.Authorization = `Bearer ${token}`
      pending.resolve(api(pending.config))
    }
  })
  failedQueue = []
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !isAuthRequest(originalRequest.url)
    ) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject, config: originalRequest })
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const { accessToken, user } = await refreshBrowserSession()
        if (!accessToken || !user) throw new Error('Invalid refresh response')

        useAuthStore.getState().setAuth(user, accessToken)
        originalRequest.headers.Authorization = `Bearer ${accessToken}`
        processQueue(null, accessToken)
        return api(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError as Error)
        useAuthStore.getState().logout()
        if (window.location.pathname !== '/login') {
          window.location.href = '/login'
        }
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(error)
  },
)

export default api
