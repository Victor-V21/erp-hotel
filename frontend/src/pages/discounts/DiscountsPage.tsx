import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

interface Discount {
  id: string; name: string; description?: string; discountType: string
  value: number; isActive: boolean; applicableTo?: string
  requiresDocument: boolean; minAge?: number; priority: number
}

const emptyForm = {
  name: '', description: '', discountType: 'Porcentaje', value: 0,
  isActive: true, applicableTo: 'Todo', requiresDocument: false, minAge: 0, priority: 0
}

export default function DiscountsPage() {
  const [discounts, setDiscounts] = useState<Discount[]>([])
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(emptyForm)

  useEffect(() => { load() }, [])

  const load = async () => {
    const { data } = await api.get<Discount[]>('/discounts')
    setDiscounts(data)
  }

  const save = async () => {
    if (editingId) {
      await api.put(`/discounts/${editingId}`, form)
    } else {
      await api.post('/discounts', form)
    }
    setShowForm(false); setEditingId(null); setForm(emptyForm); load()
  }

  const startEdit = (d: Discount) => {
    setEditingId(d.id); setForm({ name: d.name, description: d.description || '', discountType: d.discountType, value: d.value, isActive: d.isActive, applicableTo: d.applicableTo || 'Todo', requiresDocument: d.requiresDocument, minAge: d.minAge || 0, priority: d.priority }); setShowForm(true)
  }

  const deleteDiscount = async (id: string) => {
    if (confirm('¿Eliminar este descuento?')) {
      await api.delete(`/discounts/${id}`)
      load()
    }
  }

  const toggleActive = async (d: Discount) => {
    await api.put(`/discounts/${d.id}`, { isActive: !d.isActive })
    load()
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Descuentos</h1>
        <Button onClick={() => { setShowForm(!showForm); setEditingId(null); setForm(emptyForm) }}>
          {showForm ? 'Cancelar' : 'Nuevo Descuento'}
        </Button>
      </div>

      {showForm && (
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">{editingId ? 'Editar' : 'Nuevo'} Descuento</h3>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            <div><label className="text-sm">Nombre *</label><Input value={form.name} onChange={e => setForm({...form, name: e.target.value})} /></div>
            <div><label className="text-sm">Descripción</label><Input value={form.description} onChange={e => setForm({...form, description: e.target.value})} /></div>
            <div><label className="text-sm">Tipo</label>
              <select value={form.discountType} onChange={e => setForm({...form, discountType: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                <option value="Porcentaje">Porcentaje (%)</option><option value="MontoFijo">Monto Fijo (L)</option>
              </select>
            </div>
            <div><label className="text-sm">{form.discountType === 'Porcentaje' ? 'Valor (%)' : 'Valor (L)'}</label>
              <Input type="number" step="0.01" value={form.value} onChange={e => setForm({...form, value: +e.target.value})} />
            </div>
            <div><label className="text-sm">Aplica a</label>
              <select value={form.applicableTo} onChange={e => setForm({...form, applicableTo: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                <option value="Todo">Todo</option><option value="Hospedaje">Hospedaje</option>
              </select>
            </div>
            <div><label className="text-sm">Prioridad</label><Input type="number" value={form.priority} onChange={e => setForm({...form, priority: +e.target.value})} /></div>
            <div><label className="text-sm">Edad mínima</label><Input type="number" value={form.minAge || ''} onChange={e => setForm({...form, minAge: e.target.value ? +e.target.value : 0})} /></div>
            <div className="flex items-center gap-2 pt-6">
              <input type="checkbox" checked={form.requiresDocument} onChange={e => setForm({...form, requiresDocument: e.target.checked})} className="w-4 h-4" />
              <label className="text-sm">Requiere documento</label>
            </div>
          </div>
          <div className="flex gap-2">
            <Button onClick={save}>{editingId ? 'Actualizar' : 'Guardar'}</Button>
            <Button variant="outline" onClick={() => { setShowForm(false); setEditingId(null) }}>Cancelar</Button>
          </div>
        </div>
      )}

      <div className="border border-border rounded-lg overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted"><tr>
            <th className="text-left p-3">Nombre</th><th className="text-left p-3">Tipo</th><th className="text-right p-3">Valor</th>
            <th className="text-left p-3">Aplica a</th><th className="text-left p-3">Estado</th><th className="text-left p-3">Prioridad</th><th className="text-left p-3">Acciones</th>
          </tr></thead>
          <tbody>
            {discounts.map(d => (
              <tr key={d.id} className="border-t border-border">
                <td className="p-3 font-medium">{d.name}</td>
                <td className="p-3">{d.discountType === 'Porcentaje' ? '%' : 'L'}</td>
                <td className="p-3 text-right">{d.discountType === 'Porcentaje' ? `${d.value}%` : `L ${d.value.toFixed(2)}`}</td>
                <td className="p-3">{d.applicableTo || 'Todo'}</td>
                <td className="p-3">
                  <button onClick={() => toggleActive(d)} className={`px-2 py-0.5 rounded text-xs font-medium cursor-pointer ${d.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-500'}`}>
                    {d.isActive ? 'Activo' : 'Inactivo'}
                  </button>
                </td>
                <td className="p-3">{d.priority}</td>
                <td className="p-3 space-x-1">
                  <Button size="sm" variant="outline" onClick={() => startEdit(d)}>Editar</Button>
                  <Button size="sm" variant="destructive" onClick={() => deleteDiscount(d.id)}>Eliminar</Button>
                </td>
              </tr>
            ))}
            {discounts.length === 0 && <tr><td colSpan={7} className="p-6 text-center text-muted-foreground">No hay descuentos registrados</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
