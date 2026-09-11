import { useState, useEffect, useCallback, useMemo, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import axios from 'axios'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/errors'
import { useAuthStore } from '@/store/authStore'
import type { Invoice, CAI, DocumentAuthorization, CashRegister, Payment } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { Pagination } from '@/components/ui/Pagination'
import {
  Receipt,
  Search,
  Printer,
  Loader2,
  FileMinus,
  FilePlus,
  X,
  Banknote,
  CreditCard,
  Landmark,
  WalletCards,
  Undo2,
} from 'lucide-react'

type AdjustmentKind = 'credit-note' | 'debit-note'
type RefundCreditNoteOption = {
  id: string
  correlativeNumber: string
  totalAmount: number
  availableAmount: number
}

const adjustmentIntentKey = (kind: AdjustmentKind, invoiceId: string) =>
  `hotel-erp:${kind}:${invoiceId}`

const getOrCreateAdjustmentIntent = (kind: AdjustmentKind, invoiceId: string) => {
  const storageKey = adjustmentIntentKey(kind, invoiceId)
  const existing = window.sessionStorage.getItem(storageKey)
  if (existing) return existing
  const key = window.crypto.randomUUID()
  window.sessionStorage.setItem(storageKey, key)
  return key
}

const clearAdjustmentIntent = (kind: AdjustmentKind, invoiceId: string) =>
  window.sessionStorage.removeItem(adjustmentIntentKey(kind, invoiceId))

const paymentIntentKey = (invoiceId: string) => `hotel-erp:payment:${invoiceId}`

const getOrCreatePaymentIntent = (invoiceId: string) => {
  const storageKey = paymentIntentKey(invoiceId)
  const existing = window.sessionStorage.getItem(storageKey)
  if (existing) return existing
  const key = window.crypto.randomUUID()
  window.sessionStorage.setItem(storageKey, key)
  return key
}

const clearPaymentIntent = (invoiceId: string) =>
  window.sessionStorage.removeItem(paymentIntentKey(invoiceId))

const refundIntentKey = (paymentId: string) => `hotel-erp:refund:${paymentId}`

const getOrCreateRefundIntent = (paymentId: string) => {
  const storageKey = refundIntentKey(paymentId)
  const existing = window.sessionStorage.getItem(storageKey)
  if (existing) return existing
  const key = window.crypto.randomUUID()
  window.sessionStorage.setItem(storageKey, key)
  return key
}

const clearRefundIntent = (paymentId: string) =>
  window.sessionStorage.removeItem(refundIntentKey(paymentId))

const formatPaymentDate = (value: string) => new Intl.DateTimeFormat('es-HN', {
  dateStyle: 'short',
  timeStyle: 'short',
  timeZone: 'America/Tegucigalpa',
}).format(new Date(value))

const isDefinitiveAdjustmentFailure = (error: unknown) =>
  axios.isAxiosError(error) &&
  !!error.response &&
  [400, 401, 403, 404, 409, 422].includes(error.response.status)

export default function InvoicesPage() {
  const canManageCash = useAuthStore((state) => state.hasPermission('manage_cash'))
  const canCreateInvoices = useAuthStore((state) => state.hasPermission('create_invoices'))
  const [searchParams] = useSearchParams()
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [dni, setDni] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [authList, setAuthList] = useState<{
    id: string
    source: 'cai' | 'docauth'
    documentType: 'Factura' | 'NotaCredito' | 'NotaDebito'
    label: string
  }[]>([])
  const [selectedAuth, setSelectedAuth] = useState('')
  const [docTypeFilter, setDocTypeFilter] = useState('Todos')

  const [invoicePreview, setInvoicePreview] = useState<string | null>(null)
  const [invoiceLogo, setInvoiceLogo] = useState<string | null>(null)
  const [previewLogoHeight, setPreviewLogoHeight] = useState(40)
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null)
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null)
  const [loading, setLoading] = useState(false)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)

  // Credit Note Modal State
  const [showCreditModal, setShowCreditModal] = useState(false)
  const [creditReason, setCreditReason] = useState('')
  const [creditAuthorizationId, setCreditAuthorizationId] = useState('')
  const [creditActionLoading, setCreditActionLoading] = useState(false)
  const [creditItems, setCreditItems] = useState<{ id: string, idx: number, description: string, quantity: number, maxQuantity: number, unitPrice: number, isExempt: boolean, isvRate: number, isTouristTaxable: boolean, discountPercentage: number, included: boolean }[]>([])

  // Debit Note Modal State
  const [showDebitModal, setShowDebitModal] = useState(false)
  const [debitReason, setDebitReason] = useState('')
  const [debitAmount, setDebitAmount] = useState('')
  const [debitDesc, setDebitDesc] = useState('')
  const [debitAuthorizationId, setDebitAuthorizationId] = useState('')
  const [debitActionLoading, setDebitActionLoading] = useState(false)

  // Payment modal and invoice ledger
  const [showPaymentModal, setShowPaymentModal] = useState(false)
  const [payments, setPayments] = useState<Payment[]>([])
  const [paymentsLoading, setPaymentsLoading] = useState(false)
  const [paymentMethod, setPaymentMethod] = useState<'Efectivo' | 'Tarjeta' | 'Transferencia'>('Efectivo')
  const [paymentAmount, setPaymentAmount] = useState('')
  const [cashReceived, setCashReceived] = useState('')
  const [paymentReference, setPaymentReference] = useState('')
  const [cashRegisters, setCashRegisters] = useState<CashRegister[]>([])
  const [selectedCashRegisterId, setSelectedCashRegisterId] = useState('')
  const [paymentActionLoading, setPaymentActionLoading] = useState(false)

  // Refund modal
  const [showRefundModal, setShowRefundModal] = useState(false)
  const [refundPayment, setRefundPayment] = useState<Payment | null>(null)
  const [refundCreditNotes, setRefundCreditNotes] = useState<RefundCreditNoteOption[]>([])
  const [selectedRefundCreditNoteId, setSelectedRefundCreditNoteId] = useState('')
  const [refundAmount, setRefundAmount] = useState('')
  const [refundLimit, setRefundLimit] = useState(0)
  const [refundReason, setRefundReason] = useState('')
  const [refundReference, setRefundReference] = useState('')
  const [refundCashRegisters, setRefundCashRegisters] = useState<CashRegister[]>([])
  const [selectedRefundCashRegisterId, setSelectedRefundCashRegisterId] = useState('')
  const [refundSetupLoading, setRefundSetupLoading] = useState(false)
  const [refundActionLoading, setRefundActionLoading] = useState(false)

  // Pagination
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)
  const initialized = useRef(false)

  const loadAuthList = useCallback(async () => {
    try {
      const [caiRes, docAuthRes] = await Promise.all([
        api.get<CAI[]>('/cai'),
        api.get<DocumentAuthorization[]>('/document-authorizations'),
      ])
      const list = [
        ...caiRes.data.map((c) => ({
          id: c.id,
          source: 'cai' as const,
          documentType: 'Factura' as const,
          label: `${c.caiNumber} (Factura general)`,
        })),
        ...docAuthRes.data
          .filter((d) => ['Factura', 'NotaCredito', 'NotaDebito'].includes(d.documentType))
          .map((d) => ({
          id: d.id,
          source: 'docauth' as const,
          documentType: d.documentType as 'Factura' | 'NotaCredito' | 'NotaDebito',
          label: `${d.caiNumber} (${d.documentType})`,
          })),
      ]
      setAuthList(list)
      setCreditAuthorizationId(
        list.find((item) => item.source === 'docauth' && item.documentType === 'NotaCredito')?.id ?? ''
      )
      setDebitAuthorizationId(
        list.find((item) => item.source === 'docauth' && item.documentType === 'NotaDebito')?.id ?? ''
      )
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al cargar el listado de autorizaciones fiscales.' })
    }
  }, [])

  const load = useCallback(
    async (authFilter?: string) => {
      setLoading(true)
      try {
        const params: Record<string, string> = {}
        const filter = authFilter ?? selectedAuth
        if (filter) {
          const [source, id] = filter.split(':')
          if (source === 'cai') params.caiId = id
          else if (source === 'docauth') params.documentAuthorizationId = id
        }
        if (dni) params.dni = dni
        if (startDate && endDate) {
          params.start = startDate
          params.end = endDate
        }
        const { data } = await api.get<Invoice[]>('/invoices/search', { params })
        setInvoices(data)
      } catch {
        setAlertInfo({ variant: 'error', message: 'Error al consultar las facturas registradas.' })
      } finally {
        setLoading(false)
      }
    },
    [selectedAuth, dni, startDate, endDate]
  )

  const loadPayments = useCallback(async (invoiceId: string) => {
    if (!canManageCash) {
      setPayments([])
      return
    }
    setPaymentsLoading(true)
    try {
      const { data } = await api.get<Payment[]>('/payments', { params: { invoiceId } })
      setPayments(data)
    } catch (error: unknown) {
      setPayments([])
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se pudo consultar el auxiliar de pagos.') })
    } finally {
      setPaymentsLoading(false)
    }
  }, [canManageCash])

  useEffect(() => {
    if (initialized.current) return
    initialized.current = true
    const init = async () => {
      await loadAuthList()
      const filter = searchParams.get('filter')
      if (filter) {
        setSelectedAuth(filter)
        await load(filter)
      } else {
        await load()
      }
    }
    void init()
  }, [load, loadAuthList, searchParams])

  const viewPrint = async (inv: Invoice) => {
    setSelectedInvoiceId(inv.id)
    setSelectedInvoice(inv)
    void loadPayments(inv.id)
    try {
      const { data } = await api.get<{ text: string; logoBase64: string; printLogoHeight?: number }>(
        `/print/invoice/${inv.id}/preview`
      )
      setInvoicePreview(data.text)
      setInvoiceLogo(data.logoBase64 || null)
      if (data.printLogoHeight) setPreviewLogoHeight(data.printLogoHeight)
    } catch {
      setInvoicePreview(null)
      setInvoiceLogo(null)
      setAlertInfo({ variant: 'info', message: 'Vista previa no disponible para este comprobante.' })
    }
  }

  const openPaymentModal = async () => {
    if (!selectedInvoice || selectedInvoice.balanceDue <= 0) return
    setPaymentMethod('Efectivo')
    setPaymentAmount(selectedInvoice.balanceDue.toFixed(2))
    setCashReceived(selectedInvoice.balanceDue.toFixed(2))
    setPaymentReference('')
    setCashRegisters([])
    setSelectedCashRegisterId('')
    setShowPaymentModal(true)
    try {
      const { data } = await api.get<CashRegister[]>('/cash-registers')
      const openRegisters = data.filter((register) => register.isActive && register.isOpen)
      setCashRegisters(openRegisters)
      setSelectedCashRegisterId(openRegisters[0]?.id ?? '')
    } catch (error: unknown) {
      setCashRegisters([])
      setSelectedCashRegisterId('')
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se pudieron consultar las cajas abiertas.') })
    }
  }

  const handlePaymentSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!selectedInvoice) return

    const amount = Math.round(Number(paymentAmount) * 100) / 100
    const received = Math.round(Number(cashReceived) * 100) / 100
    if (!Number.isFinite(amount) || amount <= 0 || amount > selectedInvoice.balanceDue) {
      setAlertInfo({ variant: 'error', message: 'El abono debe ser mayor que cero y no exceder el saldo pendiente.' })
      return
    }
    if (paymentMethod === 'Efectivo' && (!selectedCashRegisterId || !Number.isFinite(received) || received < amount)) {
      setAlertInfo({ variant: 'error', message: 'Seleccione una caja abierta e ingrese efectivo suficiente.' })
      return
    }
    if (paymentMethod !== 'Efectivo' && paymentReference.trim().length < 3) {
      setAlertInfo({ variant: 'error', message: 'Tarjeta y transferencia requieren una referencia de al menos 3 caracteres.' })
      return
    }

    const intentId = getOrCreatePaymentIntent(selectedInvoice.id)
    setPaymentActionLoading(true)
    try {
      const { data } = await api.post<Payment>(
        '/payments',
        {
          method: paymentMethod,
          currency: 'HNL',
          amount,
          cashReceived: paymentMethod === 'Efectivo' ? received : null,
          cashRegisterId: paymentMethod === 'Efectivo' ? selectedCashRegisterId : null,
          externalReference: paymentMethod === 'Efectivo' ? null : paymentReference.trim(),
          applications: [{ invoiceId: selectedInvoice.id, amount }],
        },
        { headers: { 'Idempotency-Key': intentId } }
      )
      clearPaymentIntent(selectedInvoice.id)
      const { data: updatedInvoice } = await api.get<Invoice>(`/invoices/${selectedInvoice.id}`)
      setSelectedInvoice(updatedInvoice)
      setInvoices((current) => current.map((invoice) => invoice.id === updatedInvoice.id ? updatedInvoice : invoice))
      await loadPayments(selectedInvoice.id)
      setShowPaymentModal(false)
      setAlertInfo({
        variant: 'success',
        message: `Pago ${data.paymentNumber} registrado por L ${data.amount.toFixed(2)}. Saldo: L ${updatedInvoice.balanceDue.toFixed(2)}.`,
      })
    } catch (error: unknown) {
      if (isDefinitiveAdjustmentFailure(error)) clearPaymentIntent(selectedInvoice.id)
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se pudo registrar el pago.') })
    } finally {
      setPaymentActionLoading(false)
    }
  }

  const openRefundModal = async (payment: Payment) => {
    if (!selectedInvoice || payment.amount - payment.refundedAmount <= 0) return

    setRefundPayment(payment)
    setRefundCreditNotes([])
    setSelectedRefundCreditNoteId('')
    setRefundAmount('')
    setRefundLimit(0)
    setRefundReason('')
    setRefundReference('')
    setRefundCashRegisters([])
    setSelectedRefundCashRegisterId('')
    setShowRefundModal(true)
    setRefundSetupLoading(true)

    try {
      const [invoiceResponse, cashResponse] = await Promise.all([
        api.get<Invoice[]>('/invoices/search', { params: { originalInvoiceId: selectedInvoice.id } }),
        payment.method === 'Efectivo'
          ? api.get<CashRegister[]>('/cash-registers')
          : Promise.resolve({ data: [] as CashRegister[] }),
      ])
      const notes = invoiceResponse.data.filter((invoice) =>
        invoice.documentType === 'NotaCredito' && invoice.originalInvoiceId === selectedInvoice.id)
      const noteIds = new Set(notes.map((note) => note.id))
      const confirmedRefunds = payments.flatMap((candidate) =>
        candidate.refunds.filter((refund) => refund.status === 'Confirmado'))
      const refundedForInvoice = confirmedRefunds.reduce(
        (sum, refund) => sum + refund.applications
          .filter((application) => noteIds.has(application.creditNoteId))
          .reduce((applicationSum, application) => applicationSum + application.amount, 0),
        0)
      const customerCredit = Math.max(
        0,
        Math.round((selectedInvoice.paidAmount + selectedInvoice.creditedAmount
          - selectedInvoice.totalAmount - refundedForInvoice) * 100) / 100)
      const paymentRemaining = Math.max(
        0,
        Math.round((payment.amount - payment.refundedAmount) * 100) / 100)
      const options = notes
        .map((note) => {
          const refundedForNote = confirmedRefunds.reduce(
            (sum, refund) => sum + refund.applications
              .filter((application) => application.creditNoteId === note.id)
              .reduce((applicationSum, application) => applicationSum + application.amount, 0),
            0)
          return {
            id: note.id,
            correlativeNumber: note.correlativeNumber,
            totalAmount: note.totalAmount,
            availableAmount: Math.max(
              0,
              Math.round(Math.min(
                note.totalAmount - refundedForNote,
                customerCredit,
                paymentRemaining) * 100) / 100),
          }
        })
        .filter((note) => note.availableAmount > 0)

      if (options.length === 0) {
        setShowRefundModal(false)
        setAlertInfo({
          variant: 'info',
          message: 'Este pago no tiene un saldo a favor pendiente respaldado por una nota de crédito.',
        })
        return
      }

      const openRegisters = cashResponse.data.filter((register) => register.isActive && register.isOpen)
      setRefundCreditNotes(options)
      setSelectedRefundCreditNoteId(options[0].id)
      setRefundLimit(options[0].availableAmount)
      setRefundAmount(options[0].availableAmount.toFixed(2))
      setRefundCashRegisters(openRegisters)
      setSelectedRefundCashRegisterId(openRegisters[0]?.id ?? '')
    } catch (error: unknown) {
      setShowRefundModal(false)
      setAlertInfo({
        variant: 'error',
        message: getApiErrorMessage(error, 'No se pudo preparar el reembolso.'),
      })
    } finally {
      setRefundSetupLoading(false)
    }
  }

  const handleRefundSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!selectedInvoice || !refundPayment || !selectedRefundCreditNoteId) return

    const amount = Math.round(Number(refundAmount) * 100) / 100
    const reason = refundReason.trim()
    if (!Number.isFinite(amount) || amount <= 0 || amount > refundLimit) {
      setAlertInfo({ variant: 'error', message: 'El reembolso debe ser mayor que cero y no exceder el saldo respaldado.' })
      return
    }
    if (reason.length < 3) {
      setAlertInfo({ variant: 'error', message: 'Explique el motivo del reembolso con al menos 3 caracteres.' })
      return
    }
    if (refundPayment.method === 'Efectivo') {
      const selectedRegister = refundCashRegisters.find((register) => register.id === selectedRefundCashRegisterId)
      if (!selectedRegister || selectedRegister.currentBalance < amount) {
        setAlertInfo({ variant: 'error', message: 'Seleccione una caja abierta con saldo suficiente.' })
        return
      }
    } else if (refundReference.trim().length < 3) {
      setAlertInfo({ variant: 'error', message: 'La devolución requiere una referencia de al menos 3 caracteres.' })
      return
    }

    const intentId = getOrCreateRefundIntent(refundPayment.id)
    setRefundActionLoading(true)
    try {
      const { data } = await api.post(
        `/payments/${refundPayment.id}/refunds`,
        {
          amount,
          cashRegisterId: refundPayment.method === 'Efectivo' ? selectedRefundCashRegisterId : null,
          externalReference: refundPayment.method === 'Efectivo' ? null : refundReference.trim(),
          reason,
          applications: [{ creditNoteId: selectedRefundCreditNoteId, amount }],
        },
        { headers: { 'Idempotency-Key': intentId } }
      )
      clearRefundIntent(refundPayment.id)
      const { data: updatedInvoice } = await api.get<Invoice>(`/invoices/${selectedInvoice.id}`)
      setSelectedInvoice(updatedInvoice)
      setInvoices((current) => current.map((invoice) => invoice.id === updatedInvoice.id ? updatedInvoice : invoice))
      await loadPayments(selectedInvoice.id)
      setShowRefundModal(false)
      setAlertInfo({
        variant: 'success',
        message: `Reembolso ${data.refundNumber} registrado por L ${data.amount.toFixed(2)}.`,
      })
    } catch (error: unknown) {
      if (isDefinitiveAdjustmentFailure(error)) clearRefundIntent(refundPayment.id)
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se pudo registrar el reembolso.') })
    } finally {
      setRefundActionLoading(false)
    }
  }

  const printInvoice = async () => {
    if (!selectedInvoiceId) return
    try {
      await api.post(`/print/invoice/${selectedInvoiceId}`)
      setAlertInfo({ variant: 'success', message: 'Comprobante fiscal enviado a imprimir correctamente.' })
    } catch {
      setAlertInfo({ variant: 'error', message: 'No se pudo conectar con la impresora térmica.' })
    }
  }

  // Handle Credit Note Submit
  const handleCreditNoteSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedInvoice || !creditReason.trim()) return
    if (!creditAuthorizationId) {
      setAlertInfo({ variant: 'error', message: 'No hay una autorización activa para notas de crédito.' })
      return
    }
    const selectedItems = creditItems.filter((item) => item.included && item.quantity > 0 && item.id)
    if (selectedItems.length === 0) {
      setAlertInfo({ variant: 'error', message: 'Seleccione al menos una línea y una cantidad para acreditar.' })
      return
    }

    const intentId = getOrCreateAdjustmentIntent('credit-note', selectedInvoice.id)
    setCreditActionLoading(true)
    try {
      const { data } = await api.post(
        `/invoices/${selectedInvoice.id}/credit-note`,
        {
          originalInvoiceId: selectedInvoice.id,
          caiId: selectedInvoice.caiId,
          documentAuthorizationId: creditAuthorizationId,
          reason: creditReason.trim(),
          items: selectedItems.map((item) => ({
            originalInvoiceItemId: item.id,
            quantity: item.quantity,
          })),
        },
        { headers: { 'Idempotency-Key': intentId } }
      )

      clearAdjustmentIntent('credit-note', selectedInvoice.id)
      setAlertInfo({
        variant: 'success',
        message: `Nota de Crédito ${data.correlativeNumber} emitida exitosamente para la factura ${selectedInvoice.correlativeNumber}.`,
      })
      setShowCreditModal(false)
      setCreditReason('')
      load()
      viewPrint(data)
    } catch (error: unknown) {
      if (isDefinitiveAdjustmentFailure(error)) {
        clearAdjustmentIntent('credit-note', selectedInvoice.id)
      }
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al emitir la nota de crédito.') })
    } finally {
      setCreditActionLoading(false)
    }
  }

  // Handle Debit Note Submit
  const handleDebitNoteSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedInvoice || !debitReason.trim() || !debitAmount || Number(debitAmount) <= 0) return
    if (!debitAuthorizationId) {
      setAlertInfo({ variant: 'error', message: 'No hay una autorización activa para notas de débito.' })
      return
    }

    const intentId = getOrCreateAdjustmentIntent('debit-note', selectedInvoice.id)
    setDebitActionLoading(true)
    try {
      const { data } = await api.post(
        `/invoices/${selectedInvoice.id}/debit-note`,
        {
          originalInvoiceId: selectedInvoice.id,
          caiId: selectedInvoice.caiId,
          documentAuthorizationId: debitAuthorizationId,
          reason: debitReason.trim(),
          items: [
            {
              description: debitDesc.trim() || `Recargo por ${debitReason.trim()}`,
              quantity: 1,
              unitPrice: Number(debitAmount),
              isExempt: false,
              isvRate: 0.15,
              isTouristTaxable: false,
              discountPercentage: 0,
            },
          ],
        },
        { headers: { 'Idempotency-Key': intentId } }
      )

      clearAdjustmentIntent('debit-note', selectedInvoice.id)
      setAlertInfo({
        variant: 'success',
        message: `Nota de Débito ${data.correlativeNumber} emitida exitosamente.`,
      })
      setShowDebitModal(false)
      setDebitReason('')
      setDebitAmount('')
      setDebitDesc('')
      load()
      viewPrint(data)
    } catch (error: unknown) {
      if (isDefinitiveAdjustmentFailure(error)) {
        clearAdjustmentIntent('debit-note', selectedInvoice.id)
      }
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al emitir la nota de débito.') })
    } finally {
      setDebitActionLoading(false)
    }
  }

  const filteredInvoices = useMemo(() => {
    return invoices.filter((inv) => {
      if (docTypeFilter === 'Factura') return inv.documentType === 'Factura' || !inv.documentType
      if (docTypeFilter === 'NotaCredito') return inv.documentType === 'NotaCredito'
      if (docTypeFilter === 'NotaDebito') return inv.documentType === 'NotaDebito'
      return true
    })
  }, [invoices, docTypeFilter])

  const totalPages = Math.ceil(filteredInvoices.length / pageSize) || 1
  const paginatedInvoices = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredInvoices.slice(start, start + pageSize)
  }, [filteredInvoices, currentPage, pageSize])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <Receipt className="text-[#C69C4B]" size={24} />
            Facturación SAR, Notas de Crédito & Débito
          </h1>
          <p className="text-sm text-muted-foreground">
            Consulta de comprobantes, correlativos CAI, saldos y movimientos de cobro.
          </p>
        </div>
      </div>

      {alertInfo && (
        <InlineAlert variant={alertInfo.variant} message={alertInfo.message} onClose={() => setAlertInfo(null)} />
      )}

      {/* Filter and Search Bar */}
      <div className="p-4 rounded-xl border border-border bg-card shadow-xs grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
        <div>
          <label className="text-xs font-semibold text-muted-foreground">Filtro Documento</label>
          <select
            value={docTypeFilter}
            onChange={(e) => {
              setDocTypeFilter(e.target.value)
              setCurrentPage(1)
            }}
            className="w-full border border-input rounded-md px-2.5 py-1.5 text-xs bg-background mt-1"
          >
            <option value="Todos">Todos los Documentos</option>
            <option value="Factura">Facturas Fiscales</option>
            <option value="NotaCredito">Notas de Crédito</option>
            <option value="NotaDebito">Notas de Débito</option>
          </select>
        </div>

        <div>
          <label className="text-xs font-semibold text-muted-foreground">DNI o RTN Cliente</label>
          <Input
            placeholder="0801-1990-12345"
            value={dni}
            onChange={(e) => setDni(e.target.value)}
            className="text-xs mt-1 font-mono"
          />
        </div>

        <div>
          <label className="text-xs font-semibold text-muted-foreground">Desde</label>
          <Input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            className="text-xs mt-1"
          />
        </div>

        <div>
          <label className="text-xs font-semibold text-muted-foreground">Hasta</label>
          <Input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            className="text-xs mt-1"
          />
        </div>

        <div className="flex items-end gap-2">
          <Button onClick={() => load()} disabled={loading} className="w-full text-xs gap-1.5 h-9">
            {loading ? <Loader2 size={13} className="animate-spin" /> : <Search size={13} />}
            Filtrar
          </Button>
        </div>
      </div>

      {/* Main Grid: Table & Print Preview */}
      <div className="grid grid-cols-1 xl:grid-cols-12 gap-6">
        {/* Invoices Table */}
        <div className="xl:col-span-7 space-y-4">
          <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
            <div className="p-3.5 border-b border-border bg-muted/20 flex items-center justify-between">
              <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider">
                Comprobantes Fiscales
              </h3>
              <span className="text-xs text-muted-foreground">{filteredInvoices.length} registrado(s)</span>
            </div>

            {loading ? (
              <div className="flex items-center justify-center py-16">
                <Loader2 size={26} className="animate-spin text-[#C69C4B] mr-2" />
                <span className="text-xs text-muted-foreground">Cargando facturas...</span>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-xs">
                  <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                    <tr>
                      <th className="text-left p-3">Correlativo</th>
                      <th className="text-left p-3">Cliente</th>
                      <th className="text-left p-3 w-20">Tipo</th>
                      <th className="text-right p-3 w-24">Total</th>
                      <th className="text-right p-3 w-24">Saldo</th>
                      <th className="text-left p-3 w-20">Estado</th>
                      <th className="text-right p-3 w-24">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/60">
                    {paginatedInvoices.map((inv) => (
                      <tr
                        key={inv.id}
                        className={`cursor-pointer transition-colors ${
                          selectedInvoiceId === inv.id ? 'bg-[#C69C4B]/15 font-semibold' : 'hover:bg-accent/20'
                        }`}
                        onClick={() => viewPrint(inv)}
                      >
                        <td className="p-3 font-mono text-xs font-bold text-foreground">
                          {inv.correlativeNumber}
                        </td>
                        <td className="p-3 text-foreground truncate max-w-[120px]">
                          {inv.customerName}
                        </td>
                        <td className="p-3">
                          <span
                            className={`px-1.5 py-0.5 rounded text-[10px] font-semibold ${
                              inv.documentType === 'NotaCredito'
                                ? 'bg-amber-100 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300'
                                : inv.documentType === 'NotaDebito'
                                ? 'bg-blue-100 dark:bg-blue-950/40 text-blue-800 dark:text-blue-300'
                                : 'bg-primary/10 text-foreground'
                            }`}
                          >
                            {inv.documentType === 'NotaCredito'
                              ? 'N. Crédito'
                              : inv.documentType === 'NotaDebito'
                              ? 'N. Débito'
                              : 'Factura'}
                          </span>
                        </td>
                        <td className="p-3 text-right font-mono font-bold text-foreground">
                          L {inv.totalAmount.toFixed(2)}
                        </td>
                        <td className={`p-3 text-right font-mono font-bold ${inv.balanceDue > 0 ? 'text-amber-700 dark:text-amber-300' : 'text-emerald-700 dark:text-emerald-300'}`}>
                          L {inv.balanceDue.toFixed(2)}
                        </td>
                        <td className="p-3">
                          <span
                            className={`px-1.5 py-0.2 rounded text-[10px] font-semibold ${
                              inv.status === 'Anulada'
                                ? 'bg-red-100 dark:bg-red-950/40 text-red-700 dark:text-red-300'
                                : inv.balanceDue > 0
                                  ? 'bg-amber-100 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300'
                                  : 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300'
                            }`}
                          >
                            {inv.status}
                          </span>
                        </td>
                        <td className="p-3 text-right text-[10px] text-muted-foreground">
                          Seleccionar
                        </td>
                      </tr>
                    ))}
                    {paginatedInvoices.length === 0 && (
                      <tr>
                        <td colSpan={7} className="p-8 text-center text-muted-foreground">
                          No se encontraron comprobantes fiscales con los criterios seleccionados.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}

            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalItems={filteredInvoices.length}
              pageSize={pageSize}
              onPageChange={setCurrentPage}
              onPageSizeChange={(size) => {
                setPageSize(size)
                setCurrentPage(1)
              }}
            />
          </div>
        </div>

        {/* Right Preview and Action Column */}
        <div className="xl:col-span-5 space-y-4">
          {selectedInvoice && invoicePreview ? (
            <div className="border border-border rounded-xl p-5 bg-card space-y-4 shadow-xs">
              <div className="flex items-center justify-between border-b border-border pb-3">
                <div>
                  <h2 className="font-bold text-foreground text-sm flex items-center gap-2">
                    <Printer size={16} className="text-[#C69C4B]" />
                    Vista Previa Térmica
                  </h2>
                  <p className="text-[11px] text-muted-foreground font-mono">{selectedInvoice.correlativeNumber}</p>
                </div>
                <div className="flex flex-wrap justify-end gap-1.5">
                  {canManageCash &&
                    selectedInvoice.documentType !== 'NotaCredito' &&
                    selectedInvoice.status !== 'Anulada' &&
                    selectedInvoice.balanceDue > 0 && (
                      <Button
                        size="sm"
                        onClick={() => void openPaymentModal()}
                        className="text-xs h-7 gap-1 bg-emerald-700 hover:bg-emerald-800 text-white"
                      >
                        <WalletCards size={12} /> Registrar abono
                      </Button>
                    )}
                  {canCreateInvoices && selectedInvoice.status !== 'Anulada' && selectedInvoice.documentType === 'Factura' && (
                    <>
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={!creditAuthorizationId}
                        onClick={() => {
                          setCreditItems(
                            selectedInvoice.items.map((it, idx) => ({
                              id: it.id ?? '',
                              idx,
                              description: it.description,
                              quantity: it.quantity,
                              maxQuantity: it.quantity,
                              unitPrice: it.unitPrice,
                              isExempt: it.isExempt,
                              isvRate: it.isvRate,
                              isTouristTaxable: it.isTouristTaxable,
                              discountPercentage: it.discountPercentage,
                              included: true
                            }))
                          )
                          setShowCreditModal(true)
                        }}
                        className="text-xs h-7 text-amber-600 dark:text-amber-400 gap-1 border-amber-500/30"
                        title={creditAuthorizationId ? 'Emitir Nota de Crédito por devolución o anulación' : 'Falta autorización activa para Nota de Crédito'}
                      >
                        <FileMinus size={12} /> N. Crédito
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={!debitAuthorizationId}
                        onClick={() => setShowDebitModal(true)}
                        className="text-xs h-7 text-blue-600 dark:text-blue-400 gap-1 border-blue-500/30"
                        title={debitAuthorizationId ? 'Emitir Nota de Débito por recargo' : 'Falta autorización activa para Nota de Débito'}
                      >
                        <FilePlus size={12} /> N. Débito
                      </Button>
                    </>
                  )}
                  <Button size="sm" onClick={printInvoice} className="text-xs h-7 gap-1">
                    <Printer size={12} /> Imprimir
                  </Button>
                </div>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs">
                <div className="rounded-lg border border-border bg-muted/20 p-2.5">
                  <p className="text-[10px] uppercase tracking-wide text-muted-foreground">Total</p>
                  <p className="font-mono font-bold mt-0.5">L {selectedInvoice.totalAmount.toFixed(2)}</p>
                </div>
                <div className="rounded-lg border border-border bg-muted/20 p-2.5">
                  <p className="text-[10px] uppercase tracking-wide text-muted-foreground">Cobrado</p>
                  <p className="font-mono font-bold mt-0.5 text-emerald-700 dark:text-emerald-300">L {selectedInvoice.paidAmount.toFixed(2)}</p>
                </div>
                <div className="rounded-lg border border-border bg-muted/20 p-2.5">
                  <p className="text-[10px] uppercase tracking-wide text-muted-foreground">Notas crédito</p>
                  <p className="font-mono font-bold mt-0.5">L {selectedInvoice.creditedAmount.toFixed(2)}</p>
                </div>
                <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 p-2.5">
                  <p className="text-[10px] uppercase tracking-wide text-amber-800 dark:text-amber-300">Saldo</p>
                  <p className="font-mono font-bold mt-0.5 text-amber-800 dark:text-amber-200">L {selectedInvoice.balanceDue.toFixed(2)}</p>
                </div>
              </div>

              <div
                className="border border-neutral-300 rounded-lg p-3 overflow-x-auto shadow-inner"
                style={{ background: '#ffffff', color: '#000000', margin: '0 auto', maxWidth: '380px' }}
              >
                {invoiceLogo && (
                  <div className="text-center mb-2">
                    <img
                      src={invoiceLogo}
                      className="mx-auto object-contain"
                      style={{ maxHeight: `${previewLogoHeight}px` }}
                      alt="Logo"
                    />
                  </div>
                )}
                <pre
                  className="font-mono whitespace-pre-wrap selection:bg-neutral-200"
                  style={{ margin: 0, fontSize: '10px', lineHeight: '1.18', color: '#000000' }}
                >
                  {invoicePreview}
                </pre>
              </div>

              {canManageCash && (
                <div className="rounded-lg border border-border overflow-hidden">
                  <div className="px-3 py-2 bg-muted/30 border-b border-border flex items-center justify-between">
                    <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Auxiliar de pagos</span>
                    <span className="text-[10px] text-muted-foreground">{payments.length} movimiento(s)</span>
                  </div>
                  {paymentsLoading ? (
                    <div className="p-4 flex items-center justify-center text-xs text-muted-foreground">
                      <Loader2 size={13} className="animate-spin mr-2" /> Consultando pagos…
                    </div>
                  ) : payments.length === 0 ? (
                    <p className="p-4 text-xs text-muted-foreground text-center">Aún no hay cobros aplicados a este documento.</p>
                  ) : (
                    <div className="max-h-44 overflow-y-auto divide-y divide-border/60">
                      {payments.map((payment) => (
                        <div key={payment.id} className="p-3 flex items-start gap-2.5 text-xs">
                          <div className="mt-0.5 rounded-md bg-emerald-500/10 text-emerald-700 dark:text-emerald-300 p-1.5">
                            {payment.method === 'Efectivo' ? (
                              <Banknote size={14} />
                            ) : payment.method === 'Tarjeta' ? (
                              <CreditCard size={14} />
                            ) : (
                              <Landmark size={14} />
                            )}
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="flex items-center justify-between gap-2">
                              <span className="font-mono font-semibold truncate">{payment.paymentNumber}</span>
                              <span className="font-mono font-bold whitespace-nowrap">L {payment.amount.toFixed(2)}</span>
                            </div>
                            <p className="text-[11px] text-muted-foreground">
                              {payment.method} · {formatPaymentDate(payment.paymentDate)}
                            </p>
                            {payment.externalReference && (
                              <p className="text-[11px] text-muted-foreground truncate">Ref. {payment.externalReference}</p>
                            )}
                            {payment.refundedAmount > 0 && (
                              <div className="mt-1.5 rounded-md border border-amber-500/25 bg-amber-500/10 px-2 py-1.5">
                                <div className="flex items-center justify-between gap-2 text-[11px] text-amber-900 dark:text-amber-200">
                                  <span>{payment.status === 'Reembolsado' ? 'Reembolsado' : 'Reembolso parcial'}</span>
                                  <span className="font-mono font-semibold">L {payment.refundedAmount.toFixed(2)}</span>
                                </div>
                                {payment.refunds.map((refund) => (
                                  <p key={refund.id} className="mt-0.5 text-[10px] text-muted-foreground truncate">
                                    {refund.refundNumber} · {formatPaymentDate(refund.refundDate)}
                                  </p>
                                ))}
                              </div>
                            )}
                            {selectedInvoice.creditedAmount > 0 && payment.amount - payment.refundedAmount > 0 && (
                              <Button
                                type="button"
                                size="sm"
                                variant="outline"
                                onClick={() => void openRefundModal(payment)}
                                className="mt-2 h-7 text-[11px] gap-1 border-amber-500/30 text-amber-800 dark:text-amber-300"
                              >
                                <Undo2 size={12} /> Reembolsar
                              </Button>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>
          ) : (
            <div className="border border-dashed border-border rounded-xl p-16 text-center text-muted-foreground bg-card/20 flex flex-col items-center justify-center space-y-2">
              <Printer size={32} className="opacity-30 text-muted-foreground" />
              <p className="text-xs font-medium text-foreground">Seleccione una Factura para Ver su Comprobante</p>
              <p className="text-[11px] text-muted-foreground max-w-xs">
                Podrá visualizar el ticket térmico oficial, imprimirlo o emitir Notas de Crédito y Débito.
              </p>
            </div>
          )}
        </div>
      </div>

      {/* MODAL: REGISTRAR PAGO */}
      {showPaymentModal && selectedInvoice && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => !paymentActionLoading && setShowPaymentModal(false)}
          role="dialog"
          aria-modal="true"
          aria-labelledby="payment-dialog-title"
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-lg shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border pb-3">
              <div className="flex items-center gap-2">
                <WalletCards className="text-emerald-600" size={22} />
                <div>
                  <h3 id="payment-dialog-title" className="text-base font-bold text-foreground">Registrar abono</h3>
                  <p className="text-[11px] text-muted-foreground font-mono">{selectedInvoice.correlativeNumber}</p>
                </div>
              </div>
              <button
                type="button"
                aria-label="Cerrar registro de pago"
                disabled={paymentActionLoading}
                onClick={() => setShowPaymentModal(false)}
                className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent disabled:opacity-50 cursor-pointer"
              >
                <X size={16} />
              </button>
            </div>

            <div className="grid grid-cols-2 gap-3 text-xs">
              <div className="rounded-lg border border-border bg-muted/20 p-3">
                <p className="text-[10px] uppercase tracking-wide text-muted-foreground">Cliente</p>
                <p className="font-semibold mt-1 truncate">{selectedInvoice.customerName}</p>
              </div>
              <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 p-3">
                <p className="text-[10px] uppercase tracking-wide text-amber-800 dark:text-amber-300">Saldo disponible</p>
                <p className="font-mono font-bold mt-1 text-amber-800 dark:text-amber-200">L {selectedInvoice.balanceDue.toFixed(2)}</p>
              </div>
            </div>

            <form onSubmit={handlePaymentSubmit} className="space-y-4 text-xs">
              <fieldset>
                <legend className="font-semibold text-muted-foreground mb-1.5">Método de pago</legend>
                <div className="grid grid-cols-3 gap-2">
                  {([
                    ['Efectivo', Banknote],
                    ['Tarjeta', CreditCard],
                    ['Transferencia', Landmark],
                  ] as const).map(([method, Icon]) => (
                    <button
                      key={method}
                      type="button"
                      onClick={() => setPaymentMethod(method)}
                      className={`rounded-lg border p-2.5 flex flex-col items-center gap-1 transition-colors cursor-pointer ${
                        paymentMethod === method
                          ? 'border-emerald-600 bg-emerald-500/10 text-emerald-800 dark:text-emerald-200'
                          : 'border-border hover:bg-accent text-muted-foreground'
                      }`}
                    >
                      <Icon size={17} />
                      <span className="text-[11px] font-semibold">{method}</span>
                    </button>
                  ))}
                </div>
              </fieldset>

              <div>
                <label htmlFor="payment-amount" className="font-semibold text-muted-foreground">Importe a aplicar (L)</label>
                <Input
                  id="payment-amount"
                  type="number"
                  min="0.01"
                  max={selectedInvoice.balanceDue}
                  step="0.01"
                  required
                  value={paymentAmount}
                  onChange={(event) => setPaymentAmount(event.target.value)}
                  className="mt-1 font-mono text-right"
                />
                <p className="text-[10px] text-muted-foreground mt-1">Puede registrar un abono parcial; el saldo restante seguirá pendiente.</p>
              </div>

              {paymentMethod === 'Efectivo' ? (
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label htmlFor="payment-register" className="font-semibold text-muted-foreground">Caja abierta</label>
                    <select
                      id="payment-register"
                      required
                      value={selectedCashRegisterId}
                      onChange={(event) => setSelectedCashRegisterId(event.target.value)}
                      className="w-full border border-input rounded-md px-2.5 h-9 text-xs bg-background mt-1"
                    >
                      <option value="">{cashRegisters.length === 0 ? 'No hay cajas abiertas' : 'Seleccione una caja'}</option>
                      {cashRegisters.map((register) => (
                        <option key={register.id} value={register.id}>{register.name} · L {register.currentBalance.toFixed(2)}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label htmlFor="cash-received" className="font-semibold text-muted-foreground">Efectivo recibido (L)</label>
                    <Input
                      id="cash-received"
                      type="number"
                      min="0"
                      step="0.01"
                      required
                      value={cashReceived}
                      onChange={(event) => setCashReceived(event.target.value)}
                      className="mt-1 font-mono text-right"
                    />
                  </div>
                  <div className="col-span-2 rounded-lg border border-border bg-muted/20 px-3 py-2 flex justify-between">
                    <span className="text-muted-foreground">Cambio calculado</span>
                    <span className="font-mono font-bold">
                      L {Math.max(0, (Number(cashReceived) || 0) - (Number(paymentAmount) || 0)).toFixed(2)}
                    </span>
                  </div>
                </div>
              ) : (
                <div>
                  <label htmlFor="payment-reference" className="font-semibold text-muted-foreground">
                    Referencia de {paymentMethod.toLowerCase()}
                  </label>
                  <Input
                    id="payment-reference"
                    required
                    minLength={3}
                    maxLength={100}
                    placeholder={paymentMethod === 'Tarjeta' ? 'Voucher o autorización' : 'Número de transferencia'}
                    value={paymentReference}
                    onChange={(event) => setPaymentReference(event.target.value)}
                    className="mt-1 font-mono"
                  />
                </div>
              )}

              <div className="rounded-lg border border-blue-500/20 bg-blue-500/10 p-3 text-blue-900 dark:text-blue-200">
                El abono quedará en el auxiliar, caja y contabilidad. No altera el comprobante fiscal emitido.
              </div>

              <div className="flex justify-end gap-2 pt-3 border-t border-border">
                <Button type="button" variant="outline" disabled={paymentActionLoading} onClick={() => setShowPaymentModal(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={paymentActionLoading} className="bg-emerald-700 hover:bg-emerald-800 text-white">
                  {paymentActionLoading && <Loader2 size={13} className="animate-spin mr-1" />}
                  Confirmar abono
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: REGISTRAR REEMBOLSO */}
      {showRefundModal && selectedInvoice && refundPayment && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => !refundActionLoading && setShowRefundModal(false)}
          role="dialog"
          aria-modal="true"
          aria-labelledby="refund-dialog-title"
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-lg shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border pb-3">
              <div className="flex items-center gap-2">
                <Undo2 className="text-amber-600" size={22} />
                <div>
                  <h3 id="refund-dialog-title" className="text-base font-bold text-foreground">Registrar reembolso</h3>
                  <p className="text-[11px] text-muted-foreground font-mono">{refundPayment.paymentNumber}</p>
                </div>
              </div>
              <button
                type="button"
                aria-label="Cerrar registro de reembolso"
                disabled={refundActionLoading}
                onClick={() => setShowRefundModal(false)}
                className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent disabled:opacity-50 cursor-pointer"
              >
                <X size={16} />
              </button>
            </div>

            {refundSetupLoading ? (
              <div className="py-12 flex items-center justify-center text-xs text-muted-foreground">
                <Loader2 size={16} className="animate-spin mr-2" /> Verificando notas de crédito y caja…
              </div>
            ) : (
              <form onSubmit={handleRefundSubmit} className="space-y-4 text-xs">
                <div className="grid grid-cols-3 gap-2">
                  <div className="rounded-lg border border-border bg-muted/20 p-3">
                    <p className="text-[10px] uppercase tracking-wide text-muted-foreground">Pago original</p>
                    <p className="font-mono font-bold mt-1">L {refundPayment.amount.toFixed(2)}</p>
                  </div>
                  <div className="rounded-lg border border-border bg-muted/20 p-3">
                    <p className="text-[10px] uppercase tracking-wide text-muted-foreground">Ya reembolsado</p>
                    <p className="font-mono font-bold mt-1">L {refundPayment.refundedAmount.toFixed(2)}</p>
                  </div>
                  <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 p-3">
                    <p className="text-[10px] uppercase tracking-wide text-amber-800 dark:text-amber-300">Máximo actual</p>
                    <p className="font-mono font-bold mt-1 text-amber-800 dark:text-amber-200">L {refundLimit.toFixed(2)}</p>
                  </div>
                </div>

                <div>
                  <label htmlFor="refund-credit-note" className="font-semibold text-muted-foreground">Nota de crédito de respaldo</label>
                  <select
                    id="refund-credit-note"
                    required
                    value={selectedRefundCreditNoteId}
                    onChange={(event) => {
                      const option = refundCreditNotes.find((note) => note.id === event.target.value)
                      setSelectedRefundCreditNoteId(event.target.value)
                      setRefundLimit(option?.availableAmount ?? 0)
                      setRefundAmount(option ? option.availableAmount.toFixed(2) : '')
                    }}
                    className="w-full border border-input rounded-md px-2.5 h-9 text-xs bg-background mt-1 font-mono"
                  >
                    {refundCreditNotes.map((note) => (
                      <option key={note.id} value={note.id}>
                        {note.correlativeNumber} · L {note.availableAmount.toFixed(2)} disponible
                      </option>
                    ))}
                  </select>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label htmlFor="refund-amount" className="font-semibold text-muted-foreground">Importe a reembolsar (L)</label>
                    <Input
                      id="refund-amount"
                      type="number"
                      min="0.01"
                      max={refundLimit}
                      step="0.01"
                      required
                      value={refundAmount}
                      onChange={(event) => setRefundAmount(event.target.value)}
                      className="mt-1 font-mono text-right"
                    />
                  </div>
                  <div>
                    <label className="font-semibold text-muted-foreground">Medio de devolución</label>
                    <div className="mt-1 h-9 rounded-md border border-input bg-muted/20 px-3 flex items-center gap-2 font-semibold">
                      {refundPayment.method === 'Efectivo' ? <Banknote size={14} /> : refundPayment.method === 'Tarjeta' ? <CreditCard size={14} /> : <Landmark size={14} />}
                      {refundPayment.method}
                    </div>
                  </div>
                </div>

                {refundPayment.method === 'Efectivo' ? (
                  <div>
                    <label htmlFor="refund-register" className="font-semibold text-muted-foreground">Caja abierta con saldo suficiente</label>
                    <select
                      id="refund-register"
                      required
                      value={selectedRefundCashRegisterId}
                      onChange={(event) => setSelectedRefundCashRegisterId(event.target.value)}
                      className="w-full border border-input rounded-md px-2.5 h-9 text-xs bg-background mt-1"
                    >
                      <option value="">{refundCashRegisters.length === 0 ? 'No hay cajas abiertas' : 'Seleccione una caja'}</option>
                      {refundCashRegisters.map((register) => (
                        <option key={register.id} value={register.id}>{register.name} · L {register.currentBalance.toFixed(2)}</option>
                      ))}
                    </select>
                  </div>
                ) : (
                  <div>
                    <label htmlFor="refund-reference" className="font-semibold text-muted-foreground">
                      Referencia de la devolución por {refundPayment.method.toLowerCase()}
                    </label>
                    <Input
                      id="refund-reference"
                      required
                      minLength={3}
                      maxLength={100}
                      placeholder="Voucher, autorización o número de transferencia"
                      value={refundReference}
                      onChange={(event) => setRefundReference(event.target.value)}
                      className="mt-1 font-mono"
                    />
                  </div>
                )}

                <div>
                  <label htmlFor="refund-reason" className="font-semibold text-muted-foreground">Motivo operativo del reembolso</label>
                  <Input
                    id="refund-reason"
                    required
                    minLength={3}
                    maxLength={500}
                    placeholder="Ej. Devolución al huésped por anulación total"
                    value={refundReason}
                    onChange={(event) => setRefundReason(event.target.value)}
                    className="mt-1"
                  />
                </div>

                <div className="rounded-lg border border-amber-500/20 bg-amber-500/10 p-3 text-amber-950 dark:text-amber-200">
                  El reembolso quedará vinculado al pago y a la nota de crédito. También generará el egreso de caja o banco y su asiento contable.
                </div>

                <div className="flex justify-end gap-2 pt-3 border-t border-border">
                  <Button type="button" variant="outline" disabled={refundActionLoading} onClick={() => setShowRefundModal(false)}>
                    Cancelar
                  </Button>
                  <Button type="submit" disabled={refundActionLoading || refundLimit <= 0} className="bg-amber-700 hover:bg-amber-800 text-white">
                    {refundActionLoading && <Loader2 size={13} className="animate-spin mr-1" />}
                    Confirmar reembolso
                  </Button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}

      {/* MODAL: EMITIR NOTA DE CRÉDITO */}
      {showCreditModal && selectedInvoice && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => setShowCreditModal(false)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-lg shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border pb-3">
              <div className="flex items-center gap-2">
                <FileMinus className="text-amber-500" size={22} />
                <h3 className="text-base font-bold text-foreground">
                  Emitir Nota de Crédito SAR
                </h3>
              </div>
              <button
                onClick={() => setShowCreditModal(false)}
                className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent cursor-pointer"
              >
                <X size={16} />
              </button>
            </div>

            <form onSubmit={handleCreditNoteSubmit} className="space-y-3.5 text-xs">
              <div className="p-3 rounded-lg bg-amber-500/10 border border-amber-500/20 text-amber-900 dark:text-amber-200 space-y-1">
                <p className="font-bold">Factura Afectada: {selectedInvoice.correlativeNumber}</p>
                <p>Cliente: {selectedInvoice.customerName} · Total: L {selectedInvoice.totalAmount.toFixed(2)}</p>
              </div>

              <div>
                <label className="font-semibold text-muted-foreground">Razón / Motivo Fiscal SAR *</label>
                <Input
                  required
                  placeholder="Ej. Anulación por error en tarifa / Devolución por check-out anticipado"
                  value={creditReason}
                  onChange={(e) => setCreditReason(e.target.value)}
                  className="text-xs mt-1"
                />
              </div>

              <div>
                <label className="font-semibold text-muted-foreground">Líneas a Reversar *</label>
                <div className="mt-1 border border-border rounded-md divide-y divide-border/60 max-h-48 overflow-y-auto">
                  {creditItems.map((it, idx) => (
                    <div key={idx} className="p-2 flex items-center gap-2 hover:bg-accent/40">
                      <input
                        type="checkbox"
                        checked={it.included}
                        onChange={(e) => {
                          const newItems = [...creditItems]
                          newItems[idx].included = e.target.checked
                          setCreditItems(newItems)
                        }}
                        className="rounded border-input text-primary focus:ring-primary h-4 w-4"
                      />
                      <div className="flex-1 truncate">
                        <span className="font-medium">{it.description}</span>
                        <div className="text-muted-foreground">L {it.unitPrice.toFixed(2)} c/u</div>
                      </div>
                      <div className="flex items-center gap-1 w-20">
                        <Input
                          type="number"
                          step="1"
                          min="1"
                          max={it.maxQuantity}
                          value={it.quantity}
                          onChange={(e) => {
                            const newItems = [...creditItems]
                            newItems[idx].quantity = Number(e.target.value) || 0
                            setCreditItems(newItems)
                          }}
                          disabled={!it.included}
                          className="text-xs h-7 px-1 text-right"
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <label className="font-semibold text-muted-foreground">Autorización de Nota de Crédito</label>
                <select
                  value={creditAuthorizationId}
                  onChange={(e) => setCreditAuthorizationId(e.target.value)}
                  className="w-full border border-input rounded-md px-2.5 py-1.5 text-xs bg-background mt-1"
                >
                  {!creditAuthorizationId && <option value="">Sin autorización activa</option>}
                  {authList.filter((item) => item.source === 'docauth' && item.documentType === 'NotaCredito').map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.label}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex justify-end gap-2 pt-3 border-t border-border">
                <Button type="button" variant="outline" onClick={() => setShowCreditModal(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={creditActionLoading || !creditAuthorizationId} className="bg-amber-600 hover:bg-amber-700 text-white">
                  {creditActionLoading && <Loader2 size={13} className="animate-spin mr-1" />}
                  Emitir Nota de Crédito
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: EMITIR NOTA DE DÉBITO */}
      {showDebitModal && selectedInvoice && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => setShowDebitModal(false)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-lg shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border pb-3">
              <div className="flex items-center gap-2">
                <FilePlus className="text-blue-500" size={22} />
                <h3 className="text-base font-bold text-foreground">
                  Emitir Nota de Débito SAR
                </h3>
              </div>
              <button
                onClick={() => setShowDebitModal(false)}
                className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent cursor-pointer"
              >
                <X size={16} />
              </button>
            </div>

            <form onSubmit={handleDebitNoteSubmit} className="space-y-3.5 text-xs">
              <div className="p-3 rounded-lg bg-blue-500/10 border border-blue-500/20 text-blue-900 dark:text-blue-200 space-y-1">
                <p className="font-bold">Factura de Referencia: {selectedInvoice.correlativeNumber}</p>
                <p>Cliente: {selectedInvoice.customerName}</p>
              </div>

              <div>
                <label className="font-semibold text-muted-foreground">Concepto del Recargo / Motivo *</label>
                <Input
                  required
                  placeholder="Ej. Intereses moratorios / Daños a instalaciones / Consumo omitido"
                  value={debitReason}
                  onChange={(e) => setDebitReason(e.target.value)}
                  className="text-xs mt-1"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="font-semibold text-muted-foreground">Importe del Recargo (L) *</label>
                  <Input
                    type="number"
                    step="0.01"
                    required
                    placeholder="0.00"
                    value={debitAmount}
                    onChange={(e) => setDebitAmount(e.target.value)}
                    className="text-xs mt-1 font-mono"
                  />
                </div>
                <div>
                  <label className="font-semibold text-muted-foreground">Autorización de Nota de Débito</label>
                  <select
                    value={debitAuthorizationId}
                    onChange={(e) => setDebitAuthorizationId(e.target.value)}
                    className="w-full border border-input rounded-md px-2.5 py-1.5 text-xs bg-background mt-1"
                  >
                    {!debitAuthorizationId && <option value="">Sin autorización activa</option>}
                    {authList.filter((item) => item.source === 'docauth' && item.documentType === 'NotaDebito').map((a) => (
                      <option key={a.id} value={a.id}>
                        {a.label}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="flex justify-end gap-2 pt-3 border-t border-border">
                <Button type="button" variant="outline" onClick={() => setShowDebitModal(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={debitActionLoading || !debitAuthorizationId} className="bg-blue-600 hover:bg-blue-700 text-white">
                  {debitActionLoading && <Loader2 size={13} className="animate-spin mr-1" />}
                  Emitir Nota de Débito
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

    </div>
  )
}
