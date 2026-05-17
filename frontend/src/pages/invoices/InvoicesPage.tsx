import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { Invoice } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export default function InvoicesPage() {
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [dni, setDni] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [invoicePreview, setInvoicePreview] = useState<string | null>(null)
  const [invoiceLogo, setInvoiceLogo] = useState<string | null>(null)
  const [previewLogoHeight, setPreviewLogoHeight] = useState(40)
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null)

  useEffect(() => { load() }, [])

  const load = async () => {
    const params: any = {}
    if (dni) params.dni = dni
    if (startDate && endDate) { params.start = startDate; params.end = endDate }
    if (!dni && !startDate && !endDate) { /* load all */ }
    const { data } = await api.get<Invoice[]>('/invoices/search', { params })
    setInvoices(data)
  }

  const viewPrint = async (id: string) => {
    setSelectedInvoiceId(id)
    try {
      const { data } = await api.get<{ text: string; logoBase64: string; printLogoHeight?: number }>(`/print/invoice/${id}/preview`)
      setInvoicePreview(data.text)
      setInvoiceLogo(data.logoBase64 || null)
      setPreviewLogoHeight(data.printLogoHeight ?? 40)
    } catch { setInvoicePreview(null); setInvoiceLogo(null); setPreviewLogoHeight(40) }
  }

  const printInvoice = async () => {
    if (!selectedInvoiceId) return
    try {
      const { data } = await api.post(`/print/invoice/${selectedInvoiceId}`)
      alert(data.message || 'Factura enviada a imprimir')
    } catch (e: any) {
      alert('Error al imprimir: ' + (e.response?.data?.message || e.message))
    }
  }

  const cancelInvoice = async (id: string) => {
    if (confirm('¿Anular esta factura?')) {
      await api.post(`/invoices/${id}/cancel`)
      load()
    }
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Facturación SAR</h1>

      <div className="flex gap-2 flex-wrap">
        <Input placeholder="Buscar por DNI del huésped..." value={dni} onChange={e => setDni(e.target.value)} className="max-w-xs" />
        <Input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} className="max-w-[150px]" />
        <Input type="date" value={endDate} onChange={e => setEndDate(e.target.value)} className="max-w-[150px]" />
        <Button variant="outline" onClick={load}>Buscar</Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="border border-border rounded-lg overflow-hidden max-h-[600px] overflow-y-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted sticky top-0"><tr>
              <th className="text-left p-3">Correlativo</th><th className="text-left p-3">Cliente</th>
              <th className="text-left p-3">Fecha</th><th className="text-right p-3">Total</th><th className="text-left p-3">Estado</th><th className="text-left p-3"></th>
            </tr></thead>
            <tbody>
              {invoices.map(inv => (
                <tr key={inv.id} className="border-t border-border cursor-pointer hover:bg-accent" onClick={() => viewPrint(inv.id)}>
                  <td className="p-3 font-mono text-xs">{inv.correlativeNumber}</td>
                  <td className="p-3">{inv.customerName}</td>
                  <td className="p-3">{new Date(inv.invoiceDate).toLocaleDateString()}</td>
                  <td className="p-3 text-right font-semibold">L {inv.totalAmount.toFixed(2)}</td>
                  <td className="p-3"><span className={`px-2 py-0.5 rounded text-xs font-medium ${inv.status === 'Anulada' ? 'bg-red-100 text-red-800' : 'bg-green-100 text-green-800'}`}>{inv.status}</span></td>
                  <td className="p-3 space-x-1">
                {inv.status !== 'Anulada' && (
                  <>
                    <Button size="sm" variant="outline" onClick={(e) => { e.stopPropagation(); window.location.href = `/invoices/${inv.id}/edit` }}>Editar</Button>
                    <Button size="sm" variant="destructive" onClick={(e) => { e.stopPropagation(); cancelInvoice(inv.id) }}>Anular</Button>
                  </>
                )}
              </td>
                </tr>
              ))}
              {invoices.length === 0 && <tr><td colSpan={6} className="p-6 text-center text-muted-foreground">No hay facturas</td></tr>}
            </tbody>
          </table>
        </div>

        {/* Print preview */}
          {invoicePreview && (
          <div className="max-w-lg mx-auto space-y-4">
            <h2 className="text-lg font-semibold">Vista previa de impresión</h2>
            <div className="border border-border rounded-lg p-2 overflow-x-auto" style={{background:'#fff', color:'#000', margin:'0 auto'}}>
              {invoiceLogo && <div className="text-center mb-1"><img src={invoiceLogo} className="mx-auto" style={{maxHeight:`${Math.min(previewLogoHeight * 2, 400)}px`}} alt="Logo" /></div>}
              <pre className="font-mono whitespace-pre" style={{margin:0, fontSize:'10px', lineHeight:'1.15'}}>{invoicePreview}</pre>
            </div>
            <Button onClick={printInvoice} className="w-full">Imprimir (Original + Copia)</Button>
            <p className="text-xs text-muted-foreground text-center">
              ORIGINAL: CLIENTE | COPIA: EMISOR (archivar 5 a\u00f1os)
            </p>
          </div>
        )}
      </div>
    </div>
  )
}

