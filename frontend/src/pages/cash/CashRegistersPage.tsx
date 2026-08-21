import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { CashRegister, CashMovement } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { DollarSign, Plus, Lock, Unlock, ArrowDownRight, ArrowUpRight, Loader2 } from 'lucide-react'

export default function CashRegistersPage() {
  const [registers, setRegisters] = useState<CashRegister[]>([])
  const [movements, setMovements] = useState<CashMovement[]>([])
  const [selectedRegister, setSelectedRegister] = useState<string | null>(null)
  const [showOpen, setShowOpen] = useState(false)
  const [openAmount, setOpenAmount] = useState('0')
  const [showClose, setShowClose] = useState(false)
  const [countedAmount, setCountedAmount] = useState('0')
  const [showNewRegister, setShowNewRegister] = useState(false)
  const [newRegisterName, setNewRegisterName] = useState('')
  const [newRegisterDesc, setNewRegisterDesc] = useState('')
  const [loading, setLoading] = useState(false)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success'; message: string } | null>(null)

  useEffect(() => {
    load()
  }, [])

  const load = async () => {
    try {
      const { data } = await api.get<CashRegister[]>('/cash-registers')
      setRegisters(data)
      if (data.length > 0 && !selectedRegister) {
        loadMovements(data[0].id)
      }
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al cargar las cajas registradoras.' })
    }
  }

  const loadMovements = async (id: string) => {
    setLoading(true)
    try {
      setSelectedRegister(id)
      const { data } = await api.get<CashMovement[]>(`/cash-registers/${id}/movements`)
      setMovements(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al cargar los movimientos de la caja.' })
    } finally {
      setLoading(false)
    }
  }

  const openRegister = async () => {
    if (!selectedRegister) return
    try {
      await api.post(`/cash-registers/${selectedRegister}/open`, {
        cashRegisterId: selectedRegister,
        initialAmount: +openAmount,
      })
      setShowOpen(false)
      setAlertInfo({ variant: 'success', message: `Caja abierta con fondo inicial de L ${(+openAmount).toFixed(2)}.` })
      loadMovements(selectedRegister)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al realizar la apertura de caja.' })
    }
  }

  const closeRegister = async () => {
    if (!selectedRegister) return
    try {
      const lastBal = movements.length > 0 ? movements[0].balanceAfter : 0
      await api.post(`/cash-registers/${selectedRegister}/close`, {
        cashRegisterId: selectedRegister,
        expectedAmount: lastBal,
        countedAmount: +countedAmount,
        notes: '',
      })
      setShowClose(false)
      setAlertInfo({ variant: 'success', message: 'Cierre de caja completado y registrado correctamente.' })
      loadMovements(selectedRegister)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al realizar el cierre de caja.' })
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
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al registrar la nueva caja.' })
    }
  }

  const activeRegisterObj = registers.find((r) => r.id === selectedRegister)
  const currentBalance = movements.length > 0 ? movements[0].balanceAfter : 0

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
                <p className="text-xs text-muted-foreground">{movements.length} movimiento(s) hoy</p>
              </div>
            </div>

            <div className="border border-border rounded-xl p-5 bg-card shadow-xs flex items-center gap-3">
              <Button
                variant="outline"
                className="flex-1 gap-1.5"
                onClick={() => {
                  setShowOpen(!showOpen)
                  setShowClose(false)
                }}
              >
                <Unlock size={16} /> Abrir Turno
              </Button>
              <Button
                variant="destructive"
                className="flex-1 gap-1.5"
                onClick={() => {
                  setShowClose(!showClose)
                  setShowOpen(false)
                  setCountedAmount(String(currentBalance))
                }}
              >
                <Lock size={16} /> Cerrar Caja
              </Button>
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
                  value={openAmount}
                  onChange={(e) => setOpenAmount(e.target.value)}
                  className="mt-1"
                />
              </div>
              <div className="flex gap-2 pt-2">
                <Button size="sm" onClick={openRegister}>
                  Confirmar Apertura
                </Button>
                <Button size="sm" variant="outline" onClick={() => setShowOpen(false)}>
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
                  value={countedAmount}
                  onChange={(e) => setCountedAmount(e.target.value)}
                  className="mt-1"
                />
              </div>
              <div className="flex gap-2 pt-2">
                <Button size="sm" variant="destructive" onClick={closeRegister}>
                  Confirmar Cierre
                </Button>
                <Button size="sm" variant="outline" onClick={() => setShowClose(false)}>
                  Cancelar
                </Button>
              </div>
            </div>
          )}

          {/* Movements History */}
          <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
            <div className="p-4 border-b border-border/80 bg-muted/20 flex items-center justify-between">
              <h3 className="font-semibold text-sm text-foreground">Movimientos Registrados</h3>
              <span className="text-xs text-muted-foreground">{movements.length} registro(s)</span>
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
                      return (
                        <tr key={m.id} className="hover:bg-accent/30 transition-colors">
                          <td className="p-3.5 text-xs text-muted-foreground">
                            {new Date(m.movementDate).toLocaleString('es-HN')}
                          </td>
                          <td className="p-3.5">
                            <span
                              className={`px-2.5 py-0.5 rounded-full text-xs font-semibold flex items-center w-fit gap-1 ${
                                isPositive
                                  ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300'
                                  : 'bg-red-100 dark:bg-red-950/40 text-red-800 dark:text-red-300'
                              }`}
                            >
                              {isPositive ? <ArrowUpRight size={12} /> : <ArrowDownRight size={12} />}
                              {m.movementType}
                            </span>
                          </td>
                          <td
                            className={`p-3.5 text-right font-bold text-xs ${
                              isPositive ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'
                            }`}
                          >
                            {isPositive ? '+' : '-'}L {Math.abs(m.amount).toFixed(2)}
                          </td>
                          <td className="p-3.5 text-right font-mono text-xs font-medium text-foreground">
                            L {m.balanceAfter.toFixed(2)}
                          </td>
                          <td className="p-3.5 text-xs text-foreground">{m.description || '—'}</td>
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

