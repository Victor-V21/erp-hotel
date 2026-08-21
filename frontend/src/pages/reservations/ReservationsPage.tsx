import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { Reservation } from '@/types'
import { Button } from '@/components/ui/button'
import { useNavigate } from 'react-router-dom'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { InlineAlert } from '@/components/ui/InlineAlert'

const statusColors: Record<string, string> = {
  Pendiente: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300',
  Confirmada: 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300',
  CheckIn: 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300',
  CheckOut: 'bg-gray-100 dark:bg-gray-800/50 text-gray-800 dark:text-gray-300',
  Cancelada: 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300',
}

export default function ReservationsPage() {
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [statusFilter, setStatusFilter] = useState('')
  const navigate = useNavigate()

  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success', message: string } | null>(null)
  const [confirmCancelId, setConfirmCancelId] = useState<string | null>(null)

  useEffect(() => { load() }, [statusFilter])

  const load = async () => {
    try {
      const { data } = await api.get<Reservation[]>('/reservations', { params: { status: statusFilter || undefined } })
      setReservations(data)
    } catch (err) {
      setAlertInfo({ variant: 'error', message: 'Error al cargar reservaciones' })
    }
  }

  const handleCancelReservation = async () => {
    if (!confirmCancelId) return
    try {
      await api.post(`/reservations/${confirmCancelId}/cancel`)
      setConfirmCancelId(null)
      load()
      setAlertInfo({ variant: 'success', message: 'Reservación cancelada' })
    } catch (err) {
      setAlertInfo({ variant: 'error', message: 'Error al cancelar reservación' })
    }
  }

  const confirmReservation = async (id: string) => {
    try {
      await api.post(`/reservations/${id}/confirm`)
      load()
      setAlertInfo({ variant: 'success', message: 'Reservación confirmada' })
    } catch (err) {
      setAlertInfo({ variant: 'error', message: 'Error al confirmar reservación' })
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Reservaciones</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={async () => {
            try {
              const { data } = await api.get('/data/export/reservations', { responseType: 'blob' })
              const url = window.URL.createObjectURL(new Blob([data]))
              const link = document.createElement('a'); link.href = url; link.setAttribute('download', 'reservaciones.xlsx')
              document.body.appendChild(link); link.click(); document.body.removeChild(link); window.URL.revokeObjectURL(url)
            } catch (err) {
              setAlertInfo({ variant: 'error', message: 'Error al exportar reservaciones' })
            }
          }}>Exportar XLSX</Button>
          <Button onClick={() => navigate('/checkin')}>Nuevo Check-In</Button>
        </div>
      </div>

      <div className="flex gap-2">
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="border border-input rounded-md px-3 py-2 text-sm bg-background">
          <option value="">Todos los estados</option>
          <option value="Pendiente">Pendiente</option>
          <option value="Confirmada">Confirmada</option>
          <option value="CheckIn">Check-In</option>
          <option value="CheckOut">Check-Out</option>
          <option value="Cancelada">Cancelada</option>
        </select>
      </div>
      
      {alertInfo && (
        <InlineAlert
          variant={alertInfo.variant}
          message={alertInfo.message}
          onClose={() => setAlertInfo(null)}
        />
      )}

      <div className="border border-border rounded-lg overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted"><tr>
            <th className="text-left p-3">Huésped</th><th className="text-left p-3">Habitación</th>
            <th className="text-left p-3">Check-In</th><th className="text-left p-3">Check-Out</th>
            <th className="text-left p-3">Estado</th><th className="text-left p-3">Acciones</th>
          </tr></thead>
          <tbody>
            {reservations.map(r => (
              <tr key={r.id} className="border-t border-border">
                <td className="p-3">{r.guestName}</td>
                <td className="p-3">{r.roomNumber}</td>
                <td className="p-3">{r.checkInDate}</td>
                <td className="p-3">{r.checkOutDate}</td>
                <td className="p-3"><span className={`px-2 py-0.5 rounded text-xs font-medium ${statusColors[r.status] || ''}`}>{r.status}</span></td>
                <td className="p-3 space-x-1">
                  {r.status === 'Pendiente' && <Button size="sm" onClick={() => confirmReservation(r.id)}>Confirmar</Button>}
                  {r.status === 'Confirmada' && <Button size="sm" onClick={() => navigate('/checkin')}>Check-In</Button>}
                  {r.status === 'CheckIn' && <Button size="sm" onClick={() => navigate('/checkout')}>Check-Out</Button>}
                  {(r.status === 'Pendiente' || r.status === 'Confirmada') && <Button size="sm" variant="destructive" onClick={() => setConfirmCancelId(r.id)}>Cancelar</Button>}
                </td>
              </tr>
            ))}
            {reservations.length === 0 && <tr><td colSpan={6} className="p-6 text-center text-muted-foreground">No hay reservaciones</td></tr>}
          </tbody>
        </table>
      </div>
      
      <ConfirmDialog
        isOpen={!!confirmCancelId}
        title="Cancelar Reservación"
        description="¿Está seguro de que desea cancelar esta reservación? Esta acción no se puede deshacer."
        confirmText="Sí, cancelar"
        cancelText="No, volver"
        onConfirm={handleCancelReservation}
        onCancel={() => setConfirmCancelId(null)}
      />
    </div>
  )
}
