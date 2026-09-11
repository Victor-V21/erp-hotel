import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/errors'
import type { Guest, Reservation, Room } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { AlertCircle, BarChart3, Star } from 'lucide-react'
import { useTaxRates } from '@/hooks/useTaxRates'

interface Discount { id: string; name: string; description?: string; discountType: string; value: number; isActive: boolean; requiresDocument?: boolean; minAge?: number }
interface GuestStats { totalVisits: number; classification: string; lastVisit?: string; isFrequent: boolean }
interface CheckInResult { folioId: string; total: number }

function calculateAge(dob: string | null) {
  if (!dob) return 0;
  const birthDate = new Date(dob);
  const today = new Date();
  let age = today.getFullYear() - birthDate.getFullYear();
  const m = today.getMonth() - birthDate.getMonth();
  if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
    age--;
  }
  return age;
}

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
  const [searchParams] = useSearchParams()
  const reservationId = searchParams.get('reservationId')
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
  const [checkIn, setCheckIn] = useState({ checkInDate: hondurasDate(), checkOutDate: '', adults: 1, children: 0 })
  const [creating, setCreating] = useState(false)
  const [checkInResult, setCheckInResult] = useState<CheckInResult | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [existingReservation, setExistingReservation] = useState<Reservation | null>(null)
  const [loadingExistingReservation, setLoadingExistingReservation] = useState(Boolean(reservationId))

  useEffect(() => {
    if (!reservationId) return
    let cancelled = false

    const loadExistingReservation = async () => {
      try {
        const { data: reservation } = await api.get<Reservation>(`/reservations/${reservationId}`)
        if (reservation.status !== 'Pendiente' && reservation.status !== 'Confirmada') {
          throw new Error(`La reserva está en estado ${reservation.status} y no admite check-in`)
        }
        const [{ data: guest }, { data: room }] = await Promise.all([
          api.get<Guest>(`/guests/${reservation.guestId}`),
          api.get<Room>(`/rooms/${reservation.roomId}`),
        ])
        if (cancelled) return

        setExistingReservation(reservation)
        setSelectedGuest(guest)
        setSelectedRoom(room)
        setGuestForm({
          firstName: guest.firstName, lastName: guest.lastName, email: guest.email || '', phone: guest.phone || '',
          dateOfBirth: guest.dateOfBirth || '', nationality: guest.nationality || '', documentType: guest.documentType || 'DNI',
          documentNumber: guest.documentNumber || '', origin: guest.origin || '', hasVehicle: guest.hasVehicle,
          vehiclePlate: guest.vehiclePlate || '', company: guest.company || '', guestRTN: guest.guestRTN || '',
          preferences: guest.preferences || '', classification: guest.classification || 'Normal',
        })
        setCheckIn({
          checkInDate: reservation.checkInDate,
          checkOutDate: reservation.checkOutDate,
          adults: reservation.adults,
          children: reservation.children,
        })
        setStep(4)
      } catch (error: unknown) {
        if (!cancelled) {
          setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se pudo cargar la reservación seleccionada') })
        }
      } finally {
        if (!cancelled) setLoadingExistingReservation(false)
      }
    }

    void loadExistingReservation()
    return () => { cancelled = true }
  }, [reservationId])

  useEffect(() => {
    if (checkIn.checkInDate && checkIn.checkOutDate && checkIn.checkInDate < checkIn.checkOutDate) {
      api.get<Room[]>('/rooms/available', { params: { checkIn: checkIn.checkInDate, checkOut: checkIn.checkOutDate } }).then(({ data }) => setAvailableRooms(data))
    }
  }, [checkIn.checkInDate, checkIn.checkOutDate])

  useEffect(() => {
    api.get<Discount[]>('/discounts').then(({ data }) => {
      setDiscounts(data.filter(discount => discount.isActive && discount.discountType === 'Porcentaje'))
    })
  }, [])

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
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se pudo crear el huésped') })
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
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se pudieron guardar los datos del huésped') })
    }
  }

  const handleDiscountClick = (id: string) => {
    setSelectedDiscountId(prev => prev === id ? null : id)
  }

  const doCheckIn = async () => {
    if (!selectedGuest || !selectedRoom) return
    if (guestForm.guestRTN && guestForm.guestRTN.length !== 14) {
      setAlertInfo({ variant: 'error', message: 'El RTN debe tener exactamente 14 dígitos numéricos' })
      return
    }
    setCreating(true)
    try {
      let reservation = existingReservation
      if (!reservation) {
        const response = await api.post<Reservation>('/reservations', {
          guestId: selectedGuest.id, roomId: selectedRoom.id,
          checkInDate: checkIn.checkInDate, checkOutDate: checkIn.checkOutDate,
          adults: checkIn.adults, children: checkIn.children,
          paymentMethod: null, advancePayment: 0, notes: null,
        })
        reservation = response.data
      }

      if (selectedGuest.classification !== guestForm.classification) {
        await api.patch(`/guests/${selectedGuest.id}/classification`, { classification: guestForm.classification })
      }

      const { data: result } = await api.post<CheckInResult>('/reservations/checkin', {
        reservationId: reservation.id, roomId: selectedRoom.id,
        expectedVersion: reservation.version,
        discountIds: selectedDiscountId ? [selectedDiscountId] : null
      })

      setCheckInResult(result)
      setStep(6)
    } catch (error: unknown) {
      setAlertInfo({
        variant: 'error',
        message: getApiErrorMessage(error, 'No se pudo completar el check-in. Revise los datos e intente nuevamente.'),
      })
    } finally {
      setCreating(false)
    }
  }

  if (reservationId && loadingExistingReservation) {
    return <p className="py-12 text-center text-sm text-muted-foreground" role="status">Cargando reservación…</p>
  }

  if (reservationId && !existingReservation) {
    return (
      <div className="mx-auto max-w-lg space-y-4">
        <h1 className="text-2xl font-bold">Check-in</h1>
        {alertInfo && <InlineAlert variant={alertInfo.variant} message={alertInfo.message} />}
        <Button variant="outline" onClick={() => navigate('/reservations')}>Volver a reservaciones</Button>
      </div>
    )
  }

  if (step === 6 && checkInResult) {
    return (
      <div className="max-w-lg mx-auto space-y-4">
        <h1 className="text-2xl font-bold">Check-in completado</h1>
        <p className="text-sm text-muted-foreground">
          La estancia y el folio quedaron abiertos. La factura se emitirá al liquidar el check-out.
        </p>
        {alertInfo && (
          <InlineAlert
            variant={alertInfo.variant}
            message={alertInfo.message}
            onClose={() => setAlertInfo(null)}
          />
        )}
        <div className="rounded-lg border border-border bg-card p-4 text-sm">
          <p className="font-medium">Folio abierto</p>
          <p className="mt-1 text-muted-foreground">Estimación inicial: L {checkInResult.total.toFixed(2)}</p>
        </div>
        <Button onClick={() => navigate('/reservations')}>Volver a reservaciones</Button>
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
    return (
      <div className="space-y-4">
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-bold">Confirmar check-in</h3>
          <p className="text-sm"><strong>Huésped:</strong> {guestForm.firstName} {guestForm.lastName}</p>
          <p className="text-sm"><strong>Habitación:</strong> #{selectedRoom.roomNumber} - {selectedRoom.roomTypeName}</p>
          <p className="text-sm"><strong>Check-in:</strong> {checkIn.checkInDate} | <strong>Check-out:</strong> {checkIn.checkOutDate}</p>
          <div className="border-t border-dashed pt-3 space-y-1 text-sm">
            <p className="font-medium mb-1">Estimación inicial del folio</p>
            <div className="flex justify-between"><span>{nights} noche(s) × L {selectedRoom.pricePerNight.toFixed(2)}</span><span>L {sellingTotal.toFixed(2)}</span></div>
            <div className="flex justify-between"><span>Subtotal base:</span><span>L {subtotalBase.toFixed(2)}</span></div>
            {discountPct > 0 && <div className="flex justify-between text-green-700 dark:text-green-400"><span>Descuento ({discountPct}%):</span><span>−L {discountAmount.toFixed(2)}</span></div>}
            <div className="flex justify-between"><span>ISV {(isvRate * 100).toFixed(0)}%:</span><span>L {isv.toFixed(2)}</span></div>
            <div className="flex justify-between"><span>Tasa turística {(touristTaxRate * 100).toFixed(0)}%:</span><span>L {tourist.toFixed(2)}</span></div>
            <div className="flex justify-between font-bold text-base border-t pt-1 mt-1"><span>Total estimado:</span><span>L {totalCalc.toFixed(2)}</span></div>
          </div>
          <p className="text-xs text-muted-foreground">
            Los consumos adicionales y la liquidación se registrarán durante el check-out.
          </p>
        </div>
        <div className="flex gap-2">
          <Button onClick={doCheckIn} disabled={creating}>
            {creating ? 'Abriendo estancia…' : 'Abrir estancia y folio'}
          </Button>
          <Button variant="outline" onClick={() => setStep(4)} disabled={creating}>Atrás</Button>
        </div>
      </div>
    )
  }

  const age = calculateAge(guestForm.dateOfBirth)
  const eligibleAgeDiscounts = discounts.filter(discount => discount.minAge && age >= discount.minAge)

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold">{existingReservation ? 'Check-in de reservación' : 'Nuevo check-in'}</h1>
        {existingReservation && (
          <p className="mt-1 text-sm text-muted-foreground">
            Reserva seleccionada · {existingReservation.guestName} · Habitación #{existingReservation.roomNumber}
          </p>
        )}
      </div>

      {alertInfo && (
        <InlineAlert
          variant={alertInfo.variant}
          message={alertInfo.message}
          onClose={() => setAlertInfo(null)}
        />
      )}

      <div className="flex gap-1 text-xs" aria-label={`Paso ${step} de 5`}>
        {[1, 2, 3, 4, 5].map(currentStep => (
          <div key={currentStep} className={`flex-1 p-1.5 rounded text-center ${step >= currentStep ? 'bg-primary text-primary-foreground' : 'bg-muted text-muted-foreground'}`}>
            {currentStep === 1 ? 'Buscar' : currentStep === 2 ? 'Revisar' : currentStep === 3 ? 'Habitación' : currentStep === 4 ? 'Descuento' : 'Confirmar'}
          </div>
        ))}
      </div>

      {step === 1 && (
        <div className="space-y-4">
          <div className="flex gap-2">
            <Input aria-label="Buscar huésped" placeholder="Buscar huésped por nombre o DNI…" value={searchTerm} onChange={event => setSearchTerm(event.target.value)} />
            <Button variant="outline" onClick={searchGuests}>Buscar</Button>
          </div>
          {foundGuests.length > 0 && (
            <div className="border border-border rounded-lg max-h-60 overflow-y-auto">
              {foundGuests.map(guest => (
                <div key={guest.id} className="p-3 border-b border-border flex justify-between items-center hover:bg-accent">
                  <div className="min-w-0"><span className="font-medium break-words">{guest.fullName}</span><span className="text-muted-foreground ml-2">{guest.documentNumber}</span></div>
                  <Button size="sm" onClick={() => selectGuest(guest)}>Seleccionar</Button>
                </div>
              ))}
            </div>
          )}
          <div className="border-t border-border pt-4">
            <h3 className="font-medium mb-3">Crear un huésped</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div><label htmlFor="guest-first-name" className="text-sm">Nombres *</label><Input id="guest-first-name" value={guestForm.firstName} onChange={event => setGuestForm({...guestForm, firstName: event.target.value})} /></div>
              <div><label htmlFor="guest-last-name" className="text-sm">Apellidos *</label><Input id="guest-last-name" value={guestForm.lastName} onChange={event => setGuestForm({...guestForm, lastName: event.target.value})} /></div>
              <div><label htmlFor="guest-document" className="text-sm">DNI</label><Input id="guest-document" value={guestForm.documentNumber} onChange={event => setGuestForm({...guestForm, documentNumber: event.target.value})} /></div>
              <div><label htmlFor="guest-nationality" className="text-sm">Nacionalidad</label><Input id="guest-nationality" value={guestForm.nationality} onChange={event => setGuestForm({...guestForm, nationality: event.target.value})} /></div>
              <div><label htmlFor="guest-origin" className="text-sm">Procedencia</label><Input id="guest-origin" value={guestForm.origin} onChange={event => setGuestForm({...guestForm, origin: event.target.value})} /></div>
              <div><label htmlFor="guest-phone" className="text-sm">Teléfono</label><Input id="guest-phone" value={guestForm.phone} onChange={event => setGuestForm({...guestForm, phone: event.target.value})} /></div>
              <div><label htmlFor="guest-company" className="text-sm">Empresa</label><Input id="guest-company" value={guestForm.company} onChange={event => setGuestForm({...guestForm, company: event.target.value})} /></div>
              <div><label htmlFor="guest-rtn" className="text-sm">RTN</label><Input id="guest-rtn" inputMode="numeric" value={guestForm.guestRTN} onChange={event => setGuestForm({...guestForm, guestRTN: event.target.value.replace(/\D/g, '').slice(0, 14)})} maxLength={14} /></div>
              <div className="flex items-center gap-2">
                <input id="guest-has-vehicle" type="checkbox" checked={guestForm.hasVehicle} onChange={event => setGuestForm({...guestForm, hasVehicle: event.target.checked})} className="w-4 h-4" />
                <label htmlFor="guest-has-vehicle" className="text-sm">Tiene vehículo</label>
              </div>
              {guestForm.hasVehicle && <div><label htmlFor="guest-plate" className="text-sm">Placa</label><Input id="guest-plate" value={guestForm.vehiclePlate} onChange={event => setGuestForm({...guestForm, vehiclePlate: event.target.value})} /></div>}
            </div>
            <Button className="mt-3" onClick={createGuest} disabled={creating || !guestForm.firstName || !guestForm.lastName}>
              {creating ? 'Creando…' : 'Crear y continuar'}
            </Button>
          </div>
        </div>
      )}

      {step === 2 && selectedGuest && (
        <div className="space-y-4">
          <div className="border border-border rounded-lg p-4 bg-card">
            <h3 className="font-medium mb-3">Revisar información del huésped</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div><label htmlFor="review-first-name" className="text-sm">Nombres</label><Input id="review-first-name" value={guestForm.firstName} onChange={event => setGuestForm({...guestForm, firstName: event.target.value})} /></div>
              <div><label htmlFor="review-last-name" className="text-sm">Apellidos</label><Input id="review-last-name" value={guestForm.lastName} onChange={event => setGuestForm({...guestForm, lastName: event.target.value})} /></div>
              <div><label htmlFor="review-document" className="text-sm">DNI</label><Input id="review-document" value={guestForm.documentNumber} onChange={event => setGuestForm({...guestForm, documentNumber: event.target.value})} /></div>
              <div><label htmlFor="review-nationality" className="text-sm">Nacionalidad</label><Input id="review-nationality" value={guestForm.nationality} onChange={event => setGuestForm({...guestForm, nationality: event.target.value})} /></div>
              <div><label htmlFor="review-origin" className="text-sm">Procedencia</label><Input id="review-origin" value={guestForm.origin} onChange={event => setGuestForm({...guestForm, origin: event.target.value})} /></div>
              <div><label htmlFor="review-phone" className="text-sm">Teléfono</label><Input id="review-phone" value={guestForm.phone} onChange={event => setGuestForm({...guestForm, phone: event.target.value})} /></div>
              <div><label htmlFor="review-company" className="text-sm">Empresa</label><Input id="review-company" value={guestForm.company} onChange={event => setGuestForm({...guestForm, company: event.target.value})} /></div>
              <div><label htmlFor="review-rtn" className="text-sm">RTN</label><Input id="review-rtn" inputMode="numeric" value={guestForm.guestRTN} onChange={event => setGuestForm({...guestForm, guestRTN: event.target.value.replace(/\D/g, '').slice(0, 14)})} maxLength={14} /></div>
              <div className="flex items-center gap-2 pt-2 sm:pt-6">
                <input id="review-has-vehicle" type="checkbox" checked={guestForm.hasVehicle} onChange={event => setGuestForm({...guestForm, hasVehicle: event.target.checked})} className="w-4 h-4" />
                <label htmlFor="review-has-vehicle" className="text-sm">Tiene vehículo</label>
              </div>
              {guestForm.hasVehicle && <div><label htmlFor="review-plate" className="text-sm">Placa</label><Input id="review-plate" value={guestForm.vehiclePlate} onChange={event => setGuestForm({...guestForm, vehiclePlate: event.target.value})} /></div>}
            </div>
          </div>

          {guestStats && (
            <div className="border border-border rounded-lg p-4 bg-muted/30">
              <h4 className="font-medium text-sm mb-2 flex items-center gap-1.5"><BarChart3 size={16} aria-hidden="true" /> Estadísticas</h4>
              <div className="text-sm space-y-1">
                <p>Visitas anteriores: <strong>{guestStats.totalVisits}</strong></p>
                <p className="flex items-center gap-1">Clasificación: <strong className="flex items-center gap-1">{guestStats.isFrequent && <Star size={14} className="text-yellow-600 fill-yellow-600" aria-hidden="true" />}{guestStats.isFrequent ? 'Cliente frecuente' : guestStats.classification}</strong></p>
                <div className="flex items-center gap-2 mt-1">
                  <label htmlFor="guest-classification" className="text-xs">Clasificación manual:</label>
                  <select id="guest-classification" value={guestForm.classification} onChange={event => setGuestForm({...guestForm, classification: event.target.value})} className="border border-input rounded-md px-2 py-1 text-xs bg-background">
                    <option value="Normal">Normal</option>
                    <option value="Cliente Frecuente">Cliente frecuente</option>
                  </select>
                </div>
                {guestStats.lastVisit && <p>Última visita: {guestStats.lastVisit}</p>}
              </div>
            </div>
          )}

          <div className="flex gap-2">
            <Button onClick={updateGuest}>Guardar y continuar</Button>
            <Button variant="outline" onClick={() => setStep(1)}>Atrás</Button>
          </div>
        </div>
      )}

      {step === 3 && (
        <div className="space-y-4">
          <div className="p-3 bg-muted rounded"><strong>Huésped:</strong> {guestForm.firstName} {guestForm.lastName}</div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div><label htmlFor="checkin-date" className="text-sm">Check-in</label><Input id="checkin-date" type="date" value={checkIn.checkInDate} onChange={event => setCheckIn({...checkIn, checkInDate: event.target.value})} /></div>
            <div><label htmlFor="checkout-date" className="text-sm">Check-out</label><Input id="checkout-date" type="date" min={checkIn.checkInDate} value={checkIn.checkOutDate} onChange={event => setCheckIn({...checkIn, checkOutDate: event.target.value})} /></div>
          </div>
          <h3 className="font-medium">Habitaciones disponibles</h3>
          <div className="max-h-64 overflow-y-auto border border-border rounded-lg p-2">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
              {availableRooms.map(room => (
                <button key={room.id} type="button" className={`p-3 border rounded-lg text-left transition-colors ${selectedRoom?.id === room.id ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent'}`} onClick={() => setSelectedRoom(room)} aria-pressed={selectedRoom?.id === room.id}>
                  <div className="font-bold">#{room.roomNumber} · Piso {room.floor}</div>
                  <div className="text-sm text-muted-foreground">{room.roomTypeName}</div>
                  <div className="font-semibold">L {room.pricePerNight.toFixed(2)} / noche</div>
                </button>
              ))}
              {availableRooms.length === 0 && <p className="text-muted-foreground col-span-2">Seleccione fechas válidas para consultar habitaciones disponibles.</p>}
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
            <h3 className="font-medium mb-2">Descuentos disponibles</h3>
            {eligibleAgeDiscounts.length > 0 && (
              <div className="mb-3 flex gap-2 rounded-lg bg-blue-50 p-3 text-sm text-blue-900 dark:bg-blue-950/40 dark:text-blue-200" role="status">
                <AlertCircle size={17} className="mt-0.5 shrink-0" aria-hidden="true" />
                <span>La edad registrada coincide con {eligibleAgeDiscounts.length === 1 ? 'un descuento configurado' : 'descuentos configurados'}. Verifique el documento y la regla vigente antes de seleccionarlo.</span>
              </div>
            )}
            {discounts.length === 0 && <p className="text-sm text-muted-foreground">No hay descuentos activos.</p>}
            {discounts.map(discount => (
              <button
                type="button"
                key={discount.id}
                className={`w-full p-3 border rounded-lg mb-2 text-left transition-colors ${selectedDiscountId === discount.id ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'border-border hover:bg-accent'}`}
                onClick={() => handleDiscountClick(discount.id)}
                aria-pressed={selectedDiscountId === discount.id}
              >
                <div className="flex items-center gap-2">
                  <div className={`w-4 h-4 rounded-full border-2 flex items-center justify-center ${selectedDiscountId === discount.id ? 'border-primary' : 'border-muted-foreground'}`} aria-hidden="true">
                    {selectedDiscountId === discount.id && <div className="w-2 h-2 rounded-full bg-primary" />}
                  </div>
                  <div className="min-w-0">
                    <div className="font-medium text-sm break-words">{discount.name}</div>
                    {discount.description && <div className="text-xs text-muted-foreground break-words">{discount.description}</div>}
                    {discount.requiresDocument && <div className="text-xs text-amber-700 dark:text-amber-400">Requiere documento</div>}
                  </div>
                  <div className="ml-auto shrink-0 font-bold text-sm">{discount.discountType === 'Porcentaje' ? `${discount.value}%` : `L ${discount.value.toFixed(2)}`}</div>
                </div>
              </button>
            ))}
            {guestForm.classification === 'Cliente Frecuente' && !selectedDiscountId && (
              <div className="p-2 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded text-sm text-green-800 dark:text-green-300 flex items-center gap-1">
                <Star size={14} className="fill-green-600 text-green-600" aria-hidden="true" /> Cliente frecuente detectado. Seleccione el descuento solo si corresponde.
              </div>
            )}
          </div>

          <div className="border border-border rounded-lg p-4 bg-card">
            <h4 className="font-medium text-sm mb-2">Resumen</h4>
            <p className="text-sm">{selectedRoom.roomTypeName} · Habitación #{selectedRoom.roomNumber}</p>
          </div>

          <div className="flex gap-2">
            <Button onClick={() => setStep(5)}>Revisar check-in</Button>
            <Button variant="outline" onClick={() => existingReservation ? navigate('/reservations') : setStep(3)}>Atrás</Button>
          </div>
        </div>
      )}

      {renderStep5()}
    </div>
  )
}
