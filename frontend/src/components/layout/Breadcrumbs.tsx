import { useLocation, Link } from 'react-router-dom'
import { ChevronRight, Home } from 'lucide-react'

const routeLabels: Record<string, string> = {
  dashboard: 'Panel Operativo',
  rooms: 'Habitaciones',
  types: 'Tipos de Habitación',
  map: 'Mapa Interactivo',
  reservations: 'Reservaciones',
  checkin: 'Check-In',
  checkout: 'Check-Out',
  guests: 'Huéspedes',
  folios: 'Folios / Consumos',
  customers: 'Clientes & Empresas',
  invoices: 'Facturación SAR',
  edit: 'Edición',
  authorizations: 'Autorizaciones CAI',
  cash: 'Caja & Turnos',
  'card-settlements': 'Liquidaciones de tarjeta',
  inventory: 'Inventario',
  reports: 'Reportes Financieros',
  accounting: 'Contabilidad',
  'chart-of-accounts': 'Catálogo de Cuentas',
  'journal-entries': 'Libro Diario',
  'trial-balance': 'Balance de Comprobación',
  users: 'Usuarios & Roles',
  'audit-logs': 'Auditoría & Logs',
  discounts: 'Descuentos',
  backups: 'Respaldos',
  settings: 'Configuración',
}

export function Breadcrumbs() {
  const location = useLocation()
  const pathnames = location.pathname.split('/').filter((x) => x)

  if (pathnames.length === 0 || (pathnames.length === 1 && pathnames[0] === 'dashboard')) {
    return null
  }

  return (
    <nav className="flex items-center space-x-1 text-xs text-muted-foreground mb-4 select-none">
      <Link to="/dashboard" className="flex items-center hover:text-foreground transition-colors gap-1">
        <Home size={13} className="text-[#C69C4B]" />
        <span>Inicio</span>
      </Link>

      {pathnames.map((value, index) => {
        const to = `/${pathnames.slice(0, index + 1).join('/')}`
        const isLast = index === pathnames.length - 1
        const label = routeLabels[value] || (value.length > 12 ? `${value.slice(0, 8)}...` : value)

        return (
          <span key={to} className="flex items-center space-x-1">
            <ChevronRight size={12} className="text-muted-foreground/60" />
            {isLast ? (
              <span className="font-semibold text-foreground">{label}</span>
            ) : (
              <Link to={to} className="hover:text-foreground transition-colors">
                {label}
              </Link>
            )}
          </span>
        )
      })}
    </nav>
  )
}
