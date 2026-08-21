import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '@/lib/axios'
import { useAuthStore } from '@/store/authStore'
import type { DashboardSummary } from '@/types'
import {
  AlertTriangle,
  CalendarCheck,
  DollarSign,
  Hotel,
  RefreshCw,
  UserCheck,
  UserMinus,
  Map,
  CalendarDays,
  Receipt,
  BedDouble,
  Bed,
  ArrowRight,
  TrendingUp,
  Loader2,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { InlineAlert } from '@/components/ui/InlineAlert'

const severityClasses: Record<string, string> = {
  warning: 'border-amber-300/80 bg-amber-50 dark:border-amber-800 dark:bg-amber-950/40 text-amber-900 dark:text-amber-200',
  danger: 'border-red-300/80 bg-red-50 dark:border-red-800 dark:bg-red-950/40 text-red-900 dark:text-red-200',
  info: 'border-sky-300/80 bg-sky-50 dark:border-sky-800 dark:bg-sky-950/40 text-sky-900 dark:text-sky-200',
}

export default function DashboardPage() {
  const user = useAuthStore((s) => s.user)
  const hasRole = useAuthStore((s) => s.hasRole)
  const navigate = useNavigate()
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [alertError, setAlertError] = useState<string | null>(null)

  useEffect(() => {
    load()
  }, [])

  const load = async () => {
    setLoading(true)
    setAlertError(null)
    try {
      const { data } = await api.get<DashboardSummary>('/dashboard/summary')
      setSummary(data)
    } catch {
      setAlertError('No se pudo sincronizar el resumen operativo del dashboard.')
    } finally {
      setLoading(false)
    }
  }

  const isRecepcionista = hasRole(['Recepcion', 'Recepcionista'])
  const isContador = hasRole(['Contador', 'Contabilidad'])
  const isAdmin = hasRole('Admin')

  const totalRooms = (summary?.occupiedRooms ?? 0) + (summary?.freeRooms ?? 0)
  const occupancyPercent = totalRooms > 0 ? Math.round(((summary?.occupiedRooms ?? 0) / totalRooms) * 100) : 0

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="text-2xl font-bold text-foreground tracking-tight">
              Bienvenido, {user?.firstName} {user?.lastName}
            </h1>
            <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-[#C69C4B]/15 text-[#C69C4B] border border-[#C69C4B]/30">
              {isAdmin ? 'Administrador' : isRecepcionista ? 'Recepción' : isContador ? 'Contabilidad' : 'Colaborador'}
            </span>
          </div>
          <p className="text-sm text-muted-foreground mt-0.5">
            Panel operativo en tiempo real — Hotel Maya Central
          </p>
        </div>
        <Button variant="outline" onClick={load} disabled={loading} className="gap-1.5 self-start sm:self-auto">
          <RefreshCw size={15} className={loading ? 'animate-spin' : ''} />
          Actualizar
        </Button>
      </div>

      {alertError && (
        <InlineAlert variant="error" message={alertError} onClose={() => setAlertError(null)} />
      )}

      {/* Quick Action Shortcuts for Reception & Operations */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        <button
          onClick={() => navigate('/checkin')}
          className="p-3.5 rounded-xl border border-border bg-card hover:bg-accent/50 transition-all flex flex-col items-center text-center gap-2 group cursor-pointer shadow-2xs hover:border-[#C69C4B]/40"
        >
          <div className="p-2.5 rounded-lg bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 group-hover:scale-105 transition-transform">
            <UserCheck size={20} />
          </div>
          <div>
            <p className="text-xs font-bold text-foreground">Check-In</p>
            <p className="text-[11px] text-muted-foreground">Ingreso de huéspedes</p>
          </div>
        </button>

        <button
          onClick={() => navigate('/checkout')}
          className="p-3.5 rounded-xl border border-border bg-card hover:bg-accent/50 transition-all flex flex-col items-center text-center gap-2 group cursor-pointer shadow-2xs hover:border-[#C69C4B]/40"
        >
          <div className="p-2.5 rounded-lg bg-amber-500/10 text-amber-600 dark:text-amber-400 group-hover:scale-105 transition-transform">
            <UserMinus size={20} />
          </div>
          <div>
            <p className="text-xs font-bold text-foreground">Check-Out</p>
            <p className="text-[11px] text-muted-foreground">Cierre y cobro de folio</p>
          </div>
        </button>

        <button
          onClick={() => navigate('/rooms/map')}
          className="p-3.5 rounded-xl border border-border bg-card hover:bg-accent/50 transition-all flex flex-col items-center text-center gap-2 group cursor-pointer shadow-2xs hover:border-[#C69C4B]/40"
        >
          <div className="p-2.5 rounded-lg bg-blue-500/10 text-blue-600 dark:text-blue-400 group-hover:scale-105 transition-transform">
            <Map size={20} />
          </div>
          <div>
            <p className="text-xs font-bold text-foreground">Mapa Habitaciones</p>
            <p className="text-[11px] text-muted-foreground">Estado por piso</p>
          </div>
        </button>

        <button
          onClick={() => navigate('/reservations')}
          className="p-3.5 rounded-xl border border-border bg-card hover:bg-accent/50 transition-all flex flex-col items-center text-center gap-2 group cursor-pointer shadow-2xs hover:border-[#C69C4B]/40"
        >
          <div className="p-2.5 rounded-lg bg-purple-500/10 text-purple-600 dark:text-purple-400 group-hover:scale-105 transition-transform">
            <CalendarDays size={20} />
          </div>
          <div>
            <p className="text-xs font-bold text-foreground">Reservaciones</p>
            <p className="text-[11px] text-muted-foreground">Calendario y agenda</p>
          </div>
        </button>

        <button
          onClick={() => navigate('/cash')}
          className="p-3.5 rounded-xl border border-border bg-card hover:bg-accent/50 transition-all flex flex-col items-center text-center gap-2 group cursor-pointer shadow-2xs hover:border-[#C69C4B]/40 col-span-2 sm:col-span-1"
        >
          <div className="p-2.5 rounded-lg bg-teal-500/10 text-teal-600 dark:text-teal-400 group-hover:scale-105 transition-transform">
            <DollarSign size={20} />
          </div>
          <div>
            <p className="text-xs font-bold text-foreground">Caja y Arqueos</p>
            <p className="text-[11px] text-muted-foreground">Movimientos de turno</p>
          </div>
        </button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        <div className="rounded-xl border border-border bg-card p-5 shadow-xs flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Habitaciones Ocupadas
            </p>
            <p className="text-2xl font-bold text-foreground mt-1">
              {loading ? '...' : summary?.occupiedRooms ?? 0}
            </p>
            <div className="flex items-center gap-2 mt-1.5">
              <div className="w-20 bg-muted rounded-full h-1.5 overflow-hidden">
                <div
                  className="bg-[#C69C4B] h-full rounded-full transition-all"
                  style={{ width: `${occupancyPercent}%` }}
                />
              </div>
              <span className="text-[11px] text-muted-foreground font-medium">
                {occupancyPercent}% ocupación
              </span>
            </div>
          </div>
          <div className="p-3 rounded-xl bg-amber-500/10 text-[#C69C4B]">
            <BedDouble size={24} />
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-5 shadow-xs flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Habitaciones Libres
            </p>
            <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400 mt-1">
              {loading ? '...' : summary?.freeRooms ?? 0}
            </p>
            <p className="text-[11px] text-muted-foreground mt-1">Disponibles para venta</p>
          </div>
          <div className="p-3 rounded-xl bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
            <Bed size={24} />
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-5 shadow-xs flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Check-Ins Pendientes Hoy
            </p>
            <p className="text-2xl font-bold text-foreground mt-1">
              {loading ? '...' : summary?.pendingCheckIns ?? 0}
            </p>
            <p className="text-[11px] text-muted-foreground mt-1">
              {summary?.pendingCheckOuts ?? 0} check-out(s) pendientes
            </p>
          </div>
          <div className="p-3 rounded-xl bg-blue-500/10 text-blue-600 dark:text-blue-400">
            <CalendarCheck size={24} />
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-5 shadow-xs flex items-center justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Ventas Facturadas Hoy
            </p>
            <p className="text-2xl font-bold text-[#C69C4B] mt-1">
              {loading
                ? '...'
                : summary?.stats.find((s) => s.title.includes('Ingresos'))?.value || 'L 0.00'}
            </p>
            <p className="text-[11px] text-muted-foreground mt-1">Comprobantes SAR del día</p>
          </div>
          <div className="p-3 rounded-xl bg-teal-500/10 text-teal-600 dark:text-teal-400">
            <Receipt size={24} />
          </div>
        </div>
      </div>

      {/* Main Grid: Activity & Alertas */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <section className="xl:col-span-2 rounded-xl border border-border bg-card p-5 space-y-4 shadow-xs">
          <div className="flex items-center justify-between border-b border-border/80 pb-3">
            <div className="flex items-center gap-2">
              <Hotel size={18} className="text-[#C69C4B]" />
              <h2 className="font-bold text-sm text-foreground">Actividad Operativa Reciente</h2>
            </div>
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            {/* Upcoming Reservations */}
            <div className="rounded-xl border border-border p-4 bg-muted/10 flex flex-col">
              <div className="flex items-center justify-between mb-3">
                <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider flex items-center gap-1.5">
                  <CalendarCheck size={14} className="text-[#C69C4B]" />
                  Próximas Reservaciones
                </h3>
                <button
                  onClick={() => navigate('/reservations')}
                  className="text-xs text-[#C69C4B] hover:underline flex items-center gap-0.5 cursor-pointer font-medium"
                >
                  Ver todas <ArrowRight size={12} />
                </button>
              </div>

              <div className="space-y-2.5 flex-1">
                {summary?.upcomingReservations.length ? (
                  summary.upcomingReservations.map((r) => (
                    <div
                      key={`${r.roomNumber}-${r.checkInDate}`}
                      className="flex items-center justify-between gap-3 p-2.5 rounded-lg border border-border/60 bg-card hover:bg-accent/40 transition-colors"
                    >
                      <div>
                        <p className="font-bold text-xs text-foreground">{r.guestName}</p>
                        <p className="text-[11px] text-muted-foreground">
                          Hab. #{r.roomNumber} · <span className="font-medium text-[#C69C4B]">{r.status}</span>
                        </p>
                      </div>
                      <div className="text-right text-[11px] text-muted-foreground font-mono">
                        <p className="font-medium text-foreground">In: {r.checkInDate}</p>
                        <p>Out: {r.checkOutDate}</p>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="text-muted-foreground text-xs p-6 text-center">
                    No hay reservaciones pendientes para hoy.
                  </p>
                )}
              </div>
            </div>

            {/* Recent Invoices */}
            <div className="rounded-xl border border-border p-4 bg-muted/10 flex flex-col">
              <div className="flex items-center justify-between mb-3">
                <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider flex items-center gap-1.5">
                  <Receipt size={14} className="text-[#C69C4B]" />
                  Últimas Facturas Emitidas
                </h3>
                <button
                  onClick={() => navigate('/invoices')}
                  className="text-xs text-[#C69C4B] hover:underline flex items-center gap-0.5 cursor-pointer font-medium"
                >
                  Ver facturación <ArrowRight size={12} />
                </button>
              </div>

              <div className="space-y-2.5 flex-1">
                {summary?.recentInvoices.length ? (
                  summary.recentInvoices.map((inv) => (
                    <div
                      key={inv.correlativeNumber}
                      className="flex items-center justify-between gap-3 p-2.5 rounded-lg border border-border/60 bg-card hover:bg-accent/40 transition-colors"
                    >
                      <div>
                        <p className="font-mono text-xs font-bold text-foreground">{inv.correlativeNumber}</p>
                        <p className="text-[11px] text-muted-foreground truncate max-w-[140px]">
                          {inv.customerName}
                        </p>
                      </div>
                      <div className="text-right">
                        <p className="font-bold text-xs text-foreground">L {inv.totalAmount.toFixed(2)}</p>
                        <span
                          className={`text-[10px] px-1.5 py-0.2 rounded font-semibold ${
                            inv.status === 'Anulada'
                              ? 'bg-red-100 text-red-700 dark:bg-red-950/40 dark:text-red-300'
                              : 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300'
                          }`}
                        >
                          {inv.status}
                        </span>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="text-muted-foreground text-xs p-6 text-center">
                    No se han emitido facturas recientemente.
                  </p>
                )}
              </div>
            </div>
          </div>
        </section>

        {/* Sidebar Panel: Cash & SAR Alerts */}
        <aside className="space-y-4">
          <section className="rounded-xl border border-border bg-card p-5 space-y-3 shadow-xs">
            <h2 className="font-bold text-sm text-foreground flex items-center gap-2 border-b border-border/80 pb-2.5">
              <DollarSign size={16} className="text-[#C69C4B]" />
              Estado de Caja de Turno
            </h2>
            {summary?.cash ? (
              <div className="p-3.5 rounded-lg bg-primary/5 border border-primary/20 space-y-1">
                <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">
                  {summary.cash.registerName}
                </p>
                <p className="text-2xl font-bold text-[#C69C4B]">
                  L {summary.cash.balance.toFixed(2)}
                </p>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => navigate('/cash')}
                  className="w-full mt-2 text-xs"
                >
                  Ir al Arqueo de Caja
                </Button>
              </div>
            ) : (
              <div className="p-4 rounded-lg bg-muted/20 border border-border text-center space-y-2">
                <p className="text-xs text-muted-foreground">No hay caja abierta actualmente.</p>
                <Button size="sm" onClick={() => navigate('/cash')} className="text-xs">
                  Abrir Caja
                </Button>
              </div>
            )}
          </section>

          <section className="rounded-xl border border-border bg-card p-5 space-y-3 shadow-xs">
            <h2 className="font-bold text-sm text-foreground flex items-center gap-2 border-b border-border/80 pb-2.5">
              <AlertTriangle size={16} className="text-amber-500" />
              Alertas Fiscales & Operativas
            </h2>
            <div className="space-y-2">
              {summary?.alerts.length ? (
                summary.alerts.map((alert) => (
                  <div
                    key={alert.title}
                    className={`rounded-lg border p-3 text-xs space-y-1 ${
                      severityClasses[alert.severity] || severityClasses.info
                    }`}
                  >
                    <p className="font-bold">{alert.title}</p>
                    <p className="opacity-90">{alert.description}</p>
                  </div>
                ))
              ) : (
                <p className="text-xs text-muted-foreground p-3 text-center">
                  Sin alertas fiscales pendientes de atención.
                </p>
              )}
            </div>
          </section>
        </aside>
      </div>
    </div>
  )
}

