import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import api from '@/lib/axios'
import type { Reservation } from '@/types'
import { Button } from '@/components/ui/button'

export default function CheckOutPage() {
  const navigate = useNavigate()
  const [checkIns, setCheckIns] = useState<Reservation[]>([])
  const [selected, setSelected] = useState<Reservation | null>(null)
  const [done, setDone] = useState(false)

  useEffect(() => {
    api.get<Reservation[]>('/reservations', { params: { status: 'CheckIn' } }).then(({ data }) => setCheckIns(data))
  }, [])

  const doCheckOut = async () => {
    if (!selected) return
    try {
      await api.post(`/reservations/checkout/${selected.id}`)
      setDone(true)
    } catch (e: unknown) {
      const message = axios.isAxiosError<{ message?: string }>(e)
        ? e.response?.data?.message || e.message
        : 'Error'
      alert(message)
    }
  }

  if (done) {
    return (
      <div className="max-w-lg mx-auto space-y-4 text-center">
        <h1 className="text-2xl font-bold text-green-600">✓ Check-Out Completado</h1>
        <p className="text-muted-foreground">La habitación ha sido liberada y marcada para limpieza.</p>
        <Button onClick={() => navigate('/reservations')}>Volver a Reservaciones</Button>
      </div>
    )
  }

  return (
    <div className="max-w-lg mx-auto space-y-4">
      <h1 className="text-2xl font-bold">Check-Out</h1>
      <p className="text-sm text-muted-foreground">Seleccione el huésped para liberar la habitación.</p>

      <div className="border border-border rounded-lg">
        <div className="p-3 bg-muted font-medium">Huéspedes con Check-In activo</div>
        {checkIns.map(r => (
          <div key={r.id}
            className={`p-3 border-t border-border flex justify-between items-center cursor-pointer hover:bg-accent ${selected?.id === r.id ? 'bg-accent' : ''}`}
            onClick={() => setSelected(r)}>
            <div><span className="font-medium">{r.guestName}</span><span className="text-muted-foreground ml-2">#{r.roomNumber}</span></div>
            <div className="text-sm">{r.checkInDate} → {r.checkOutDate}</div>
          </div>
        ))}
        {checkIns.length === 0 && <div className="p-6 text-center text-muted-foreground">No hay huéspedes con Check-In activo</div>}
      </div>

      {selected && (
        <Button onClick={doCheckOut} className="w-full">Liberar Habitación #{selected.roomNumber}</Button>
      )}
    </div>
  )
}
