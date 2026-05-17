import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { RoomType } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const ISV_RATE = 0.15
const TOURIST_RATE = 0.04
const TAX_FACTOR = 1 + ISV_RATE + TOURIST_RATE

function breakdown(price: number) {
  const subtotal = price / TAX_FACTOR
  return {
    subtotal: Math.round(subtotal * 100) / 100,
    isv: Math.round(subtotal * ISV_RATE * 100) / 100,
    tourist: Math.round(subtotal * TOURIST_RATE * 100) / 100,
    total: price
  }
}

export default function RoomTypesPage() {
  const [types, setTypes] = useState<RoomType[]>([])
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState({ name: '', description: '', pricePerNight: 0, capacity: 1 })

  useEffect(() => { load() }, [])

  const load = async () => {
    const { data } = await api.get<RoomType[]>('/room-types')
    setTypes(data)
  }

  const save = async () => {
    if (editingId) {
      await api.put(`/room-types/${editingId}`, { name: form.name, description: form.description || null, pricePerNight: form.pricePerNight, capacity: form.capacity })
    } else {
      await api.post('/room-types', { name: form.name, description: form.description || null, pricePerNight: form.pricePerNight, capacity: form.capacity })
    }
    setShowForm(false); setEditingId(null)
    setForm({ name: '', description: '', pricePerNight: 0, capacity: 1 })
    load()
  }

  const startEdit = (t: RoomType) => {
    setEditingId(t.id)
    setForm({ name: t.name, description: t.description || '', pricePerNight: t.pricePerNight, capacity: t.capacity })
    setShowForm(true)
  }

  const deleteType = async (id: string) => {
    if (confirm('¿Eliminar este tipo de habitación?')) {
      await api.delete(`/room-types/${id}`)
      load()
    }
  }

  const bd = breakdown(form.pricePerNight)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Tipos de Habitación</h1>
        <Button onClick={() => { setShowForm(!showForm); setEditingId(null); setForm({ name: '', description: '', pricePerNight: 0, capacity: 1 }) }}>
          {showForm ? 'Cancelar' : 'Nuevo Tipo'}
        </Button>
      </div>

      {showForm && (
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">{editingId ? 'Editar' : 'Nuevo'} Tipo de Habitación</h3>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-sm">Nombre *</label><Input value={form.name} onChange={e => setForm({...form, name: e.target.value})} /></div>
            <div><label className="text-sm">Descripción</label><Input value={form.description} onChange={e => setForm({...form, description: e.target.value})} /></div>
            <div>
              <label className="text-sm">Precio al Huésped (L) *</label>
              <Input type="number" step="0.01" value={form.pricePerNight} onChange={e => setForm({...form, pricePerNight: +e.target.value})} />
            </div>
            <div><label className="text-sm">Capacidad</label><Input type="number" value={form.capacity} onChange={e => setForm({...form, capacity: +e.target.value})} /></div>
          </div>

          {form.pricePerNight > 0 && (
            <div className="bg-muted p-3 rounded text-sm space-y-1">
              <p className="font-semibold">Desglose de Precio por Noche:</p>
              <div className="grid grid-cols-2 gap-x-4 gap-y-1">
                <span>Subtotal base:</span><span className="text-right font-mono">L {bd.subtotal.toFixed(2)}</span>
                <span>ISV 15%:</span><span className="text-right font-mono">L {bd.isv.toFixed(2)}</span>
                <span>Tasa Turística 4%:</span><span className="text-right font-mono">L {bd.tourist.toFixed(2)}</span>
                <span className="font-bold border-t pt-1">TOTAL AL HUÉSPED:</span><span className="text-right font-mono font-bold border-t pt-1">L {bd.total.toFixed(2)}</span>
              </div>
            </div>
          )}

          <div className="flex gap-2">
            <Button onClick={save}>{editingId ? 'Actualizar' : 'Guardar'}</Button>
            <Button variant="outline" onClick={() => { setShowForm(false); setEditingId(null) }}>Cancelar</Button>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        {types.map(t => {
          const b = breakdown(t.pricePerNight)
          return (
            <div key={t.id} className="border border-border rounded-lg p-4 bg-card">
              <div className="font-bold">{t.name}</div>
              <div className="text-sm text-muted-foreground">{t.description}</div>
              <div className="text-lg font-bold mt-2">L {t.pricePerNight.toFixed(2)}</div>
              <div className="text-xs text-muted-foreground">
                Base: L {b.subtotal.toFixed(2)} | ISV: L {b.isv.toFixed(2)} | Turíst.: L {b.tourist.toFixed(2)}
              </div>
              <div className="text-sm">Capacidad: {t.capacity} personas</div>
              <div className="flex gap-2 mt-2">
                <Button size="sm" variant="outline" onClick={() => startEdit(t)}>Editar</Button>
                <Button size="sm" variant="destructive" onClick={() => deleteType(t.id)}>Eliminar</Button>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
