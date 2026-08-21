import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { Discount } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { Percent, Plus, Edit2, Trash2 } from 'lucide-react'

const emptyForm = {
  name: '',
  description: '',
  discountType: 'Porcentaje',
  value: 0,
  isActive: true,
  applicableTo: 'Todo',
  requiresDocument: false,
  minAge: 0,
  priority: 0,
}

export default function DiscountsPage() {
  const [discounts, setDiscounts] = useState<Discount[]>([])
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(emptyForm)
  const [deleteTarget, setDeleteTarget] = useState<Discount | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success'; message: string } | null>(null)

  useEffect(() => {
    load()
  }, [])

  const load = async () => {
    try {
      const { data } = await api.get<Discount[]>('/discounts')
      setDiscounts(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al cargar el catálogo de descuentos.' })
    }
  }

  const save = async () => {
    if (!form.name.trim()) {
      setAlertInfo({ variant: 'error', message: 'El nombre del descuento es requerido.' })
      return
    }
    try {
      if (editingId) {
        await api.put(`/discounts/${editingId}`, form)
        setAlertInfo({ variant: 'success', message: 'Descuento actualizado correctamente.' })
      } else {
        await api.post('/discounts', form)
        setAlertInfo({ variant: 'success', message: 'Descuento registrado exitosamente.' })
      }
      setShowForm(false)
      setEditingId(null)
      setForm(emptyForm)
      load()
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al guardar el descuento.' })
    }
  }

  const startEdit = (d: Discount) => {
    setEditingId(d.id)
    setForm({
      name: d.name,
      description: d.description || '',
      discountType: d.discountType,
      value: d.value,
      isActive: d.isActive,
      applicableTo: d.applicableTo || 'Todo',
      requiresDocument: d.requiresDocument,
      minAge: d.minAge || 0,
      priority: d.priority,
    })
    setShowForm(true)
  }

  const handleDeleteConfirm = async () => {
    if (!deleteTarget) return
    try {
      await api.delete(`/discounts/${deleteTarget.id}`)
      setAlertInfo({ variant: 'success', message: `Descuento "${deleteTarget.name}" eliminado.` })
      setDeleteTarget(null)
      load()
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al eliminar el descuento.' })
    }
  }

  const toggleActive = async (d: Discount) => {
    try {
      const payload = {
        name: d.name,
        description: d.description || null,
        discountType: d.discountType,
        value: d.value,
        isActive: !d.isActive,
        applicableTo: d.applicableTo || 'Todo',
        requiresDocument: d.requiresDocument,
        minAge: d.minAge || 0,
        priority: d.priority,
      }
      await api.put(`/discounts/${d.id}`, payload)
      load()
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al actualizar el estado del descuento.' })
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <Percent className="text-[#C69C4B]" size={24} />
            Descuentos y Tarifas Especiales
          </h1>
          <p className="text-sm text-muted-foreground">
            Configuración de descuentos para huéspedes, tercera edad y promociones.
          </p>
        </div>
        <Button
          onClick={() => {
            setShowForm(!showForm)
            setEditingId(null)
            setForm(emptyForm)
          }}
        >
          {showForm ? 'Cancelar' : <><Plus size={16} className="mr-1.5" /> Nuevo Descuento</>}
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
            {editingId ? 'Editar Descuento' : 'Nuevo Descuento'}
          </h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3.5">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Nombre *
              </label>
              <Input
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="Ej. Tercera Edad (25%)"
                className="mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Descripción
              </label>
              <Input
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                placeholder="Detalle o justificación"
                className="mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Tipo
              </label>
              <select
                value={form.discountType}
                onChange={(e) => setForm({ ...form, discountType: e.target.value })}
                className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1"
              >
                <option value="Porcentaje">Porcentaje (%)</option>
                <option value="MontoFijo">Monto Fijo (L)</option>
              </select>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                {form.discountType === 'Porcentaje' ? 'Valor (%)' : 'Valor en Lempiras (L)'}
              </label>
              <Input
                type="number"
                step="0.01"
                value={form.value}
                onChange={(e) => setForm({ ...form, value: +e.target.value })}
                className="mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Aplica a
              </label>
              <select
                value={form.applicableTo}
                onChange={(e) => setForm({ ...form, applicableTo: e.target.value })}
                className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1"
              >
                <option value="Todo">Todo (Hospedaje + Consumos)</option>
                <option value="Hospedaje">Solo Hospedaje</option>
              </select>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Prioridad
              </label>
              <Input
                type="number"
                value={form.priority}
                onChange={(e) => setForm({ ...form, priority: +e.target.value })}
                className="mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Edad mínima requerida
              </label>
              <Input
                type="number"
                value={form.minAge || ''}
                onChange={(e) => setForm({ ...form, minAge: e.target.value ? +e.target.value : 0 })}
                placeholder="Ej. 60"
                className="mt-1"
              />
            </div>
            <div className="flex items-center gap-2 pt-6">
              <input
                type="checkbox"
                id="reqDoc"
                checked={form.requiresDocument}
                onChange={(e) => setForm({ ...form, requiresDocument: e.target.checked })}
                className="w-4 h-4 accent-primary rounded cursor-pointer"
              />
              <label htmlFor="reqDoc" className="text-sm font-medium text-foreground cursor-pointer">
                Requiere comprobante / DNI
              </label>
            </div>
          </div>
          <div className="flex gap-2.5 pt-2">
            <Button onClick={save}>{editingId ? 'Actualizar Descuento' : 'Guardar Descuento'}</Button>
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

      <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/40 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              <tr>
                <th className="text-left p-3.5">Nombre</th>
                <th className="text-left p-3.5">Tipo</th>
                <th className="text-right p-3.5">Valor</th>
                <th className="text-left p-3.5">Aplica a</th>
                <th className="text-left p-3.5">Estado</th>
                <th className="text-center p-3.5">Prioridad</th>
                <th className="text-right p-3.5">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60">
              {discounts.map((d) => (
                <tr key={d.id} className="hover:bg-accent/30 transition-colors">
                  <td className="p-3.5 font-medium text-foreground">
                    <div>{d.name}</div>
                    {d.description && <div className="text-xs text-muted-foreground">{d.description}</div>}
                  </td>
                  <td className="p-3.5 text-xs text-muted-foreground">
                    {d.discountType === 'Porcentaje' ? 'Porcentual' : 'Monto Fijo'}
                  </td>
                  <td className="p-3.5 text-right font-bold text-foreground">
                    {d.discountType === 'Porcentaje' ? `${d.value}%` : `L ${d.value.toFixed(2)}`}
                  </td>
                  <td className="p-3.5 text-xs text-muted-foreground">{d.applicableTo || 'Todo'}</td>
                  <td className="p-3.5">
                    <button
                      onClick={() => toggleActive(d)}
                      className={`px-2.5 py-0.5 rounded-full text-xs font-medium cursor-pointer transition-colors border ${
                        d.isActive
                          ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300 border-emerald-300 dark:border-emerald-800 hover:bg-emerald-200'
                          : 'bg-muted text-muted-foreground border-border hover:bg-accent'
                      }`}
                    >
                      {d.isActive ? 'Activo' : 'Inactivo'}
                    </button>
                  </td>
                  <td className="p-3.5 text-center text-xs font-mono">{d.priority}</td>
                  <td className="p-3.5 text-right space-x-1.5">
                    <Button size="sm" variant="outline" onClick={() => startEdit(d)} title="Editar">
                      <Edit2 size={13} />
                    </Button>
                    <Button
                      size="sm"
                      variant="destructive"
                      onClick={() => setDeleteTarget(d)}
                      title="Eliminar"
                    >
                      <Trash2 size={13} />
                    </Button>
                  </td>
                </tr>
              ))}
              {discounts.length === 0 && (
                <tr>
                  <td colSpan={7} className="p-8 text-center text-muted-foreground">
                    No hay descuentos registrados.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <ConfirmDialog
        isOpen={!!deleteTarget}
        title="Eliminar Descuento"
        description={`¿Está seguro de que desea eliminar el descuento "${deleteTarget?.name}"? Esta acción no se puede deshacer.`}
        confirmText="Eliminar"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}

