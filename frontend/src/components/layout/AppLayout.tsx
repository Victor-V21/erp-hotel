import { useState, useEffect } from 'react'
import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import { Breadcrumbs } from './Breadcrumbs'
import { CommandPalette } from './CommandPalette'
import { useUIStore } from '@/store/uiStore'
import { cn } from '@/lib/utils'
import { Moon, Sun, Search, Command } from 'lucide-react'

export default function AppLayout() {
  const { sidebarOpen, darkMode, toggleDarkMode } = useUIStore()
  const [commandOpen, setCommandOpen] = useState(false)

  // Global shortcut
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setCommandOpen((prev) => !prev)
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  return (
    <div className="min-h-screen bg-background">
      <Sidebar />
      <CommandPalette isOpen={commandOpen} onClose={() => setCommandOpen(false)} />
      <main
        className={cn(
          'transition-all duration-300 min-h-screen flex flex-col',
          sidebarOpen ? 'ml-64' : 'ml-16'
        )}
      >
        <header className="h-14 border-b border-border bg-card/90 backdrop-blur-xs flex items-center justify-between px-6 gap-4 select-none">
          {/* Quick Search Button */}
          <button
            onClick={() => setCommandOpen(true)}
            className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-border bg-muted/20 hover:bg-muted/40 text-xs text-muted-foreground transition-colors cursor-pointer w-48 sm:w-64"
          >
            <Search size={14} className="text-[#C69C4B]" />
            <span className="truncate">Buscar módulo...</span>
            <kbd className="ml-auto pointer-events-none inline-flex h-4.5 select-none items-center gap-0.5 rounded border border-border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground opacity-100">
              <span className="text-xs">Ctrl</span> K
            </kbd>
          </button>

          {/* Right Controls */}
          <div className="flex items-center gap-3">
            <span className="text-xs font-semibold text-muted-foreground hidden sm:inline">
              Hotel Maya Central v1.0
            </span>
            <button
              onClick={toggleDarkMode}
              className="p-2 rounded-lg hover:bg-accent text-muted-foreground transition-colors cursor-pointer"
              title={darkMode ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'}
            >
              {darkMode ? <Sun size={16} /> : <Moon size={16} />}
            </button>
          </div>
        </header>

        <div className="p-6 flex-1">
          <Breadcrumbs />
          <Outlet />
        </div>
      </main>
    </div>
  )
}

