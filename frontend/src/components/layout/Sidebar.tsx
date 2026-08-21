import { NavLink, useNavigate } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { useUIStore } from '@/store/uiStore'
import { useAuthStore } from '@/store/authStore'
import {
  LayoutDashboard,
  Map,
  UserCheck,
  UserMinus,
  CalendarDays,
  BedDouble,
  Tags,
  Receipt,
  ShieldCheck,
  Building2,
  ClipboardList,
  Users,
  DollarSign,
  Boxes,
  BarChart3,
  BookOpen,
  ScrollText,
  Scale,
  Percent,
  HardDriveDownload,
  SlidersHorizontal,
  UserCog,
  LogOut,
  ChevronLeft,
  Menu,
  Hotel,
} from 'lucide-react'

interface NavItem {
  to: string
  icon: React.ComponentType<{ size?: number; className?: string }>
  label: string
  roles: string[]
}

interface NavSection {
  title: string
  items: NavItem[]
}

const navSections: NavSection[] = [
  {
    title: 'Operaciones',
    items: [
      { to: '/dashboard', icon: LayoutDashboard, label: 'Dashboard', roles: ['Admin', 'Recepcionista', 'Contador'] },
      { to: '/rooms/map', icon: Map, label: 'Mapa Interactivo', roles: ['Admin', 'Recepcionista'] },
      { to: '/checkin', icon: UserCheck, label: 'Check-In', roles: ['Admin', 'Recepcionista'] },
      { to: '/checkout', icon: UserMinus, label: 'Check-Out', roles: ['Admin', 'Recepcionista'] },
      { to: '/reservations', icon: CalendarDays, label: 'Reservaciones', roles: ['Admin', 'Recepcionista'] },
    ],
  },
  {
    title: 'Habitaciones',
    items: [
      { to: '/rooms', icon: BedDouble, label: 'Habitaciones', roles: ['Admin', 'Recepcionista'] },
      { to: '/rooms/types', icon: Tags, label: 'Tipos de Hab.', roles: ['Admin', 'Recepcionista'] },
    ],
  },
  {
    title: 'Facturación & Clientes',
    items: [
      { to: '/invoices', icon: Receipt, label: 'Facturación SAR', roles: ['Admin', 'Recepcionista', 'Contador'] },
      { to: '/authorizations', icon: ShieldCheck, label: 'Autorizaciones CAI', roles: ['Admin', 'Recepcionista', 'Contador'] },
      { to: '/customers', icon: Building2, label: 'Clientes / Empresas', roles: ['Admin', 'Recepcionista', 'Contador'] },
      { to: '/guests', icon: Users, label: 'Huéspedes', roles: ['Admin', 'Recepcionista', 'Contador'] },
      { to: '/folios', icon: ClipboardList, label: 'Folios / Consumos', roles: ['Admin', 'Recepcionista', 'Contador'] },
    ],
  },
  {
    title: 'Caja e Inventario',
    items: [
      { to: '/cash', icon: DollarSign, label: 'Caja', roles: ['Admin', 'Recepcionista', 'Contador'] },
      { to: '/inventory', icon: Boxes, label: 'Inventario', roles: ['Admin'] },
    ],
  },
  {
    title: 'Contabilidad & Reportes',
    items: [
      { to: '/reports', icon: BarChart3, label: 'Reportes SAR', roles: ['Admin', 'Contador'] },
      { to: '/accounting/chart-of-accounts', icon: BookOpen, label: 'Catálogo Cuentas', roles: ['Admin', 'Contador'] },
      { to: '/accounting/journal-entries', icon: ScrollText, label: 'Asientos Contables', roles: ['Admin', 'Contador'] },
      { to: '/accounting/trial-balance', icon: Scale, label: 'Balance Comprob.', roles: ['Admin', 'Contador'] },
    ],
  },
  {
    title: 'Administración',
    items: [
      { to: '/users', icon: UserCog, label: 'Usuarios & Roles', roles: ['Admin'] },
      { to: '/audit-logs', icon: ShieldCheck, label: 'Auditoría & Logs', roles: ['Admin'] },
      { to: '/discounts', icon: Percent, label: 'Descuentos', roles: ['Admin', 'Recepcionista'] },
      { to: '/backups', icon: HardDriveDownload, label: 'Respaldos BD', roles: ['Admin'] },
      { to: '/settings', icon: SlidersHorizontal, label: 'Configuración', roles: ['Admin'] },
    ],
  },
]

export default function Sidebar() {
  const { sidebarOpen, toggleSidebar } = useUIStore()
  const { user, hasRole, logout } = useAuthStore()
  const navigate = useNavigate()

  return (
    <aside
      className={cn(
        'fixed left-0 top-0 z-40 h-screen bg-card border-r border-border flex flex-col transition-all duration-300 select-none shadow-xs',
        sidebarOpen ? 'w-64' : 'w-16'
      )}
    >
      {/* Header */}
      <div className="flex h-14 items-center justify-between px-3.5 border-b border-border bg-card/80 backdrop-blur-xs">
        {sidebarOpen ? (
          <div className="flex items-center gap-2.5 overflow-hidden">
            <div className="p-1.5 rounded-lg bg-primary/10 text-[#C69C4B]">
              <Hotel size={20} />
            </div>
            <div className="flex flex-col">
              <span className="font-bold text-sm leading-tight text-foreground tracking-tight">
                Hotel Maya Central
              </span>
              <span className="text-[10px] text-muted-foreground font-medium uppercase tracking-wider">
                Sistema de Gestión
              </span>
            </div>
          </div>
        ) : (
          <div className="mx-auto p-1.5 rounded-lg bg-primary/10 text-[#C69C4B]">
            <Hotel size={20} />
          </div>
        )}
        <button
          onClick={toggleSidebar}
          className={cn(
            'p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-accent transition-colors cursor-pointer',
            !sidebarOpen && 'hidden'
          )}
          title={sidebarOpen ? 'Colapsar menú' : 'Expandir menú'}
        >
          <ChevronLeft size={18} />
        </button>
      </div>

      {!sidebarOpen && (
        <div className="flex justify-center py-2 border-b border-border/50">
          <button
            onClick={toggleSidebar}
            className="p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-accent transition-colors cursor-pointer"
            title="Expandir menú"
          >
            <Menu size={18} />
          </button>
        </div>
      )}

      {/* Navigation Sections */}
      <nav className="flex-1 overflow-y-auto py-2 px-2 space-y-3 custom-scrollbar">
        {navSections.map((section, idx) => {
          // Filter items based on user roles
          const visibleItems = section.items.filter((item) => {
            if (!item.roles || item.roles.length === 0) return true
            return hasRole(item.roles)
          })

          if (visibleItems.length === 0) return null

          return (
            <div key={section.title} className="space-y-1">
              {idx > 0 && <div className="border-t border-border/60 my-2 mx-1" />}
              {sidebarOpen ? (
                <div className="px-2.5 py-1 text-[11px] font-semibold uppercase tracking-wider text-muted-foreground/80">
                  {section.title}
                </div>
              ) : (
                <div className="h-1" />
              )}

              <div className="space-y-0.5">
                {visibleItems.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    title={!sidebarOpen ? item.label : undefined}
                    className={({ isActive }) =>
                      cn(
                        'flex items-center gap-3 px-2.5 py-2 rounded-lg text-xs sm:text-sm font-medium transition-all group relative',
                        isActive
                          ? 'bg-primary text-primary-foreground shadow-xs font-semibold'
                          : 'text-muted-foreground hover:text-foreground hover:bg-accent/60'
                      )
                    }
                  >
                    <item.icon
                      size={18}
                      className="shrink-0 transition-transform group-hover:scale-105"
                    />
                    {sidebarOpen && (
                      <span className="truncate leading-none">{item.label}</span>
                    )}
                  </NavLink>
                ))}
              </div>
            </div>
          )
        })}
      </nav>

      {/* Footer / User Profile & Logout */}
      <div className="border-t border-border p-2 space-y-2 bg-card/50">
        {sidebarOpen && user && (
          <div className="px-2.5 py-1.5 rounded-lg bg-accent/30 flex items-center justify-between text-xs">
            <div className="truncate">
              <p className="font-semibold text-foreground truncate">
                {user.firstName} {user.lastName}
              </p>
              <p className="text-[11px] text-muted-foreground truncate capitalize">
                {user.roles?.join(', ') || 'Usuario'}
              </p>
            </div>
          </div>
        )}

        <button
          className={cn(
            'flex items-center gap-3 px-2.5 py-2 w-full rounded-lg text-xs sm:text-sm font-medium text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors cursor-pointer',
            !sidebarOpen && 'justify-center'
          )}
          onClick={() => {
            logout()
            navigate('/login', { replace: true })
          }}
          title="Cerrar Sesión"
        >
          <LogOut size={18} className="shrink-0" />
          {sidebarOpen && <span>Cerrar Sesión</span>}
        </button>
      </div>
    </aside>
  )
}

