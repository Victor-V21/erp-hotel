import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { CAI } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export default function CAIPage() {
  const [cais, setCais] = useState<CAI[]>([])
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ caiNumber: '', issueDate: '', dueDate: '', initialRange: '', finalRange: '' })
  const [error, setError] = useState('')

  useEffect(() => { load() }, [])

  const load = async () => {
    const { data } = await api.get<CAI[]>('/cai')
    setCais(data)
  }

  const save = async () => {
    setError('')
    if (!form.caiNumber || !form.issueDate || !form.dueDate || !form.initialRange || !form.finalRange) {
      setError('Todos los campos son obligatorios')
      return
    }
    if (form.issueDate > form.dueDate) {
      setError('La fecha de emisión no puede ser posterior a la fecha de vencimiento')
      return
    }
    await api.post('/cai', form)
    setShowForm(false)
    setForm({ caiNumber: '', issueDate: '', dueDate: '', initialRange: '', finalRange: '' })
    load()
  }

  const deleteCAI = async (id: string) => {
    if (confirm('¿Eliminar este CAI?')) {
      await api.delete(`/cai/${id}`)
      load()
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">CAI - Autorización SAR</h1>
        <Button onClick={() => setShowForm(!showForm)}>Nuevo CAI</Button>
      </div>

      {showForm && (
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">Nuevo CAI</h3>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-sm">Número CAI *</label><Input value={form.caiNumber} onChange={e => setForm({...form, caiNumber: e.target.value})} /></div>
            <div><label className="text-sm">Fecha Emisión *</label><Input type="date" value={form.issueDate} onChange={e => setForm({...form, issueDate: e.target.value})} /></div>
            <div><label className="text-sm">Fecha Vencimiento *</label><Input type="date" value={form.dueDate} onChange={e => setForm({...form, dueDate: e.target.value})} /></div>
            <div><label className="text-sm">Rango Inicial *</label><Input value={form.initialRange} onChange={e => setForm({...form, initialRange: e.target.value})} placeholder="000-001-01-00000001" /></div>
            <div><label className="text-sm">Rango Final *</label><Input value={form.finalRange} onChange={e => setForm({...form, finalRange: e.target.value})} placeholder="000-001-01-00001000" /></div>
          </div>
          {error && <div className="text-sm text-red-600 p-2 bg-red-50 rounded">{error}</div>}
          <div className="flex gap-2"><Button onClick={save}>Guardar</Button><Button variant="outline" onClick={() => setShowForm(false)}>Cancelar</Button></div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {cais.map(c => (
          <div key={c.id} className={`border rounded-lg p-4 ${c.isExpiringSoon ? 'border-red-300 bg-red-50' : 'border-border bg-card'}`}>
            <div className="flex justify-between">
              <div className="font-mono font-bold">{c.caiNumber}</div>
              <span className={`px-2 py-0.5 rounded text-xs font-medium ${c.status === 'Activo' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>{c.status}</span>
            </div>
            <div className="text-sm mt-1">Vence: {c.dueDate} {c.isExpiringSoon && <span className="text-red-600 font-bold">(PRÓXIMO A VENCER)</span>}</div>
            <div className="text-sm text-muted-foreground">Rango: {c.initialRange} → {c.finalRange}</div>
            <div className="text-sm text-muted-foreground">Actual: {c.currentCorrelative}</div>
            <Button variant="destructive" size="sm" className="mt-2" onClick={() => deleteCAI(c.id)}>Eliminar</Button>
          </div>
        ))}
      </div>
    </div>
  )
}
