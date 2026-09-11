import { NavLink, useNavigate } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { useUIStore } from '@/store/uiStore'
import { useAuthStore } from '@/store/authStore'
import api from '@/lib/axios'
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
  CreditCard,
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
  X,
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
      { to: '/dashboard', icon: LayoutDashboard, label: 'Dashboard', roles: ['Admin', 'Recepcion', 'Caja', 'Contador'] },
      { to: '/rooms/map', icon: Map, label: 'Mapa Interactivo', roles: ['Admin', 'Recepcion'] },
      { to: '/checkin', icon: UserCheck, label: 'Check-In', roles: ['Admin', 'Recepcion'] },
      { to: '/checkout', icon: UserMinus, label: 'Check-Out', roles: ['Admin', 'Recepcion'] },
      { to: '/reservations', icon: CalendarDays, label: 'Reservaciones', roles: ['Admin', 'Recepcion'] },
    ],
  },
  {
    title: 'Habitaciones',
    items: [
      { to: '/rooms', icon: BedDouble, label: 'Habitaciones', roles: ['Admin', 'Recepcion'] },
      { to: '/rooms/types', icon: Tags, label: 'Tipos de Hab.', roles: ['Admin', 'Recepcion'] },
    ],
  },
  {
    title: 'Facturación & Clientes',
    items: [
      { to: '/invoices', icon: Receipt, label: 'Facturación SAR', roles: ['Admin', 'Recepcion', 'Caja', 'Contador'] },
      { to: '/authorizations', icon: ShieldCheck, label: 'Autorizaciones CAI', roles: ['Admin', 'Contador'] },
      { to: '/customers', icon: Building2, label: 'Clientes / Empresas', roles: ['Admin', 'Recepcion', 'Contador'] },
      { to: '/guests', icon: Users, label: 'Huéspedes', roles: ['Admin', 'Recepcion', 'Contador'] },
      { to: '/folios', icon: ClipboardList, label: 'Folios / Consumos', roles: ['Admin', 'Recepcion', 'Caja', 'Contador'] },
    ],
  },
  {
    title: 'Caja e Inventario',
    items: [
      { to: '/cash', icon: DollarSign, label: 'Caja', roles: ['Admin', 'Recepcion', 'Caja'] },
      { to: '/card-settlements', icon: CreditCard, label: 'Liquidaciones Tarjeta', roles: ['Admin', 'Contador'] },
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
      { to: '/audit-logs', icon: ShieldCheck, label: 'Auditoría & Logs', roles: ['Admin', 'Contador'] },
      { to: '/discounts', icon: Percent, label: 'Descuentos', roles: ['Admin'] },
      { to: '/backups', icon: HardDriveDownload, label: 'Respaldos BD', roles: ['Admin'] },
      { to: '/settings', icon: SlidersHorizontal, label: 'Configuración', roles: ['Admin'] },
    ],
  },
]

interface SidebarProps {
  mobileOpen: boolean
  onMobileClose: () => void
}

export default function Sidebar({ mobileOpen, onMobileClose }: SidebarProps) {
  const { sidebarOpen, toggleSidebar } = useUIStore()
  const { user, hasRole, logout } = useAuthStore()
  const navigate = useNavigate()
  const expanded = sidebarOpen || mobileOpen

  const handleLogout = async () => {
    try {
      await api.post('/auth/logout')
    } finally {
      logout()
      onMobileClose()
      navigate('/login', { replace: true })
    }
  }

  return (
    <aside
      className={cn(
        'fixed left-0 top-0 z-40 h-screen w-64 bg-card border-r border-border flex flex-col transition-[width,transform] duration-300 select-none shadow-xs',
        mobileOpen ? 'translate-x-0' : '-translate-x-full',
        sidebarOpen ? 'md:translate-x-0 md:w-64' : 'md:translate-x-0 md:w-16'
      )}
      aria-label="Navegación principal"
    >
      {/* Header */}
      <div className="flex h-14 items-center justify-between px-3.5 border-b border-border bg-card/80 backdrop-blur-xs">
        {expanded ? (
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
          type="button"
          onClick={() => mobileOpen ? onMobileClose() : toggleSidebar()}
          className={cn(
            'p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-accent transition-colors cursor-pointer',
            !expanded && 'hidden'
          )}
          title={mobileOpen ? 'Cerrar menú' : 'Colapsar menú'}
          aria-label={mobileOpen ? 'Cerrar menú' : 'Colapsar menú'}
        >
          <X size={18} className="md:hidden" />
          <ChevronLeft size={18} className="hidden md:block" />
        </button>
      </div>

      {!expanded && (
        <div className="hidden md:flex justify-center py-2 border-b border-border/50">
          <button
            type="button"
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
              {expanded ? (
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
                    title={!expanded ? item.label : undefined}
                    onClick={onMobileClose}
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
                    {expanded && (
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
        {expanded && user && (
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
            !expanded && 'justify-center'
          )}
          onClick={handleLogout}
          title="Cerrar Sesión"
        >
          <LogOut size={18} className="shrink-0" />
          {expanded && <span>Cerrar Sesión</span>}
        </button>
      </div>
    </aside>
  )
}
