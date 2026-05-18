import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { Reservation } from '@/types'
import { Button } from '@/components/ui/button'
import { useNavigate } from 'react-router-dom'

const statusColors: Record<string, string> = {
  Pendiente: 'bg-yellow-100 text-yellow-800',
  Confirmada: 'bg-blue-100 text-blue-800',
  CheckIn: 'bg-green-100 text-green-800',
  CheckOut: 'bg-gray-100 text-gray-800',
  Cancelada: 'bg-red-100 text-red-800',
}

export default function ReservationsPage() {
  const [reservations, setReservations] = useState<Reservation[]>([])
  const [statusFilter, setStatusFilter] = useState('')
  const navigate = useNavigate()

  useEffect(() => { load() }, [statusFilter])

  const load = async () => {
    const { data } = await api.get<Reservation[]>('/reservations', { params: { status: statusFilter || undefined } })
    setReservations(data)
  }

  const cancelReservation = async (id: string) => {
    if (confirm('¿Cancelar esta reservación?')) {
      await api.post(`/reservations/${id}/cancel`)
      load()
    }
  }

  const confirmReservation = async (id: string) => {
    await api.post(`/reservations/${id}/confirm`)
    load()
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Reservaciones</h1>
        <Button onClick={() => navigate('/checkin')}>Nuevo Check-In</Button>
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
                  {(r.status === 'Pendiente' || r.status === 'Confirmada') && <Button size="sm" variant="destructive" onClick={() => cancelReservation(r.id)}>Cancelar</Button>}
                </td>
              </tr>
            ))}
            {reservations.length === 0 && <tr><td colSpan={6} className="p-6 text-center text-muted-foreground">No hay reservaciones</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
