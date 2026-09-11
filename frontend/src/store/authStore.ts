import { create } from 'zustand'
import { setAccessToken } from '@/lib/authSession'
import type { User } from '@/types'

// Elimina credenciales que pudieron quedar de versiones anteriores.
try {
  localStorage.removeItem('accessToken')
  localStorage.removeItem('refreshToken')
  localStorage.removeItem('user')
} catch {
  // La sesión actual ya no depende de almacenamiento persistente del navegador.
}

interface AuthState {
  user: User | null
  accessToken: string | null
  isAuthenticated: boolean
  isInitialized: boolean
  setAuth: (user: User, accessToken: string) => void
  finishInitialization: () => void
  logout: () => void
  hasPermission: (permission: string | string[]) => boolean
  hasRole: (role: string | string[]) => boolean
}

function normalizeRole(role: string): string {
  const lower = role.trim().toLowerCase()
  if (lower === 'admin' || lower === 'administrador') return 'admin'
  if (lower === 'recepcion' || lower === 'recepcionista' || lower === 'reception' || lower === 'recep') return 'recepcion'
  if (lower === 'contador' || lower === 'contabilidad' || lower === 'contable' || lower === 'accountant') return 'contador'
  return lower
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isInitialized: false,

  setAuth: (user, accessToken) => {
    setAccessToken(accessToken)
    set({ user, accessToken, isAuthenticated: true, isInitialized: true })
  },

  finishInitialization: () => set({ isInitialized: true }),

  logout: () => {
    setAccessToken(null)
    set({ user: null, accessToken: null, isAuthenticated: false, isInitialized: true })
  },

  hasPermission: (permission) => {
    const { user } = get()
    if (!user) return false
    const userRoles = (user.roles || []).map(normalizeRole)
    if (userRoles.includes('admin')) return true
    if (!user.permissions) return false
    if (Array.isArray(permission)) {
      return permission.some((item) => user.permissions.includes(item))
    }
    return user.permissions.includes(permission)
  },

  hasRole: (role) => {
    const { user } = get()
    if (!user || !user.roles) return false
    const userRoles = user.roles.map(normalizeRole)
    if (userRoles.includes('admin')) return true

    const checkList = Array.isArray(role) ? role : [role]
    return checkList.map(normalizeRole).some((item) => userRoles.includes(item))
  },
}))
