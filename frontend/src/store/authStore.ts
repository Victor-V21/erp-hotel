import { create } from 'zustand'
import type { User } from '@/types'

function isJwtExpired(token: string | null): boolean {
  if (!token) return true
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return true
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')))
    if (!payload.exp) return false
    return payload.exp * 1000 < Date.now()
  } catch {
    return true
  }
}

function getStoredUser(): User | null {
  try {
    const raw = localStorage.getItem('user')
    if (!raw) return null
    return JSON.parse(raw)
  } catch {
    localStorage.removeItem('user')
    return null
  }
}

function getStoredAccessToken(): string | null {
  const token = localStorage.getItem('accessToken')
  if (!token) return null
  if (isJwtExpired(token)) {
    // Keep in storage briefly for refresh attempt or clear if invalid
    return token
  }
  return token
}

interface AuthState {
  user: User | null
  accessToken: string | null
  isAuthenticated: boolean
  setAuth: (user: User, accessToken: string, refreshToken: string) => void
  logout: () => void
  hasPermission: (permission: string | string[]) => boolean
  hasRole: (role: string | string[]) => boolean
}

const initialToken = getStoredAccessToken()
const initialUser = getStoredUser()

function normalizeRole(role: string): string {
  const lower = role.trim().toLowerCase()
  if (lower === 'admin' || lower === 'administrador') return 'admin'
  if (lower === 'recepcion' || lower === 'recepcionista' || lower === 'reception' || lower === 'recep') return 'recepcionista'
  if (lower === 'contador' || lower === 'contabilidad' || lower === 'contable' || lower === 'accountant') return 'contador'
  return lower
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: initialUser,
  accessToken: initialToken,
  isAuthenticated: !!initialToken && !!initialUser,

  setAuth: (user, accessToken, refreshToken) => {
    try {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('accessToken', accessToken)
      localStorage.setItem('refreshToken', refreshToken)
    } catch (e) {
      console.error('Error saving auth tokens to localStorage', e)
    }
    set({ user, accessToken, isAuthenticated: true })
  },

  logout: () => {
    localStorage.removeItem('user')
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    set({ user: null, accessToken: null, isAuthenticated: false })
  },

  hasPermission: (permission: string | string[]) => {
    const { user } = get()
    if (!user) return false
    const userRoles = (user.roles || []).map(normalizeRole)
    if (userRoles.includes('admin')) return true
    if (!user.permissions) return false
    if (Array.isArray(permission)) {
      return permission.some((p) => user.permissions.includes(p))
    }
    return user.permissions.includes(permission)
  },

  hasRole: (role: string | string[]) => {
    const { user } = get()
    if (!user || !user.roles) return false
    const userRoles = user.roles.map(normalizeRole)
    if (userRoles.includes('admin')) return true

    const checkList = Array.isArray(role) ? role : [role]
    const normalizedCheckList = checkList.map(normalizeRole)

    return normalizedCheckList.some((r) => userRoles.includes(r))
  },
}))


