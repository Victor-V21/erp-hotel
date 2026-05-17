import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import { useUIStore } from '@/store/uiStore'
import { cn } from '@/lib/utils'
import { Moon, Sun } from 'lucide-react'

export default function AppLayout() {
  const { sidebarOpen, darkMode, toggleDarkMode } = useUIStore()

  return (
    <div className="min-h-screen bg-background">
      <Sidebar />
      <main
        className={cn(
          'transition-all duration-300 min-h-screen',
          sidebarOpen ? 'ml-64' : 'ml-16'
        )}
      >
        <header className="h-14 border-b border-border bg-card flex items-center justify-end px-6 gap-3">
          <span className="text-sm text-muted-foreground">
            Hotel ERP v1.0
          </span>
          <button
            onClick={toggleDarkMode}
            className="p-2 rounded-md hover:bg-accent text-muted-foreground cursor-pointer"
          >
            {darkMode ? <Sun size={16} /> : <Moon size={16} />}
          </button>
        </header>
        <div className="p-6">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
