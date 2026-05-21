import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { DocumentAuthorization } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const docTypes = ['Factura', 'NotaCredito', 'NotaDebito']

const defaultForm = { documentType: 'Factura', caiNumber: '', issueDate: '', dueDate: '', initialRange: '', finalRange: '' }

export default function DocumentAuthorizationsPage() {
  const [items, setItems] = useState<DocumentAuthorization[]>([])
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState(defaultForm)
  const [error, setError] = useState('')

  useEffect(() => { load() }, [])

  const load = async () => {
    const { data } = await api.get<DocumentAuthorization[]>('/document-authorizations')
    setItems(data)
  }

  const save = async () => {
    setError('')
    if (!form.caiNumber || !form.issueDate || !form.dueDate || !form.initialRange || !form.finalRange) {
      setError('Todos los campos son obligatorios'); return
    }
    if (form.issueDate > form.dueDate) {
      setError('La fecha de emisión no puede ser posterior a la fecha de vencimiento'); return
    }
    await api.post('/document-authorizations', form)
    setShowForm(false); setForm(defaultForm); load()
  }

  const deleteItem = async (id: string) => {
    if (confirm('¿Eliminar esta autorización?')) {
      await api.delete(`/document-authorizations/${id}`)
      load()
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Autorizaciones Fiscales por Tipo de Documento</h1>
        <Button onClick={() => setShowForm(!showForm)}>Nueva Autorización</Button>
      </div>

      {showForm && (
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">Nueva Autorización Fiscal</h3>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-sm">Tipo Documento *</label>
              <select value={form.documentType} onChange={e => setForm({...form, documentType: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                {docTypes.map(d => <option key={d}>{d}</option>)}
              </select>
            </div>
            <div><label className="text-sm">Número CAI *</label><Input value={form.caiNumber} onChange={e => setForm({...form, caiNumber: e.target.value})} /></div>
            <div><label className="text-sm">Fecha Emisión *</label><Input type="date" value={form.issueDate} onChange={e => setForm({...form, issueDate: e.target.value})} /></div>
            <div><label className="text-sm">Fecha Vencimiento *</label><Input type="datetime-local" value={form.dueDate} onChange={e => setForm({...form, dueDate: e.target.value})} /></div>
            <div><label className="text-sm">Rango Inicial *</label><Input value={form.initialRange} onChange={e => setForm({...form, initialRange: e.target.value})} placeholder="000-001-01-00000001" /></div>
            <div><label className="text-sm">Rango Final *</label><Input value={form.finalRange} onChange={e => setForm({...form, finalRange: e.target.value})} placeholder="000-001-01-00001000" /></div>
          </div>
          {error && <div className="text-sm text-red-600 p-2 bg-red-50 rounded">{error}</div>}
          <div className="flex gap-2"><Button onClick={save}>Guardar</Button><Button variant="outline" onClick={() => setShowForm(false)}>Cancelar</Button></div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {items.map(a => (
          <div key={a.id} className={`border rounded-lg p-4 ${a.isExpiringSoon ? 'border-red-300 bg-red-50' : 'border-border bg-card'}`}>
            <div className="flex justify-between">
              <div><span className="text-xs font-medium bg-primary/10 px-2 py-0.5 rounded">{a.documentType}</span> <span className="font-mono font-bold">{a.caiNumber}</span></div>
              <span className={`px-2 py-0.5 rounded text-xs font-medium ${a.status === 'Activo' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>{a.status}</span>
            </div>
            <div className="text-sm mt-1">Vence: {new Date(a.dueDate).toLocaleDateString()} {a.isExpiringSoon && <span className="text-red-600 font-bold">(PRÓXIMO A VENCER)</span>}</div>
            <div className="text-sm text-muted-foreground">Rango: {a.initialRange} → {a.finalRange}</div>
            <div className="text-sm text-muted-foreground">Actual: {a.currentCorrelative}</div>
            <Button variant="destructive" size="sm" className="mt-2" onClick={() => deleteItem(a.id)}>Eliminar</Button>
          </div>
        ))}
        {items.length === 0 && <div className="col-span-2 p-6 text-center text-muted-foreground">No hay autorizaciones fiscales registradas</div>}
      </div>
    </div>
  )
}
