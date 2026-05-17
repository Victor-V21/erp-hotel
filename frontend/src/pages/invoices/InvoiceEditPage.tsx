import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import api from '@/lib/axios'
import type { Invoice, InvoiceItem } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export default function InvoiceEditPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [invoice, setInvoice] = useState<Invoice | null>(null)
  const [customerName, setCustomerName] = useState('')
  const [rtnCliente, setRtnCliente] = useState('')
  const [items, setItems] = useState<InvoiceItem[]>([])
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    api.get<Invoice>(`/invoices/${id}`).then(({ data }) => {
      setInvoice(data)
      setCustomerName(data.customerName)
      setRtnCliente(data.rtnCliente || '')
      setItems(data.items)
    })
  }, [id])

  const total = items.reduce((sum, i) => {
    const line = i.quantity * i.unitPrice
    const isv = i.isExempt ? 0 : line * 0.15
    const tourist = i.isTouristTaxable ? line * 0.04 : 0
    return sum + line + isv + tourist
  }, 0)

  const save = async () => {
    if (!invoice) return
    setSaving(true)
    try {
      const payload = {
        caiId: invoice.caiId,
        customerId: invoice.customerId,
        guestId: null,
        rtnCliente: rtnCliente || null,
        customerName,
        customerAddress: invoice.customerAddress || '',
        documentType: invoice.documentType,
        items
      }
      await api.put(`/invoices/${id}`, payload)
      alert('Factura actualizada')
      navigate(`/invoices`)
    } finally { setSaving(false) }
  }

  if (!invoice) return <div className="p-6 text-center text-muted-foreground">Cargando...</div>

  return (
    <div className="max-w-2xl mx-auto space-y-4">
      <h1 className="text-2xl font-bold">Editar Factura</h1>
      <p className="text-sm text-muted-foreground">Factura: {invoice.correlativeNumber}</p>

      <div className="border border-border rounded-lg p-4 bg-card space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <div><label className="text-sm">Cliente</label><Input value={customerName} onChange={e => setCustomerName(e.target.value)} /></div>
          <div><label className="text-sm">RTN</label><Input value={rtnCliente} onChange={e => setRtnCliente(e.target.value)} maxLength={14} /></div>
        </div>

        <div className="space-y-2">
          <h3 className="font-medium">Items</h3>
          {items.map((item, idx) => (
            <div key={idx} className="flex gap-2 items-end border-b pb-2">
              <div className="flex-1"><label className="text-xs">Descripción</label><Input value={item.description} onChange={e => { const newItems = [...items]; newItems[idx] = {...newItems[idx], description: e.target.value}; setItems(newItems) }} /></div>
              <div className="w-16"><label className="text-xs">Cant</label><Input type="number" min={1} value={item.quantity} onChange={e => { const newItems = [...items]; newItems[idx] = {...newItems[idx], quantity: +e.target.value, lineTotal: +e.target.value * item.unitPrice}; setItems(newItems) }} /></div>
              <div className="w-24"><label className="text-xs">Precio</label><Input type="number" step="0.01" value={item.unitPrice} onChange={e => { const newItems = [...items]; newItems[idx] = {...newItems[idx], unitPrice: +e.target.value, lineTotal: item.quantity * +e.target.value}; setItems(newItems) }} /></div>
              <div className="w-20"><label className="text-xs">Total</label><div className="h-10 flex items-center text-sm font-semibold">L {item.lineTotal.toFixed(2)}</div></div>
              <Button size="sm" variant="destructive" onClick={() => setItems(items.filter((_, i) => i !== idx))} className="mb-0.5">X</Button>
            </div>
          ))}
          <Button size="sm" variant="outline" onClick={() => setItems([...items, { description: '', quantity: 1, unitPrice: 0, lineTotal: 0, isExempt: false, isvRate: 0.15, isTouristTaxable: true, discountPercentage: 0 }])}>
            + Agregar Item
          </Button>
        </div>

        <div className="text-right font-bold text-lg">Total: L {total.toFixed(2)}</div>

        <div className="flex gap-2">
          <Button onClick={save} disabled={saving}>{saving ? 'Guardando...' : 'Guardar Cambios'}</Button>
          <Button variant="outline" onClick={() => navigate('/invoices')}>Cancelar</Button>
        </div>
      </div>
    </div>
  )
}
