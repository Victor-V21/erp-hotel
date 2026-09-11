import { useState, useEffect, useCallback } from 'react'
import api from '@/lib/axios'
import type { RoomType } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { useTaxRates } from '@/hooks/useTaxRates'

function breakdown(price: number, isvRate: number, touristTaxRate: number) {
  const taxFactor = 1 + isvRate + touristTaxRate
  const subtotal = price / taxFactor
  return {
    subtotal: Math.round(subtotal * 100) / 100,
    isv: Math.round(subtotal * isvRate * 100) / 100,
    tourist: Math.round(subtotal * touristTaxRate * 100) / 100,
    total: price
  }
}

export default function RoomTypesPage() {
  const { isvRate, touristTaxRate } = useTaxRates()
  const [types, setTypes] = useState<RoomType[]>([])
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState({ name: '', description: '', pricePerNight: 0, capacity: 1 })
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const { data } = await api.get<RoomType[]>('/room-types')
      setTypes(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al cargar tipos de habitación' })
    }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0)
    return () => window.clearTimeout(timer)
  }, [load])

  const save = async () => {
    try {
      if (editingId) {
        await api.put(`/room-types/${editingId}`, { name: form.name, description: form.description || null, pricePerNight: form.pricePerNight, capacity: form.capacity })
      } else {
        await api.post('/room-types', { name: form.name, description: form.description || null, pricePerNight: form.pricePerNight, capacity: form.capacity })
      }
      setShowForm(false); setEditingId(null)
      setForm({ name: '', description: '', pricePerNight: 0, capacity: 1 })
      load()
      setAlertInfo({ variant: 'success', message: 'Tipo de habitación guardado correctamente' })
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al guardar tipo de habitación' })
    }
  }

  const startEdit = (t: RoomType) => {
    setEditingId(t.id)
    setForm({ name: t.name, description: t.description || '', pricePerNight: t.pricePerNight, capacity: t.capacity })
    setShowForm(true)
  }

  const confirmDelete = async () => {
    if (!confirmDeleteId) return
    try {
      await api.delete(`/room-types/${confirmDeleteId}`)
      load()
      setAlertInfo({ variant: 'success', message: 'Tipo de habitación eliminado correctamente' })
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al eliminar tipo de habitación' })
    } finally {
      setConfirmDeleteId(null)
    }
  }

  const bd = breakdown(form.pricePerNight, isvRate, touristTaxRate)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Tipos de Habitación</h1>
        <Button onClick={() => { setShowForm(!showForm); setEditingId(null); setForm({ name: '', description: '', pricePerNight: 0, capacity: 1 }) }}>
          {showForm ? 'Cancelar' : 'Nuevo Tipo'}
        </Button>
      </div>

      {alertInfo && (
        <InlineAlert variant={alertInfo.variant} message={alertInfo.message} onClose={() => setAlertInfo(null)} />
      )}

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
                <span>ISV {(isvRate * 100).toFixed(0)}%:</span><span className="text-right font-mono">L {bd.isv.toFixed(2)}</span>
                <span>Tasa Turística {(touristTaxRate * 100).toFixed(0)}%:</span><span className="text-right font-mono">L {bd.tourist.toFixed(2)}</span>
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
          const b = breakdown(t.pricePerNight, isvRate, touristTaxRate)
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
                <Button size="sm" variant="destructive" onClick={() => setConfirmDeleteId(t.id)}>Eliminar</Button>
              </div>
            </div>
          )
        })}
      </div>

      <ConfirmDialog
        isOpen={!!confirmDeleteId}
        title="Eliminar Tipo de Habitación"
        description="¿Está seguro de que desea eliminar este tipo de habitación? Esta acción no se puede deshacer."
        confirmText="Eliminar"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={confirmDelete}
        onCancel={() => setConfirmDeleteId(null)}
      />
    </div>
  )
}
