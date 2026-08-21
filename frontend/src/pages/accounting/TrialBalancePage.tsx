import { useCallback, useEffect, useState } from 'react'
import api from '@/lib/axios'
import type { TrialBalanceItem } from '@/types'
import { Button } from '@/components/ui/button'
import { Loader2 } from 'lucide-react'

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
    load()
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
        <div className="flex items-center justify-center py-8"><Loader2 size={24} className="animate-spin text-muted-foreground" /></div>
      ) : (
        <>
          <div className="border border-border rounded-lg overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted">
                <tr>
                  <th className="text-left p-3">Cuenta</th>
                  <th className="text-left p-3">Nombre</th>
                  <th className="text-left p-3">Tipo</th>
                  <th className="text-right p-3">Débito</th>
                  <th className="text-right p-3">Crédito</th>
                  <th className="text-right p-3">Saldo</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.accountId} className="border-t border-border">
                    <td className="p-3 font-mono text-xs">{item.accountNumber}</td>
                    <td className="p-3">{item.accountName}</td>
                    <td className="p-3">{item.accountType}</td>
                    <td className="p-3 text-right font-mono">{formatCurrency(item.debit)}</td>
                    <td className="p-3 text-right font-mono">{formatCurrency(item.credit)}</td>
                    <td className="p-3 text-right font-mono">{formatCurrency(item.balance)}</td>
                  </tr>
                ))}
                {items.length === 0 && (
                  <tr><td colSpan={6} className="p-6 text-center text-muted-foreground">No hay movimientos para mostrar</td></tr>
                )}
              </tbody>
              <tfoot className="bg-muted font-medium">
                <tr>
                  <td className="p-3" colSpan={3}>Totales</td>
                  <td className="p-3 text-right font-mono">{formatCurrency(totals.debit)}</td>
                  <td className="p-3 text-right font-mono">{formatCurrency(totals.credit)}</td>
                  <td className="p-3 text-right font-mono">{formatCurrency(totals.balance)}</td>
                </tr>
              </tfoot>
            </table>
          </div>
          <div className={`text-sm font-medium ${totals.debit === totals.credit ? 'text-green-600' : 'text-red-600'}`}>
            {totals.debit === totals.credit ? '✓ Balance cuadrado' : '✗ El balance no cuadra'}
          </div>
        </>
      )}
    </div>
  )
}
