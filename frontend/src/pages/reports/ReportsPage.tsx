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
  const [activeTab, setActiveTab] = useState<'sar' | 'dmr1' | 'income' | 'balance' | 'exports'>('sar')
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
    if (activeTab === 'sar' || activeTab === 'dmr1') fetchTaxSummary()
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
          onClick={() => setActiveTab('dmr1')}
          className={`pb-2.5 px-3.5 border-b-2 transition-colors cursor-pointer flex items-center gap-2 ${
            activeTab === 'dmr1'
              ? 'border-[#C69C4B] text-[#C69C4B] font-bold'
              : 'border-transparent text-muted-foreground hover:text-foreground'
          }`}
        >
          <Building size={15} /> Declaración DMR-1
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

      {/* TAB 1.5: DMR-1 DECLARATION */}
      {activeTab === 'dmr1' && (
        <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
          <div className="p-4 rounded-xl border border-border bg-card shadow-sm flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">Período Fiscal DMR-1:</span>
              <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="text-sm w-40 font-medium" />
              <span className="text-sm text-muted-foreground">al</span>
              <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="text-sm w-40 font-medium" />
              <Button onClick={fetchTaxSummary} disabled={loading} className="gap-2 bg-[#C69C4B] hover:bg-[#b0883b] text-white">
                {loading ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
                Calcular DMR-1
              </Button>
            </div>
            <Button
              variant="outline"
              className="gap-2 border-dashed border-[#C69C4B] text-[#C69C4B]"
            >
              <Download size={16} /> Descargar Formato Guía PDF
            </Button>
          </div>

          <div className="bg-card rounded-xl border border-border shadow-xs overflow-hidden">
            <div className="bg-muted/40 p-5 border-b border-border">
              <h3 className="text-lg font-bold text-foreground">Borrador de Declaración Mensual (DMR-1)</h3>
              <p className="text-sm text-muted-foreground mt-1">Valores precalculados para digitación en la plataforma en línea del SAR (Sistema de Administración de Rentas).</p>
            </div>
            
            <div className="p-6">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
                <div className="bg-blue-50/50 dark:bg-blue-950/20 rounded-xl p-5 border border-blue-100 dark:border-blue-900/50">
                  <span className="text-xs font-bold text-blue-700 dark:text-blue-400 uppercase tracking-widest block mb-2">Crédito Fiscal (Casilla 30)</span>
                  <div className="text-3xl font-mono font-bold text-blue-900 dark:text-blue-100">{fmt(taxSummary?.isvOnPurchases || 0)}</div>
                  <span className="text-xs text-blue-600/70 dark:text-blue-400/70 mt-1 block">Impuesto pagado en compras</span>
                </div>
                
                <div className="bg-emerald-50/50 dark:bg-emerald-950/20 rounded-xl p-5 border border-emerald-100 dark:border-emerald-900/50">
                  <span className="text-xs font-bold text-emerald-700 dark:text-emerald-400 uppercase tracking-widest block mb-2">Débito Fiscal (Casilla 40)</span>
                  <div className="text-3xl font-mono font-bold text-emerald-900 dark:text-emerald-100">{fmt(taxSummary?.isvCollected15 || 0)}</div>
                  <span className="text-xs text-emerald-600/70 dark:text-emerald-400/70 mt-1 block">Impuesto cobrado en ventas</span>
                </div>

                <div className="bg-[#C69C4B]/5 rounded-xl p-5 border border-[#C69C4B]/30 relative overflow-hidden">
                  <div className="absolute top-0 right-0 p-3 opacity-10"><Building size={64} /></div>
                  <span className="text-xs font-bold text-[#C69C4B] uppercase tracking-widest block mb-2">Impuesto a Pagar (Casilla 55)</span>
                  <div className="text-4xl font-mono font-extrabold text-[#C69C4B] tracking-tight">{fmt(taxSummary?.netIsvToPay || 0)}</div>
                  <span className="text-xs text-[#C69C4B]/70 mt-1 block">Valor neto a enterar al Estado</span>
                </div>
              </div>

              <div className="space-y-4 text-sm">
                <h4 className="font-semibold text-muted-foreground uppercase tracking-wider text-xs border-b border-border pb-2">Otras Retenciones</h4>
                <div className="flex justify-between items-center py-2 px-4 bg-muted/20 rounded-md">
                  <span className="font-medium">Retención 4% Tasa Turística (Casilla 70)</span>
                  <span className="font-mono font-bold">{fmt(taxSummary?.touristTaxCollected || 0)}</span>
                </div>
                <div className="flex justify-between items-center py-2 px-4 bg-muted/20 rounded-md">
                  <span className="font-medium">Retención ISR 12.5% (Servicios Profesionales)</span>
                  <span className="font-mono font-bold text-muted-foreground">L 0.00</span>
                </div>
                <div className="flex justify-between items-center py-2 px-4 bg-muted/20 rounded-md">
                  <span className="font-medium">Retención 1% (Anticipo ISR)</span>
                  <span className="font-mono font-bold text-muted-foreground">L 0.00</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* TAB 2: INCOME STATEMENT (P&L) */}
      {activeTab === 'income' && (
        <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
          <div className="p-4 rounded-xl border border-border bg-card shadow-sm flex flex-wrap items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">Período Fiscal:</span>
              <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="text-sm w-40 font-medium" />
              <span className="text-sm text-muted-foreground">al</span>
              <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="text-sm w-40 font-medium" />
              <Button onClick={fetchIncomeStatement} disabled={loading} className="gap-2 bg-[#C69C4B] hover:bg-[#b0883b] text-white">
                {loading ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
                Generar Estado
              </Button>
            </div>
          </div>

          {/* Executive Summary P&L */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="border border-emerald-200 dark:border-emerald-900/50 rounded-xl bg-emerald-50 dark:bg-emerald-950/20 p-6 flex flex-col justify-center">
              <span className="text-xs font-bold text-emerald-700 dark:text-emerald-400 uppercase tracking-widest mb-1">Ingresos Operativos Totales</span>
              <span className="text-3xl font-mono font-bold text-emerald-900 dark:text-emerald-100">{fmt(incomeStatement?.totalRevenues || 0)}</span>
            </div>
            <div className="border border-red-200 dark:border-red-900/50 rounded-xl bg-red-50 dark:bg-red-950/20 p-6 flex flex-col justify-center">
              <span className="text-xs font-bold text-red-700 dark:text-red-400 uppercase tracking-widest mb-1">Costos y Gastos Totales</span>
              <span className="text-3xl font-mono font-bold text-red-900 dark:text-red-100">{fmt(incomeStatement?.totalExpenses || 0)}</span>
            </div>
            <div className={`border rounded-xl p-6 flex flex-col justify-center ${
                  (incomeStatement?.netIncome || 0) >= 0 ? 'bg-[#C69C4B]/10 border-[#C69C4B]/30' : 'bg-red-50 dark:bg-red-950/20 border-red-200 dark:border-red-900/50'
                }`}>
              <span className="text-xs font-bold text-foreground uppercase tracking-widest mb-1">Utilidad Operativa Neta</span>
              <span className={`text-4xl font-mono font-extrabold tracking-tight ${
                  (incomeStatement?.netIncome || 0) >= 0 ? 'text-[#C69C4B]' : 'text-red-600 dark:text-red-400'
                }`}>
                {fmt(incomeStatement?.netIncome || 0)}
              </span>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            {/* Revenues */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-sm">
              <div className="p-4 border-b border-border bg-muted/40">
                <h3 className="font-bold text-sm uppercase tracking-wider text-foreground flex items-center gap-2">
                  <TrendingUp size={16} className="text-emerald-600 dark:text-emerald-400" />
                  Desglose de Ingresos
                </h3>
              </div>
              <div className="divide-y divide-border/60 text-sm">
                {incomeStatement?.revenues.map((r, idx) => (
                  <div key={idx} className="p-4 flex justify-between items-center hover:bg-accent/40 transition-colors">
                    <div>
                      <span className="font-mono text-muted-foreground mr-3">{r.accountNumber}</span>
                      <span className="font-medium text-foreground">{r.accountName}</span>
                    </div>
                    <span className="font-mono font-bold text-foreground">{fmt(r.amount)}</span>
                  </div>
                ))}
                {(!incomeStatement?.revenues || incomeStatement.revenues.length === 0) && (
                  <div className="p-8 text-center text-muted-foreground">Sin ingresos registrados en el período.</div>
                )}
              </div>
            </div>

            {/* Expenses */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-sm">
              <div className="p-4 border-b border-border bg-muted/40">
                <h3 className="font-bold text-sm uppercase tracking-wider text-foreground flex items-center gap-2">
                  <TrendingUp size={16} className="text-red-600 dark:text-red-400 rotate-180" />
                  Desglose de Gastos
                </h3>
              </div>
              <div className="divide-y divide-border/60 text-sm">
                {incomeStatement?.expenses.map((e, idx) => (
                  <div key={idx} className="p-4 flex justify-between items-center hover:bg-accent/40 transition-colors">
                    <div>
                      <span className="font-mono text-muted-foreground mr-3">{e.accountNumber}</span>
                      <span className="font-medium text-foreground">{e.accountName}</span>
                    </div>
                    <span className="font-mono font-bold text-foreground">{fmt(e.amount)}</span>
                  </div>
                ))}
                {(!incomeStatement?.expenses || incomeStatement.expenses.length === 0) && (
                  <div className="p-8 text-center text-muted-foreground">Sin gastos registrados en el período.</div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* TAB 3: BALANCE SHEET */}
      {activeTab === 'balance' && (
        <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
          <div className="p-4 rounded-xl border border-border bg-card shadow-sm flex items-center justify-between">
            <div className="flex items-center gap-3">
              <span className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">Posición Financiera Al:</span>
              <Input
                type="date"
                value={asOfDate}
                onChange={(e) => setAsOfDate(e.target.value)}
                className="text-sm w-40 font-medium"
              />
              <Button onClick={fetchBalanceSheet} disabled={loading} className="gap-2 bg-[#C69C4B] hover:bg-[#b0883b] text-white">
                {loading ? <Loader2 size={16} className="animate-spin" /> : <RefreshCw size={16} />}
                Calcular Balance
              </Button>
            </div>

            <div className="flex items-center gap-2">
              <span
                className={`text-sm font-bold px-4 py-2 rounded-lg border flex items-center gap-2 ${
                  balanceSheet?.isBalanced
                    ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-800 dark:text-emerald-300'
                    : 'border-red-500/30 bg-red-500/10 text-red-800 dark:text-red-300'
                }`}
              >
                {balanceSheet?.isBalanced ? (
                  <>
                    <CheckCircle2 size={18} /> Balance Cuadrado
                  </>
                ) : (
                  'Balance Descuadrado'
                )}
              </span>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Activos */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-sm flex flex-col">
              <div className="p-6 border-b border-border bg-muted/40 flex flex-col gap-1">
                <h3 className="font-bold text-sm uppercase tracking-wider text-muted-foreground">Total Activos</h3>
                <span className="font-mono font-extrabold text-3xl text-foreground">{fmt(balanceSheet?.totalAssets || 0)}</span>
              </div>
              <div className="divide-y divide-border/60 text-sm flex-1">
                {balanceSheet?.assets.map((a, idx) => (
                  <div key={idx} className="p-4 flex justify-between items-center hover:bg-accent/40 transition-colors">
                    <div>
                      <span className="font-mono text-muted-foreground mr-3">{a.accountNumber}</span>
                      <span className="font-medium text-foreground">{a.accountName}</span>
                    </div>
                    <span className="font-mono font-bold">{fmt(a.amount)}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Pasivos */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-sm flex flex-col">
              <div className="p-6 border-b border-border bg-muted/40 flex flex-col gap-1">
                <h3 className="font-bold text-sm uppercase tracking-wider text-muted-foreground">Total Pasivos</h3>
                <span className="font-mono font-extrabold text-3xl text-foreground">
                  {fmt(balanceSheet?.totalLiabilities || 0)}
                </span>
              </div>
              <div className="divide-y divide-border/60 text-sm flex-1">
                {balanceSheet?.liabilities.map((l, idx) => (
                  <div key={idx} className="p-4 flex justify-between items-center hover:bg-accent/40 transition-colors">
                    <div>
                      <span className="font-mono text-muted-foreground mr-3">{l.accountNumber}</span>
                      <span className="font-medium text-foreground">{l.accountName}</span>
                    </div>
                    <span className="font-mono font-bold">{fmt(l.amount)}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Patrimonio */}
            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-sm flex flex-col">
              <div className="p-6 border-b border-border bg-muted/40 flex flex-col gap-1">
                <h3 className="font-bold text-sm uppercase tracking-wider text-muted-foreground">Total Patrimonio</h3>
                <span className="font-mono font-extrabold text-3xl text-foreground">
                  {fmt(balanceSheet?.totalEquity || 0)}
                </span>
              </div>
              <div className="divide-y divide-border/60 text-sm flex-1">
                {balanceSheet?.equity.map((eq, idx) => (
                  <div key={idx} className="p-4 flex justify-between items-center hover:bg-accent/40 transition-colors">
                    <div>
                      <span className="font-mono text-muted-foreground mr-3">{eq.accountNumber}</span>
                      <span className="font-medium text-foreground">{eq.accountName}</span>
                    </div>
                    <span className="font-mono font-bold">{fmt(eq.amount)}</span>
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
