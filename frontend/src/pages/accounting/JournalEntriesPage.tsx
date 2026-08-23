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

function AccountCombobox({
  accounts,
  value,
  onChange,
}: {
  accounts: AccountingAccount[]
  value: string
  onChange: (id: string) => void
}) {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const selected = accounts.find((a) => a.id === value)

  const filtered = accounts.filter(
    (a) =>
      a.accountNumber.includes(search) ||
      a.accountName.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="relative w-full">
      <div
        className="flex items-center w-full border border-input rounded-md px-2 py-1.5 text-xs bg-background cursor-pointer"
        onClick={() => setOpen(true)}
      >
        <span className="truncate flex-1">
          {selected ? `${selected.accountNumber} - ${selected.accountName}` : '-- Buscar Cuenta --'}
        </span>
      </div>

      {open && (
        <div className="absolute top-full left-0 mt-1 w-[300px] sm:w-full max-h-60 overflow-y-auto bg-card border border-border rounded-md shadow-lg z-50 p-1">
          <input
            autoFocus
            className="w-full px-2 py-1.5 mb-1 text-xs border border-border rounded-sm bg-muted/20 outline-none focus:border-primary"
            placeholder="Buscar por código o nombre..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onBlur={() => setTimeout(() => setOpen(false), 150)}
          />
          {filtered.length === 0 ? (
            <div className="px-2 py-2 text-xs text-muted-foreground text-center">No hay coincidencias</div>
          ) : (
            filtered.map((acc) => (
              <div
                key={acc.id}
                onMouseDown={() => {
                  onChange(acc.id)
                  setSearch('')
                  setOpen(false)
                }}
                className="px-2 py-1.5 text-xs hover:bg-accent rounded-sm cursor-pointer truncate"
              >
                <span className="font-mono font-medium">{acc.accountNumber}</span> - {acc.accountName}
              </div>
            ))
          )}
        </div>
      )}
    </div>
  )
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

  const handleKeyDown = (e: React.KeyboardEvent, idx: number) => {
    if (e.key === 'Enter' && idx === items.length - 1) {
      e.preventDefault()
      handleAddItem()
    }
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

    // Validación de Cierre Contable (Prevención Lateral)
    const today = new Date()
    const selectedDate = new Date(transDate)
    const firstDayOfCurrentMonth = new Date(today.getFullYear(), today.getMonth(), 1)
    
    if (selectedDate < firstDayOfCurrentMonth) {
      setAlertInfo({ 
        variant: 'error', 
        message: 'Acción denegada: El mes contable seleccionado ya se encuentra cerrado. No se permiten asientos manuales en períodos históricos.' 
      })
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
            className="bg-card border border-border rounded-xl w-full max-w-6xl shadow-2xl flex flex-col h-[90vh] animate-in zoom-in-95 overflow-hidden"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border p-4 bg-muted/30">
              <div className="flex items-center gap-2">
                <ScrollText className="text-[#C69C4B]" size={22} />
                <h3 className="text-lg font-bold text-foreground">Crear Asiento Contable Manual</h3>
              </div>
              <button
                onClick={() => setShowCreateModal(false)}
                className="p-1.5 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent transition-colors"
              >
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleCreateSubmit} className="flex-1 overflow-hidden flex flex-col lg:flex-row">
              {/* Left Panel: Inputs */}
              <div className="flex-1 overflow-y-auto p-6 space-y-6 lg:border-r border-border">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      Fecha de Asiento *
                    </label>
                    <Input
                      type="date"
                      required
                      value={transDate}
                      onChange={(e) => setTransDate(e.target.value)}
                      className="text-sm mt-1.5 font-medium"
                    />
                  </div>
                  <div>
                    <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      Tipo de Asiento
                    </label>
                    <select
                      value={entryType}
                      onChange={(e) => setEntryType(e.target.value)}
                      className="w-full border border-input rounded-md px-3 py-2 text-sm font-medium bg-background mt-1.5"
                    >
                      <option value="Diario">Diario Operativo</option>
                      <option value="Ajuste">Ajuste / Reclasificación</option>
                      <option value="Cierre">Cierre de Período</option>
                    </select>
                  </div>
                </div>

                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Concepto / Glosa General *
                  </label>
                  <Input
                    required
                    placeholder="Ej. Ajuste mensual por depreciación..."
                    value={entryDescription}
                    onChange={(e) => setEntryDescription(e.target.value)}
                    className="text-sm mt-1.5"
                  />
                </div>

                {/* Dynamic Entry Lines */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between border-b border-border pb-2">
                    <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      Movimientos
                    </label>
                    <Button type="button" size="sm" variant="outline" onClick={handleAddItem} className="h-7 text-xs gap-1.5">
                      <Plus size={14} /> Nueva Línea
                    </Button>
                  </div>

                  <div className="space-y-3">
                    {items.map((item, idx) => (
                      <div
                        key={idx}
                        className="group relative p-3 rounded-lg border border-border bg-muted/10 grid grid-cols-12 gap-3 items-start"
                      >
                        <div className="col-span-12 sm:col-span-6 space-y-1.5">
                          <label className="text-[10px] font-semibold text-muted-foreground uppercase">Cuenta</label>
                          <AccountCombobox
                            accounts={accounts}
                            value={item.accountId}
                            onChange={(val) => handleItemChange(idx, 'accountId', val)}
                          />
                        </div>
                        <div className="col-span-6 sm:col-span-3 space-y-1.5">
                          <label className="text-[10px] font-semibold text-muted-foreground uppercase">Débito</label>
                          <Input
                            type="number"
                            step="0.01"
                            min="0"
                            placeholder="0.00"
                            value={item.debit}
                            onChange={(e) => handleItemChange(idx, 'debit', e.target.value)}
                            className="text-xs font-mono"
                          />
                        </div>
                        <div className="col-span-6 sm:col-span-3 space-y-1.5 relative">
                          <label className="text-[10px] font-semibold text-muted-foreground uppercase">Crédito</label>
                          <Input
                            type="number"
                            step="0.01"
                            min="0"
                            placeholder="0.00"
                            value={item.credit}
                            onChange={(e) => handleItemChange(idx, 'credit', e.target.value)}
                            onKeyDown={(e) => handleKeyDown(e, idx)}
                            className="text-xs font-mono"
                          />
                          <button
                            type="button"
                            onClick={() => handleRemoveItem(idx)}
                            disabled={items.length <= 2}
                            className="absolute -right-8 top-6 p-1.5 rounded text-red-500 hover:bg-red-500/10 disabled:opacity-30 disabled:pointer-events-none"
                            title="Eliminar"
                          >
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>

              {/* Right Panel: Preview & Validation */}
              <div className="w-full lg:w-96 bg-muted/20 p-6 flex flex-col justify-between">
                <div>
                  <h4 className="font-semibold text-sm mb-4 text-foreground flex items-center gap-2">
                    <Scale size={16} /> Verificación de Partida Doble
                  </h4>

                  {/* Floating Live Balance Indicator */}
                  <div className={`p-3 mb-4 rounded-lg border flex items-center gap-2 text-sm font-bold shadow-sm transition-colors ${formTotals.isBalanced ? 'bg-emerald-100/50 border-emerald-200 text-emerald-700 dark:bg-emerald-950/30 dark:border-emerald-900/50 dark:text-emerald-400' : 'bg-red-100/50 border-red-200 text-red-700 dark:bg-red-950/30 dark:border-red-900/50 dark:text-red-400'}`}>
                    {formTotals.isBalanced ? <CheckCircle2 size={18} /> : <AlertTriangle size={18} />}
                    {formTotals.isBalanced ? '¡Cuadrado!' : `Descuadre: L ${formTotals.diff.toFixed(2)}`}
                  </div>

                  <div className="space-y-4">
                    <div className="p-4 rounded-xl border border-border bg-card shadow-sm">
                      <div className="flex justify-between items-center mb-2">
                        <span className="text-sm text-muted-foreground">Total Débitos</span>
                        <span className="text-lg font-mono font-bold text-foreground">L {formTotals.debit.toFixed(2)}</span>
                      </div>
                      <div className="flex justify-between items-center pb-3 border-b border-border">
                        <span className="text-sm text-muted-foreground">Total Créditos</span>
                        <span className="text-lg font-mono font-bold text-foreground">L {formTotals.credit.toFixed(2)}</span>
                      </div>
                      <div className="flex justify-between items-center pt-3">
                        <span className="text-sm font-semibold">Diferencia</span>
                        <span className={`text-lg font-mono font-bold ${formTotals.isBalanced ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'}`}>
                          L {formTotals.diff.toFixed(2)}
                        </span>
                      </div>
                    </div>

                    <div className={`p-4 rounded-xl border ${formTotals.isBalanced ? 'bg-emerald-50 border-emerald-200 dark:bg-emerald-950/20 dark:border-emerald-900/50' : 'bg-red-50 border-red-200 dark:bg-red-950/20 dark:border-red-900/50'}`}>
                      {formTotals.isBalanced ? (
                        <div className="flex flex-col items-center justify-center text-center space-y-2 text-emerald-700 dark:text-emerald-400">
                          <CheckCircle2 size={32} />
                          <div>
                            <p className="font-bold">Asiento Cuadrado</p>
                            <p className="text-xs opacity-90 mt-1">Listo para ser registrado en el libro mayor.</p>
                          </div>
                        </div>
                      ) : (
                        <div className="flex flex-col items-center justify-center text-center space-y-2 text-red-700 dark:text-red-400">
                          <AlertTriangle size={32} />
                          <div>
                            <p className="font-bold">Asiento Descuadrado</p>
                            <p className="text-xs opacity-90 mt-1">Revisa los montos ingresados antes de guardar.</p>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                <div className="mt-8 space-y-3">
                  <Button
                    type="submit"
                    disabled={actionLoading || !formTotals.isBalanced}
                    className="w-full py-6 text-base font-bold bg-[#C69C4B] hover:bg-[#b0883b] text-white disabled:opacity-50"
                  >
                    {actionLoading && <Loader2 size={18} className="animate-spin mr-2" />}
                    Confirmar y Guardar
                  </Button>
                  <Button type="button" variant="outline" className="w-full py-6" onClick={() => setShowCreateModal(false)}>
                    Cancelar
                  </Button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
