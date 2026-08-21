import { useState, useEffect, useCallback, useMemo } from 'react'
import api from '@/lib/axios'
import type { AccountingEntry, AccountingAccount } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { Pagination } from '@/components/ui/Pagination'
import {
  ScrollText,
  Plus,
  FileText,
  X,
  Loader2,
  Trash2,
  CheckCircle2,
  AlertTriangle,
  RefreshCw,
  Search,
  Calendar,
  Scale,
} from 'lucide-react'

function formatCurrency(n: number) {
  return 'L ' + n.toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

interface FormEntryItem {
  accountId: string
  debit: string
  credit: string
  description: string
}

export default function JournalEntriesPage() {
  const [entries, setEntries] = useState<AccountingEntry[]>([])
  const [accounts, setAccounts] = useState<AccountingAccount[]>([])
  const [loading, setLoading] = useState(true)
  const [selected, setSelected] = useState<AccountingEntry | null>(null)
  const [searchTerm, setSearchTerm] = useState('')

  // Pagination
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)

  // New Journal Entry Modal State
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [transDate, setTransDate] = useState(new Date().toISOString().slice(0, 10))
  const [entryType, setEntryType] = useState('Diario')
  const [entryDescription, setEntryDescription] = useState('')
  const [items, setItems] = useState<FormEntryItem[]>([
    { accountId: '', debit: '', credit: '', description: '' },
    { accountId: '', debit: '', credit: '', description: '' },
  ])
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [actionLoading, setActionLoading] = useState(false)

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      const [entriesRes, accountsRes] = await Promise.all([
        api.get<AccountingEntry[]>('/accounts/journal-entries'),
        api.get<AccountingAccount[]>('/accounts/flat'),
      ])
      setEntries(entriesRes.data)
      setAccounts(accountsRes.data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al consultar el libro diario y catálogo contable.' })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadData()
  }, [loadData])

  // Flattened accounts map for quick name lookup
  const accountMap = useMemo(() => {
    const map = new Map<string, AccountingAccount>()
    accounts.forEach((a) => map.set(a.id, a))
    return map
  }, [accounts])

  // Calculation for Double Entry Live Balance
  const formTotals = useMemo(() => {
    const debit = items.reduce((sum, it) => sum + (Number(it.debit) || 0), 0)
    const credit = items.reduce((sum, it) => sum + (Number(it.credit) || 0), 0)
    const diff = Math.abs(debit - credit)
    const isBalanced = debit > 0 && diff < 0.001
    return { debit, credit, diff, isBalanced }
  }, [items])

  const handleAddItem = () => {
    setItems([...items, { accountId: '', debit: '', credit: '', description: '' }])
  }

  const handleRemoveItem = (idx: number) => {
    if (items.length <= 2) return
    setItems(items.filter((_, i) => i !== idx))
  }

  const handleItemChange = (idx: number, field: keyof FormEntryItem, value: string) => {
    const updated = [...items]
    updated[idx] = { ...updated[idx], [field]: value }
    // If setting debit, clear credit or viceversa
    if (field === 'debit' && value && Number(value) > 0) {
      updated[idx].credit = ''
    } else if (field === 'credit' && value && Number(value) > 0) {
      updated[idx].debit = ''
    }
    setItems(updated)
  }

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!entryDescription.trim()) {
      setAlertInfo({ variant: 'error', message: 'La descripción del asiento es obligatoria.' })
      return
    }

    if (!formTotals.isBalanced) {
      setAlertInfo({
        variant: 'error',
        message: `El asiento no cumple el principio de partida doble. Débitos: L ${formTotals.debit.toFixed(2)} vs Créditos: L ${formTotals.credit.toFixed(2)}.`,
      })
      return
    }

    // Check account validity
    for (const item of items) {
      if (!item.accountId) {
        setAlertInfo({ variant: 'error', message: 'Todas las líneas deben tener una cuenta contable asignada.' })
        return
      }
      const deb = Number(item.debit) || 0
      const cred = Number(item.credit) || 0
      if (deb === 0 && cred === 0) {
        setAlertInfo({ variant: 'error', message: 'Cada línea debe contener un importe en débito o crédito.' })
        return
      }
    }

    setActionLoading(true)
    try {
      await api.post('/accounts/journal-entries', {
        transactionDate: transDate,
        description: entryDescription.trim(),
        entryType: entryType,
        items: items.map((it) => ({
          accountId: it.accountId,
          debit: Number(it.debit) || 0,
          credit: Number(it.credit) || 0,
          description: it.description?.trim() || null,
        })),
      })

      setAlertInfo({ variant: 'success', message: 'Asiento contable manual registrado y cuadrado exitosamente.' })
      setShowCreateModal(false)
      setEntryDescription('')
      setItems([
        { accountId: '', debit: '', credit: '', description: '' },
        { accountId: '', debit: '', credit: '', description: '' },
      ])
      loadData()
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'Error al registrar el asiento contable.' })
    } finally {
      setActionLoading(false)
    }
  }

  const filteredEntries = useMemo(() => {
    return entries.filter(
      (e) =>
        e.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
        e.entryType.toLowerCase().includes(searchTerm.toLowerCase()) ||
        e.items.some((it) => it.accountName.toLowerCase().includes(searchTerm.toLowerCase()) || it.accountNumber.includes(searchTerm))
    )
  }, [entries, searchTerm])

  const totalPages = Math.ceil(filteredEntries.length / pageSize) || 1
  const paginatedEntries = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredEntries.slice(start, start + pageSize)
  }, [filteredEntries, currentPage, pageSize])

  const grandTotals = useMemo(() => {
    return entries.reduce(
      (acc, e) => ({
        debit: acc.debit + e.totalDebit,
        credit: acc.credit + e.totalCredit,
      }),
      { debit: 0, credit: 0 }
    )
  }, [entries])

  if (selected) {
    return (
      <div className="space-y-6 animate-in fade-in">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
              <ScrollText className="text-[#C69C4B]" size={24} />
              Asiento Contable #{selected.id.slice(0, 8).toUpperCase()}
            </h1>
            <p className="text-sm text-muted-foreground">
              Comprobante de diario y desglose de movimientos por partida doble.
            </p>
          </div>
          <Button variant="outline" onClick={() => setSelected(null)} className="gap-1.5 self-start sm:self-auto">
            <X size={15} /> Volver al Listado
          </Button>
        </div>

        <div className="border border-border rounded-xl p-5 bg-card shadow-xs grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div>
            <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Fecha de Asiento</p>
            <p className="text-sm font-bold text-foreground mt-0.5">{selected.transactionDate}</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Tipo de Comprobante</p>
            <span className="inline-block mt-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-[#C69C4B]/15 text-[#C69C4B] border border-[#C69C4B]/30">
              {selected.entryType}
            </span>
          </div>
          <div className="sm:col-span-3 border-t border-border pt-3">
            <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Concepto / Glosa General</p>
            <p className="text-sm text-foreground font-medium mt-0.5">{selected.description}</p>
          </div>
        </div>

        <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
          <table className="w-full text-xs">
            <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
              <tr>
                <th className="text-left p-3.5 w-32">Código</th>
                <th className="text-left p-3.5">Cuenta Contable</th>
                <th className="text-left p-3.5">Detalle / Referencia</th>
                <th className="text-right p-3.5 w-36">Débito (Debe)</th>
                <th className="text-right p-3.5 w-36">Crédito (Haber)</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/60">
              {selected.items.map((item) => (
                <tr key={item.id} className="hover:bg-accent/20 transition-colors">
                  <td className="p-3.5 font-mono font-semibold text-foreground">{item.accountNumber}</td>
                  <td className="p-3.5 font-medium text-foreground">{item.accountName}</td>
                  <td className="p-3.5 text-muted-foreground">{item.description || '—'}</td>
                  <td className="p-3.5 text-right font-mono font-semibold text-foreground">
                    {item.debit > 0 ? formatCurrency(item.debit) : '—'}
                  </td>
                  <td className="p-3.5 text-right font-mono font-semibold text-foreground">
                    {item.credit > 0 ? formatCurrency(item.credit) : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot className="bg-muted/30 font-bold border-t border-border">
              <tr>
                <td colSpan={3} className="p-3.5 text-right text-xs uppercase tracking-wider text-muted-foreground">
                  Totales Cuadrados:
                </td>
                <td className="p-3.5 text-right font-mono text-sm text-foreground">
                  {formatCurrency(selected.totalDebit)}
                </td>
                <td className="p-3.5 text-right font-mono text-sm text-foreground">
                  {formatCurrency(selected.totalCredit)}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <ScrollText className="text-[#C69C4B]" size={24} />
            Libro Diario y Asientos Contables
          </h1>
          <p className="text-sm text-muted-foreground">
            Registro cronológico de operaciones contables, ajustes y pólizas de diario por partida doble.
          </p>
        </div>
        <div className="flex items-center gap-2.5">
          <Button variant="outline" onClick={loadData} disabled={loading} className="gap-1.5">
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            Actualizar
          </Button>
          <Button onClick={() => setShowCreateModal(true)} className="gap-1.5">
            <Plus size={16} />
            Nuevo Asiento Manual
          </Button>
        </div>
      </div>

      {alertInfo && (
        <InlineAlert variant={alertInfo.variant} message={alertInfo.message} onClose={() => setAlertInfo(null)} />
      )}

      {/* Summary Grand Totals & Search Bar */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="border border-border rounded-xl p-4 bg-card shadow-xs flex items-center justify-between">
          <div>
            <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Total Débitos Registrados</p>
            <p className="text-xl font-bold text-foreground mt-0.5 font-mono">{formatCurrency(grandTotals.debit)}</p>
          </div>
        </div>
        <div className="border border-border rounded-xl p-4 bg-card shadow-xs flex items-center justify-between">
          <div>
            <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Total Créditos Registrados</p>
            <p className="text-xl font-bold text-foreground mt-0.5 font-mono">{formatCurrency(grandTotals.credit)}</p>
          </div>
        </div>
        <div className="border border-border rounded-xl p-4 bg-card shadow-xs flex items-center justify-between">
          <div>
            <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Estado de Cuadre Global</p>
            <div className="flex items-center gap-1.5 mt-1 text-sm font-bold">
              {Math.abs(grandTotals.debit - grandTotals.credit) < 0.01 ? (
                <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-1">
                  <CheckCircle2 size={16} /> Partida Doble Cuadrada
                </span>
              ) : (
                <span className="text-red-600 flex items-center gap-1">
                  <AlertTriangle size={16} /> Descuadrado
                </span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="p-4 rounded-xl border border-border bg-card shadow-xs">
        <div className="relative">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por concepto, tipo de asiento, código o nombre de cuenta..."
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value)
              setCurrentPage(1)
            }}
            className="pl-8 text-xs h-9"
          />
        </div>
      </div>

      {/* Journal Entries Table */}
      <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
        <div className="p-3.5 border-b border-border bg-muted/20 flex items-center justify-between">
          <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider">Comprobantes de Diario</h3>
          <span className="text-xs text-muted-foreground">{filteredEntries.length} asiento(s)</span>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-16 space-y-2">
            <Loader2 size={26} className="animate-spin text-[#C69C4B] mr-2" />
            <span className="text-xs text-muted-foreground">Cargando libro diario...</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3.5 w-28">Fecha</th>
                  <th className="text-left p-3.5">Concepto / Glosa</th>
                  <th className="text-left p-3.5 w-24">Tipo</th>
                  <th className="text-right p-3.5 w-32">Total Débito</th>
                  <th className="text-right p-3.5 w-32">Total Crédito</th>
                  <th className="text-center p-3.5 w-20">Líneas</th>
                  <th className="text-right p-3.5 w-24">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {paginatedEntries.map((e) => (
                  <tr key={e.id} className="hover:bg-accent/20 transition-colors">
                    <td className="p-3.5 font-mono text-muted-foreground">{e.transactionDate}</td>
                    <td className="p-3.5 font-medium text-foreground max-w-sm truncate">{e.description}</td>
                    <td className="p-3.5">
                      <span className="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-[#C69C4B]/15 text-[#C69C4B] border border-[#C69C4B]/30">
                        {e.entryType}
                      </span>
                    </td>
                    <td className="p-3.5 text-right font-mono font-bold text-foreground">
                      {formatCurrency(e.totalDebit)}
                    </td>
                    <td className="p-3.5 text-right font-mono font-bold text-foreground">
                      {formatCurrency(e.totalCredit)}
                    </td>
                    <td className="p-3.5 text-center text-muted-foreground font-semibold">{e.items.length}</td>
                    <td className="p-3.5 text-right">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => setSelected(e)}
                        className="h-7 text-xs gap-1"
                      >
                        <FileText size={12} /> Ver
                      </Button>
                    </td>
                  </tr>
                ))}
                {paginatedEntries.length === 0 && (
                  <tr>
                    <td colSpan={7} className="p-8 text-center text-muted-foreground">
                      No se encontraron asientos contables con los filtros seleccionados.
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
          totalItems={filteredEntries.length}
          pageSize={pageSize}
          onPageChange={setCurrentPage}
          onPageSizeChange={(size) => {
            setPageSize(size)
            setCurrentPage(1)
          }}
        />
      </div>

      {/* CREATE MANUAL JOURNAL ENTRY MODAL */}
      {showCreateModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => setShowCreateModal(false)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-3xl shadow-2xl space-y-4 animate-in zoom-in-95 max-h-[90vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border pb-3">
              <div className="flex items-center gap-2">
                <ScrollText className="text-[#C69C4B]" size={22} />
                <h3 className="text-lg font-bold text-foreground">Crear Asiento Contable Manual</h3>
              </div>
              <button
                onClick={() => setShowCreateModal(false)}
                className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent cursor-pointer"
              >
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleCreateSubmit} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3.5">
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Fecha de Asiento *
                  </label>
                  <Input
                    type="date"
                    required
                    value={transDate}
                    onChange={(e) => setTransDate(e.target.value)}
                    className="text-xs mt-1"
                  />
                </div>
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Tipo de Asiento
                  </label>
                  <select
                    value={entryType}
                    onChange={(e) => setEntryType(e.target.value)}
                    className="w-full border border-input rounded-md px-3 py-2 text-xs bg-background mt-1"
                  >
                    <option value="Diario">Diario Operativo</option>
                    <option value="Ajuste">Ajuste / Reclasificación</option>
                    <option value="Cierre">Cierre de Período</option>
                  </select>
                </div>
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Estado de Cuadre
                  </label>
                  <div className="mt-1 p-2 rounded-md border text-xs font-bold flex items-center justify-center gap-1.5 h-9 bg-muted/20">
                    {formTotals.isBalanced ? (
                      <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-1">
                        <CheckCircle2 size={14} /> Cuadrado
                      </span>
                    ) : (
                      <span className="text-amber-600 dark:text-amber-400 flex items-center gap-1">
                        <Scale size={14} /> Descuadre: L {formTotals.diff.toFixed(2)}
                      </span>
                    )}
                  </div>
                </div>
              </div>

              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Concepto / Glosa General *
                </label>
                <Input
                  required
                  placeholder="Ej. Ajuste mensual por depreciación de mobiliario y equipo hotelero"
                  value={entryDescription}
                  onChange={(e) => setEntryDescription(e.target.value)}
                  className="text-xs mt-1"
                />
              </div>

              {/* Dynamic Entry Lines */}
              <div className="space-y-2.5">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Cuentas y Movimientos (Partida Doble)
                  </label>
                  <Button type="button" size="sm" variant="outline" onClick={handleAddItem} className="h-7 text-xs gap-1">
                    <Plus size={13} /> Agregar Línea
                  </Button>
                </div>

                <div className="space-y-2 max-h-60 overflow-y-auto pr-1">
                  {items.map((item, idx) => (
                    <div
                      key={idx}
                      className="grid grid-cols-1 sm:grid-cols-12 gap-2 p-2.5 rounded-lg border border-border/80 bg-muted/10 items-center"
                    >
                      <div className="sm:col-span-5">
                        <select
                          required
                          value={item.accountId}
                          onChange={(e) => handleItemChange(idx, 'accountId', e.target.value)}
                          className="w-full border border-input rounded-md px-2 py-1.5 text-xs bg-background"
                        >
                          <option value="">-- Seleccionar Cuenta --</option>
                          {accounts.map((acc) => (
                            <option key={acc.id} value={acc.id}>
                              {acc.accountNumber} - {acc.accountName} ({acc.accountType})
                            </option>
                          ))}
                        </select>
                      </div>

                      <div className="sm:col-span-2">
                        <Input
                          type="number"
                          step="0.01"
                          min="0"
                          placeholder="Débito (L)"
                          value={item.debit}
                          onChange={(e) => handleItemChange(idx, 'debit', e.target.value)}
                          className="text-xs font-mono"
                        />
                      </div>

                      <div className="sm:col-span-2">
                        <Input
                          type="number"
                          step="0.01"
                          min="0"
                          placeholder="Crédito (L)"
                          value={item.credit}
                          onChange={(e) => handleItemChange(idx, 'credit', e.target.value)}
                          className="text-xs font-mono"
                        />
                      </div>

                      <div className="sm:col-span-2">
                        <Input
                          placeholder="Referencia opcional"
                          value={item.description}
                          onChange={(e) => handleItemChange(idx, 'description', e.target.value)}
                          className="text-xs"
                        />
                      </div>

                      <div className="sm:col-span-1 flex justify-center">
                        <Button
                          type="button"
                          size="sm"
                          variant="destructive"
                          disabled={items.length <= 2}
                          onClick={() => handleRemoveItem(idx)}
                          className="h-7 w-7 p-0"
                          title="Eliminar línea"
                        >
                          <Trash2 size={12} />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>

                {/* Form Totals Bar */}
                <div className="flex justify-between items-center p-3 rounded-lg bg-muted/40 border border-border text-xs font-mono font-bold">
                  <span>Totales del Asiento:</span>
                  <div className="flex gap-6">
                    <span>
                      Débito: <strong className="text-foreground">L {formTotals.debit.toFixed(2)}</strong>
                    </span>
                    <span>
                      Crédito: <strong className="text-foreground">L {formTotals.credit.toFixed(2)}</strong>
                    </span>
                  </div>
                </div>
              </div>

              <div className="flex justify-end gap-2.5 pt-3 border-t border-border">
                <Button type="button" variant="outline" onClick={() => setShowCreateModal(false)}>
                  Cancelar
                </Button>
                <Button
                  type="submit"
                  disabled={actionLoading || !formTotals.isBalanced}
                  className="gap-1.5 bg-[#C69C4B] hover:bg-[#b0883b] text-white"
                >
                  {actionLoading && <Loader2 size={14} className="animate-spin" />}
                  Guardar Asiento Cuadrado
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
