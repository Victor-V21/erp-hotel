import { create } from 'zustand'

const initialDarkMode = localStorage.getItem('darkMode') === 'true'
if (initialDarkMode) {
  document.documentElement.classList.add('dark')
} else {
  document.documentElement.classList.remove('dark')
}

const initialSidebarOpen = localStorage.getItem('sidebarOpen') !== 'false'

interface UIState {
  sidebarOpen: boolean
  darkMode: boolean
  toggleSidebar: () => void
  setSidebarOpen: (open: boolean) => void
  toggleDarkMode: () => void
}

export const useUIStore = create<UIState>((set, get) => ({
  sidebarOpen: initialSidebarOpen,
  darkMode: initialDarkMode,

  toggleSidebar: () =>
    set((state) => {
      const next = !state.sidebarOpen
      localStorage.setItem('sidebarOpen', String(next))
      return { sidebarOpen: next }
    }),

  setSidebarOpen: (open: boolean) => {
    localStorage.setItem('sidebarOpen', String(open))
    set({ sidebarOpen: open })
  },

  toggleDarkMode: () => {
    const newMode = !get().darkMode
    localStorage.setItem('darkMode', String(newMode))
    if (newMode) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
    set({ darkMode: newMode })
  },
}))

