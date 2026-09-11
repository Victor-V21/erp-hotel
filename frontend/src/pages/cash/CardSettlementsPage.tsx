import { useCallback, useEffect, useMemo, useState } from 'react'
import axios from 'axios'
import {
  BadgeDollarSign,
  Check,
  CreditCard,
  Landmark,
  Loader2,
  ReceiptText,
  RefreshCw,
} from 'lucide-react'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/errors'
import type { CardSettlement, EligibleCardPayment } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'

const intentStorageKey = 'hotel-erp:card-settlement:create'

const getOrCreateIntent = () => {
  const existing = window.sessionStorage.getItem(intentStorageKey)
  if (existing) return existing
  const created = window.crypto.randomUUID()
  window.sessionStorage.setItem(intentStorageKey, created)
  return created
}

const clearIntent = () => window.sessionStorage.removeItem(intentStorageKey)

const isDefinitiveFailure = (error: unknown) =>
  axios.isAxiosError(error) &&
  !!error.response &&
  [400, 401, 403, 404, 409, 422].includes(error.response.status)

const roundCurrency = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100

const hondurasToday = () => {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'America/Tegucigalpa',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date())
  const value = Object.fromEntries(parts.map((part) => [part.type, part.value]))
  return `${value.year}-${value.month}-${value.day}`
}

const formatDateTime = (value: string) => new Intl.DateTimeFormat('es-HN', {
  dateStyle: 'short',
  timeStyle: 'short',
  timeZone: 'America/Tegucigalpa',
}).format(new Date(value))

const compactNumber = (value: string) =>
  value.length > 20 ? `${value.slice(0, 7)}…${value.slice(-8)}` : value

export default function CardSettlementsPage() {
  const [eligiblePayments, setEligiblePayments] = useState<EligibleCardPayment[]>([])
  const [settlements, setSettlements] = useState<CardSettlement[]>([])
  const [selected, setSelected] = useState<Record<string, boolean>>({})
  const [settlementDate, setSettlementDate] = useState(hondurasToday)
  const [externalReference, setExternalReference] = useState('')
  const [commissionAmount, setCommissionAmount] = useState('0.00')
  const [withholdingAmount, setWithholdingAmount] = useState('0.00')
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [alertInfo, setAlertInfo] = useState<{
    variant: 'error' | 'success' | 'info'
    message: string
  } | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [eligibleResponse, settlementsResponse] = await Promise.all([
        api.get<EligibleCardPayment[]>('/card-settlements/eligible-payments'),
        api.get<CardSettlement[]>('/card-settlements'),
      ])
      setEligiblePayments(eligibleResponse.data)
      setSettlements(settlementsResponse.data)
      const availableIds = new Set(eligibleResponse.data.map((payment) => payment.id))
      setSelected((current) => Object.fromEntries(
        Object.entries(current).filter(([id, checked]) => checked && availableIds.has(id))
      ))
    } catch (error) {
      setAlertInfo({
        variant: 'error',
        message: getApiErrorMessage(error, 'No se pudieron cargar las liquidaciones de tarjeta.'),
      })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0)
    return () => window.clearTimeout(timer)
  }, [load])

  const selectedPayments = useMemo(
    () => eligiblePayments.filter((payment) => selected[payment.id]),
    [eligiblePayments, selected]
  )
  const grossAmount = roundCurrency(
    selectedPayments.reduce((total, payment) => total + payment.availableAmount, 0)
  )
  const commission = Number(commissionAmount)
  const withholding = Number(withholdingAmount)
  const componentsValid = Number.isFinite(commission) && commission >= 0 &&
    Number.isFinite(withholding) && withholding >= 0 &&
    roundCurrency(commission + withholding) <= grossAmount
  const bankDepositAmount = componentsValid
    ? roundCurrency(grossAmount - commission - withholding)
    : 0
  const pendingTotal = roundCurrency(
    eligiblePayments.reduce((total, payment) => total + payment.availableAmount, 0)
  )

  const togglePayment = (paymentId: string) => {
    setSelected((current) => ({ ...current, [paymentId]: !current[paymentId] }))
  }

  const selectAll = () => {
    const shouldSelect = selectedPayments.length !== eligiblePayments.length
    setSelected(Object.fromEntries(eligiblePayments.map((payment) => [payment.id, shouldSelect])))
  }

  const createSettlement = async () => {
    const reference = externalReference.trim()
    if (selectedPayments.length === 0) {
      setAlertInfo({ variant: 'error', message: 'Seleccione al menos un cobro con tarjeta.' })
      return
    }
    if (reference.length < 3) {
      setAlertInfo({ variant: 'error', message: 'Ingrese la referencia del adquirente con al menos 3 caracteres.' })
      return
    }
    if (!settlementDate || settlementDate > hondurasToday()) {
      setAlertInfo({ variant: 'error', message: 'Ingrese una fecha de liquidación válida que no esté en el futuro.' })
      return
    }
    if (!componentsValid) {
      setAlertInfo({ variant: 'error', message: 'Comisión y retención deben ser importes válidos que no excedan el bruto.' })
      return
    }

    setSubmitting(true)
    const intent = getOrCreateIntent()
    try {
      const { data } = await api.post<CardSettlement>(
        '/card-settlements',
        {
          currency: 'HNL',
          grossAmount,
          bankDepositAmount,
          commissionAmount: roundCurrency(commission),
          withholdingAmount: roundCurrency(withholding),
          externalReference: reference,
          settlementDate,
          applications: selectedPayments.map((payment) => ({
            paymentId: payment.id,
            amount: payment.availableAmount,
          })),
        },
        { headers: { 'Idempotency-Key': intent } }
      )
      clearIntent()
      setSelected({})
      setExternalReference('')
      setCommissionAmount('0.00')
      setWithholdingAmount('0.00')
      setAlertInfo({
        variant: 'success',
        message: `${data.settlementNumber} registrada: bruto L ${data.grossAmount.toFixed(2)}, depósito L ${data.bankDepositAmount.toFixed(2)}.`,
      })
      await load()
    } catch (error) {
      if (isDefinitiveFailure(error)) clearIntent()
      setAlertInfo({
        variant: 'error',
        message: getApiErrorMessage(
          error,
          axios.isAxiosError(error) && !error.response
            ? 'No se confirmó la liquidación. Reintente sin modificar los datos para conservar la misma intención.'
            : 'No se pudo registrar la liquidación.'
        ),
      })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="flex items-center gap-2.5 text-2xl font-bold tracking-tight text-foreground">
            <CreditCard size={24} className="text-[#C69C4B]" />
            Liquidaciones de tarjeta
          </h1>
          <p className="mt-1 max-w-2xl text-sm text-muted-foreground">
            Concilie cobros del POS con el depósito bancario, la comisión y la retención informadas por el adquirente.
          </p>
        </div>
        <Button variant="outline" onClick={() => void load()} disabled={loading || submitting}>
          <RefreshCw size={15} className={loading ? 'mr-2 animate-spin' : 'mr-2'} />
          Actualizar
        </Button>
      </div>

      {alertInfo && (
        <InlineAlert
          variant={alertInfo.variant}
          message={alertInfo.message}
          onClose={() => setAlertInfo(null)}
        />
      )}

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-border bg-card p-5 shadow-xs">
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Pendiente por liquidar</p>
          <p className="mt-1 text-2xl font-bold text-foreground">L {pendingTotal.toFixed(2)}</p>
          <p className="text-xs text-muted-foreground">{eligiblePayments.length} cobros disponibles</p>
        </div>
        <div className="rounded-xl border border-border bg-card p-5 shadow-xs">
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Seleccionado</p>
          <p className="mt-1 text-2xl font-bold text-[#C69C4B]">L {grossAmount.toFixed(2)}</p>
          <p className="text-xs text-muted-foreground">{selectedPayments.length} cobros en el lote</p>
        </div>
        <div className="rounded-xl border border-border bg-card p-5 shadow-xs">
          <p className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Depósito calculado</p>
          <p className="mt-1 text-2xl font-bold text-emerald-700 dark:text-emerald-400">L {bankDepositAmount.toFixed(2)}</p>
          <p className="text-xs text-muted-foreground">Bruto menos comisión y retención</p>
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.5fr)_minmax(320px,0.7fr)]">
        <section className="overflow-hidden rounded-xl border border-border bg-card shadow-xs" aria-labelledby="eligible-title">
          <div className="flex items-center justify-between border-b border-border bg-muted/20 p-4">
            <div>
              <h2 id="eligible-title" className="font-semibold text-foreground">Cobros disponibles</h2>
              <p className="text-xs text-muted-foreground">Seleccione los comprobantes incluidos por el adquirente.</p>
            </div>
            {eligiblePayments.length > 0 && (
              <Button size="sm" variant="outline" onClick={selectAll} disabled={submitting}>
                {selectedPayments.length === eligiblePayments.length ? 'Quitar todos' : 'Seleccionar todos'}
              </Button>
            )}
          </div>

          {loading ? (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 size={20} className="animate-spin text-[#C69C4B]" /> Cargando cobros...
            </div>
          ) : eligiblePayments.length === 0 ? (
            <div className="px-6 py-14 text-center">
              <Check size={28} className="mx-auto mb-2 text-emerald-600" />
              <p className="font-medium text-foreground">No hay cobros pendientes</p>
              <p className="mt-1 text-sm text-muted-foreground">Los pagos con tarjeta ya están conciliados o fueron reembolsados.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-muted/40 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  <tr>
                    <th className="w-12 p-3.5 text-center">Incluir</th>
                    <th className="p-3.5 text-left">Pago</th>
                    <th className="p-3.5 text-left">Referencia POS</th>
                    <th className="p-3.5 text-left">Fecha y hora</th>
                    <th className="p-3.5 text-right">Disponible</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border/60">
                  {eligiblePayments.map((payment) => (
                    <tr key={payment.id} className={selected[payment.id] ? 'bg-primary/5' : 'hover:bg-accent/30'}>
                      <td className="p-3.5 text-center">
                        <input
                          type="checkbox"
                          checked={!!selected[payment.id]}
                          onChange={() => togglePayment(payment.id)}
                          disabled={submitting}
                          aria-label={`Incluir ${payment.paymentNumber}`}
                          className="h-4 w-4 rounded border-input accent-[#C69C4B]"
                        />
                      </td>
                      <td className="p-3.5 font-mono text-xs font-semibold text-foreground" title={payment.paymentNumber}>{compactNumber(payment.paymentNumber)}</td>
                      <td className="p-3.5 text-xs text-foreground">{payment.externalReference}</td>
                      <td className="p-3.5 text-xs text-muted-foreground">{formatDateTime(payment.paymentDate)}</td>
                      <td className="p-3.5 text-right font-semibold text-foreground">L {payment.availableAmount.toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="h-fit rounded-xl border border-border bg-card p-5 shadow-xs" aria-labelledby="settlement-form-title">
          <div className="mb-5 flex items-start gap-3">
            <div className="rounded-lg bg-primary/10 p-2 text-[#C69C4B]"><Landmark size={18} /></div>
            <div>
              <h2 id="settlement-form-title" className="font-semibold text-foreground">Registrar lote</h2>
              <p className="text-xs text-muted-foreground">Los componentes deben sumar exactamente el bruto.</p>
            </div>
          </div>
          <div className="space-y-4">
            <div>
              <label htmlFor="settlement-date" className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Fecha de depósito *</label>
              <Input id="settlement-date" type="date" max={hondurasToday()} value={settlementDate} onChange={(event) => setSettlementDate(event.target.value)} className="mt-1" disabled={submitting} />
            </div>
            <div>
              <label htmlFor="settlement-reference" className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Referencia del adquirente *</label>
              <Input id="settlement-reference" maxLength={100} value={externalReference} onChange={(event) => setExternalReference(event.target.value)} placeholder="Ej. BAC-LOTE-009821" className="mt-1" disabled={submitting} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="settlement-commission" className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Comisión (L)</label>
                <Input id="settlement-commission" type="number" min="0" step="0.01" value={commissionAmount} onChange={(event) => setCommissionAmount(event.target.value)} className="mt-1" disabled={submitting} />
              </div>
              <div>
                <label htmlFor="settlement-withholding" className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Retención (L)</label>
                <Input id="settlement-withholding" type="number" min="0" step="0.01" value={withholdingAmount} onChange={(event) => setWithholdingAmount(event.target.value)} className="mt-1" disabled={submitting} />
              </div>
            </div>
            <div className="space-y-2 rounded-lg border border-border bg-muted/25 p-3 text-sm">
              <div className="flex justify-between"><span className="text-muted-foreground">Bruto seleccionado</span><strong>L {grossAmount.toFixed(2)}</strong></div>
              <div className="flex justify-between"><span className="text-muted-foreground">Comisión + retención</span><span>L {componentsValid ? roundCurrency(commission + withholding).toFixed(2) : '—'}</span></div>
              <div className="flex justify-between border-t border-border pt-2"><span className="font-medium">Depósito bancario</span><strong className="text-emerald-700 dark:text-emerald-400">L {bankDepositAmount.toFixed(2)}</strong></div>
            </div>
            <Button className="w-full" onClick={createSettlement} disabled={submitting || selectedPayments.length === 0 || !componentsValid}>
              {submitting ? <Loader2 size={16} className="mr-2 animate-spin" /> : <BadgeDollarSign size={16} className="mr-2" />}
              Confirmar liquidación
            </Button>
          </div>
        </section>
      </div>

      <section className="overflow-hidden rounded-xl border border-border bg-card shadow-xs" aria-labelledby="history-title">
        <div className="border-b border-border bg-muted/20 p-4">
          <h2 id="history-title" className="flex items-center gap-2 font-semibold text-foreground"><ReceiptText size={17} className="text-[#C69C4B]" /> Historial de liquidaciones</h2>
          <p className="text-xs text-muted-foreground">Trazabilidad del bruto, el depósito y los descuentos del adquirente.</p>
        </div>
        {settlements.length === 0 ? (
          <div className="p-10 text-center text-sm text-muted-foreground">Todavía no hay liquidaciones registradas.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/40 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                <tr><th className="p-3.5 text-left">Liquidación</th><th className="p-3.5 text-left">Fecha / referencia</th><th className="p-3.5 text-right">Bruto</th><th className="p-3.5 text-right">Comisión</th><th className="p-3.5 text-right">Retención</th><th className="p-3.5 text-right">Depósito</th><th className="p-3.5 text-left">Pagos</th></tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {settlements.map((settlement) => (
                  <tr key={settlement.id} className="align-top hover:bg-accent/30">
                    <td className="p-3.5"><div className="font-mono text-xs font-semibold text-foreground" title={settlement.settlementNumber}>{compactNumber(settlement.settlementNumber)}</div><div className="mt-1 text-[11px] text-muted-foreground">{settlement.recordedByUserName}</div></td>
                    <td className="p-3.5"><div className="text-xs text-foreground">{settlement.settlementDate}</div><div className="mt-1 text-[11px] font-medium text-muted-foreground">{settlement.externalReference}</div></td>
                    <td className="p-3.5 text-right font-semibold">L {settlement.grossAmount.toFixed(2)}</td>
                    <td className="p-3.5 text-right text-muted-foreground">L {settlement.commissionAmount.toFixed(2)}</td>
                    <td className="p-3.5 text-right text-muted-foreground">L {settlement.withholdingAmount.toFixed(2)}</td>
                    <td className="p-3.5 text-right font-semibold text-emerald-700 dark:text-emerald-400">L {settlement.bankDepositAmount.toFixed(2)}</td>
                    <td className="p-3.5 text-xs text-muted-foreground" title={settlement.applications.map((application) => application.paymentNumber).join(', ')}>{settlement.applications.map((application) => compactNumber(application.paymentNumber)).join(', ')}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  )
}
