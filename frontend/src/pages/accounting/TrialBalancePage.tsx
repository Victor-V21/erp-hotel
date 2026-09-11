import { useCallback, useEffect, useState } from 'react'
import api from '@/lib/axios'
import type { TrialBalanceItem } from '@/types'
import { Button } from '@/components/ui/button'
import { Loader2, ArrowUpRight, ArrowDownRight, Scale, CheckCircle2, AlertCircle } from 'lucide-react'

function formatCurrency(n: number) {
  return 'L ' + n.toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function TrialBalancePage() {
  const [items, setItems] = useState<TrialBalanceItem[]>([])
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await api.get<TrialBalanceItem[]>('/accounts/trial-balance')
      setItems(data)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0)
    return () => window.clearTimeout(timer)
  }, [load])

  const totals = items.reduce(
    (acc, item) => ({
      debit: acc.debit + item.debit,
      credit: acc.credit + item.credit,
      balance: acc.balance + item.balance,
    }),
    { debit: 0, credit: 0, balance: 0 }
  )

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Balance de Comprobación</h1>
          <p className="text-sm text-muted-foreground">Saldos y movimientos acumulados por cuenta.</p>
        </div>
        <Button variant="outline" onClick={load} disabled={loading}>
          {loading ? <Loader2 size={16} className="animate-spin mr-1" /> : null}
          Actualizar
        </Button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-16"><Loader2 size={32} className="animate-spin text-muted-foreground" /></div>
      ) : (
        <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
          {/* Executive Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="border border-border rounded-xl bg-card p-5 shadow-sm flex flex-col justify-between">
              <div className="flex items-center gap-2 text-muted-foreground mb-3">
                <ArrowUpRight size={18} className="text-blue-500" />
                <span className="text-sm font-semibold uppercase tracking-wider">Total Débitos</span>
              </div>
              <div className="text-3xl font-bold font-mono tracking-tight text-foreground">
                {formatCurrency(totals.debit)}
              </div>
            </div>
            
            <div className="border border-border rounded-xl bg-card p-5 shadow-sm flex flex-col justify-between">
              <div className="flex items-center gap-2 text-muted-foreground mb-3">
                <ArrowDownRight size={18} className="text-amber-500" />
                <span className="text-sm font-semibold uppercase tracking-wider">Total Créditos</span>
              </div>
              <div className="text-3xl font-bold font-mono tracking-tight text-foreground">
                {formatCurrency(totals.credit)}
              </div>
            </div>

            <div className={`border rounded-xl p-5 shadow-sm flex flex-col justify-between transition-colors ${totals.debit === totals.credit ? 'bg-emerald-50 border-emerald-200 dark:bg-emerald-950/20 dark:border-emerald-900/50' : 'bg-red-50 border-red-200 dark:bg-red-950/20 dark:border-red-900/50'}`}>
              <div className={`flex items-center gap-2 mb-3 ${totals.debit === totals.credit ? 'text-emerald-700 dark:text-emerald-400' : 'text-red-700 dark:text-red-400'}`}>
                <Scale size={18} />
                <span className="text-sm font-semibold uppercase tracking-wider">Estado del Balance</span>
              </div>
              <div className="flex items-center gap-2">
                {totals.debit === totals.credit ? (
                  <>
                    <CheckCircle2 size={28} className="text-emerald-600 dark:text-emerald-400" />
                    <span className="text-2xl font-bold text-emerald-700 dark:text-emerald-400 tracking-tight">Cuadrado</span>
                  </>
                ) : (
                  <>
                    <AlertCircle size={28} className="text-red-600 dark:text-red-400" />
                    <div className="flex flex-col">
                      <span className="text-xl font-bold text-red-700 dark:text-red-400 leading-tight">Descuadrado</span>
                      <span className="text-xs font-mono font-medium text-red-600/80 dark:text-red-400/80">Diferencia: {formatCurrency(Math.abs(totals.debit - totals.credit))}</span>
                    </div>
                  </>
                )}
              </div>
            </div>
          </div>

          <div className="border border-border rounded-xl overflow-hidden shadow-sm bg-card">
            <div className="overflow-x-auto">
              <table className="w-full text-sm whitespace-nowrap">
                <thead className="bg-muted/50 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  <tr>
                    <th className="text-left p-4">Cuenta</th>
                    <th className="text-left p-4">Nombre</th>
                    <th className="text-left p-4">Tipo</th>
                    <th className="text-right p-4">Débito</th>
                    <th className="text-right p-4">Crédito</th>
                    <th className="text-right p-4">Saldo</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border/60">
                  {items.map((item) => (
                    <tr key={item.accountId} className="hover:bg-accent/40 transition-colors">
                      <td className="p-4 font-mono text-xs text-muted-foreground">{item.accountNumber}</td>
                      <td className="p-4 font-medium text-foreground">{item.accountName}</td>
                      <td className="p-4">
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-secondary text-secondary-foreground border border-border/50">
                          {item.accountType}
                        </span>
                      </td>
                      <td className="p-4 text-right font-mono tabular-nums text-foreground">{formatCurrency(item.debit)}</td>
                      <td className="p-4 text-right font-mono tabular-nums text-foreground">{formatCurrency(item.credit)}</td>
                      <td className="p-4 text-right font-mono tabular-nums font-medium text-foreground">{formatCurrency(item.balance)}</td>
                    </tr>
                  ))}
                  {items.length === 0 && (
                    <tr><td colSpan={6} className="p-8 text-center text-muted-foreground">No hay movimientos contables registrados.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
