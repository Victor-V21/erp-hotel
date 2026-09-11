import { useState, useEffect, useCallback } from 'react'
import api from '@/lib/axios'
import type { Room, RoomType } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { BedDouble, Plus, Search, Loader2 } from 'lucide-react'

const statusColors: Record<string, string> = {
  Libre: 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300 border-emerald-300 dark:border-emerald-800',
  Ocupada: 'bg-red-100 dark:bg-red-950/40 text-red-800 dark:text-red-300 border-red-300 dark:border-red-800',
  Limpieza: 'bg-amber-100 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300 border-amber-300 dark:border-amber-800',
  Mantenimiento: 'bg-orange-100 dark:bg-orange-950/40 text-orange-800 dark:text-orange-300 border-orange-300 dark:border-orange-800',
  Bloqueada: 'bg-gray-100 dark:bg-gray-800/60 text-gray-800 dark:text-gray-300 border-gray-300 dark:border-gray-600',
}

export default function RoomsPage() {
  const [rooms, setRooms] = useState<Room[]>([])
  const [roomTypes, setRoomTypes] = useState<RoomType[]>([])
  const [filter, setFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState({ roomNumber: '', floor: 1, roomTypeId: '', observations: '' })
  const [loading, setLoading] = useState(true)
  const [deleteTarget, setDeleteTarget] = useState<Room | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success'; message: string } | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [r, rt] = await Promise.all([
        api.get<Room[]>('/rooms'),
        api.get<RoomType[]>('/room-types'),
      ])
      setRooms(r.data)
      setRoomTypes(rt.data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al cargar habitaciones y tipos desde el servidor.' })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0)
    return () => window.clearTimeout(timer)
  }, [load])

  const changeStatus = async (id: string, status: string) => {
    try {
      await api.put(`/rooms/${id}`, { status })
      setAlertInfo({ variant: 'success', message: `Estado de la habitación actualizado a "${status}".` })
      load()
    } catch {
      setAlertInfo({ variant: 'error', message: 'No fue posible cambiar el estado de la habitación.' })
    }
  }

  const saveRoom = async () => {
    if (!form.roomNumber.trim()) {
      setAlertInfo({ variant: 'error', message: 'El número de habitación es obligatorio.' })
      return
    }
    if (!form.roomTypeId) {
      setAlertInfo({ variant: 'error', message: 'Debe seleccionar un tipo de habitación.' })
      return
    }
    try {
      if (editingId) {
        await api.put(`/rooms/${editingId}`, form)
        setAlertInfo({ variant: 'success', message: `Habitación #${form.roomNumber} actualizada con éxito.` })
      } else {
        await api.post('/rooms', form)
        setAlertInfo({ variant: 'success', message: `Habitación #${form.roomNumber} creada con éxito.` })
      }
      setShowForm(false)
      setEditingId(null)
      setForm({ roomNumber: '', floor: 1, roomTypeId: '', observations: '' })
      load()
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al guardar la habitación.' })
    }
  }

  const handleDeleteConfirm = async () => {
    if (!deleteTarget) return
    try {
      await api.delete(`/rooms/${deleteTarget.id}`)
      setAlertInfo({ variant: 'success', message: `Habitación #${deleteTarget.roomNumber} eliminada.` })
      setDeleteTarget(null)
      load()
    } catch {
      setAlertInfo({ variant: 'error', message: 'No se pudo eliminar la habitación (verifique si tiene reservas activas).' })
    }
  }

  const startEdit = (r: Room) => {
    setEditingId(r.id)
    setForm({
      roomNumber: r.roomNumber,
      floor: r.floor,
      roomTypeId: r.roomTypeId,
      observations: r.observations || '',
    })
    setShowForm(true)
  }

  const filtered = rooms.filter((r) => {
    if (statusFilter && r.status !== statusFilter) return false
    if (
      filter &&
      !r.roomNumber.toLowerCase().includes(filter.toLowerCase()) &&
      !r.roomTypeName.toLowerCase().includes(filter.toLowerCase())
    )
      return false
    return true
  })

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <BedDouble className="text-[#C69C4B]" size={24} />
            Gestión de Habitaciones
          </h1>
          <p className="text-sm text-muted-foreground">
            Control de inventario de cuartos, pisos y cambios de estado.
          </p>
        </div>
        <Button
          onClick={() => {
            setShowForm(!showForm)
            setEditingId(null)
            setForm({
              roomNumber: '',
              floor: 1,
              roomTypeId: roomTypes[0]?.id || '',
              observations: '',
            })
          }}
        >
          {showForm ? 'Cancelar' : <><Plus size={16} className="mr-1.5" /> Nueva Habitación</>}
        </Button>
      </div>

      {alertInfo && (
        <InlineAlert
          variant={alertInfo.variant}
          message={alertInfo.message}
          onClose={() => setAlertInfo(null)}
        />
      )}

      {showForm && (
        <div className="border border-border rounded-xl p-5 bg-card space-y-4 shadow-sm animate-in fade-in">
          <h3 className="font-semibold text-foreground text-md">
            {editingId ? 'Editar Habitación' : 'Nueva Habitación'}
          </h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-3.5">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Número / Código *
              </label>
              <Input
                value={form.roomNumber}
                onChange={(e) => setForm({ ...form, roomNumber: e.target.value })}
                placeholder="Ej. 101"
                className="mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Piso
              </label>
              <Input
                type="number"
                value={form.floor}
                onChange={(e) => setForm({ ...form, floor: +e.target.value })}
                className="mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Tipo de Habitación *
              </label>
              <select
                value={form.roomTypeId}
                onChange={(e) => setForm({ ...form, roomTypeId: e.target.value })}
                className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1"
              >
                <option value="">Seleccionar tipo...</option>
                {roomTypes.map((rt) => (
                  <option key={rt.id} value={rt.id}>
                    {rt.name} — L {rt.pricePerNight.toFixed(2)}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Observaciones
              </label>
              <Input
                value={form.observations}
                onChange={(e) => setForm({ ...form, observations: e.target.value })}
                placeholder="Ej. Vista al mar, balcón"
                className="mt-1"
              />
            </div>
          </div>
          <div className="flex gap-2.5 pt-2">
            <Button onClick={saveRoom}>{editingId ? 'Actualizar Habitación' : 'Guardar Habitación'}</Button>
            <Button
              variant="outline"
              onClick={() => {
                setShowForm(false)
                setEditingId(null)
              }}
            >
              Cancelar
            </Button>
          </div>
        </div>
      )}

      <div className="flex flex-col sm:flex-row gap-2.5 items-stretch sm:items-center">
        <div className="relative flex-1 max-w-sm">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por número o tipo..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            className="pl-9"
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="border border-input rounded-md px-3 py-2 text-sm bg-background max-w-xs"
        >
          <option value="">Todos los estados</option>
          {Object.keys(statusColors).map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>

      {loading ? (
        <div className="flex flex-col items-center justify-center py-16 space-y-2">
          <Loader2 size={28} className="animate-spin text-[#C69C4B]" />
          <span className="text-sm text-muted-foreground">Cargando inventario de habitaciones...</span>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {filtered.map((room) => (
            <div
              key={room.id}
              className="border border-border rounded-xl p-4.5 bg-card space-y-3 shadow-xs hover:border-[#C69C4B]/60 transition-all flex flex-col justify-between"
            >
              <div className="space-y-2">
                <div className="flex justify-between items-start">
                  <div>
                    <span className="text-xl font-bold text-foreground">#{room.roomNumber}</span>
                    <span className="text-xs text-muted-foreground ml-2">Piso {room.floor}</span>
                  </div>
                  <span
                    className={`px-2.5 py-0.5 rounded-full text-xs font-semibold border ${
                      statusColors[room.status] || ''
                    }`}
                  >
                    {room.status}
                  </span>
                </div>

                <div className="space-y-1 text-sm">
                  <div className="font-medium text-foreground">{room.roomTypeName}</div>
                  <div className="text-sm font-bold text-[#C69C4B]">
                    L {room.pricePerNight.toFixed(2)} <span className="text-xs font-normal text-muted-foreground">/ noche</span>
                  </div>
                  <div className="text-xs text-muted-foreground">Capacidad: {room.capacity} persona(s)</div>
                  {room.observations && (
                    <div className="text-xs text-muted-foreground italic bg-accent/20 p-2 rounded-md border border-border/40 mt-1">
                      {room.observations}
                    </div>
                  )}
                </div>
              </div>

              <div className="space-y-2.5 pt-2 border-t border-border/60">
                <div className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">
                  Cambiar estado
                </div>
                <div className="flex gap-1 flex-wrap">
                  {['Libre', 'Ocupada', 'Limpieza', 'Mantenimiento', 'Bloqueada'].map((status) => (
                    <button
                      key={status}
                      onClick={() => changeStatus(room.id, status)}
                      className={`text-[11px] px-2 py-1 rounded-md cursor-pointer border transition-colors ${
                        room.status === status
                          ? 'bg-primary text-primary-foreground border-primary font-semibold'
                          : 'bg-background text-muted-foreground border-border hover:bg-accent hover:text-foreground'
                      }`}
                    >
                      {status}
                    </button>
                  ))}
                </div>

                <div className="flex gap-2 pt-1">
                  <Button size="sm" variant="outline" onClick={() => startEdit(room)} className="text-xs flex-1">
                    Editar
                  </Button>
                  <Button
                    size="sm"
                    variant="destructive"
                    onClick={() => setDeleteTarget(room)}
                    className="text-xs flex-1"
                  >
                    Eliminar
                  </Button>
                </div>
              </div>
            </div>
          ))}
          {filtered.length === 0 && (
            <div className="col-span-full p-8 text-center bg-card border border-border rounded-xl text-muted-foreground">
              No se encontraron habitaciones coincidentes.
            </div>
          )}
        </div>
      )}

      <ConfirmDialog
        isOpen={!!deleteTarget}
        title="Eliminar Habitación"
        description={`¿Está seguro de que desea eliminar la habitación #${deleteTarget?.roomNumber}? Esta acción no se puede deshacer.`}
        confirmText="Eliminar Habitación"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}
