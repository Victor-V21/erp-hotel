import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '@/lib/axios'
import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import api from '@/lib/axios'
import type { Guest, Room, InvoicePrintData } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { BarChart3, Star, AlertCircle } from 'lucide-react'
import { useTaxRates } from '@/hooks/useTaxRates'

interface Discount { id: string; name: string; description?: string; discountType: string; value: number; isActive: boolean; requiresDocument?: boolean; minAge?: number }
interface GuestStats { totalVisits: number; classification: string; lastVisit?: string; isFrequent: boolean }

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
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)

  useEffect(() => {
    if (checkIn.checkInDate && checkIn.checkOutDate && checkIn.checkInDate < checkIn.checkOutDate) {
      api.get<Room[]>('/rooms/available', { params: { checkIn: checkIn.checkInDate, checkOut: checkIn.checkOutDate } }).then(({ data }) => setAvailableRooms(data))
    }
  }, [checkIn.checkInDate, checkIn.checkOutDate])

  useEffect(() => { api.get<Discount[]>('/discounts').then(({ data }) => setDiscounts(data.filter(d => d.isActive))) }, [])

  useEffect(() => {
    if (step === 4 && guestForm.dateOfBirth) {
      const age = calculateAge(guestForm.dateOfBirth);
      if (age >= 60) {
        const discountTerceraEdad = discounts.find(d => (d.minAge && d.minAge <= age) || d.name.toLowerCase().includes('tercera'));
        if (discountTerceraEdad) {
          setSelectedDiscountId(discountTerceraEdad.id);
        }
      }
    }
  }, [step, guestForm.dateOfBirth, discounts]);

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
      setAlertInfo({ variant: 'error', message: 'El RTN debe tener exactamente 14 dígitos numéricos' })
      return
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


