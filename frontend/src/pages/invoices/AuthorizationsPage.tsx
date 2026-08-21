import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import api from '@/lib/axios'
import type { CAI, DocumentAuthorization } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

interface UnifiedAuth {
  id: string
  source: 'cai' | 'document'
  documentType: string
  caiNumber: string
  issueDate: string
  dueDate: string
  initialRange: string
  finalRange: string
  currentCorrelative: string
  status: string
  isExpiringSoon: boolean
  attachmentPath?: string
}

const docTypes = ['Factura (general)', 'Factura', 'NotaCredito', 'NotaDebito']

const schema = z.object({
  docType: z.string(),
  caiNumber: z.string().min(10, 'El número CAI debe tener al menos 10 caracteres'),
  issueDate: z.string().min(1, 'La fecha de emisión es requerida'),
  dueDate: z.string().min(1, 'La fecha de vencimiento es requerida'),
  initialRange: z.string().regex(/^\d{3}-\d{3}-\d{2}-\d{8}$/, 'Formato: 000-001-01-00000001'),
  finalRange: z.string().regex(/^\d{3}-\d{3}-\d{2}-\d{8}$/, 'Formato: 000-001-01-00000001'),
}).refine(data => !data.issueDate || !data.dueDate || data.dueDate > data.issueDate, {
  message: 'La fecha de vencimiento debe ser posterior a la de emisión',
  path: ['dueDate'],
})

type FormData = z.infer<typeof schema>

export default function AuthorizationsPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<UnifiedAuth[]>([])
  const [showForm, setShowForm] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [error, setError] = useState('')
  const [serverError, setServerError] = useState('')
  const fileRef = useRef<HTMLInputElement>(null)

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { docType: 'Factura (general)', caiNumber: '', issueDate: '', dueDate: '', initialRange: '', finalRange: '' },
  })

  const docType = watch('docType')

  useEffect(() => { load() }, [])

  const load = async () => {
    const [caiRes, docAuthRes] = await Promise.all([
      api.get<CAI[]>('/cai'),
      api.get<DocumentAuthorization[]>('/document-authorizations')
    ])
    const unified: UnifiedAuth[] = [
      ...caiRes.data.map(c => ({ ...c, source: 'cai' as const, documentType: 'Factura (general)' })),
      ...docAuthRes.data.map(d => ({ ...d, source: 'document' as const, documentType: d.documentType })),
    ]
    setItems(unified)
  }

  const onSubmit = async (data: FormData) => {
    setError('')
    setServerError('')
    if (file && !file.name.toLowerCase().endsWith('.pdf')) {
      setError('El archivo debe ser PDF'); return
    }
    try {
      if (data.docType === 'Factura (general)') {
        await api.post('/cai', {
          caiNumber: data.caiNumber,
          issueDate: data.issueDate,
          dueDate: data.dueDate,
          initialRange: data.initialRange,
          finalRange: data.finalRange,
        })
      } else {
        const docTypeMap: Record<string, string> = { Factura: 'Factura', NotaCredito: 'NotaCredito', NotaDebito: 'NotaDebito' }
        const fd = new FormData()
        fd.append('documentType', docTypeMap[data.docType])
        fd.append('caiNumber', data.caiNumber)
        fd.append('issueDate', data.issueDate)
        fd.append('dueDate', new Date(data.dueDate).toISOString())
        fd.append('initialRange', data.initialRange)
        fd.append('finalRange', data.finalRange)
        if (file) fd.append('file', file)
        await api.post('/document-authorizations/with-file', fd, {
          headers: { 'Content-Type': 'multipart/form-data' }
        })
      }
      setShowForm(false)
      reset()
      setFile(null)
      if (fileRef.current) fileRef.current.value = ''
      load()
    } catch (e: any) {
      setServerError(e?.response?.data ?? 'Error al guardar la autorización')
    }
  }

  const deleteItem = async (item: UnifiedAuth) => {
    if (item.source === 'cai') {
      if (confirm('¿Desactivar este CAI? (No se eliminarán las facturas asociadas)')) {
        await api.delete(`/cai/${item.id}`)
        load()
      }
    } else {
      if (confirm('¿Eliminar esta autorización?')) {
        await api.delete(`/document-authorizations/${item.id}`)
        load()
      }
    }
  }

  const viewFile = (item: UnifiedAuth) => {
    if (item.source === 'document' && item.attachmentPath)
      window.open(`/api/document-authorizations/${item.id}/file`, '_blank')
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Autorizaciones Fiscales (CAI/SAR)</h1>
        <Button onClick={() => setShowForm(!showForm)}>Nueva Autorización</Button>
      </div>

      {showForm && (
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">Nueva Autorización Fiscal</h3>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-sm">Tipo Documento *</label>
              <select {...register('docType')} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                {docTypes.map(d => <option key={d}>{d}</option>)}
              </select>
            </div>
            <div>
              <label className="text-sm">Número CAI *</label>
              <Input {...register('caiNumber')} />
              {errors.caiNumber && <p className="text-xs text-red-500 mt-1">{errors.caiNumber.message}</p>}
            </div>
            <div>
              <label className="text-sm">Fecha Emisión *</label>
              <Input type="date" {...register('issueDate')} />
              {errors.issueDate && <p className="text-xs text-red-500 mt-1">{errors.issueDate.message}</p>}
            </div>
            <div>
              <label className="text-sm">Fecha Vencimiento *</label>
              <Input type={docType === 'Factura (general)' ? 'date' : 'datetime-local'} {...register('dueDate')} />
              {errors.dueDate && <p className="text-xs text-red-500 mt-1">{errors.dueDate.message}</p>}
            </div>
            <div>
              <label className="text-sm">Rango Inicial *</label>
              <Input {...register('initialRange')} placeholder="000-001-01-00000001" />
              {errors.initialRange && <p className="text-xs text-red-500 mt-1">{errors.initialRange.message}</p>}
            </div>
            <div>
              <label className="text-sm">Rango Final *</label>
              <Input {...register('finalRange')} placeholder="000-001-01-00001000" />
              {errors.finalRange && <p className="text-xs text-red-500 mt-1">{errors.finalRange.message}</p>}
            </div>
            <div>
              <label className="text-sm">Archivo PDF (opcional)</label>
              <input ref={fileRef} type="file" accept=".pdf" onChange={e => setFile(e.target.files?.[0] || null)} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background file:mr-2 file:py-1 file:px-3 file:border-0 file:text-sm file:bg-primary file:text-primary-foreground file:rounded-md" />
            </div>
          </div>
          {error && <div className="text-sm text-red-600 dark:text-red-400 p-2 bg-red-50 dark:bg-red-900/20 rounded">{error}</div>}
          {serverError && <div className="text-sm text-red-600 dark:text-red-400 p-2 bg-red-50 dark:bg-red-900/20 rounded">{serverError}</div>}
          <div className="flex gap-2">
            <Button onClick={handleSubmit(onSubmit)}>Guardar</Button>
            <Button variant="outline" onClick={() => { setShowForm(false); reset(); setFile(null); if (fileRef.current) fileRef.current.value = '' }}>Cancelar</Button>
          </div>
        </div>
      )}

      <div className="border border-border rounded-lg overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted"><tr>
            <th className="text-left p-3">Tipo Doc.</th>
            <th className="text-left p-3">CAI</th>
            <th className="text-left p-3">Rango</th>
            <th className="text-left p-3">Correlativo</th>
            <th className="text-left p-3">Emisión</th>
            <th className="text-left p-3">Vencimiento</th>
            <th className="text-left p-3">Estado</th>
            <th className="text-left p-3">Archivo</th>
            <th className="text-left p-3"></th>
          </tr></thead>
          <tbody>
            {items.map(item => (
              <tr key={`${item.source}-${item.id}`} className={`border-t border-border ${item.isExpiringSoon ? 'bg-red-50 dark:bg-red-900/20' : ''}`}>
                <td className="p-3">
                  <span className="text-xs font-medium bg-primary/10 px-2 py-0.5 rounded">{item.documentType}</span>
                </td>
                <td className="p-3 font-mono font-bold">{item.caiNumber}</td>
                <td className="p-3 text-xs text-muted-foreground">{item.initialRange} → {item.finalRange}</td>
                <td className="p-3 font-mono text-xs">{item.currentCorrelative}</td>
                <td className="p-3 text-sm">{new Date(item.issueDate).toLocaleDateString()}</td>
                <td className="p-3 text-sm">
                  {new Date(item.dueDate).toLocaleDateString()}
                  {item.isExpiringSoon && <span className="ml-1 text-red-600 font-bold text-xs">(PRÓXIMO A VENCER)</span>}
                </td>
                <td className="p-3">
                  <span className={`px-2 py-0.5 rounded text-xs font-medium ${item.status === 'Activo' ? 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300' : item.status === 'Desactivado' ? 'bg-gray-100 dark:bg-gray-800/50 text-gray-800 dark:text-gray-300' : 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300'}`}>{item.status}</span>
                </td>
                <td className="p-3">
                  <div className="flex gap-1">
                    {item.source === 'document' && item.attachmentPath &&
                      <Button size="sm" variant="outline" onClick={() => viewFile(item)}>Ver PDF</Button>
                    }
                    <Button size="sm" variant="outline" onClick={() => navigate(`/invoices?filter=${item.source}:${item.id}`)}>Ver Facturas</Button>
                  </div>
                </td>
                <td className="p-3">
                  <Button variant="destructive" size="sm" onClick={() => deleteItem(item)}>{item.source === 'cai' ? 'Desactivar' : 'Eliminar'}</Button>
                </td>
              </tr>
            ))}
            {items.length === 0 && <tr><td colSpan={9} className="p-6 text-center text-muted-foreground">No hay autorizaciones fiscales registradas</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
