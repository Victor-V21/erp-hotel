import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { Room, RoomType } from '@/types'
import { Bed, BedDouble, Users, X, Loader2, RotateCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { InlineAlert } from '@/components/ui/InlineAlert'

const statusColors: Record<string, string> = {
  Libre: '#22c55e',
  Ocupada: '#ef4444',
  Reservada: '#eab308',
  Mantenimiento: '#6b7280',
}

const roomTypeIcons: Record<string, typeof Bed> = {
  individual: Bed,
  double: BedDouble,
  quadruple: Users,
}

export default function FloorMapPage() {
  const [rooms, setRooms] = useState<Room[]>([])
  const [roomTypes, setRoomTypes] = useState<RoomType[]>([])
  const [selectedRoom, setSelectedRoom] = useState<Room | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    load()
  }, [])

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const [r, rt] = await Promise.all([
        api.get<Room[]>('/rooms'),
        api.get<RoomType[]>('/room-types'),
      ])
      setRooms(r.data)
      setRoomTypes(rt.data)
    } catch (err) {
      console.error('Error al cargar mapa de habitaciones', err)
      setError('No fue posible cargar el mapa de habitaciones. Verifique su conexión al servidor.')
    } finally {
      setLoading(false)
    }
  }

  const floors = rooms.reduce<Record<number, Room[]>>((acc, room) => {
    if (!acc[room.floor]) acc[room.floor] = []
    acc[room.floor].push(room)
    return acc
  }, {})

  const sortedFloors = Object.keys(floors)
    .map(Number)
    .sort((a, b) => a - b)

  const getRoomTypeIcon = (typeName: string) => {
    const lower = (typeName || '').toLowerCase()
    for (const [key, Icon] of Object.entries(roomTypeIcons)) {
      if (lower.includes(key)) return Icon
    }
    return Bed
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight">Mapa de Habitaciones</h1>
          <p className="text-sm text-muted-foreground">Distribución espacial y estado en tiempo real por piso.</p>
        </div>

        <div className="flex items-center gap-3 flex-wrap">
          <div className="flex gap-3 text-xs sm:text-sm bg-card p-2 rounded-lg border border-border">
            {Object.entries(statusColors).map(([status, color]) => (
              <div key={status} className="flex items-center gap-1.5">
                <div className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: color }} />
                <span className="text-muted-foreground font-medium">{status}</span>
              </div>
            ))}
          </div>

          <Button variant="outline" size="sm" onClick={load} disabled={loading}>
            <RotateCw size={14} className={loading ? 'animate-spin mr-1.5' : 'mr-1.5'} />
            Actualizar
          </Button>
        </div>
      </div>

      {error && (
        <InlineAlert
          variant="error"
          title="Error al cargar el mapa"
          message={error}
          onClose={() => setError(null)}
        />
      )}

      {loading ? (
        <div className="flex flex-col items-center justify-center py-20 space-y-3">
          <Loader2 size={32} className="animate-spin text-[#C69C4B]" />
          <p className="text-sm text-muted-foreground">Cargando distribución de pisos y habitaciones...</p>
        </div>
      ) : sortedFloors.length === 0 && !error ? (
        <div className="p-8 text-center bg-card border border-border rounded-xl space-y-2">
          <p className="text-muted-foreground">No hay habitaciones registradas en el sistema.</p>
          <Button onClick={() => window.location.href = '/rooms'} variant="outline" size="sm">
            Ir a Gestión de Habitaciones
          </Button>
        </div>
      ) : (
        sortedFloors.map((floor) => (
          <div key={floor} className="space-y-3 bg-card/40 border border-border/70 p-4 rounded-xl">
            <h2 className="text-md font-semibold border-b border-border/80 pb-2 text-foreground flex items-center justify-between">
              <span>Piso {floor}</span>
              <span className="text-xs font-normal text-muted-foreground">
                {floors[floor].length} habitación(es)
              </span>
            </h2>
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 xl:grid-cols-8 gap-3 pt-1">
              {floors[floor].map((room) => {
                const bgColor = statusColors[room.status] || '#6b7280'
                const Icon = getRoomTypeIcon(room.roomTypeName)
                return (
                  <button
                    key={room.id}
                    onClick={() => setSelectedRoom(room)}
                    className="rounded-xl p-3.5 text-white cursor-pointer transition-all duration-200 hover:scale-[1.03] hover:shadow-lg flex flex-col items-center gap-1.5 shadow-sm border border-white/10"
                    style={{ backgroundColor: bgColor }}
                  >
                    <span className="text-lg font-bold tracking-tight">#{room.roomNumber}</span>
                    <Icon size={18} className="opacity-95" />
                    <span className="text-[11px] opacity-90 font-medium truncate max-w-full text-center">
                      {room.roomTypeName || 'Estándar'}
                    </span>
                  </button>
                )
              })}
            </div>
          </div>
        ))
      )}

      {selectedRoom && (
        <div
          className="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center z-50 p-4 animate-in fade-in"
          onClick={() => setSelectedRoom(null)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-md shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between pb-2 border-b border-border">
              <h3 className="text-xl font-bold text-foreground">
                Habitación #{selectedRoom.roomNumber}
              </h3>
              <button
                onClick={() => setSelectedRoom(null)}
                className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent transition-colors cursor-pointer"
              >
                <X size={18} />
              </button>
            </div>
            <div className="space-y-2.5 text-sm">
              <div className="flex justify-between items-center py-1">
                <span className="text-muted-foreground">Estado actual</span>
                <span
                  className="font-medium px-2.5 py-0.5 rounded-full text-white text-xs shadow-xs"
                  style={{ backgroundColor: statusColors[selectedRoom.status] || '#6b7280' }}
                >
                  {selectedRoom.status}
                </span>
              </div>
              <div className="flex justify-between py-1 border-t border-border/50">
                <span className="text-muted-foreground">Tipo de habitación</span>
                <span className="font-medium text-foreground">{selectedRoom.roomTypeName}</span>
              </div>
              <div className="flex justify-between py-1 border-t border-border/50">
                <span className="text-muted-foreground">Piso</span>
                <span className="font-medium text-foreground">{selectedRoom.floor}</span>
              </div>
              <div className="flex justify-between py-1 border-t border-border/50">
                <span className="text-muted-foreground">Capacidad</span>
                <span className="font-medium text-foreground">{selectedRoom.capacity} personas</span>
              </div>
              <div className="flex justify-between py-1 border-t border-border/50">
                <span className="text-muted-foreground">Tarifa por noche</span>
                <span className="font-bold text-[#C69C4B] text-base">
                  L {selectedRoom.pricePerNight.toFixed(2)}
                </span>
              </div>
              {selectedRoom.observations && (
                <div className="pt-2 border-t border-border space-y-1">
                  <span className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">
                    Observaciones
                  </span>
                  <p className="text-xs text-muted-foreground/90 italic bg-accent/20 p-2.5 rounded-lg border border-border/40">
                    {selectedRoom.observations}
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

