import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

interface FolioItem {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
  isExempt: boolean
  isvRate: number
  isTouristTaxable: boolean
  discountPercentage: number
}

interface Folio {
  id: string
  reservationId: string
  guestId: string
  guestName: string
  roomId: string
  roomNumber: string
  openingDate: string
  closingDate: string | null
  totalAmount: number
  status: string
  items: FolioItem[]
}

export default function FoliosPage() {
  const [folios, setFolios] = useState<Folio[]>([])
  const [selectedFolio, setSelectedFolio] = useState<Folio | null>(null)
  const [showAddItem, setShowAddItem] = useState(false)
  const [newItem, setNewItem] = useState({ description: '', quantity: 1, unitPrice: 0, isExempt: false, isvRate: 0.15, isTouristTaxable: false, discountPercentage: 0 })

  useEffect(() => { load() }, [])

  const load = async () => {
    const { data } = await api.get<Folio[]>('/folios')
    setFolios(data)
  }

  const viewFolio = async (id: string) => {
    const { data } = await api.get<Folio>(`/folios/${id}`)
    setSelectedFolio(data)
    setShowAddItem(false)
  }

  const closeDetail = () => { setSelectedFolio(null); setShowAddItem(false) }

  const addItem = async () => {
    if (!selectedFolio || !newItem.description || newItem.unitPrice <= 0) return
    await api.post(`/folios/${selectedFolio.id}/items`, newItem)
    setShowAddItem(false)
    setNewItem({ description: '', quantity: 1, unitPrice: 0, isExempt: false, isvRate: 0.15, isTouristTaxable: false, discountPercentage: 0 })
    viewFolio(selectedFolio.id)
  }

  if (selectedFolio) {
    const itemTotal = selectedFolio.items.reduce((s, i) => s + i.lineTotal, 0)
    const taxTotal = selectedFolio.items.reduce((s, i) => {
      const afterDiscount = i.lineTotal - (i.lineTotal * i.discountPercentage / 100)
      const tax = i.isExempt ? 0 : afterDiscount * i.isvRate
      const tourist = i.isTouristTaxable ? afterDiscount * 0.04 : 0
      return s + tax + tourist
    }, 0)
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-bold">Folio #{selectedFolio.roomNumber}</h1>
          <Button variant="outline" onClick={closeDetail}>Volver</Button>
        </div>

        <div className="border border-border rounded-lg p-4 bg-card space-y-2">
          <div className="grid grid-cols-2 gap-2 text-sm">
            <div><strong>Huésped:</strong> {selectedFolio.guestName}</div>
            <div><strong>Habitación:</strong> #{selectedFolio.roomNumber}</div>
            <div><strong>Apertura:</strong> {new Date(selectedFolio.openingDate).toLocaleString()}</div>
            <div><strong>Estado:</strong> <span className={`px-2 py-0.5 rounded text-xs font-medium ${selectedFolio.status === 'Abierto' ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>{selectedFolio.status}</span></div>
            {selectedFolio.closingDate && <div><strong>Cierre:</strong> {new Date(selectedFolio.closingDate).toLocaleString()}</div>}
          </div>
        </div>

        <div className="border border-border rounded-lg overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted"><tr>
              <th className="text-left p-3">Descripción</th><th className="text-right p-3">Cant.</th>
              <th className="text-right p-3">Precio</th><th className="text-right p-3">Total</th>
              <th className="text-right p-3">Dto%</th><th className="text-right p-3">ISV</th><th className="text-center p-3">T.T.</th>
            </tr></thead>
            <tbody>
              {selectedFolio.items.map(i => (
                <tr key={i.id} className="border-t border-border">
                  <td className="p-3">{i.description}</td>
                  <td className="p-3 text-right">{i.quantity}</td>
                  <td className="p-3 text-right">L {i.unitPrice.toFixed(2)}</td>
                  <td className="p-3 text-right">L {i.lineTotal.toFixed(2)}</td>
                  <td className="p-3 text-right">{i.discountPercentage > 0 ? `${i.discountPercentage}%` : '-'}</td>
                  <td className="p-3 text-right">{i.isExempt ? 'Exento' : `${(i.isvRate * 100).toFixed(0)}%`}</td>
                  <td className="p-3 text-center">{i.isTouristTaxable ? 'Sí' : 'No'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {selectedFolio.status === 'Abierto' && (
          <div className="space-y-3">
            {showAddItem ? (
              <div className="border border-border rounded-lg p-4 bg-card space-y-3">
                <h3 className="font-medium">Agregar Consumo</h3>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                  <div className="col-span-2"><label className="text-sm">Descripción *</label><Input value={newItem.description} onChange={e => setNewItem({...newItem, description: e.target.value})} /></div>
                  <div><label className="text-sm">Cantidad</label><Input type="number" min="1" value={newItem.quantity} onChange={e => setNewItem({...newItem, quantity: parseInt(e.target.value) || 1})} /></div>
                  <div><label className="text-sm">Precio Unit. *</label><Input type="number" step="0.01" min="0" value={newItem.unitPrice} onChange={e => setNewItem({...newItem, unitPrice: parseFloat(e.target.value) || 0})} /></div>
                  <div><label className="text-sm">ISV</label>
                    <select value={newItem.isvRate} onChange={e => setNewItem({...newItem, isvRate: parseFloat(e.target.value)})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                      <option value={0.15}>15%</option><option value={0.18}>18%</option><option value={0}>Exento</option>
                    </select>
                  </div>
                  <div><label className="text-sm">Tasa Turística</label>
                    <select value={newItem.isTouristTaxable ? 'si' : 'no'} onChange={e => setNewItem({...newItem, isTouristTaxable: e.target.value === 'si'})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
                      <option value="no">No</option><option value="si">Sí</option>
                    </select>
                  </div>
                  <div><label className="text-sm">Dto. %</label><Input type="number" min="0" max="100" value={newItem.discountPercentage} onChange={e => setNewItem({...newItem, discountPercentage: parseFloat(e.target.value) || 0})} /></div>
                </div>
                <div className="flex gap-2"><Button onClick={addItem}>Agregar</Button><Button variant="outline" onClick={() => setShowAddItem(false)}>Cancelar</Button></div>
              </div>
            ) : (
              <Button onClick={() => setShowAddItem(true)}>Agregar Consumo</Button>
            )}
          </div>
        )}

        <div className="border-t border-dashed pt-3 text-right space-y-1 text-sm">
          <div className="flex justify-end gap-4"><span>Subtotal:</span><span className="font-mono w-24 text-right">L {itemTotal.toFixed(2)}</span></div>
          <div className="flex justify-end gap-4"><span>Impuestos:</span><span className="font-mono w-24 text-right">L {taxTotal.toFixed(2)}</span></div>
          <div className="flex justify-end gap-4 text-lg font-bold"><span>TOTAL:</span><span className="font-mono w-24 text-right">L {(itemTotal + taxTotal).toFixed(2)}</span></div>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Folios / Consumos</h1>
        <Button variant="outline" onClick={load}>Actualizar</Button>
      </div>

      <div className="border border-border rounded-lg overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted"><tr>
            <th className="text-left p-3">Huésped</th><th className="text-left p-3">Habitación</th>
            <th className="text-left p-3">Apertura</th><th className="text-right p-3">Total</th>
            <th className="text-left p-3">Items</th><th className="text-left p-3">Estado</th><th className="text-left p-3"></th>
          </tr></thead>
          <tbody>
            {folios.map(f => (
              <tr key={f.id} className="border-t border-border">
                <td className="p-3">{f.guestName}</td>
                <td className="p-3">#{f.roomNumber}</td>
                <td className="p-3">{new Date(f.openingDate).toLocaleDateString()}</td>
                <td className="p-3 text-right font-semibold">L {f.totalAmount.toFixed(2)}</td>
                <td className="p-3">{f.items?.length || 0}</td>
                <td className="p-3"><span className={`px-2 py-0.5 rounded text-xs font-medium ${f.status === 'Abierto' ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>{f.status}</span></td>
                <td className="p-3"><Button size="sm" variant="outline" onClick={() => viewFolio(f.id)}>Ver</Button></td>
              </tr>
            ))}
            {folios.length === 0 && <tr><td colSpan={7} className="p-6 text-center text-muted-foreground">No hay folios registrados</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
