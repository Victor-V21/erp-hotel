import { useMemo, useState, useEffect } from 'react'
import api from '@/lib/axios'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import {
  BarChart3,
  FileSpreadsheet,
  Download,
  Calendar,
  TrendingUp,
  Scale,
  DollarSign,
  Loader2,
  CheckCircle2,
  Receipt,
  Building,
  RefreshCw,
} from 'lucide-react'

interface TaxSummary {
  from: string
  to: string
  salesTaxed15: number
  salesTaxed18: number
  salesExempt: number
  isvCollected15: number
  isvCollected18: number
  isvOnPurchases: number
  netIsvToPay: number
  touristTaxCollected: number
}

interface FinancialLine {
  accountNumber: string
  accountName: string
  amount: number
}

interface IncomeStatement {
  from: string
  to: string
  revenues: FinancialLine[]
  totalRevenues: number
  expenses: FinancialLine[]
  totalExpenses: number
  netIncome: number
}

interface BalanceSheet {
  asOfDate: string
  assets: FinancialLine[]
  totalAssets: number
  liabilities: FinancialLine[]
  totalLiabilities: number
  equity: FinancialLine[]
  totalEquity: number
  totalLiabilitiesAndEquity: number
  isBalanced: boolean
}

const quickExports = [
  { label: 'Huéspedes (Directorio Completo)', path: '/data/export/guests', fileName: 'huespedes_hotel_maya.xlsx' },
  { label: 'Facturas SAR Emitidas', path: '/data/export/invoices', fileName: 'facturas_sar_hotel_maya.xlsx' },
  { label: 'Reservaciones y Ocupación', path: '/data/export/reservations', fileName: 'reservaciones_hotel_maya.xlsx' },
  { label: 'Catálogo de Habitaciones', path: '/data/export/rooms', fileName: 'habitaciones_hotel_maya.xlsx' },
]

function currentMonthRange() {
  const now = new Date()
  const first = new Date(now.getFullYear(), now.getMonth(), 1)
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  return {
    from: first.toISOString().slice(0, 10),
    to: today.toISOString().slice(0, 10),
  }
}

function fmt(n: number) {
  return 'L ' + n.toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function ReportsPage() {
  const initial = useMemo(() => currentMonthRange(), [])
  const [activeTab, setActiveTab] = useState<'sar' | 'income' | 'balance' | 'exports'>('sar')
  const [from, setFrom] = useState(initial.from)
  const [to, setTo] = useState(initial.to)
  const [asOfDate, setAsOfDate] = useState(initial.to)

  const [taxSummary, setTaxSummary] = useState<TaxSummary | null>(null)
  const [incomeStatement, setIncomeStatement] = useState<IncomeStatement | null>(null)
  const [balanceSheet, setBalanceSheet] = useState<BalanceSheet | null>(null)

  const [loading, setLoading] = useState(false)
  const [exportBusy, setExportBusy] = useState<string | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)

  const downloadBlob = async (url: string, fileName: string) => {
    try {
      const { data } = await api.get(url, { responseType: 'blob' })
      const blobUrl = window.URL.createObjectURL(new Blob([data]))
      const link = document.createElement('a')
      link.href = blobUrl
      link.setAttribute('download', fileName)
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(blobUrl)
      setAlertInfo({ variant: 'success', message: `Archivo ${fileName} descargado con éxito.` })
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al generar la descarga del archivo Excel.' })
    }
  }

  const fetchTaxSummary = async () => {
    setLoading(true)
    try {
      const { data } = await api.get<TaxSummary>(`/reports/tax-summary?from=${from}&to=${to}`)
      setTaxSummary(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al consultar el resumen de impuestos SAR.' })
    } finally {
      setLoading(false)
    }
  }

  const fetchIncomeStatement = async () => {
    setLoading(true)
    try {
      const { data } = await api.get<IncomeStatement>(`/reports/financial/income-statement?from=${from}&to=${to}`)
      setIncomeStatement(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al generar el Estado de Resultados.' })
    } finally {
      setLoading(false)
    }
  }

  const fetchBalanceSheet = async () => {
    setLoading(true)
    try {
      const { data } = await api.get<BalanceSheet>(`/reports/financial/balance-sheet?asOfDate=${asOfDate}`)
      setBalanceSheet(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al generar el Balance General.' })
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (activeTab === 'sar') fetchTaxSummary()
    else if (activeTab === 'income') fetchIncomeStatement()
    else if (activeTab === 'balance') fetchBalanceSheet()
  }, [activeTab])

  const exportSarBook = async (kind: 'sales' | 'purchases') => {
    setExportBusy(kind)
    try {
      const path = kind === 'sales' ? '/reports/sar/sales-book' : '/reports/sar/purchases-book'
      const fileName =
        kind === 'sales'
          ? `Libro_Ventas_${from.replaceAll('-', '')}_${to.replaceAll('-', '')}.xlsx`
          : `Libro_Compras_${from.replaceAll('-', '')}_${to.replaceAll('-', '')}.xlsx`
      await downloadBlob(`${path}?from=${from}&to=${to}`, fileName)
    } finally {
      setExportBusy(null)
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <BarChart3 className="text-[#C69C4B]" size={24} />
            Reportes Financieros & Libros Fiscales SAR
          </h1>
          <p className="text-sm text-muted-foreground">
            Declaración mensual de impuestos, Estados Financieros y exportaciones oficiales de Hotel Maya Central.
          </p>
        </div>
      </div>

      {alertInfo && (
        <InlineAlert variant={alertInfo.variant} message={alertInfo.message} onClose={() => setAlertInfo(null)} />
      )}

      {/* Tabs Selector */}
      <div className="flex border-b border-border space-x-2 text-xs font-semibold select-none overflow-x-auto">
        <button
          onClick={() => setActiveTab('sar')}
          className={`pb-2.5 px-3.5 border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'sar'
              ? 'border-[#C69C4B] text-[#C69C4B] font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Receipt size={15} /> Declaración SAR & Libros
        </button>

        <button
          onClick={() => setActiveTab('income')}
          className={`pb-2.5 px-3.5 border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'income'
              ? 'border-[#C69C4B] text-[#C69C4B] font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <TrendingUp size={15} /> Estado de Resultados (P&L)
        </button>

        <button
          onClick={() => setActiveTab('balance')}
          className={`pb-2.5 px-3.5 border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'balance'
              ? 'border-[#C69C4B] text-[#C69C4B] font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Scale size={15} /> Balance General
        </button>

        <button
          onClick={() => setActiveTab('exports')}
          className={`pb-2.5 px-3.5 border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'exports'
              ? 'border-[#C69C4B] text-[#C69C4B] font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <FileSpreadsheet size={15} /> Exportaciones Maestras
        </button>
      </div>

      {/* TAB 1: SAR & TAX DECLARATION */}
      {activeTab === 'sar' && (
        <div className="space-y-6 animate-in fade-in">
          {/* Filter Bar */}
          <div className="p-4 rounded-xl border border-border bg-card shadow-xs flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="text-xs font-semibold text-muted-foreground">Rango del Período:</span>
              <Input
                type="date"
                value={from}
                onChange={(e) => setFrom(e.target.value)}
                className="text-xs w-36 h-8"
              />
              <span className="text-xs text-muted-foreground">al</span>
              <Input
                type="date"
                value={to}
                onChange={(e) => setTo(e.target.value)}
                className="text-xs w-36 h-8"
              />
              <Button size="sm" onClick={fetchTaxSummary} disabled={loading} className="h-8 gap-1 text-xs">
                {loading ? <Loader2 size={13} className="animate-spin" /> : <RefreshCw size={13} />}
                Consultar
              </Button>
            </div>

            <div className="flex items-center gap-2">
              <Button
                size="sm"
                variant="outline"
                onClick={() => exportSarBook('sales')}
                disabled={exportBusy === 'sales'}
                className="h-8 gap-1.5 text-xs text-emerald-600 dark:text-emerald-400 border-emerald-500/30"
              >
                {exportBusy === 'sales' ? <Loader2 size={13} className="animate-spin" /> : <Download size={13} />}
                Libro de Ventas XLSX
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={() => exportSarBook('purchases')}
                disabled={exportBusy === 'purchases'}
                className="h-8 gap-1.5 text-xs text-blue-600 dark:text-blue-400 border-blue-500/30"
              >
                {exportBusy === 'purchases' ? <Loader2 size={13} className="animate-spin" /> : <Download size={13} />}
                Libro de Compras XLSX
              </Button>
            </div>
          </div>

          {/* Tax Summary Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="border border-border rounded-xl p-4 bg-card shadow-xs">
              <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Ventas Gravadas (15%)</p>
              <p className="text-xl font-bold text-foreground mt-1 font-mono">{fmt(taxSummary?.salesTaxed15 || 0)}</p>
              <p className="text-[11px] text-muted-foreground mt-1">Base imponible general</p>
            </div>

            <div className="border border-border rounded-xl p-4 bg-card shadow-xs">
              <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">ISV Débito Fiscal (15%)</p>
              <p className="text-xl font-bold text-emerald-600 dark:text-emerald-400 mt-1 font-mono">
                {fmt(taxSummary?.isvCollected15 || 0)}
              </p>
              <p className="text-[11px] text-muted-foreground mt-1">Impuesto cobrado en ventas</p>
            </div>

            <div className="border border-border rounded-xl p-4 bg-card shadow-xs">
              <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">ISV Crédito Fiscal (Compras)</p>
              <p className="text-xl font-bold text-blue-600 dark:text-blue-400 mt-1 font-mono">
                {fmt(taxSummary?.isvOnPurchases || 0)}
              </p>
              <p className="text-[11px] text-muted-foreground mt-1">Impuesto pagado a proveedores</p>
            </div>

            <div className="border border-border rounded-xl p-4 bg-card shadow-xs">
              <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">ISV Neto a Enterar al SAR</p>
              <p className="text-xl font-bold text-[#C69C4B] mt-1 font-mono">{fmt(taxSummary?.netIsvToPay || 0)}</p>
              <p className="text-[11px] text-muted-foreground mt-1">Débito menos Crédito fiscal</p>
            </div>
          </div>

          <div className="border border-border rounded-xl p-5 bg-card shadow-xs flex items-center justify-between">
            <div>
              <h3 className="font-bold text-sm text-foreground">Tasa Turística del 4% (Ley de Fomento al Turismo)</h3>
              <p className="text-xs text-muted-foreground mt-0.5">
                Retención aplicable exclusivamente a servicios de hospedaje y alojamiento temporal.
              </p>
            </div>
            <div className="text-right">
              <span className="text-xs text-muted-foreground uppercase font-semibold">Total a Enterar:</span>
              <p className="text-2xl font-bold text-[#C69C4B] font-mono">
                {fmt(taxSummary?.touristTaxCollected || 0)}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* TAB 2: INCOME STATEMENT (P&L) */}
      {activeTab === 'income' && (
        <div className="space-y-6 animate-in fade-in">
          <div className="p-4 rounded-xl border border-border bg-card shadow-xs flex items-center gap-3">
            <span className="text-xs font-semibold text-muted-foreground">Período:</span>
            <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="text-xs w-36 h-8" />
            <span className="text-xs text-muted-foreground">al</span>
            <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="text-xs w-36 h-8" />
            <Button size="sm" onClick={fetchIncomeStatement} disabled={loading} className="h-8 gap-1 text-xs">
              {loading ? <Loader2 size={13} className="animate-spin" /> : <RefreshCw size={13} />}
              Generar Estado
            </Button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Revenues */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
              <div className="p-3.5 border-b border-border bg-emerald-500/10 text-emerald-900 dark:text-emerald-300 flex justify-between items-center">
                <h3 className="font-bold text-xs uppercase tracking-wider">Ingresos Operativos</h3>
                <span className="font-mono font-bold text-sm">{fmt(incomeStatement?.totalRevenues || 0)}</span>
              </div>
              <div className="divide-y divide-border/60 text-xs">
                {incomeStatement?.revenues.map((r, idx) => (
                  <div key={idx} className="p-3 flex justify-between items-center hover:bg-accent/20">
                    <div>
                      <span className="font-mono text-muted-foreground mr-2">{r.accountNumber}</span>
                      <span className="font-medium text-foreground">{r.accountName}</span>
                    </div>
                    <span className="font-mono font-semibold text-foreground">{fmt(r.amount)}</span>
                  </div>
                ))}
                {(!incomeStatement?.revenues || incomeStatement.revenues.length === 0) && (
                  <div className="p-6 text-center text-muted-foreground">Sin ingresos registrados en el período.</div>
                )}
              </div>
            </div>

            {/* Expenses */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
              <div className="p-3.5 border-b border-border bg-red-500/10 text-red-900 dark:text-red-300 flex justify-between items-center">
                <h3 className="font-bold text-xs uppercase tracking-wider">Costos & Gastos Operativos</h3>
                <span className="font-mono font-bold text-sm">{fmt(incomeStatement?.totalExpenses || 0)}</span>
              </div>
              <div className="divide-y divide-border/60 text-xs">
                {incomeStatement?.expenses.map((e, idx) => (
                  <div key={idx} className="p-3 flex justify-between items-center hover:bg-accent/20">
                    <div>
                      <span className="font-mono text-muted-foreground mr-2">{e.accountNumber}</span>
                      <span className="font-medium text-foreground">{e.accountName}</span>
                    </div>
                    <span className="font-mono font-semibold text-foreground">{fmt(e.amount)}</span>
                  </div>
                ))}
                {(!incomeStatement?.expenses || incomeStatement.expenses.length === 0) && (
                  <div className="p-6 text-center text-muted-foreground">Sin gastos registrados en el período.</div>
                )}
              </div>
            </div>
          </div>

          {/* Net Income Banner */}
          <div className="border border-border rounded-xl p-5 bg-card shadow-xs flex items-center justify-between">
            <div>
              <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">
                Utilidad Operativa Neta del Ejercicio
              </p>
              <p className="text-xs text-muted-foreground mt-0.5">Ingresos Totales menos Gastos Totales</p>
            </div>
            <div className="text-right">
              <p
                className={`text-2xl font-bold font-mono ${
                  (incomeStatement?.netIncome || 0) >= 0 ? 'text-[#C69C4B]' : 'text-red-600'
                }`}
              >
                {fmt(incomeStatement?.netIncome || 0)}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* TAB 3: BALANCE SHEET */}
      {activeTab === 'balance' && (
        <div className="space-y-6 animate-in fade-in">
          <div className="p-4 rounded-xl border border-border bg-card shadow-xs flex items-center justify-between">
            <div className="flex items-center gap-3">
              <span className="text-xs font-semibold text-muted-foreground">A la fecha:</span>
              <Input
                type="date"
                value={asOfDate}
                onChange={(e) => setAsOfDate(e.target.value)}
                className="text-xs w-36 h-8"
              />
              <Button size="sm" onClick={fetchBalanceSheet} disabled={loading} className="h-8 gap-1 text-xs">
                {loading ? <Loader2 size={13} className="animate-spin" /> : <RefreshCw size={13} />}
                Calcular Balance
              </Button>
            </div>

            <div className="flex items-center gap-2">
              <span
                className={`text-xs font-semibold px-2.5 py-1 rounded-full border flex items-center gap-1.5 ${
                  balanceSheet?.isBalanced
                    ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-800 dark:text-emerald-300'
                    : 'border-red-500/30 bg-red-500/10 text-red-800 dark:text-red-300'
                }`}
              >
                {balanceSheet?.isBalanced ? (
                  <>
                    <CheckCircle2 size={14} /> Ecuación Contable Cuadrada (Activo = Pasivo + Pat.)
                  </>
                ) : (
                  'Ecuación Descuadrada'
                )}
              </span>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Activos */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
              <div className="p-3.5 border-b border-border bg-muted/20 flex justify-between items-center">
                <h3 className="font-bold text-xs uppercase tracking-wider text-foreground">Activos</h3>
                <span className="font-mono font-bold text-xs text-[#C69C4B]">{fmt(balanceSheet?.totalAssets || 0)}</span>
              </div>
              <div className="divide-y divide-border/60 text-xs flex-1">
                {balanceSheet?.assets.map((a, idx) => (
                  <div key={idx} className="p-2.5 flex justify-between items-center hover:bg-accent/20">
                    <div>
                      <span className="font-mono text-muted-foreground mr-1.5">{a.accountNumber}</span>
                      <span className="font-medium text-foreground">{a.accountName}</span>
                    </div>
                    <span className="font-mono font-semibold">{fmt(a.amount)}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Pasivos */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
              <div className="p-3.5 border-b border-border bg-muted/20 flex justify-between items-center">
                <h3 className="font-bold text-xs uppercase tracking-wider text-foreground">Pasivos</h3>
                <span className="font-mono font-bold text-xs text-red-600 dark:text-red-400">
                  {fmt(balanceSheet?.totalLiabilities || 0)}
                </span>
              </div>
              <div className="divide-y divide-border/60 text-xs flex-1">
                {balanceSheet?.liabilities.map((l, idx) => (
                  <div key={idx} className="p-2.5 flex justify-between items-center hover:bg-accent/20">
                    <div>
                      <span className="font-mono text-muted-foreground mr-1.5">{l.accountNumber}</span>
                      <span className="font-medium text-foreground">{l.accountName}</span>
                    </div>
                    <span className="font-mono font-semibold">{fmt(l.amount)}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Patrimonio */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
              <div className="p-3.5 border-b border-border bg-muted/20 flex justify-between items-center">
                <h3 className="font-bold text-xs uppercase tracking-wider text-foreground">Patrimonio</h3>
                <span className="font-mono font-bold text-xs text-blue-600 dark:text-blue-400">
                  {fmt(balanceSheet?.totalEquity || 0)}
                </span>
              </div>
              <div className="divide-y divide-border/60 text-xs flex-1">
                {balanceSheet?.equity.map((eq, idx) => (
                  <div key={idx} className="p-2.5 flex justify-between items-center hover:bg-accent/20">
                    <div>
                      <span className="font-mono text-muted-foreground mr-1.5">{eq.accountNumber}</span>
                      <span className="font-medium text-foreground">{eq.accountName}</span>
                    </div>
                    <span className="font-mono font-semibold">{fmt(eq.amount)}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* TAB 4: MASTER EXPORTS */}
      {activeTab === 'exports' && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 animate-in fade-in">
          {quickExports.map((qe) => (
            <div
              key={qe.label}
              className="border border-border rounded-xl p-5 bg-card shadow-xs flex items-center justify-between gap-4"
            >
              <div>
                <h3 className="font-bold text-sm text-foreground">{qe.label}</h3>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Exportación estructurada de datos en formato Microsoft Excel (.xlsx)
                </p>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => downloadBlob(qe.path, qe.fileName)}
                className="gap-1.5 text-xs shrink-0"
              >
                <Download size={14} /> Exportar XLSX
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
