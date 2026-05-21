import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { Guest } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

function sanitize(g: typeof defaultForm) {
  return {
    ...g,
    email: g.email || null, phone: g.phone || null, dateOfBirth: g.dateOfBirth || null,
    documentNumber: g.documentNumber || null, nationality: g.nationality || null,
    origin: g.origin || null, vehiclePlate: g.vehiclePlate || null, company: g.company || null,
    guestRTN: g.guestRTN || null, preferences: g.preferences || null,
    taxpayerType: g.taxpayerType || null,
    exonerationOrderNumber: g.exonerationOrderNumber || null,
    sefinExonerationCertificateNumber: g.sefinExonerationCertificateNumber || null,
    sagRegistryNumber: g.sagRegistryNumber || null,
  }
}

const defaultForm = {
  firstName: '', lastName: '', email: '', phone: '', dateOfBirth: '',
  nationality: '', documentType: 'DNI', documentNumber: '', origin: '',
  hasVehicle: false, vehiclePlate: '', company: '', guestRTN: '',
  preferences: '', classification: 'Normal',
  taxpayerType: 'ConsumidorFinal',
  exonerationOrderNumber: '', sefinExonerationCertificateNumber: '', sagRegistryNumber: ''
}

export default function GuestsPage() {
  const [guests, setGuests] = useState<Guest[]>([])
  const [search, setSearch] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(defaultForm)

  useEffect(() => { load() }, [])

  const load = async () => {
    const { data } = await api.get<Guest[]>('/guests', { params: { search: search || undefined } })
    setGuests(data)
  }

  const save = async () => {
    if (editingId) {
      await api.put(`/guests/${editingId}`, sanitize(form))
    } else {
      await api.post('/guests', sanitize(form))
    }
    setShowForm(false); setEditingId(null); setForm(defaultForm); load()
  }

  const startEdit = (g: Guest) => {
    setEditingId(g.id)
    setForm({
      firstName: g.firstName, lastName: g.lastName, email: g.email || '', phone: g.phone || '',
      dateOfBirth: g.dateOfBirth || '', nationality: g.nationality || '', documentType: g.documentType || 'DNI',
      documentNumber: g.documentNumber || '', origin: g.origin || '', hasVehicle: g.hasVehicle,
      vehiclePlate: g.vehiclePlate || '', company: g.company || '', guestRTN: g.guestRTN || '',
      preferences: g.preferences || '', classification: g.classification || 'Normal',
      taxpayerType: (g as any).taxpayerType || 'ConsumidorFinal',
      exonerationOrderNumber: (g as any).exonerationOrderNumber || '',
      sefinExonerationCertificateNumber: (g as any).sefinExonerationCertificateNumber || '',
      sagRegistryNumber: (g as any).sagRegistryNumber || ''
    })
    setShowForm(true)
  }

  const deleteGuest = async (id: string) => {
    if (confirm('¿Eliminar este huésped?')) {
      await api.delete(`/guests/${id}`)
      load()
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Huéspedes</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={async () => {
            const { data } = await api.get('/data/export/guests', { responseType: 'blob' })
            const url = window.URL.createObjectURL(new Blob([data]))
            const link = document.createElement('a'); link.href = url; link.setAttribute('download', 'huespedes.xlsx')
            document.body.appendChild(link); link.click(); document.body.removeChild(link); window.URL.revokeObjectURL(url)
          }}>Exportar XLSX</Button>
          <Button onClick={() => { setShowForm(!showForm); setEditingId(null); setForm(defaultForm) }}>Nuevo Huésped</Button>
        </div>
      </div>

      <div className="flex gap-2">
        <Input placeholder="Buscar por nombre o DNI..." value={search} onChange={e => setSearch(e.target.value)} className="max-w-xs" />
        <Button variant="outline" onClick={load}>Buscar</Button>
      </div>

      {showForm && (
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">{editingId ? 'Editar' : 'Nuevo'} Huésped</h3>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            <div><label className="text-sm">Nombres *</label><Input value={form.firstName} onChange={e => setForm({...form, firstName: e.target.value})} /></div>
            <div><label className="text-sm">Apellidos *</label><Input value={form.lastName} onChange={e => setForm({...form, lastName: e.target.value})} /></div>
            <div><label className="text-sm">DNI</label><Input value={form.documentNumber} onChange={e => setForm({...form, documentNumber: e.target.value})} /></div>
            <div><label className="text-sm">Nacionalidad</label><Input value={form.nationality} onChange={e => setForm({...form, nationality: e.target.value})} /></div>
            <div><label className="text-sm">Procedencia</label><Input value={form.origin} onChange={e => setForm({...form, origin: e.target.value})} /></div>
            <div><label className="text-sm">Teléfono</label><Input value={form.phone} onChange={e => setForm({...form, phone: e.target.value})} /></div>
            <div><label className="text-sm">Email</label><Input type="email" value={form.email} onChange={e => setForm({...form, email: e.target.value})} /></div>
            <div><label className="text-sm">Empresa</label><Input value={form.company} onChange={e => setForm({...form, company: e.target.value})} /></div>
            <div><label className="text-sm">RTN</label><Input value={form.guestRTN} onChange={e => setForm({...form, guestRTN: e.target.value})} /></div>
            <div className="flex items-center gap-2">
              <input type="checkbox" checked={form.hasVehicle} onChange={e => setForm({...form, hasVehicle: e.target.checked})} className="w-4 h-4" />
              <label className="text-sm">Tiene vehículo</label>
            </div>
            {form.hasVehicle && <div><label className="text-sm">Placa</label><Input value={form.vehiclePlate} onChange={e => setForm({...form, vehiclePlate: e.target.value})} /></div>}
            <div><label className="text-sm">Clasificación</label>
              <select value={form.classification} onChange={e => setForm({...form, classification: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                <option>Normal</option><option>VIP</option><option>Frecuente</option>
              </select>
            </div>
            <div><label className="text-sm">Tipo Contribuyente</label>
              <select value={form.taxpayerType} onChange={e => setForm({...form, taxpayerType: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                <option value="ConsumidorFinal">Consumidor Final</option>
                <option value="Gravado">Gravado</option>
                <option value="Exonerado">Exonerado</option>
              </select>
            </div>
            {form.taxpayerType === 'Exonerado' && (
              <>
                <div><label className="text-sm">O.C. Exenta</label><Input value={form.exonerationOrderNumber} onChange={e => setForm({...form, exonerationOrderNumber: e.target.value})} /></div>
                <div><label className="text-sm">Constancia SEFIN</label><Input value={form.sefinExonerationCertificateNumber} onChange={e => setForm({...form, sefinExonerationCertificateNumber: e.target.value})} /></div>
                <div><label className="text-sm">Registro SAG</label><Input value={form.sagRegistryNumber} onChange={e => setForm({...form, sagRegistryNumber: e.target.value})} /></div>
              </>
            )}
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
            <th className="text-left p-3">Nombre</th><th className="text-left p-3">DNI</th>
            <th className="text-left p-3">Nacionalidad</th><th className="text-left p-3">Procedencia</th>
            <th className="text-left p-3">Teléfono</th><th className="text-left p-3">Empresa</th><th className="text-left p-3">Acciones</th>
          </tr></thead>
          <tbody>
            {guests.map(g => (
              <tr key={g.id} className="border-t border-border">
                <td className="p-3">{g.fullName}</td>
                <td className="p-3">{g.documentNumber}</td>
                <td className="p-3">{g.nationality}</td>
                <td className="p-3">{g.origin}</td>
                <td className="p-3">{g.phone}</td>
                <td className="p-3">{g.company}</td>
                <td className="p-3 space-x-1">
                  <Button size="sm" variant="outline" onClick={() => startEdit(g)}>Editar</Button>
                  <Button size="sm" variant="destructive" onClick={() => deleteGuest(g.id)}>Eliminar</Button>
                </td>
              </tr>
            ))}
            {guests.length === 0 && <tr><td colSpan={7} className="p-6 text-center text-muted-foreground">No hay huéspedes registrados</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
