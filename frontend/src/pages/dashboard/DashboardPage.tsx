import { useAuthStore } from '@/store/authStore'

export default function DashboardPage() {
  const user = useAuthStore((s) => s.user)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">
          Bienvenido, {user?.firstName} {user?.lastName}
        </h1>
        <p className="text-muted-foreground">
          Panel de control del sistema Hotel ERP
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard title="Habitaciones Ocupadas" value="0" />
        <StatCard title="Habitaciones Libres" value="0" />
        <StatCard title="Reservas del Día" value="0" />
        <StatCard title="Ingresos del Día" value="L 0.00" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="rounded-lg border border-border bg-card p-6">
          <h2 className="font-semibold mb-4">Check-ins Pendientes</h2>
          <p className="text-muted-foreground text-sm">No hay check-ins pendientes</p>
        </div>
        <div className="rounded-lg border border-border bg-card p-6">
          <h2 className="font-semibold mb-4">Check-outs Pendientes</h2>
          <p className="text-muted-foreground text-sm">No hay check-outs pendientes</p>
        </div>
      </div>
    </div>
  )
}

function StatCard({ title, value }: { title: string; value: string }) {
  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <p className="text-sm text-muted-foreground">{title}</p>
      <p className="text-2xl font-bold text-foreground mt-1">{value}</p>
    </div>
  )
}
