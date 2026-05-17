import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import type { CashRegister, CashMovement } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export default function CashRegistersPage() {
  const [registers, setRegisters] = useState<CashRegister[]>([])
  const [movements, setMovements] = useState<CashMovement[]>([])
  const [selectedRegister, setSelectedRegister] = useState<string | null>(null)
  const [showOpen, setShowOpen] = useState(false)
  const [openAmount, setOpenAmount] = useState('0')
  const [showClose, setShowClose] = useState(false)
  const [countedAmount, setCountedAmount] = useState('0')

  useEffect(() => { load() }, [])

  const load = async () => {
    const { data } = await api.get<CashRegister[]>('/cash-registers')
    setRegisters(data)
  }

  const loadMovements = async (id: string) => {
    setSelectedRegister(id)
    const { data } = await api.get<CashMovement[]>(`/cash-registers/${id}/movements`)
    setMovements(data)
  }

  const openRegister = async () => {
    if (!selectedRegister) return
    await api.post(`/cash-registers/${selectedRegister}/open`, { cashRegisterId: selectedRegister, initialAmount: +openAmount })
    setShowOpen(false)
    loadMovements(selectedRegister)
  }

  const closeRegister = async () => {
    if (!selectedRegister) return
    const lastBal = movements.length > 0 ? movements[0].balanceAfter : 0
    await api.post(`/cash-registers/${selectedRegister}/close`, { cashRegisterId: selectedRegister, expectedAmount: lastBal, countedAmount: +countedAmount, notes: '' })
    setShowClose(false)
    loadMovements(selectedRegister)
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Caja</h1>

      <div className="flex gap-2">
        {registers.map(r => (
          <Button key={r.id} variant={selectedRegister === r.id ? 'default' : 'outline'} onClick={() => loadMovements(r.id)}>{r.name}</Button>
        ))}
      </div>

      {selectedRegister && (
        <>
          <div className="flex gap-2">
            <Button onClick={() => setShowOpen(true)}>Abrir Caja</Button>
            <Button variant="destructive" onClick={() => setShowClose(true)}>Cerrar Caja</Button>
          </div>

          {showOpen && (
            <div className="border border-border rounded-lg p-4 bg-card space-y-2 max-w-xs">
              <label className="text-sm">Monto Apertura</label>
              <Input type="number" value={openAmount} onChange={e => setOpenAmount(e.target.value)} />
              <div className="flex gap-2"><Button size="sm" onClick={openRegister}>Confirmar</Button><Button size="sm" variant="outline" onClick={() => setShowOpen(false)}>Cancelar</Button></div>
            </div>
          )}

          {showClose && (
            <div className="border border-border rounded-lg p-4 bg-card space-y-2 max-w-xs">
              <label className="text-sm">Monto Contado</label>
              <Input type="number" value={countedAmount} onChange={e => setCountedAmount(e.target.value)} />
              <div className="flex gap-2"><Button size="sm" onClick={closeRegister}>Confirmar Cierre</Button><Button size="sm" variant="outline" onClick={() => setShowClose(false)}>Cancelar</Button></div>
            </div>
          )}

          <div className="border border-border rounded-lg overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted"><tr>
                <th className="text-left p-3">Fecha</th><th className="text-left p-3">Tipo</th>
                <th className="text-right p-3">Monto</th><th className="text-right p-3">Saldo</th><th className="text-left p-3">Descripción</th>
              </tr></thead>
              <tbody>
                {movements.map(m => (
                  <tr key={m.id} className="border-t border-border">
                    <td className="p-3">{new Date(m.movementDate).toLocaleString()}</td>
                    <td className="p-3">{m.movementType}</td>
                    <td className={`p-3 text-right font-semibold ${m.movementType === 'Ingreso' || m.movementType === 'Apertura' ? 'text-green-600' : 'text-red-600'}`}>
                      {m.movementType === 'Ingreso' || m.movementType === 'Apertura' ? '+' : '-'}L {Math.abs(m.amount).toFixed(2)}
                    </td>
                    <td className="p-3 text-right">L {m.balanceAfter.toFixed(2)}</td>
                    <td className="p-3">{m.description}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
