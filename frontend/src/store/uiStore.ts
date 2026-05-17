import { create } from 'zustand'

interface UIState {
  sidebarOpen: boolean
  darkMode: boolean
  toggleSidebar: () => void
  toggleDarkMode: () => void
}

export const useUIStore = create<UIState>((set, get) => ({
  sidebarOpen: true,
  darkMode: localStorage.getItem('darkMode') === 'true',

  toggleSidebar: () => set((state) => ({ sidebarOpen: !state.sidebarOpen })),

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
