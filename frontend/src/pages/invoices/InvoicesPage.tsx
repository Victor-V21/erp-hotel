import { useState, useEffect, useCallback, useMemo } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import api from '@/lib/axios'
import type { Invoice, CAI, DocumentAuthorization } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { Pagination } from '@/components/ui/Pagination'
import {
  Receipt,
  Search,
  Download,
  Printer,
  Edit2,
  Ban,
  Loader2,
  FileMinus,
  FilePlus,
  X,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react'

export default function InvoicesPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [dni, setDni] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [authList, setAuthList] = useState<{ id: string; source: 'cai' | 'docauth'; label: string }[]>([])
  const [selectedAuth, setSelectedAuth] = useState('')
  const [docTypeFilter, setDocTypeFilter] = useState('Todos')

  const [invoicePreview, setInvoicePreview] = useState<string | null>(null)
  const [invoiceLogo, setInvoiceLogo] = useState<string | null>(null)
  const [previewLogoHeight, setPreviewLogoHeight] = useState(40)
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null)
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null)
  const [loading, setLoading] = useState(false)
  const [cancelTarget, setCancelTarget] = useState<Invoice | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)

  // Credit Note Modal State
  const [showCreditModal, setShowCreditModal] = useState(false)
  const [creditReason, setCreditReason] = useState('')
  const [creditCaiId, setCreditCaiId] = useState('')
  const [creditActionLoading, setCreditActionLoading] = useState(false)
  const [creditItems, setCreditItems] = useState<{ idx: number, description: string, quantity: number, maxQuantity: number, unitPrice: number, isExempt: boolean, isvRate: number, isTouristTaxable: boolean, discountPercentage: number, included: boolean }[]>([])

  // Debit Note Modal State
  const [showDebitModal, setShowDebitModal] = useState(false)
  const [debitReason, setDebitReason] = useState('')
  const [debitAmount, setDebitAmount] = useState('')
  const [debitDesc, setDebitDesc] = useState('')
  const [debitCaiId, setDebitCaiId] = useState('')
  const [debitActionLoading, setDebitActionLoading] = useState(false)

  // Pagination
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)

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
          label: `${c.caiNumber} (Factura general)`,
        })),
        ...docAuthRes.data.map((d) => ({
          id: d.id,
          source: 'docauth' as const,
          label: `${d.caiNumber} (${d.documentType})`,
        })),
      ]
      setAuthList(list)
      if (caiRes.data.length > 0) {
        setCreditCaiId(caiRes.data[0].id)
        setDebitCaiId(caiRes.data[0].id)
      }
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

  useEffect(() => {
    const init = async () => {
      await loadAuthList()
      const filter = searchParams.get('filter')
      if (filter) {
        setSelectedAuth(filter)
        load(filter)
      } else {
        load()
      }
    }
    init()
  }, [])

  const viewPrint = async (inv: Invoice) => {
    setSelectedInvoiceId(inv.id)
    setSelectedInvoice(inv)
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

  const printInvoice = async () => {
    if (!selectedInvoiceId) return
    try {
      await api.post(`/print/invoice/${selectedInvoiceId}`)
      setAlertInfo({ variant: 'success', message: 'Comprobante fiscal enviado a imprimir correctamente.' })
    } catch {
      setAlertInfo({ variant: 'error', message: 'No se pudo conectar con la impresora térmica.' })
    }
  }

  const handleCancelInvoiceConfirm = async () => {
    if (!cancelTarget) return
    try {
      await api.post(`/invoices/${cancelTarget.id}/cancel`)
      setAlertInfo({ variant: 'success', message: `Factura ${cancelTarget.correlativeNumber} anulada correctamente.` })
      setCancelTarget(null)
      load()
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'Error al anular la factura.' })
    }
  }

  // Handle Credit Note Submit
  const handleCreditNoteSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedInvoice || !creditReason.trim()) return

    setCreditActionLoading(true)
    try {
      const { data } = await api.post('/invoices/credit-note', {
        originalInvoiceId: selectedInvoice.id,
        caiId: creditCaiId || selectedInvoice.caiId,
        reason: creditReason.trim(),
        items: creditItems.filter(it => it.included && it.quantity > 0).map((it) => ({
          description: it.description,
          quantity: it.quantity,
          unitPrice: it.unitPrice,
          isExempt: it.isExempt,
          isvRate: it.isvRate,
          isTouristTaxable: it.isTouristTaxable,
          discountPercentage: it.discountPercentage,
        })),
      })

      setAlertInfo({
        variant: 'success',
        message: `Nota de Crédito ${data.correlativeNumber} emitida exitosamente para la factura ${selectedInvoice.correlativeNumber}.`,
      })
      setShowCreditModal(false)
      setCreditReason('')
      load()
      viewPrint(data)
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'Error al emitir la nota de crédito.' })
    } finally {
      setCreditActionLoading(false)
    }
  }

  // Handle Debit Note Submit
  const handleDebitNoteSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedInvoice || !debitReason.trim() || !debitAmount || Number(debitAmount) <= 0) return

    setDebitActionLoading(true)
    try {
      const { data } = await api.post(`/invoices/${selectedInvoice.id}/debit-note`, {
        caiId: debitCaiId || selectedInvoice.caiId,
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
      })

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
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'Error al emitir la nota de débito.' })
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
            Control tributario oficial, correlativos fiscales CAI y reimpresión de comprobantes.
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
                        <td className="p-3">
                          <span
                            className={`px-1.5 py-0.2 rounded text-[10px] font-semibold ${
                              inv.status === 'Anulada'
                                ? 'bg-red-100 dark:bg-red-950/40 text-red-700 dark:text-red-300'
                                : 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300'
                            }`}
                          >
                            {inv.status}
                          </span>
                        </td>
                        <td className="p-3 text-right space-x-1">
                          {inv.status !== 'Anulada' && (
                            <>
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={(e) => {
                                  e.stopPropagation()
                                  navigate(`/invoices/${inv.id}/edit`)
                                }}
                                className="h-6 w-6 p-0"
                                title="Editar"
                              >
                                <Edit2 size={11} />
                              </Button>
                              <Button
                                size="sm"
                                variant="destructive"
                                onClick={(e) => {
                                  e.stopPropagation()
                                  setCancelTarget(inv)
                                }}
                                className="h-6 w-6 p-0"
                                title="Anular"
                              >
                                <Ban size={11} />
                              </Button>
                            </>
                          )}
                        </td>
                      </tr>
                    ))}
                    {paginatedInvoices.length === 0 && (
                      <tr>
                        <td colSpan={6} className="p-8 text-center text-muted-foreground">
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
                <div className="flex gap-1.5">
                  {selectedInvoice.status !== 'Anulada' && selectedInvoice.documentType !== 'NotaCredito' && (
                    <>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          setCreditItems(
                            selectedInvoice.items.map((it, idx) => ({
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
                        title="Emitir Nota de Crédito por devolución o anulación"
                      >
                        <FileMinus size={12} /> N. Crédito
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => setShowDebitModal(true)}
                        className="text-xs h-7 text-blue-600 dark:text-blue-400 gap-1 border-blue-500/30"
                        title="Emitir Nota de Débito por recargo"
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

              <div
                className="border border-neutral-300 rounded-lg p-3 overflow-x-auto shadow-inner"
                style={{ background: '#ffffff', color: '#000000', margin: '0 auto', maxWidth: '380px' }}
              >
                {invoiceLogo && (
                  <div className="text-center mb-2">
                    <img
                      src={invoiceLogo}
                      className="mx-auto object-contain max-h-16"
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
                          step="0.01"
                          min="0"
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
                <label className="font-semibold text-muted-foreground">Autorización Fiscal CAI</label>
                <select
                  value={creditCaiId}
                  onChange={(e) => setCreditCaiId(e.target.value)}
                  className="w-full border border-input rounded-md px-2.5 py-1.5 text-xs bg-background mt-1"
                >
                  {authList.map((a) => (
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
                <Button type="submit" disabled={creditActionLoading} className="bg-amber-600 hover:bg-amber-700 text-white">
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
                  <label className="font-semibold text-muted-foreground">Autorización Fiscal CAI</label>
                  <select
                    value={debitCaiId}
                    onChange={(e) => setDebitCaiId(e.target.value)}
                    className="w-full border border-input rounded-md px-2.5 py-1.5 text-xs bg-background mt-1"
                  >
                    {authList.map((a) => (
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
                <Button type="submit" disabled={debitActionLoading} className="bg-blue-600 hover:bg-blue-700 text-white">
                  {debitActionLoading && <Loader2 size={13} className="animate-spin mr-1" />}
                  Emitir Nota de Débito
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Confirm Invoice Cancellation */}
      <ConfirmDialog
        isOpen={!!cancelTarget}
        title="Anular Factura Fiscal"
        description={`¿Está seguro de que desea anular la factura con correlativo ${cancelTarget?.correlativeNumber}? La SAR exige registrar la anulación y no se podrá revertir.`}
        confirmText="Anular Factura"
        cancelText="Conservar"
        variant="destructive"
        onConfirm={handleCancelInvoiceConfirm}
        onCancel={() => setCancelTarget(null)}
      />
    </div>
  )
}
