import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '@/lib/axios'
import type { Guest, Room, InvoicePrintData } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { BarChart3, Star } from 'lucide-react'
import { useTaxRates } from '@/hooks/useTaxRates'

interface Discount { id: string; name: string; description?: string; discountType: string; value: number; isActive: boolean; requiresDocument?: boolean }
interface GuestStats { totalVisits: number; classification: string; lastVisit?: string; isFrequent: boolean }

function sanitizeForm(f: typeof defaultForm) {
  return { ...f, email: f.email || null, phone: f.phone || null, dateOfBirth: f.dateOfBirth || null, documentNumber: f.documentNumber || null, nationality: f.nationality || null, origin: f.origin || null, vehiclePlate: f.vehiclePlate || null, company: f.company || null, guestRTN: f.guestRTN || null, preferences: f.preferences || null }
}

const defaultForm = {
  firstName: '', lastName: '', email: '', phone: '', dateOfBirth: '', nationality: '', documentType: 'DNI',
  documentNumber: '', origin: '', hasVehicle: false, vehiclePlate: '', company: '', guestRTN: '',
  preferences: '', classification: 'Normal'
}

export default function CheckInPage() {
  const navigate = useNavigate()
  const { isvRate, touristTaxRate } = useTaxRates()
  const taxFactor = 1 + isvRate + touristTaxRate
  const [step, setStep] = useState(1)
  const [availableRooms, setAvailableRooms] = useState<Room[]>([])
  const [guestForm, setGuestForm] = useState(defaultForm)
  const [searchTerm, setSearchTerm] = useState('')
  const [foundGuests, setFoundGuests] = useState<Guest[]>([])
  const [selectedGuest, setSelectedGuest] = useState<Guest | null>(null)
  const [guestStats, setGuestStats] = useState<GuestStats | null>(null)
  const [selectedRoom, setSelectedRoom] = useState<Room | null>(null)
  const [discounts, setDiscounts] = useState<Discount[]>([])
  const [selectedDiscountId, setSelectedDiscountId] = useState<string | null>(null)
  const hondurasDate = () => { const d = new Date(); d.setHours(d.getHours() - 6); return d.toISOString().split('T')[0] }
  const [checkIn, setCheckIn] = useState({ checkInDate: hondurasDate(), checkOutDate: '', adults: 1, children: 0, paymentMethod: 'Efectivo', advancePayment: 0 })
  const [cashReceived, setCashReceived] = useState('')
  const [creating, setCreating] = useState(false)
  const [invoiceResult, setInvoiceResult] = useState<any>(null)
  const [invoicePreview, setInvoicePreview] = useState<string | null>(null)
  const [invoiceLogo, setInvoiceLogo] = useState<string | null>(null)
  const [previewLogoHeight, setPreviewLogoHeight] = useState(40)
  const [previewWidth, setPreviewWidth] = useState(46)

  useEffect(() => {
    if (checkIn.checkInDate && checkIn.checkOutDate && checkIn.checkInDate < checkIn.checkOutDate) {
      api.get<Room[]>('/rooms/available', { params: { checkIn: checkIn.checkInDate, checkOut: checkIn.checkOutDate } }).then(({ data }) => setAvailableRooms(data))
    }
  }, [checkIn.checkInDate, checkIn.checkOutDate])

  useEffect(() => { api.get<Discount[]>('/discounts').then(({ data }) => setDiscounts(data.filter(d => d.isActive))) }, [])

  const searchGuests = async () => {
    if (!searchTerm) return
    const { data } = await api.get<Guest[]>('/guests', { params: { search: searchTerm } })
    setFoundGuests(data)
  }

  const selectGuest = async (guest: Guest) => {
    setSelectedGuest(guest)
    setGuestForm({
      firstName: guest.firstName, lastName: guest.lastName, email: guest.email || '', phone: guest.phone || '',
      dateOfBirth: guest.dateOfBirth || '', nationality: guest.nationality || '', documentType: guest.documentType || 'DNI',
      documentNumber: guest.documentNumber || '', origin: guest.origin || '', hasVehicle: guest.hasVehicle,
      vehiclePlate: guest.vehiclePlate || '', company: guest.company || '', guestRTN: guest.guestRTN || '',
      preferences: guest.preferences || '', classification: guest.classification || 'Normal'
    })
    try {
      const { data: stats } = await api.get<GuestStats>(`/guests/${guest.id}/stats`)
      setGuestStats(stats)
      if (stats.isFrequent) setGuestForm(f => ({ ...f, classification: 'Cliente Frecuente' }))
    } catch { setGuestStats(null) }
    setStep(2)
  }

  const createGuest = async () => {
    setCreating(true)
    try {
      const { data } = await api.post<Guest>('/guests', sanitizeForm(guestForm))
      setSelectedGuest(data)
      setGuestStats({ totalVisits: 0, classification: 'Normal', isFrequent: false })
      setStep(3)
    } finally { setCreating(false) }
  }

  const updateGuest = async () => {
    if (!selectedGuest) return
    try {
      await api.put(`/guests/${selectedGuest.id}`, sanitizeForm(guestForm))
      // Save classification separately
      if (selectedGuest.classification !== guestForm.classification) {
        await api.patch(`/guests/${selectedGuest.id}/classification`, { classification: guestForm.classification })
      }
      setStep(3)
    } catch { setStep(3) }
  }

  const handleDiscountClick = (id: string) => {
    setSelectedDiscountId(prev => prev === id ? null : id)
  }

  const doCheckIn = async () => {
    if (!selectedGuest || !selectedRoom) return
    if (guestForm.guestRTN && guestForm.guestRTN.length !== 14) {
      return alert('El RTN debe tener exactamente 14 dígitos numéricos')
    }
    try {
      const { data: reservation } = await api.post<{ id: string }>('/reservations', {
        guestId: selectedGuest.id, roomId: selectedRoom.id,
        checkInDate: checkIn.checkInDate, checkOutDate: checkIn.checkOutDate,
        adults: checkIn.adults, children: checkIn.children,
        paymentMethod: checkIn.paymentMethod, advancePayment: checkIn.advancePayment, notes: null
      })

      if (selectedGuest.classification !== guestForm.classification) {
        await api.patch(`/guests/${selectedGuest.id}/classification`, { classification: guestForm.classification })
      }

      const nights = checkIn.checkOutDate && checkIn.checkInDate
        ? Math.max(0, (new Date(checkIn.checkOutDate).getTime() - new Date(checkIn.checkInDate).getTime()) / 86400000) : 0
      const sellingTotal = nights * (selectedRoom?.pricePerNight || 0)
      const discountPct = selectedDiscountId ? (discounts.find(d => d.id === selectedDiscountId)?.value || 0) : 0
      const subtotalBase = sellingTotal / taxFactor
      const discountAmount = subtotalBase * discountPct / 100
      const afterDiscount = subtotalBase - discountAmount
      const isv = afterDiscount * isvRate
      const tourist = afterDiscount * touristTaxRate
      const totalCalc = afterDiscount + isv + tourist

      const cashAmount = checkIn.paymentMethod === 'Efectivo' ? (parseFloat(cashReceived) || 0) : 0
      const cashChange = Math.max(0, cashAmount - totalCalc)
      const { data: result } = await api.post('/reservations/checkin', {
        reservationId: reservation.id, roomId: selectedRoom.id,
        discountIds: selectedDiscountId ? [selectedDiscountId] : null,
        paymentMethod: checkIn.paymentMethod,
        cashReceived: cashAmount > 0 ? cashAmount : null,
        cashChange: cashChange > 0 ? cashChange : null
      })

      setInvoiceResult(result)
      const { data: preview } = await api.get<{text: string; logoBase64?: string; printLogoHeight?: number; printWidth?: number}>(`/print/invoice/${result.invoiceId}/preview`)
      setInvoicePreview(preview.text)
      setInvoiceLogo(preview.logoBase64 || null)
      setPreviewLogoHeight(preview.printLogoHeight ?? 40)
      setPreviewWidth(preview.printWidth || 46)
      setStep(6)
    } catch (e: any) {
      const body = e.response?.data
      const msg = typeof body === 'string' ? body : body?.title || body?.message || 'Error al hacer check-in'
      alert(msg)
    }
  }

  const printInvoice = async () => {
    if (!invoiceResult?.invoiceId) return
    try {
      const { data } = await api.post(`/print/invoice/${invoiceResult.invoiceId}`)
      alert(data.message || 'Factura enviada a imprimir')
    } catch (e: any) {
      alert('Error al imprimir: ' + (e.response?.data?.message || e.message))
    }
  }

  if (step === 6 && invoicePreview) {
    return (
      <div className="max-w-lg mx-auto space-y-4">
        <h1 className="text-2xl font-bold">Check-In Completado</h1>
        <p className="text-sm text-muted-foreground">Factura generada. Imprima Original (cliente) + Copia (emisor).</p>
        <div className="border border-border rounded-lg p-2" style={{background:'#fff', color:'#000', maxWidth:`${Math.min(previewWidth * 6.5, 550)}px`, margin:'0 auto'}}>
          {invoiceLogo && <div className="text-center mb-1"><img src={invoiceLogo} className="mx-auto" style={{maxHeight:`${Math.min(previewLogoHeight * 2, 400)}px`}} alt="Logo" /></div>}
          <pre className="font-mono whitespace-pre-wrap" style={{margin:0, fontSize:'10px', lineHeight:'1.15'}}>{invoicePreview}</pre>
        </div>
        <div className="flex gap-2">
          <Button onClick={printInvoice} className="flex-1">Imprimir (Original + Copia)</Button>
          <Button variant="outline" onClick={() => navigate('/reservations')}>Finalizar</Button>
        </div>
        <p className="text-xs text-muted-foreground text-center">
          ORIGINAL: CLIENTE | COPIA: EMISOR (archivar 5 a\u00f1os)
        </p>
      </div>
    )
  }

  const renderStep5 = () => {
    if (step !== 5 || !selectedRoom) return null
    const nights = checkIn.checkOutDate && checkIn.checkInDate
      ? Math.max(0, (new Date(checkIn.checkOutDate).getTime() - new Date(checkIn.checkInDate).getTime()) / 86400000) : 0
    const sellingTotal = nights * (selectedRoom?.pricePerNight || 0)
    const discountPct = selectedDiscountId ? (discounts.find(d => d.id === selectedDiscountId)?.value || 0) : 0
    const subtotalBase = sellingTotal / taxFactor
    const discountAmount = subtotalBase * discountPct / 100
    const afterDiscount = subtotalBase - discountAmount
    const isv = afterDiscount * isvRate
    const tourist = afterDiscount * touristTaxRate
    const totalCalc = afterDiscount + isv + tourist
    const recibido = parseFloat(cashReceived) || 0
    const cambio = Math.max(0, recibido - totalCalc)
    return (
      <div className="space-y-4">
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-bold">Confirmar Check-In</h3>
          <p className="text-sm"><strong>Huésped:</strong> {guestForm.firstName} {guestForm.lastName}</p>
          <p className="text-sm"><strong>Habitación:</strong> #{selectedRoom.roomNumber} - {selectedRoom.roomTypeName}</p>
          <p className="text-sm"><strong>Check-In:</strong> {checkIn.checkInDate} | <strong>Check-Out:</strong> {checkIn.checkOutDate}</p>
          <div className="border-t border-dashed pt-3 space-y-1 text-sm">
            <p className="font-medium mb-1">Desglose</p>
            <div className="flex justify-between"><span>{nights} noche(s) x L {selectedRoom.pricePerNight.toFixed(2)}</span><span>L {sellingTotal.toFixed(2)}</span></div>
            <div className="flex justify-between text-xs text-muted-foreground">Sujeto a impuestos</div>
            <div className="flex justify-between"><span>Subtotal base:</span><span>L {subtotalBase.toFixed(2)}</span></div>
            {discountPct > 0 && <div className="flex justify-between text-green-600"><span>Descuento ({discountPct}%):</span><span>-L {discountAmount.toFixed(2)}</span></div>}
            <div className="flex justify-between"><span>ISV {(isvRate * 100).toFixed(0)}%:</span><span>L {isv.toFixed(2)}</span></div>
            <div className="flex justify-between"><span>Tasa Turística {(touristTaxRate * 100).toFixed(0)}%:</span><span>L {tourist.toFixed(2)}</span></div>
            <div className="flex justify-between font-bold text-base border-t pt-1 mt-1"><span>TOTAL A PAGAR:</span><span>L {totalCalc.toFixed(2)}</span></div>
          </div>
          <div className="border-t border-dashed pt-3 space-y-2">
            <label className="text-sm font-medium">Método de Pago</label>
            <select value={checkIn.paymentMethod} onChange={e => { setCheckIn({...checkIn, paymentMethod: e.target.value}); setCashReceived('') }}
              className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background">
              <option value="Efectivo">Efectivo</option><option value="Tarjeta">Tarjeta</option><option value="Transferencia">Transferencia</option>
            </select>
          </div>
          {checkIn.paymentMethod === 'Efectivo' && (
            <div className="space-y-2">
              <label className="text-sm font-medium">Cantidad Recibida (L)</label>
              <Input type="number" step="0.01" min="0" value={cashReceived} onChange={e => setCashReceived(e.target.value)} placeholder="0.00" />
              {recibido >= totalCalc && (
                <div className="flex justify-between text-sm text-green-600 font-medium">
                  <span>Cambio a devolver:</span><span>L {cambio.toFixed(2)}</span>
                </div>
              )}
            </div>
          )}
        </div>
        <div className="flex gap-2">
          <Button onClick={doCheckIn} disabled={checkIn.paymentMethod === 'Efectivo' && (recibido <= 0 || recibido < totalCalc)}>
            {checkIn.paymentMethod === 'Efectivo' && (recibido <= 0 || recibido < totalCalc) ? (recibido <= 0 ? 'Ingrese cantidad recibida' : 'Cantidad insuficiente') : 'Cobrar y Generar Factura'}
          </Button>
          <Button variant="outline" onClick={() => setStep(4)}>Atrás</Button>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold">Check-In</h1>

      <div className="flex gap-1 text-xs">
        {[1, 2, 3, 4, 5].map(s => (
          <div key={s} className={`flex-1 p-1.5 rounded text-center ${step >= s ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'}`}>
            {s === 1 ? 'Buscar' : s === 2 ? 'Revisar' : s === 3 ? 'Habitación' : s === 4 ? 'Dto.' : 'Pago'}
          </div>
        ))}
      </div>

      {step === 1 && (
        <div className="space-y-4">
          <div className="flex gap-2">
            <Input placeholder="Buscar huésped por nombre o DNI..." value={searchTerm} onChange={e => setSearchTerm(e.target.value)} />
            <Button variant="outline" onClick={searchGuests}>Buscar</Button>
          </div>
          {foundGuests.length > 0 && (
            <div className="border border-border rounded-lg max-h-60 overflow-y-auto">
              {foundGuests.map(g => (
                <div key={g.id} className="p-3 border-b border-border flex justify-between items-center cursor-pointer hover:bg-accent" onClick={() => selectGuest(g)}>
                  <div><span className="font-medium">{g.fullName}</span><span className="text-muted-foreground ml-2">{g.documentNumber}</span></div>
                  <Button size="sm">Seleccionar</Button>
                </div>
              ))}
            </div>
          )}
          <div className="border-t border-border pt-4">
            <h3 className="font-medium mb-3">O crear nuevo huésped:</h3>
            <div className="grid grid-cols-2 gap-3">
              <div><label className="text-sm">Nombres *</label><Input value={guestForm.firstName} onChange={e => setGuestForm({...guestForm, firstName: e.target.value})} /></div>
              <div><label className="text-sm">Apellidos *</label><Input value={guestForm.lastName} onChange={e => setGuestForm({...guestForm, lastName: e.target.value})} /></div>
              <div><label className="text-sm">DNI</label><Input value={guestForm.documentNumber} onChange={e => setGuestForm({...guestForm, documentNumber: e.target.value})} /></div>
              <div><label className="text-sm">Nacionalidad</label><Input value={guestForm.nationality} onChange={e => setGuestForm({...guestForm, nationality: e.target.value})} /></div>
              <div><label className="text-sm">Procedencia</label><Input value={guestForm.origin} onChange={e => setGuestForm({...guestForm, origin: e.target.value})} /></div>
              <div><label className="text-sm">Teléfono</label><Input value={guestForm.phone} onChange={e => setGuestForm({...guestForm, phone: e.target.value})} /></div>
              <div><label className="text-sm">Empresa</label><Input value={guestForm.company} onChange={e => setGuestForm({...guestForm, company: e.target.value})} /></div>
              <div><label className="text-sm">RTN</label><Input value={guestForm.guestRTN} onChange={e => setGuestForm({...guestForm, guestRTN: e.target.value.replace(/\D/g, '').slice(0, 14)})} maxLength={14} /></div>
              <div className="flex items-center gap-2">
                <input type="checkbox" checked={guestForm.hasVehicle} onChange={e => setGuestForm({...guestForm, hasVehicle: e.target.checked})} className="w-4 h-4" />
                <label className="text-sm">Tiene vehículo</label>
              </div>
              {guestForm.hasVehicle && <div><label className="text-sm">Placa</label><Input value={guestForm.vehiclePlate} onChange={e => setGuestForm({...guestForm, vehiclePlate: e.target.value})} /></div>}
            </div>
            <Button className="mt-3" onClick={createGuest} disabled={creating || !guestForm.firstName || !guestForm.lastName}>
              {creating ? 'Creando...' : 'Crear y Continuar'}
            </Button>
          </div>
        </div>
      )}

      {step === 2 && selectedGuest && (
        <div className="space-y-4">
          <div className="border border-border rounded-lg p-4 bg-card">
            <h3 className="font-medium mb-3">Revisar Información del Huésped</h3>
            <div className="grid grid-cols-2 gap-3">
              <div><label className="text-sm">Nombres</label><Input value={guestForm.firstName} onChange={e => setGuestForm({...guestForm, firstName: e.target.value})} /></div>
              <div><label className="text-sm">Apellidos</label><Input value={guestForm.lastName} onChange={e => setGuestForm({...guestForm, lastName: e.target.value})} /></div>
              <div><label className="text-sm">DNI</label><Input value={guestForm.documentNumber} onChange={e => setGuestForm({...guestForm, documentNumber: e.target.value})} /></div>
              <div><label className="text-sm">Nacionalidad</label><Input value={guestForm.nationality} onChange={e => setGuestForm({...guestForm, nationality: e.target.value})} /></div>
              <div><label className="text-sm">Procedencia</label><Input value={guestForm.origin} onChange={e => setGuestForm({...guestForm, origin: e.target.value})} /></div>
              <div><label className="text-sm">Teléfono</label><Input value={guestForm.phone} onChange={e => setGuestForm({...guestForm, phone: e.target.value})} /></div>
              <div><label className="text-sm">Empresa</label><Input value={guestForm.company} onChange={e => setGuestForm({...guestForm, company: e.target.value})} /></div>
              <div><label className="text-sm">RTN</label><Input value={guestForm.guestRTN} onChange={e => setGuestForm({...guestForm, guestRTN: e.target.value.replace(/\D/g, '').slice(0, 14)})} maxLength={14} /></div>
              <div className="flex items-center gap-2 pt-6">
                <input type="checkbox" checked={guestForm.hasVehicle} onChange={e => setGuestForm({...guestForm, hasVehicle: e.target.checked})} className="w-4 h-4" />
                <label className="text-sm">Tiene vehículo</label>
              </div>
              {guestForm.hasVehicle && <div><label className="text-sm">Placa</label><Input value={guestForm.vehiclePlate} onChange={e => setGuestForm({...guestForm, vehiclePlate: e.target.value})} /></div>}
            </div>
          </div>

          {guestStats && (
            <div className="border border-border rounded-lg p-4 bg-muted/30">
              <h4 className="font-medium text-sm mb-2 flex items-center gap-1.5">
                <BarChart3 size={16} /> Estadísticas
              </h4>
              <div className="text-sm space-y-1">
                <p>Visitas anteriores: <strong>{guestStats.totalVisits}</strong></p>
                <p className="flex items-center gap-1">
                  Clasificación:{' '}
                  <strong className="flex items-center gap-1">
                    {guestStats.isFrequent && <Star size={14} className="text-yellow-500 fill-yellow-500" />}
                    {guestStats.isFrequent ? 'Cliente Frecuente' : guestStats.classification}
                  </strong>
                </p>
                <div className="flex items-center gap-2 mt-1">
                  <label className="text-xs">Clasificación manual:</label>
                  <select value={guestForm.classification} onChange={e => setGuestForm({...guestForm, classification: e.target.value})}
                    className="border border-input rounded-md px-2 py-1 text-xs bg-background">
                    <option value="Normal">Normal</option>
                    <option value="Cliente Frecuente">Cliente Frecuente</option>
                  </select>
                </div>
                {guestStats.lastVisit && <p>Última visita: {guestStats.lastVisit}</p>}
              </div>
            </div>
          )}

          <div className="flex gap-2">
            <Button onClick={updateGuest}>Información correcta, continuar</Button>
            <Button variant="outline" onClick={() => setStep(1)}>Atrás</Button>
          </div>
        </div>
      )}

      {step === 3 && (
        <div className="space-y-4">
          <div className="p-3 bg-muted rounded"><strong>Huésped:</strong> {guestForm.firstName} {guestForm.lastName}</div>
          <div className="grid grid-cols-2 gap-3">
            <div><label className="text-sm">Check-In</label><Input type="date" value={checkIn.checkInDate} onChange={e => setCheckIn({...checkIn, checkInDate: e.target.value})} /></div>
            <div><label className="text-sm">Check-Out</label><Input type="date" min={checkIn.checkInDate} value={checkIn.checkOutDate} onChange={e => setCheckIn({...checkIn, checkOutDate: e.target.value})} /></div>
          </div>
          <h3 className="font-medium">Habitaciones disponibles:</h3>
          <div className="max-h-64 overflow-y-auto border border-border rounded-lg p-2">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
            {availableRooms.map(r => (
              <div key={r.id} className={`p-3 border rounded-lg cursor-pointer transition-colors ${selectedRoom?.id === r.id ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent'}`} onClick={() => setSelectedRoom(r)}>
                <div className="font-bold">#{r.roomNumber} - Piso {r.floor}</div>
                <div className="text-sm text-muted-foreground">{r.roomTypeName}</div>
                <div className="font-semibold">L {r.pricePerNight.toFixed(2)} / noche</div>
              </div>
            ))}
            {availableRooms.length === 0 && <p className="text-muted-foreground col-span-2">No hay habitaciones disponibles</p>}
          </div>
          </div>
          <div className="flex gap-2">
            <Button onClick={() => setStep(4)} disabled={!selectedRoom || !checkIn.checkOutDate}>Continuar</Button>
            <Button variant="outline" onClick={() => setStep(2)}>Atrás</Button>
          </div>
        </div>
      )}

      {step === 4 && selectedRoom && (
        <div className="space-y-4">
          <div className="border border-border rounded-lg p-4 bg-card">
            <h3 className="font-medium mb-2">Descuentos Disponibles</h3>
            {discounts.length === 0 && <p className="text-sm text-muted-foreground">No hay descuentos activos</p>}
            {discounts.map(d => (
              <div key={d.id}
                className={`p-3 border rounded-lg mb-2 cursor-pointer transition-colors ${selectedDiscountId === d.id ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'border-border hover:bg-accent'}`}
                onClick={() => handleDiscountClick(d.id)}>
                <div className="flex items-center gap-2">
                  <div className={`w-4 h-4 rounded-full border-2 flex items-center justify-center ${selectedDiscountId === d.id ? 'border-primary' : 'border-muted-foreground'}`}>
                    {selectedDiscountId === d.id && <div className="w-2 h-2 rounded-full bg-primary" />}
                  </div>
                  <div>
                    <div className="font-medium text-sm">{d.name}</div>
                    {d.description && <div className="text-xs text-muted-foreground">{d.description}</div>}
                    {d.requiresDocument && <div className="text-xs text-amber-600">Requiere documento</div>}
                  </div>
                  <div className="ml-auto font-bold text-sm">{d.discountType === 'Porcentaje' ? `${d.value}%` : `L ${d.value.toFixed(2)}`}</div>
                </div>
              </div>
            ))}
            {guestForm.classification === 'Cliente Frecuente' && !selectedDiscountId && (
              <div className="p-2 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded text-sm text-green-800 dark:text-green-300 flex items-center gap-1">
                <Star size={14} className="fill-green-500 text-green-500" /> Cliente Frecuente detectado. Seleccione el descuento si aplica.
              </div>
            )}
          </div>

          <div className="border border-border rounded-lg p-4 bg-card">
            <h4 className="font-medium text-sm mb-2">Resumen</h4>
            <p className="text-sm">{selectedRoom.roomTypeName} - #{selectedRoom.roomNumber}</p>
          </div>

          <div className="flex gap-2">
            <Button onClick={() => setStep(5)}>Continuar al Pago</Button>
            <Button variant="outline" onClick={() => setStep(3)}>Atrás</Button>
          </div>
        </div>
      )}

      {renderStep5()}
    </div>
  )
}


