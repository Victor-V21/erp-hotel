import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '@/lib/axios'
import type {
  Reservation,
  Folio,
  CAI,
  DocumentAuthorization,
  Discount,
  CashRegister,
} from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { InlineAlert } from '@/components/ui/InlineAlert'
import {
  UserMinus,
  Search,
  BedDouble,
  Receipt,
  Printer,
  Plus,
  DollarSign,
  CreditCard,
  Building,
  CheckCircle2,
  Loader2,
  Calendar,
  Sparkles,
  ArrowRight,
  RefreshCw,
  Clock,
} from 'lucide-react'

export default function CheckOutPage() {
  const navigate = useNavigate()
  const [checkIns, setCheckIns] = useState<Reservation[]>([])
  const [selectedReservation, setSelectedReservation] = useState<Reservation | null>(null)
  const [folio, setFolio] = useState<Folio | null>(null)
  const [loadingList, setLoadingList] = useState(true)
  const [loadingFolio, setLoadingFolio] = useState(false)
  const [searchTerm, setSearchTerm] = useState('')

  // Billing and SAR authorizations
  const [caiList, setCaiList] = useState<CAI[]>([])
  const [selectedCaiId, setSelectedCaiId] = useState<string>('')
  const [discounts, setDiscounts] = useState<Discount[]>([])
  const [selectedDiscountId, setSelectedDiscountId] = useState<string>('')
  const [cashRegisters, setCashRegisters] = useState<CashRegister[]>([])
  const [selectedCashRegisterId, setSelectedCashRegisterId] = useState<string>('')

  // Extra Folio Item Form
  const [showAddCharge, setShowAddCharge] = useState(false)
  const [chargeDesc, setChargeDesc] = useState('')
  const [chargeQty, setChargeQty] = useState(1)
  const [chargePrice, setChargePrice] = useState('')
  const [chargeIsTaxable, setChargeIsTaxable] = useState(true)

  // Payment Form
  const [paymentMethod, setPaymentMethod] = useState<'Efectivo' | 'Tarjeta' | 'Transferencia'>('Efectivo')
  const [cashGiven, setCashGiven] = useState('')
  const [customerRtn, setCustomerRtn] = useState('')
  const [customerName, setCustomerName] = useState('')
  const [customerAddress, setCustomerAddress] = useState('')
  const [taxpayerType, setTaxpayerType] = useState('ConsumidorFinal')

  // Completion & Print Preview
  const [completedInvoiceId, setCompletedInvoiceId] = useState<string | null>(null)
  const [printPreview, setPrintPreview] = useState<string | null>(null)
  const [printLogo, setPrintLogo] = useState<string | null>(null)
  const [actionLoading, setActionLoading] = useState(false)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [confirmReleaseOnly, setConfirmReleaseOnly] = useState(false)

  // Load active checkins and auxiliary data
  const loadCheckIns = useCallback(async () => {
    setLoadingList(true)
    try {
      const [resRes, caiRes, discRes, cashRes] = await Promise.all([
        api.get<Reservation[]>('/reservations', { params: { status: 'CheckIn' } }),
        api.get<CAI[]>('/cai'),
        api.get<Discount[]>('/discounts'),
        api.get<CashRegister[]>('/cash-registers'),
      ])
      setCheckIns(resRes.data)
      setCaiList(caiRes.data)
      if (caiRes.data.length > 0) setSelectedCaiId(caiRes.data[0].id)
      setDiscounts(discRes.data.filter((d) => d.isActive))
      setCashRegisters(cashRes.data.filter((c) => c.isActive))
      if (cashRes.data.length > 0) setSelectedCashRegisterId(cashRes.data[0].id)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al consultar huéspedes con Check-In activo.' })
    } finally {
      setLoadingList(false)
    }
  }, [])

  useEffect(() => {
    loadCheckIns()
  }, [loadCheckIns])

  // When a reservation is selected, fetch its folio
  const handleSelectReservation = async (reservation: Reservation) => {
    setSelectedReservation(reservation)
    setCustomerName(reservation.guestName)
    setLoadingFolio(true)
    setFolio(null)
    setCompletedInvoiceId(null)
    setPrintPreview(null)

    try {
      const { data } = await api.get<Folio>(`/folios/by-reservation/${reservation.id}`)
      setFolio(data)
      setCashGiven(String(data.totalAmount.toFixed(2)))
    } catch {
      setAlertInfo({ variant: 'error', message: 'No se pudo cargar el folio de la reservación seleccionada.' })
    } finally {
      setLoadingFolio(false)
    }
  }

  // Add extra charge to folio
  const handleAddFolioCharge = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!folio || !chargeDesc.trim() || !chargePrice || Number(chargePrice) <= 0) return

    try {
      await api.post(`/folios/${folio.id}/items`, {
        description: chargeDesc.trim(),
        quantity: Number(chargeQty),
        unitPrice: Number(chargePrice),
        isExempt: !chargeIsTaxable,
        isvRate: chargeIsTaxable ? 0.15 : 0,
        isTouristTaxable: false,
        discountPercentage: 0,
      })

      // Reload folio
      const { data } = await api.get<Folio>(`/folios/${folio.id}`)
      setFolio(data)
      setCashGiven(String(data.totalAmount.toFixed(2)))
      setChargeDesc('')
      setChargePrice('')
      setChargeQty(1)
      setShowAddCharge(false)
      setAlertInfo({ variant: 'success', message: 'Cargo agregado al folio exitosamente.' })
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al agregar el cargo al folio.' })
    }
  }

  // Calculate totals with discount
  const activeDiscountObj = discounts.find((d) => d.id === selectedDiscountId)
  const discountPercent = activeDiscountObj ? activeDiscountObj.value : 0
  const subtotalGross = folio?.items.reduce((sum, item) => sum + item.lineTotal, 0) ?? 0
  const discountAmount = (subtotalGross * discountPercent) / 100
  const subtotalNet = subtotalGross - discountAmount
  const isvAmount = folio?.items.reduce((sum, item) => {
    if (item.isExempt) return sum
    const itemNet = item.lineTotal * (1 - discountPercent / 100)
    return sum + itemNet * (item.isvRate || 0.15)
  }, 0) ?? 0
  const touristTaxAmount = folio?.items.reduce((sum, item) => {
    if (!item.isTouristTaxable) return sum
    const itemNet = item.lineTotal * (1 - discountPercent / 100)
    return sum + itemNet * 0.04
  }, 0) ?? 0
  const finalTotal = subtotalNet + isvAmount + touristTaxAmount
  const changeDue = Math.max(0, (Number(cashGiven) || 0) - finalTotal)

  // Full Check-out & Invoicing execution
  const handleCompleteCheckOut = async () => {
    if (!selectedReservation || !folio) return
    if (!selectedCaiId) {
      setAlertInfo({ variant: 'error', message: 'Debe seleccionar una autorización CAI activa para facturar.' })
      return
    }

    setActionLoading(true)
    try {
      // 1. Create SAR Invoice
      const invoicePayload = {
        caiId: selectedCaiId,
        guestId: selectedReservation.guestId,
        customerName: customerName.trim() || selectedReservation.guestName,
        rtnCliente: customerRtn.trim() || null,
        customerAddress: customerAddress.trim() || 'Santa Rosa de Copán',
        taxpayerType: customerRtn.trim() ? 'Empresa' : taxpayerType,
        paymentMethod: paymentMethod,
        items: folio.items.map((it) => ({
          description: it.description,
          quantity: it.quantity,
          unitPrice: it.unitPrice,
          isExempt: it.isExempt,
          isvRate: it.isvRate || 0.15,
          isTouristTaxable: it.isTouristTaxable,
          discountPercentage: discountPercent,
        })),
      }

      const invoiceRes = await api.post('/invoices', invoicePayload)
      const createdInvoice = invoiceRes.data

      // 2. Execute reservation checkout to free room and set to Limpieza
      await api.post(`/reservations/checkout/${selectedReservation.id}`)

      // 3. Register cash movement if paid in cash and cash register is selected
      if (paymentMethod === 'Efectivo' && selectedCashRegisterId) {
        try {
          await api.post(`/cash-registers/${selectedCashRegisterId}/movements`, {
            cashRegisterId: selectedCashRegisterId,
            movementType: 'Ingreso',
            amount: finalTotal,
            description: `Cobro Check-Out Hab. #${selectedReservation.roomNumber} - Factura ${createdInvoice.correlativeNumber}`,
          })
        } catch (e) {
          console.warn('Could not record cash movement', e)
        }
      }

      // 4. Fetch Print Preview
      try {
        const previewRes = await api.get(`/print/invoice/${createdInvoice.id}/preview`)
        setPrintPreview(previewRes.data.text)
        setPrintLogo(previewRes.data.logoBase64 || null)
      } catch (e) {
        console.warn('Print preview unavailable', e)
      }

      setCompletedInvoiceId(createdInvoice.id)
      setAlertInfo({
        variant: 'success',
        message: `Check-out completado. Factura ${createdInvoice.correlativeNumber} emitida y habitación #${selectedReservation.roomNumber} liberada.`,
      })
      loadCheckIns()
    } catch (err: any) {
      setAlertInfo({
        variant: 'error',
        message: err.response?.data?.message || err.message || 'Error durante el proceso de facturación y Check-Out.',
      })
    } finally {
      setActionLoading(false)
    }
  }

  // Release Room without Invoicing
  const handleReleaseOnly = async () => {
    if (!selectedReservation) return
    setActionLoading(true)
    try {
      await api.post(`/reservations/checkout/${selectedReservation.id}`)
      setAlertInfo({
        variant: 'success',
        message: `Habitación #${selectedReservation.roomNumber} liberada correctamente y enviada a limpieza.`,
      })
      setConfirmReleaseOnly(false)
      setSelectedReservation(null)
      setFolio(null)
      loadCheckIns()
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al liberar la habitación.' })
    } finally {
      setActionLoading(false)
    }
  }

  const printThermalInvoice = async () => {
    if (!completedInvoiceId) return
    try {
      const { data } = await api.post(`/print/invoice/${completedInvoiceId}`)
      setAlertInfo({ variant: 'success', message: data.message || 'Factura enviada a imprimir con éxito.' })
    } catch (e: any) {
      setAlertInfo({ variant: 'error', message: 'Error al imprimir: ' + (e.response?.data?.message || e.message) })
    }
  }

  const filteredCheckIns = checkIns.filter(
    (c) =>
      c.guestName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      c.roomNumber.toLowerCase().includes(searchTerm.toLowerCase())
  )

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <UserMinus className="text-[#C69C4B]" size={24} />
            Liquidación de Folio & Check-Out
          </h1>
          <p className="text-sm text-muted-foreground">
            Desglose de consumos, facturación fiscal SAR, recepción de pago y liberación de habitaciones.
          </p>
        </div>
        <Button variant="outline" onClick={loadCheckIns} disabled={loadingList} className="gap-1.5 self-start sm:self-auto">
          <RefreshCw size={14} className={loadingList ? 'animate-spin' : ''} />
          Actualizar Lista
        </Button>
      </div>

      {alertInfo && (
        <InlineAlert variant={alertInfo.variant} message={alertInfo.message} onClose={() => setAlertInfo(null)} />
      )}

      {/* Main Grid: Selection List & Checkout Flow */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left column: Checked-In Guests List */}
        <div className="lg:col-span-4 space-y-3">
          <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col h-full max-h-[720px]">
            <div className="p-3.5 border-b border-border bg-muted/20 space-y-2.5">
              <div className="flex items-center justify-between">
                <h3 className="font-semibold text-sm text-foreground">Huéspedes en Hospedaje</h3>
                <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-[#C69C4B]/15 text-[#C69C4B]">
                  {checkIns.length} activos
                </span>
              </div>
              <div className="relative">
                <Search size={14} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Buscar huésped o habitación..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-8 text-xs h-8"
                />
              </div>
            </div>

            <div className="overflow-y-auto flex-1 divide-y divide-border/60">
              {loadingList ? (
                <div className="flex items-center justify-center py-16 space-y-2">
                  <Loader2 size={24} className="animate-spin text-[#C69C4B] mr-2" />
                  <span className="text-xs text-muted-foreground">Cargando check-ins...</span>
                </div>
              ) : filteredCheckIns.length > 0 ? (
                filteredCheckIns.map((r) => {
                  const isSelected = selectedReservation?.id === r.id
                  return (
                    <div
                      key={r.id}
                      onClick={() => handleSelectReservation(r)}
                      className={`p-3.5 cursor-pointer transition-colors flex items-center justify-between gap-3 ${
                        isSelected
                          ? 'bg-[#C69C4B]/15 border-l-4 border-l-[#C69C4B]'
                          : 'hover:bg-accent/40'
                      }`}
                    >
                      <div className="space-y-0.5">
                        <p className="font-bold text-xs text-foreground">{r.guestName}</p>
                        <div className="flex items-center gap-1.5 text-[11px] text-muted-foreground">
                          <span className="font-semibold text-[#C69C4B]">Hab. #{r.roomNumber}</span>
                          <span>·</span>
                          <span className="flex items-center gap-0.5">
                            <Clock size={11} /> {r.checkInDate} → {r.checkOutDate}
                          </span>
                        </div>
                      </div>
                      <ArrowRight size={14} className={`text-muted-foreground ${isSelected ? 'text-[#C69C4B]' : ''}`} />
                    </div>
                  )
                })
              ) : (
                <div className="p-8 text-center text-muted-foreground text-xs">
                  No hay huéspedes en estado Check-In actualmente.
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Right column: Folio Breakdown, Payment, & Invoicing */}
        <div className="lg:col-span-8">
          {completedInvoiceId ? (
            /* Completed Check-Out Screen */
            <div className="border border-border rounded-xl p-6 bg-card space-y-6 shadow-sm animate-in fade-in text-center max-w-xl mx-auto">
              <div className="w-16 h-16 rounded-full bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 mx-auto flex items-center justify-center">
                <CheckCircle2 size={36} />
              </div>
              <div>
                <h2 className="text-xl font-bold text-foreground">¡Check-Out y Facturación Exitosos!</h2>
                <p className="text-sm text-muted-foreground mt-1">
                  La habitación ha sido liberada y marcada para limpieza. Comprobante fiscal registrado en el sistema.
                </p>
              </div>

              {printPreview && (
                <div
                  className="border border-neutral-300 rounded-lg p-3 overflow-x-auto shadow-inner text-left max-w-md mx-auto"
                  style={{ background: '#ffffff', color: '#000000' }}
                >
                  {printLogo && (
                    <div className="text-center mb-2">
                      <img src={printLogo} className="mx-auto object-contain max-h-16" alt="Logo" />
                    </div>
                  )}
                  <pre
                    className="font-mono whitespace-pre-wrap"
                    style={{ margin: 0, fontSize: '10.5px', lineHeight: '1.18', color: '#000000' }}
                  >
                    {printPreview}
                  </pre>
                </div>
              )}

              <div className="flex flex-col sm:flex-row gap-3 justify-center pt-2">
                <Button onClick={printThermalInvoice} className="gap-2">
                  <Printer size={16} /> Imprimir Comprobante Fiscal
                </Button>
                <Button
                  variant="outline"
                  onClick={() => {
                    setSelectedReservation(null)
                    setFolio(null)
                    setCompletedInvoiceId(null)
                  }}
                >
                  Procesar Otro Check-Out
                </Button>
              </div>
            </div>
          ) : selectedReservation ? (
            /* Active Folio Review & Checkout Panel */
            <div className="space-y-6 animate-in fade-in">
              {/* Selected Reservation Banner */}
              <div className="p-4 rounded-xl border border-border bg-card shadow-xs flex flex-wrap items-center justify-between gap-4">
                <div className="flex items-center gap-3">
                  <div className="p-3 rounded-xl bg-primary/10 text-[#C69C4B]">
                    <BedDouble size={24} />
                  </div>
                  <div>
                    <h2 className="font-bold text-base text-foreground">
                      {selectedReservation.guestName}
                    </h2>
                    <p className="text-xs text-muted-foreground">
                      Habitación <span className="font-bold text-foreground">#{selectedReservation.roomNumber}</span> · Estancia: {selectedReservation.checkInDate} al {selectedReservation.checkOutDate}
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setShowAddCharge(!showAddCharge)}
                    className="gap-1.5 text-xs"
                  >
                    <Plus size={13} /> Agregar Cargo Extra
                  </Button>
                  <Button
                    size="sm"
                    variant="destructive"
                    onClick={() => setConfirmReleaseOnly(true)}
                    className="text-xs"
                  >
                    Liberar sin Facturar
                  </Button>
                </div>
              </div>

              {/* Add Extra Item Inline Form */}
              {showAddCharge && (
                <form
                  onSubmit={handleAddFolioCharge}
                  className="p-4 rounded-xl border border-border bg-card space-y-3 shadow-xs animate-in fade-in"
                >
                  <h4 className="font-semibold text-xs text-foreground uppercase tracking-wider">
                    Registrar Cargo Adicional al Folio (Minibar, Restaurante, Servicio)
                  </h4>
                  <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
                    <div className="sm:col-span-2">
                      <label className="text-xs text-muted-foreground">Descripción del Cargo *</label>
                      <Input
                        required
                        placeholder="Ej. Consumo Restaurante / Minibar"
                        value={chargeDesc}
                        onChange={(e) => setChargeDesc(e.target.value)}
                        className="text-xs mt-1"
                      />
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">Cantidad</label>
                      <Input
                        type="number"
                        min="1"
                        value={chargeQty}
                        onChange={(e) => setChargeQty(Math.max(1, Number(e.target.value)))}
                        className="text-xs mt-1"
                      />
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">Precio Unitario (L) *</label>
                      <Input
                        type="number"
                        step="0.01"
                        required
                        placeholder="0.00"
                        value={chargePrice}
                        onChange={(e) => setChargePrice(e.target.value)}
                        className="text-xs mt-1"
                      />
                    </div>
                  </div>
                  <div className="flex items-center justify-between pt-1">
                    <label className="flex items-center gap-2 text-xs text-muted-foreground cursor-pointer">
                      <input
                        type="checkbox"
                        checked={chargeIsTaxable}
                        onChange={(e) => setChargeIsTaxable(e.target.checked)}
                        className="rounded border-border"
                      />
                      Aplica ISV 15%
                    </label>
                    <div className="flex gap-2">
                      <Button size="sm" type="button" variant="outline" onClick={() => setShowAddCharge(false)}>
                        Cancelar
                      </Button>
                      <Button size="sm" type="submit">
                        Guardar Cargo
                      </Button>
                    </div>
                  </div>
                </form>
              )}

              {/* Folio Items Table */}
              <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
                <div className="p-3.5 border-b border-border/80 bg-muted/20 flex items-center justify-between">
                  <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider flex items-center gap-2">
                    <Receipt size={14} className="text-[#C69C4B]" />
                    Detalle de Cargos y Consumos del Folio
                  </h3>
                  <span className="text-xs text-muted-foreground font-medium">
                    {folio?.items.length || 0} línea(s)
                  </span>
                </div>

                {loadingFolio ? (
                  <div className="flex items-center justify-center py-12">
                    <Loader2 size={24} className="animate-spin text-[#C69C4B] mr-2" />
                    <span className="text-xs text-muted-foreground">Cargando desglose de cargos...</span>
                  </div>
                ) : (
                  <table className="w-full text-xs">
                    <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                      <tr>
                        <th className="text-left p-3">Descripción</th>
                        <th className="text-center p-3 w-16">Cant.</th>
                        <th className="text-right p-3 w-24">P. Unitario</th>
                        <th className="text-right p-3 w-20">ISV</th>
                        <th className="text-right p-3 w-28">Total Línea</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border/60">
                      {folio?.items.map((item, idx) => (
                        <tr key={idx} className="hover:bg-accent/20 transition-colors">
                          <td className="p-3 text-foreground font-medium">{item.description}</td>
                          <td className="p-3 text-center text-muted-foreground">{item.quantity}</td>
                          <td className="p-3 text-right font-mono text-muted-foreground">
                            L {item.unitPrice.toFixed(2)}
                          </td>
                          <td className="p-3 text-right font-mono text-muted-foreground">
                            {item.isExempt ? 'Exento' : `${((item.isvRate || 0.15) * 100).toFixed(0)}%`}
                          </td>
                          <td className="p-3 text-right font-mono font-bold text-foreground">
                            L {item.lineTotal.toFixed(2)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>

              {/* Settlement Configuration: Taxes, Discounts, Payment & Billing Info */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                {/* Left Sub-card: Billing & Fiscal Data */}
                <div className="border border-border rounded-xl p-4 bg-card space-y-3.5 shadow-xs">
                  <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider flex items-center gap-1.5">
                    <Building size={14} className="text-[#C69C4B]" />
                    Datos Fiscales de Facturación SAR
                  </h3>

                  <div>
                    <label className="text-xs text-muted-foreground">Autorización Fiscal CAI *</label>
                    <select
                      value={selectedCaiId}
                      onChange={(e) => setSelectedCaiId(e.target.value)}
                      className="w-full border border-input rounded-md px-2.5 py-1.5 text-xs bg-background mt-1"
                    >
                      {caiList.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.caiNumber} (Vence: {c.dueDate})
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="text-xs text-muted-foreground">Cliente / Razón Social *</label>
                    <Input
                      value={customerName}
                      onChange={(e) => setCustomerName(e.target.value)}
                      placeholder="Nombre del huésped o empresa"
                      className="text-xs mt-1"
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <label className="text-xs text-muted-foreground">RTN Cliente (Opcional)</label>
                      <Input
                        value={customerRtn}
                        onChange={(e) => setCustomerRtn(e.target.value)}
                        placeholder="08011990123456"
                        maxLength={14}
                        className="text-xs mt-1 font-mono"
                      />
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground">Tipo Contribuyente</label>
                      <select
                        value={taxpayerType}
                        onChange={(e) => setTaxpayerType(e.target.value)}
                        className="w-full border border-input rounded-md px-2.5 py-1.5 text-xs bg-background mt-1"
                      >
                        <option value="ConsumidorFinal">Consumidor Final</option>
                        <option value="Empresa">Empresa / Crédito Fiscal</option>
                        <option value="Exonerado">Exonerado Dipl./SEFIN</option>
                      </select>
                    </div>
                  </div>

                  <div>
                    <label className="text-xs text-muted-foreground">Descuento Aplicable</label>
                    <select
                      value={selectedDiscountId}
                      onChange={(e) => setSelectedDiscountId(e.target.value)}
                      className="w-full border border-input rounded-md px-2.5 py-1.5 text-xs bg-background mt-1"
                    >
                      <option value="">Sin descuento adicional</option>
                      {discounts.map((d) => (
                        <option key={d.id} value={d.id}>
                          {d.name} ({d.value}% descuento)
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {/* Right Sub-card: Payment Method & Totals Breakdown */}
                <div className="border border-border rounded-xl p-4 bg-card space-y-3.5 shadow-xs flex flex-col justify-between">
                  <div className="space-y-3">
                    <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider flex items-center gap-1.5">
                      <DollarSign size={14} className="text-[#C69C4B]" />
                      Método de Pago y Liquidación
                    </h3>

                    <div className="grid grid-cols-3 gap-2">
                      {(['Efectivo', 'Tarjeta', 'Transferencia'] as const).map((m) => (
                        <button
                          key={m}
                          type="button"
                          onClick={() => setPaymentMethod(m)}
                          className={`p-2.5 rounded-lg border text-xs font-semibold flex flex-col items-center gap-1 transition-all cursor-pointer ${
                            paymentMethod === m
                              ? 'bg-[#C69C4B]/15 border-[#C69C4B] text-foreground'
                              : 'bg-muted/10 border-border text-muted-foreground hover:text-foreground'
                          }`}
                        >
                          {m === 'Efectivo' && <DollarSign size={16} />}
                          {m === 'Tarjeta' && <CreditCard size={16} />}
                          {m === 'Transferencia' && <Building size={16} />}
                          <span>{m}</span>
                        </button>
                      ))}
                    </div>

                    {paymentMethod === 'Efectivo' && (
                      <div className="grid grid-cols-2 gap-2 p-3 bg-muted/20 rounded-lg border border-border/80">
                        <div>
                          <label className="text-[11px] font-semibold text-muted-foreground">Efectivo Recibido (L)</label>
                          <Input
                            type="number"
                            step="0.01"
                            value={cashGiven}
                            onChange={(e) => setCashGiven(e.target.value)}
                            className="text-xs font-mono mt-1 font-bold"
                          />
                        </div>
                        <div>
                          <label className="text-[11px] font-semibold text-muted-foreground">Cambio a Devolver</label>
                          <p className="text-base font-bold text-emerald-600 dark:text-emerald-400 mt-2 font-mono">
                            L {changeDue.toFixed(2)}
                          </p>
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Totals Summary */}
                  <div className="border-t border-border pt-3 space-y-1.5 text-xs">
                    <div className="flex justify-between text-muted-foreground">
                      <span>Subtotal Bruto:</span>
                      <span className="font-mono">L {subtotalGross.toFixed(2)}</span>
                    </div>
                    {discountAmount > 0 && (
                      <div className="flex justify-between text-amber-600 dark:text-amber-400">
                        <span>Descuento ({discountPercent}%):</span>
                        <span className="font-mono">-L {discountAmount.toFixed(2)}</span>
                      </div>
                    )}
                    <div className="flex justify-between text-muted-foreground">
                      <span>ISV (15%):</span>
                      <span className="font-mono">L {isvAmount.toFixed(2)}</span>
                    </div>
                    {touristTaxAmount > 0 && (
                      <div className="flex justify-between text-muted-foreground">
                        <span>Tasa Turística (4%):</span>
                        <span className="font-mono">L {touristTaxAmount.toFixed(2)}</span>
                      </div>
                    )}
                    <div className="flex justify-between text-base font-bold text-foreground border-t border-border pt-2">
                      <span>Total a Pagar:</span>
                      <span className="text-[#C69C4B] font-mono">L {finalTotal.toFixed(2)}</span>
                    </div>

                    <Button
                      onClick={handleCompleteCheckOut}
                      disabled={actionLoading}
                      className="w-full mt-3 h-10 gap-2 font-semibold text-sm shadow-md"
                    >
                      {actionLoading ? (
                        <Loader2 size={16} className="animate-spin" />
                      ) : (
                        <Sparkles size={16} />
                      )}
                      Facturar y Finalizar Check-Out
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            /* Empty State */
            <div className="border border-dashed border-border rounded-xl p-16 text-center text-muted-foreground bg-card/20 flex flex-col items-center justify-center space-y-3">
              <div className="p-4 rounded-full bg-muted/40 text-muted-foreground">
                <UserMinus size={32} />
              </div>
              <h3 className="font-semibold text-foreground text-sm">Seleccione un Huésped de la Lista</h3>
              <p className="text-xs text-muted-foreground max-w-sm">
                Haga clic en cualquiera de los huéspedes en hospedaje para cargar su folio acumulado, procesar el pago y emitir la factura SAR.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* Confirm Release Only Dialog */}
      <ConfirmDialog
        isOpen={confirmReleaseOnly}
        title="Liberar Habitación sin Facturar"
        description={`¿Está seguro de que desea realizar el Check-Out de la habitación #${selectedReservation?.roomNumber} (${selectedReservation?.guestName}) sin emitir factura fiscal SAR?`}
        confirmText="Liberar Habitación"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleReleaseOnly}
        onCancel={() => setConfirmReleaseOnly(false)}
      />
    </div>
  )
}
