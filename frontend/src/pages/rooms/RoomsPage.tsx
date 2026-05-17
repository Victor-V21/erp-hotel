import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { Room, RoomType } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const statusColors: Record<string, string> = {
  Libre: 'bg-green-100 text-green-800 border-green-300',
  Ocupada: 'bg-red-100 text-red-800 border-red-300',
  Limpieza: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  Mantenimiento: 'bg-orange-100 text-orange-800 border-orange-300',
  Reservada: 'bg-blue-100 text-blue-800 border-blue-300',
  Bloqueada: 'bg-gray-100 text-gray-800 border-gray-300',
}

export default function RoomsPage() {
  const [rooms, setRooms] = useState<Room[]>([])
  const [roomTypes, setRoomTypes] = useState<RoomType[]>([])
  const [filter, setFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState({ roomNumber: '', floor: 1, roomTypeId: '', observations: '' })

  useEffect(() => { load() }, [])

  const load = async () => {
    const [r, rt] = await Promise.all([
      api.get<Room[]>('/rooms'),
      api.get<RoomType[]>('/room-types')
    ])
    setRooms(r.data)
    setRoomTypes(rt.data)
  }

  const changeStatus = async (id: string, status: string) => {
    await api.put(`/rooms/${id}`, { status })
    load()
  }

  const saveRoom = async () => {
    if (editingId) {
      await api.put(`/rooms/${editingId}`, form)
    } else {
      await api.post('/rooms', form)
    }
    setShowForm(false); setEditingId(null); setForm({ roomNumber: '', floor: 1, roomTypeId: '', observations: '' }); load()
  }

  const deleteRoom = async (id: string) => {
    if (confirm('¿Eliminar esta habitación?')) {
      await api.delete(`/rooms/${id}`)
      load()
    }
  }

  const startEdit = (r: Room) => {
    setEditingId(r.id)
    setForm({ roomNumber: r.roomNumber, floor: r.floor, roomTypeId: r.roomTypeId, observations: r.observations || '' })
    setShowForm(true)
  }

  const filtered = rooms.filter(r => {
    if (statusFilter && r.status !== statusFilter) return false
    if (filter && !r.roomNumber.includes(filter) && !r.roomTypeName.toLowerCase().includes(filter.toLowerCase())) return false
    return true
  })

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Habitaciones</h1>
        <Button onClick={() => { setShowForm(!showForm); setEditingId(null); setForm({ roomNumber: '', floor: 1, roomTypeId: roomTypes[0]?.id || '', observations: '' }) }}>
          {showForm ? 'Cancelar' : 'Nueva Habitación'}
        </Button>
      </div>

      {showForm && (
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">{editingId ? 'Editar' : 'Nueva'} Habitación</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <div><label className="text-sm">Número *</label><Input value={form.roomNumber} onChange={e => setForm({...form, roomNumber: e.target.value})} /></div>
            <div><label className="text-sm">Piso</label><Input type="number" value={form.floor} onChange={e => setForm({...form, floor: +e.target.value})} /></div>
            <div><label className="text-sm">Tipo</label>
              <select value={form.roomTypeId} onChange={e => setForm({...form, roomTypeId: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                <option value="">Seleccionar...</option>
                {roomTypes.map(rt => <option key={rt.id} value={rt.id}>{rt.name} - L{rt.pricePerNight.toFixed(2)}</option>)}
              </select>
            </div>
            <div><label className="text-sm">Observaciones</label><Input value={form.observations} onChange={e => setForm({...form, observations: e.target.value})} /></div>
          </div>
          <div className="flex gap-2"><Button onClick={saveRoom}>{editingId ? 'Actualizar' : 'Guardar'}</Button></div>
        </div>
      )}

      <div className="flex gap-2">
        <Input placeholder="Buscar por número o tipo..." value={filter} onChange={e => setFilter(e.target.value)} className="max-w-xs" />
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="border border-input rounded-md px-3 text-sm bg-background">
          <option value="">Todos los estados</option>
          {Object.keys(statusColors).map(s => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
        {filtered.map(room => (
          <div key={room.id} className="border border-border rounded-lg p-4 bg-card space-y-2">
            <div className="flex justify-between items-start">
              <div>
                <span className="text-lg font-bold">#{room.roomNumber}</span>
                <span className="text-sm text-muted-foreground ml-2">Piso {room.floor}</span>
              </div>
              <span className={`px-2 py-0.5 rounded-full text-xs font-medium border ${statusColors[room.status] || ''}`}>
                {room.status}
              </span>
            </div>
            <div className="text-sm text-muted-foreground">{room.roomTypeName}</div>
            <div className="text-sm font-semibold">L {room.pricePerNight.toFixed(2)} / noche</div>
            <div className="text-xs text-muted-foreground">Capacidad: {room.capacity} personas</div>
            {room.observations && <div className="text-xs text-muted-foreground italic">{room.observations}</div>}

            <div className="flex gap-1 flex-wrap mt-2">
              {['Libre', 'Ocupada', 'Limpieza', 'Mantenimiento', 'Reservada', 'Bloqueada'].map(status => (
                <button key={status} onClick={() => changeStatus(room.id, status)}
                  className={`text-xs px-2 py-1 rounded cursor-pointer border transition-colors ${
                    room.status === status ? 'bg-primary text-primary-foreground border-primary' : 'bg-background text-muted-foreground border-border hover:bg-accent'
                  }`}>{status}</button>
              ))}
            </div>

            <div className="flex gap-1 pt-1">
              <Button size="sm" variant="outline" onClick={() => startEdit(room)} className="text-xs">Editar</Button>
              <Button size="sm" variant="destructive" onClick={() => deleteRoom(room.id)} className="text-xs">Eliminar</Button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
