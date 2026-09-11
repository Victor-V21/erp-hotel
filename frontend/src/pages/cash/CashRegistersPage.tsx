import { useState, useEffect, useCallback } from 'react'
import api from '@/lib/axios'
import type { CashRegister, CashMovement } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { getApiErrorMessage } from '@/lib/errors'
import { DollarSign, Plus, Lock, Unlock, ArrowDownRight, ArrowUpRight, Loader2 } from 'lucide-react'

interface CashRegisterOperation {
  movementId: string
  message: string
  currentBalance: number
  expectedAmount?: number | null
  countedAmount?: number | null
  difference?: number | null
}

const idempotencyStorageKey = (action: 'open' | 'close', registerId: string) =>
  `cash-register:${registerId}:${action}:idempotency-key`

function getOrCreateIdempotencyKey(action: 'open' | 'close', registerId: string) {
  const storageKey = idempotencyStorageKey(action, registerId)
  const existing = sessionStorage.getItem(storageKey)
  if (existing) return existing
  const created = crypto.randomUUID()
  sessionStorage.setItem(storageKey, created)
  return created
}

function clearIdempotencyKey(action: 'open' | 'close', registerId: string) {
  sessionStorage.removeItem(idempotencyStorageKey(action, registerId))
}

export default function CashRegistersPage() {
  const [registers, setRegisters] = useState<CashRegister[]>([])
  const [movements, setMovements] = useState<CashMovement[]>([])
  const [selectedRegister, setSelectedRegister] = useState<string | null>(null)
  const [showOpen, setShowOpen] = useState(false)
  const [openAmount, setOpenAmount] = useState('0')
  const [showClose, setShowClose] = useState(false)
  const [countedAmount, setCountedAmount] = useState('0')
  const [closeNotes, setCloseNotes] = useState('')
  const [showNewRegister, setShowNewRegister] = useState(false)
  const [newRegisterName, setNewRegisterName] = useState('')
  const [newRegisterDesc, setNewRegisterDesc] = useState('')
  const [loading, setLoading] = useState(false)
  const [actionLoading, setActionLoading] = useState(false)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success'; message: string } | null>(null)

  const loadMovements = useCallback(async (id: string) => {
    setLoading(true)
    try {
      setSelectedRegister(id)
      const { data } = await api.get<CashMovement[]>(`/cash-registers/${id}/movements`)
      setMovements(data)
    } catch (error) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al cargar los movimientos de la caja.') })
    } finally {
      setLoading(false)
    }
  }, [])

  const load = useCallback(async () => {
    try {
      const { data } = await api.get<CashRegister[]>('/cash-registers')
      setRegisters(data)
      if (data.length > 0 && !selectedRegister) {
        await loadMovements(data[0].id)
      }
    } catch (error) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al cargar las cajas registradoras.') })
    }
  }, [loadMovements, selectedRegister])

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0)
    return () => window.clearTimeout(timer)
  }, [load])

  const openRegister = async () => {
    if (!selectedRegister) return
    const amount = Number(openAmount)
    if (!Number.isFinite(amount) || amount < 0) {
      setAlertInfo({ variant: 'error', message: 'Ingrese un fondo inicial válido, igual o mayor que cero.' })
      return
    }
    const key = getOrCreateIdempotencyKey('open', selectedRegister)
    setActionLoading(true)
    try {
      const { data } = await api.post<CashRegisterOperation>(
        `/cash-registers/${selectedRegister}/open`,
        { initialAmount: amount },
        { headers: { 'Idempotency-Key': key } }
      )
      clearIdempotencyKey('open', selectedRegister)
      setShowOpen(false)
      setAlertInfo({ variant: 'success', message: `${data.message}. Saldo inicial: L ${data.currentBalance.toFixed(2)}.` })
      await Promise.all([load(), loadMovements(selectedRegister)])
    } catch (error) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al realizar la apertura de caja.') })
    } finally {
      setActionLoading(false)
    }
  }

  const closeRegister = async () => {
    if (!selectedRegister) return
    const counted = Number(countedAmount)
    if (!Number.isFinite(counted) || counted < 0) {
      setAlertInfo({ variant: 'error', message: 'Ingrese un monto físico contado válido, igual o mayor que cero.' })
      return
    }
    const difference = Math.round((counted - currentBalance) * 100) / 100
    if (difference !== 0 && closeNotes.trim().length < 3) {
      setAlertInfo({ variant: 'error', message: 'Explique la diferencia de caja con al menos 3 caracteres.' })
      return
    }
    const key = getOrCreateIdempotencyKey('close', selectedRegister)
    setActionLoading(true)
    try {
      const { data } = await api.post<CashRegisterOperation>(
        `/cash-registers/${selectedRegister}/close`,
        { countedAmount: counted, notes: closeNotes.trim() || null },
        { headers: { 'Idempotency-Key': key } }
      )
      clearIdempotencyKey('close', selectedRegister)
      setShowClose(false)
      setCloseNotes('')
      setAlertInfo({
        variant: 'success',
        message: `${data.message}. Sistema: L ${(data.expectedAmount ?? 0).toFixed(2)}, contado: L ${(data.countedAmount ?? 0).toFixed(2)}, diferencia: L ${(data.difference ?? 0).toFixed(2)}.`,
      })
      await Promise.all([load(), loadMovements(selectedRegister)])
    } catch (error) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al realizar el cierre de caja.') })
    } finally {
      setActionLoading(false)
    }
  }

  const createRegister = async () => {
    if (!newRegisterName.trim()) {
      setAlertInfo({ variant: 'error', message: 'El nombre de la caja es requerido.' })
      return
    }
    try {
      await api.post('/cash-registers', { name: newRegisterName, description: newRegisterDesc || null })
      setNewRegisterName('')
      setNewRegisterDesc('')
      setShowNewRegister(false)
      setAlertInfo({ variant: 'success', message: 'Nueva caja registradora creada exitosamente.' })
      load()
    } catch (error) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al registrar la nueva caja.') })
    }
  }

  const activeRegisterObj = registers.find((r) => r.id === selectedRegister)
  const currentBalance = activeRegisterObj?.currentBalance ?? 0
  const countedValue = Number(countedAmount)
  const liveDifference = Number.isFinite(countedValue)
    ? Math.round((countedValue - currentBalance) * 100) / 100
    : 0

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <DollarSign className="text-[#C69C4B]" size={24} />
            Control de Caja y Arqueos
          </h1>
          <p className="text-sm text-muted-foreground">
            Aperturas, cobros en efectivo, egresos y cierres de turno.
          </p>
        </div>
        <Button onClick={() => setShowNewRegister(!showNewRegister)}>
          {showNewRegister ? 'Cancelar' : <><Plus size={16} className="mr-1.5" /> Nueva Caja</>}
        </Button>
      </div>

      {alertInfo && (
        <InlineAlert
          variant={alertInfo.variant}
          message={alertInfo.message}
          onClose={() => setAlertInfo(null)}
        />
      )}

      {showNewRegister && (
        <div className="border border-border rounded-xl p-5 bg-card space-y-4 shadow-sm animate-in fade-in max-w-lg">
          <h3 className="font-semibold text-foreground text-md">Crear Nueva Caja Registradora</h3>
          <div className="space-y-3">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Nombre de la Caja *
              </label>
              <Input
                value={newRegisterName}
                onChange={(e) => setNewRegisterName(e.target.value)}
                placeholder="Ej. Caja Recepción Noche"
                className="mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Descripción / Ubicación
              </label>
              <Input
                value={newRegisterDesc}
                onChange={(e) => setNewRegisterDesc(e.target.value)}
                placeholder="Ej. Terminal 2 - Lobby principal"
                className="mt-1"
              />
            </div>
          </div>
          <div className="flex gap-2 pt-2">
            <Button onClick={createRegister}>Guardar Caja</Button>
            <Button variant="outline" onClick={() => setShowNewRegister(false)}>
              Cancelar
            </Button>
          </div>
        </div>
      )}

      {/* Register Selector Tabs */}
      <div className="flex items-center gap-2 flex-wrap border-b border-border pb-3">
        {registers.map((r) => (
          <Button
            key={r.id}
            variant={selectedRegister === r.id ? 'default' : 'outline'}
            onClick={() => loadMovements(r.id)}
            className="text-sm font-medium"
          >
            {r.name}
          </Button>
        ))}
      </div>

      {selectedRegister && activeRegisterObj && (
        <div className="space-y-6">
          {/* Register Status Banner */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="border border-border rounded-xl p-5 bg-card shadow-xs flex items-center justify-between">
              <div>
                <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">
                  Caja Seleccionada
                </p>
                <p className="text-lg font-bold text-foreground mt-0.5">{activeRegisterObj.name}</p>
                <p className="text-xs text-muted-foreground">{activeRegisterObj.description || 'Activa'}</p>
                <p className={`text-xs font-semibold mt-2 ${activeRegisterObj.isOpen ? 'text-emerald-600 dark:text-emerald-400' : 'text-muted-foreground'}`}>
                  {activeRegisterObj.isOpen ? 'Turno abierto' : 'Turno cerrado'}
                </p>
              </div>
              <div className="p-3 rounded-full bg-primary/10 text-[#C69C4B]">
                <DollarSign size={24} />
              </div>
            </div>

            <div className="border border-border rounded-xl p-5 bg-card shadow-xs flex items-center justify-between">
              <div>
                <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">
                  Saldo Actual en Caja
                </p>
                <p className="text-2xl font-bold text-[#C69C4B] mt-0.5">
                  L {currentBalance.toFixed(2)}
                </p>
                <p className="text-xs text-muted-foreground">
                  {movements.length} {movements.length === 1 ? 'movimiento acumulado' : 'movimientos acumulados'}
                </p>
              </div>
            </div>

            <div className="border border-border rounded-xl p-5 bg-card shadow-xs flex items-center gap-3">
              {activeRegisterObj.isOpen ? (
                <Button
                  variant="destructive"
                  className="w-full gap-1.5"
                  disabled={actionLoading}
                  onClick={() => {
                    setShowClose(!showClose)
                    setShowOpen(false)
                    setCountedAmount(currentBalance.toFixed(2))
                    setCloseNotes('')
                  }}
                >
                  <Lock size={16} /> Cerrar y arquear caja
                </Button>
              ) : (
                <Button
                  variant="outline"
                  className="w-full gap-1.5"
                  disabled={actionLoading}
                  onClick={() => {
                    setShowOpen(!showOpen)
                    setShowClose(false)
                  }}
                >
                  <Unlock size={16} /> Abrir turno
                </Button>
              )}
            </div>
          </div>

          {/* Action Modals / Cards */}
          {showOpen && (
            <div className="border border-border rounded-xl p-5 bg-card space-y-3 shadow-md max-w-sm animate-in fade-in">
              <h3 className="font-semibold text-sm text-foreground flex items-center gap-2">
                <Unlock size={16} className="text-[#C69C4B]" />
                Apertura de Caja
              </h3>
              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Fondo Inicial de Caja (L)
                </label>
                <Input
                  type="number"
                  step="0.01"
                  min="0"
                  value={openAmount}
                  onChange={(e) => setOpenAmount(e.target.value)}
                  className="mt-1"
                />
              </div>
              <div className="flex gap-2 pt-2">
                <Button size="sm" onClick={openRegister} disabled={actionLoading}>
                  {actionLoading && <Loader2 size={14} className="mr-1.5 animate-spin" />}
                  Confirmar Apertura
                </Button>
                <Button size="sm" variant="outline" onClick={() => setShowOpen(false)} disabled={actionLoading}>
                  Cancelar
                </Button>
              </div>
            </div>
          )}

          {showClose && (
            <div className="border border-border rounded-xl p-5 bg-card space-y-3 shadow-md max-w-sm animate-in fade-in">
              <h3 className="font-semibold text-sm text-foreground flex items-center gap-2 text-destructive">
                <Lock size={16} />
                Cierre y Arqueo de Caja
              </h3>
              <div className="text-xs text-muted-foreground bg-muted/40 p-2 rounded-md">
                Saldo del sistema: <span className="font-bold text-foreground">L {currentBalance.toFixed(2)}</span>
              </div>
              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Monto Físico Contado (L)
                </label>
                <Input
                  type="number"
                  step="0.01"
                  min="0"
                  value={countedAmount}
                  onChange={(e) => setCountedAmount(e.target.value)}
                  className="mt-1"
                />
              </div>
              <div className={`text-xs p-2 rounded-md ${liveDifference === 0 ? 'bg-emerald-50 dark:bg-emerald-950/20 text-emerald-700 dark:text-emerald-300' : 'bg-amber-50 dark:bg-amber-950/20 text-amber-800 dark:text-amber-300'}`}>
                Diferencia: <span className="font-bold">L {liveDifference.toFixed(2)}</span>
              </div>
              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground" htmlFor="cash-close-notes">
                  Explicación {liveDifference !== 0 ? '*' : '(opcional)'}
                </label>
                <textarea
                  id="cash-close-notes"
                  value={closeNotes}
                  onChange={(event) => setCloseNotes(event.target.value)}
                  maxLength={500}
                  rows={3}
                  placeholder="Explique sobrantes, faltantes o cualquier incidencia del arqueo"
                  className="mt-1 flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-xs outline-none transition-colors placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/30 disabled:cursor-not-allowed disabled:opacity-50"
                />
              </div>
              <div className="flex gap-2 pt-2">
                <Button
                  size="sm"
                  variant="destructive"
                  onClick={closeRegister}
                  disabled={actionLoading || (liveDifference !== 0 && closeNotes.trim().length < 3)}
                >
                  {actionLoading && <Loader2 size={14} className="mr-1.5 animate-spin" />}
                  Confirmar Cierre
                </Button>
                <Button size="sm" variant="outline" onClick={() => setShowClose(false)} disabled={actionLoading}>
                  Cancelar
                </Button>
              </div>
            </div>
          )}

          {/* Movements History */}
          <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
            <div className="p-4 border-b border-border/80 bg-muted/20 flex items-center justify-between">
              <h3 className="font-semibold text-sm text-foreground">Movimientos Registrados</h3>
              <span className="text-xs text-muted-foreground">
                {movements.length} {movements.length === 1 ? 'registro' : 'registros'}
              </span>
            </div>

            {loading ? (
              <div className="flex items-center justify-center py-12 space-y-2">
                <Loader2 size={24} className="animate-spin text-[#C69C4B] mr-2" />
                <span className="text-sm text-muted-foreground">Cargando movimientos...</span>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-muted/40 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                    <tr>
                      <th className="text-left p-3.5">Fecha y Hora</th>
                      <th className="text-left p-3.5">Tipo</th>
                      <th className="text-right p-3.5">Monto</th>
                      <th className="text-right p-3.5">Saldo Posterior</th>
                      <th className="text-left p-3.5">Descripción / Referencia</th>
                      <th className="text-left p-3.5">Cajero</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/60">
                    {movements.map((m) => {
                      const isPositive =
                        m.movementType === 'Ingreso' ||
                        m.movementType === 'Apertura' ||
                        m.movementType === 'Venta'
                      const isNeutral = m.movementType === 'Cierre' || m.movementType === 'Arqueo'
                      return (
                        <tr key={m.id} className="hover:bg-accent/30 transition-colors">
                          <td className="p-3.5 text-xs text-muted-foreground">
                            {new Date(m.movementDate).toLocaleString('es-HN', {
                              timeZone: 'America/Tegucigalpa',
                              dateStyle: 'short',
                              timeStyle: 'short',
                            })}
                          </td>
                          <td className="p-3.5">
                            <span
                              className={`px-2.5 py-0.5 rounded-full text-xs font-semibold flex items-center w-fit gap-1 ${
                                isPositive
                                  ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300'
                                  : isNeutral
                                    ? 'bg-slate-100 dark:bg-slate-900/50 text-slate-700 dark:text-slate-300'
                                  : 'bg-red-100 dark:bg-red-950/40 text-red-800 dark:text-red-300'
                              }`}
                            >
                              {isPositive ? <ArrowUpRight size={12} /> : isNeutral ? <Lock size={12} /> : <ArrowDownRight size={12} />}
                              {m.movementType}
                            </span>
                          </td>
                          <td
                            className={`p-3.5 text-right font-bold text-xs ${
                              isPositive
                                ? 'text-emerald-600 dark:text-emerald-400'
                                : isNeutral
                                  ? 'text-muted-foreground'
                                  : 'text-red-600 dark:text-red-400'
                            }`}
                          >
                            {isNeutral ? '—' : `${isPositive ? '+' : '-'}L ${Math.abs(m.amount).toFixed(2)}`}
                          </td>
                          <td className="p-3.5 text-right font-mono text-xs font-medium text-foreground">
                            L {m.balanceAfter.toFixed(2)}
                          </td>
                          <td className="p-3.5 text-xs text-foreground">
                            <div>{m.description || '—'}</div>
                            {m.movementType === 'Cierre' && m.expectedAmount != null && m.countedAmount != null && (
                              <div className="mt-1 text-[11px] leading-relaxed text-muted-foreground">
                                Sistema L {m.expectedAmount.toFixed(2)} · Contado L {m.countedAmount.toFixed(2)} · Diferencia L {(m.difference ?? 0).toFixed(2)}
                              </div>
                            )}
                            {m.notes && <div className="mt-1 text-[11px] italic text-muted-foreground">Nota: {m.notes}</div>}
                          </td>
                          <td className="p-3.5 text-xs text-muted-foreground">{m.userName || 'Sistema'}</td>
                        </tr>
                      )
                    })}
                    {movements.length === 0 && (
                      <tr>
                        <td colSpan={6} className="p-8 text-center text-muted-foreground">
                          No hay movimientos registrados en esta caja.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
