import { useState, useEffect } from 'react'
import type { LucideIcon } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import {
  Search,
  LayoutDashboard,
  Map,
  UserCheck,
  UserMinus,
  CalendarDays,
  BedDouble,
  Receipt,
  Users,
  Building2,
  DollarSign,
  CreditCard,
  Boxes,
  BarChart3,
  ScrollText,
  ShieldCheck,
  UserCog,
  SlidersHorizontal,
  ArrowRight,
} from 'lucide-react'

interface NavigationItem {
  title: string
  subtitle: string
  path: string
  icon: LucideIcon
  roles?: string[]
}

const navActions: NavigationItem[] = [
  { title: 'Dashboard Operativo', subtitle: 'Panel de control en tiempo real', path: '/dashboard', icon: LayoutDashboard },
  { title: 'Mapa Interactivo', subtitle: 'Estado visual de habitaciones por piso', path: '/rooms/map', icon: Map, roles: ['Admin', 'Recepcion'] },
  { title: 'Check-In Express', subtitle: 'Registro y asignación de habitaciones', path: '/checkin', icon: UserCheck, roles: ['Admin', 'Recepcion'] },
  { title: 'Check-Out & Folios', subtitle: 'Liquidación, cobro y facturación', path: '/checkout', icon: UserMinus, roles: ['Admin', 'Recepcion'] },
  { title: 'Reservaciones', subtitle: 'Agenda y calendario hotelero', path: '/reservations', icon: CalendarDays, roles: ['Admin', 'Recepcion'] },
  { title: 'Habitaciones', subtitle: 'Catálogo y tarifas', path: '/rooms', icon: BedDouble, roles: ['Admin', 'Recepcion'] },
  { title: 'Facturación SAR', subtitle: 'Comprobantes fiscales, notas de crédito/débito', path: '/invoices', icon: Receipt, roles: ['Admin', 'Recepcion', 'Caja', 'Contador'] },
  { title: 'Huéspedes', subtitle: 'Directorio y documentos DNI', path: '/guests', icon: Users, roles: ['Admin', 'Recepcion', 'Contador'] },
  { title: 'Clientes & Empresas', subtitle: 'Directorio fiscal con RTN', path: '/customers', icon: Building2, roles: ['Admin', 'Recepcion', 'Contador'] },
  { title: 'Caja & Turnos', subtitle: 'Aperturas y arqueos de caja', path: '/cash', icon: DollarSign, roles: ['Admin', 'Recepcion', 'Caja'] },
  { title: 'Liquidaciones de tarjeta', subtitle: 'Depósitos, comisiones y retenciones del adquirente', path: '/card-settlements', icon: CreditCard, roles: ['Admin', 'Contador'] },
  { title: 'Inventario', subtitle: 'Productos y stock hotelero', path: '/inventory', icon: Boxes, roles: ['Admin'] },
  { title: 'Reportes SAR & P&L', subtitle: 'Libros fiscales y Estado de Resultados', path: '/reports', icon: BarChart3, roles: ['Admin', 'Contador'] },
  { title: 'Libro Diario & Asientos', subtitle: 'Partida doble y ajustes contables', path: '/accounting/journal-entries', icon: ScrollText, roles: ['Admin', 'Contador'] },
  { title: 'Usuarios & Roles', subtitle: 'Gestión de accesos y colaboradores', path: '/users', icon: UserCog, roles: ['Admin'] },
  { title: 'Auditoría & Trazabilidad', subtitle: 'Firmas SHA-256 e integridad', path: '/audit-logs', icon: ShieldCheck, roles: ['Admin', 'Contador'] },
  { title: 'Configuración del Hotel', subtitle: 'Datos comerciales y membrete SAR', path: '/settings', icon: SlidersHorizontal, roles: ['Admin'] },
]

export function CommandPalette({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const [query, setQuery] = useState('')
  const navigate = useNavigate()
  const hasRole = useAuthStore((s) => s.hasRole)

  // Keyboard shortcut listener
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        if (isOpen) onClose()
        else {
          setQuery('')
          // Trigger open via custom event or state
        }
      }
      if (e.key === 'Escape' && isOpen) {
        onClose()
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onClose])

  const handleSelect = (path: string) => {
    navigate(path)
    onClose()
  }

  if (!isOpen) return null

  const filtered = navActions.filter((item) => {
    if (item.roles && !hasRole(item.roles)) return false
    const q = query.toLowerCase()
    return (
      item.title.toLowerCase().includes(q) ||
      item.subtitle.toLowerCase().includes(q) ||
      item.path.toLowerCase().includes(q)
    )
  })

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="command-palette-title"
      className="fixed inset-0 z-50 flex items-start justify-center pt-24 p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
      onClick={onClose}
    >
      <div
        className="bg-card border border-border rounded-xl w-full max-w-xl shadow-2xl overflow-hidden animate-in zoom-in-95"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Search input header */}
        <div className="flex items-center px-4 border-b border-border bg-muted/20">
          <Search size={18} className="text-[#C69C4B] shrink-0 mr-3" />
          <input
            autoFocus
            aria-label="Buscar módulo o acción"
            placeholder="Ir a un módulo o buscar acción rápida... (Ej. Check-in, Facturas, Asientos)"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="w-full py-3.5 bg-transparent text-foreground placeholder:text-muted-foreground text-sm outline-hidden"
          />
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded border border-border bg-muted text-muted-foreground">
            ESC
          </span>
        </div>

        {/* Results List */}
        <div className="max-h-80 overflow-y-auto divide-y divide-border/50 p-2 space-y-1">
          {filtered.map((item) => {
            const Icon = item.icon
            return (
              <button
                key={item.path}
                onClick={() => handleSelect(item.path)}
                className="w-full p-2.5 rounded-lg flex items-center justify-between hover:bg-accent/50 transition-colors text-left group cursor-pointer"
              >
                <div className="flex items-center gap-3">
                  <div className="p-2 rounded-md bg-muted text-[#C69C4B] group-hover:scale-105 transition-transform">
                    <Icon size={16} />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-foreground">{item.title}</p>
                    <p className="text-[11px] text-muted-foreground">{item.subtitle}</p>
                  </div>
                </div>
                <ArrowRight size={14} className="text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
              </button>
            )
          })}
          {filtered.length === 0 && (
            <div className="p-8 text-center text-xs text-muted-foreground">
              No se encontraron módulos con el término "{query}".
            </div>
          )}
        </div>

        {/* Footer info */}
        <div className="p-2.5 border-t border-border bg-muted/10 flex items-center justify-between text-[11px] text-muted-foreground px-4">
          <span id="command-palette-title">Hotel Maya Central — Navegación rápida</span>
          <span>Presione Enter para seleccionar</span>
        </div>
      </div>
    </div>
  )
}
