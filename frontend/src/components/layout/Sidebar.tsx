import { NavLink } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { useUIStore } from '@/store/uiStore'
import {
  LayoutDashboard,
  Hotel,
  CalendarCheck,
  Users,
  UserCircle,
  FileText,
  Receipt,
  DollarSign,
  Package,
  BookOpen,
  BarChart3,
  Settings,
  LogOut,
  ChevronLeft,
  Menu,
} from 'lucide-react'

const navItems = [
  { to: '/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/rooms', icon: Hotel, label: 'Habitaciones' },
  { to: '/rooms/types', icon: Hotel, label: 'Tipos Hab.' },
  { to: '/checkin', icon: CalendarCheck, label: 'Check-In' },
  { to: '/checkout', icon: CalendarCheck, label: 'Check-Out' },
  { to: '/reservations', icon: CalendarCheck, label: 'Reservaciones' },
  { to: '/guests', icon: Users, label: 'Huéspedes' },
  { to: '/invoices', icon: FileText, label: 'Facturación' },
  { to: '/cai', icon: Receipt, label: 'CAI' },
  { to: '/cash', icon: DollarSign, label: 'Caja' },
  { to: '/customers', icon: UserCircle, label: 'Clientes' },
  { to: '/pos', icon: DollarSign, label: 'POS' },
  { to: '/inventory', icon: Package, label: 'Inventario' },
  { to: '/accounting', icon: BookOpen, label: 'Contabilidad' },
  { to: '/reports', icon: BarChart3, label: 'Reportes' },
  { to: '/discounts', icon: Settings, label: 'Descuentos' },
  { to: '/settings', icon: Settings, label: 'Configuración' },
]

export default function Sidebar() {
  const { sidebarOpen, toggleSidebar } = useUIStore()

  return (
    <aside
      className={cn(
        'fixed left-0 top-0 z-40 h-screen bg-card border-r border-border transition-all duration-300',
        sidebarOpen ? 'w-64' : 'w-16'
      )}
    >
      <div className="flex h-14 items-center justify-between px-4 border-b border-border">
        {sidebarOpen && (
          <span className="font-bold text-lg text-foreground">Hotel ERP</span>
        )}
        <button
          onClick={toggleSidebar}
          className="p-1.5 rounded-md hover:bg-accent text-muted-foreground cursor-pointer"
        >
          {sidebarOpen ? <ChevronLeft size={18} /> : <Menu size={18} />}
        </button>
      </div>

      <nav className="flex-1 overflow-y-auto py-2 space-y-1 px-2">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )
            }
          >
            <item.icon size={18} className="shrink-0" />
            {sidebarOpen && <span>{item.label}</span>}
          </NavLink>
        ))}
      </nav>

      <div className="border-t border-border p-2">
        <button
          className="flex items-center gap-3 px-3 py-2 w-full rounded-md text-sm text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors cursor-pointer"
          onClick={() => {
            localStorage.clear()
            window.location.href = '/login'
          }}
        >
          <LogOut size={18} className="shrink-0" />
          {sidebarOpen && <span>Cerrar Sesión</span>}
        </button>
      </div>
    </aside>
  )
}
