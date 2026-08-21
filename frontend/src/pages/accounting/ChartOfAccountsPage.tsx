import { useState, useEffect, useCallback } from 'react'
import api from '@/lib/axios'
import type { AccountingAccount, CreateAccountRequest } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { ChevronDown, ChevronRight, Plus, Edit2, Trash2, Loader2, BookOpen } from 'lucide-react'

const accountTypeColors: Record<string, string> = {
  Activo: 'text-blue-600 dark:text-blue-400',
  Pasivo: 'text-red-600 dark:text-red-400',
  Patrimonio: 'text-emerald-600 dark:text-emerald-400',
  Ingreso: 'text-teal-600 dark:text-teal-400',
  Gasto: 'text-amber-600 dark:text-amber-400',
}

const accountTypeBadge: Record<string, string> = {
  Activo: 'bg-blue-100 dark:bg-blue-950/40 text-blue-800 dark:text-blue-300 border border-blue-300 dark:border-blue-800',
  Pasivo: 'bg-red-100 dark:bg-red-950/40 text-red-800 dark:text-red-300 border border-red-300 dark:border-red-800',
  Patrimonio: 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800',
  Ingreso: 'bg-teal-100 dark:bg-teal-950/40 text-teal-800 dark:text-teal-300 border border-teal-300 dark:border-teal-800',
  Gasto: 'bg-amber-100 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-800',
}

function formatCurrency(n: number) {
  return 'L ' + n.toLocaleString('es-HN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function AccountRow({
  account,
  depth,
  onEdit,
  onDelete,
}: {
  account: AccountingAccount
  depth: number
  onEdit: (account: AccountingAccount) => void
  onDelete: (account: AccountingAccount) => void
}) {
  const [expanded, setExpanded] = useState(depth < 2)
  const hasChildren = account.children && account.children.length > 0

  return (
    <>
      <tr className="border-t border-border hover:bg-accent/30 transition-colors">
        <td className="p-3" style={{ paddingLeft: `${14 + depth * 24}px` }}>
          <div className="flex items-center gap-1.5">
            {hasChildren ? (
              <button
                onClick={() => setExpanded(!expanded)}
                className="p-1 rounded hover:bg-accent cursor-pointer text-muted-foreground hover:text-foreground"
              >
                {expanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
              </button>
            ) : (
              <span className="w-6" />
            )}
            <span className="font-mono text-xs font-semibold text-foreground">
              {account.accountNumber}
            </span>
          </div>
        </td>
        <td className="p-3 text-sm text-foreground font-medium">{account.accountName}</td>
        <td className="p-3">
          <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${accountTypeBadge[account.accountType] || ''}`}>
            {account.accountType}
          </span>
        </td>
        <td className={`p-3 text-sm text-right font-mono font-semibold ${accountTypeColors[account.accountType] || ''}`}>
          {formatCurrency(account.balance)}
        </td>
        <td className="p-3 text-right space-x-1.5">
          <Button size="sm" variant="outline" onClick={() => onEdit(account)} className="h-7 w-7 p-0" title="Editar">
            <Edit2 size={12} />
          </Button>
          <Button size="sm" variant="destructive" onClick={() => onDelete(account)} className="h-7 w-7 p-0" title="Eliminar">
            <Trash2 size={12} />
          </Button>
        </td>
      </tr>
      {expanded &&
        hasChildren &&
        account.children.map((child) => (
          <AccountRow
            key={child.id}
            account={child}
            depth={depth + 1}
            onEdit={onEdit}
            onDelete={onDelete}
          />
        ))}
    </>
  )
}

export default function ChartOfAccountsPage() {
  const [accounts, setAccounts] = useState<AccountingAccount[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<CreateAccountRequest>({ accountNumber: '', accountName: '', accountType: 'Activo' })
  const [editingAccount, setEditingAccount] = useState<AccountingAccount | null>(null)
  const [editName, setEditName] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<AccountingAccount | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success'; message: string } | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await api.get<AccountingAccount[]>('/accounts')
      setAccounts(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al cargar el catálogo de cuentas contables.' })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const save = async () => {
    if (!form.accountNumber.trim() || !form.accountName.trim()) {
      setAlertInfo({ variant: 'error', message: 'Número y nombre de cuenta son obligatorios.' })
      return
    }
    try {
      await api.post('/accounts', form)
      setShowForm(false)
      setForm({ accountNumber: '', accountName: '', accountType: 'Activo' })
      setAlertInfo({ variant: 'success', message: `Cuenta ${form.accountNumber} creada con éxito.` })
      load()
    } catch (e: any) {
      setAlertInfo({ variant: 'error', message: e.response?.data?.message || 'Error al crear la cuenta contable.' })
    }
  }

  const handleEditClick = (account: AccountingAccount) => {
    setEditingAccount(account)
    setEditName(account.accountName)
  }

  const saveEdit = async () => {
    if (!editingAccount || !editName.trim()) return
    try {
      await api.put(`/accounts/${editingAccount.id}`, { accountName: editName })
      setAlertInfo({ variant: 'success', message: 'Nombre de la cuenta actualizado.' })
      setEditingAccount(null)
      load()
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al actualizar la cuenta contable.' })
    }
  }

  const handleDeleteConfirm = async () => {
    if (!deleteTarget) return
    try {
      await api.delete(`/accounts/${deleteTarget.id}`)
      setAlertInfo({ variant: 'success', message: `Cuenta ${deleteTarget.accountNumber} eliminada.` })
      setDeleteTarget(null)
      load()
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al eliminar la cuenta (verifique si posee subcuentas o asientos).' })
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <BookOpen className="text-[#C69C4B]" size={24} />
            Catálogo de Cuentas Contables
          </h1>
          <p className="text-sm text-muted-foreground">
            Estructura jerárquica del plan contable y saldos acumulados.
          </p>
        </div>
        <Button onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancelar' : <><Plus size={16} className="mr-1.5" /> Nueva Cuenta</>}
        </Button>
      </div>

      {alertInfo && (
        <InlineAlert
          variant={alertInfo.variant}
          message={alertInfo.message}
          onClose={() => setAlertInfo(null)}
        />
      )}

      {showForm && (
        <div className="border border-border rounded-xl p-5 bg-card space-y-4 shadow-sm animate-in fade-in">
          <h3 className="font-semibold text-foreground text-md">Nueva Cuenta Contable</h3>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3.5">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Código / Número *
              </label>
              <Input
                value={form.accountNumber}
                onChange={(e) => setForm({ ...form, accountNumber: e.target.value })}
                placeholder="Ej. 1101"
                className="mt-1"
              />
            </div>
            <div className="md:col-span-2">
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Nombre de la Cuenta *
              </label>
              <Input
                value={form.accountName}
                onChange={(e) => setForm({ ...form, accountName: e.target.value })}
                placeholder="Ej. Caja General"
                className="mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Clasificación / Tipo
              </label>
              <select
                value={form.accountType}
                onChange={(e) => setForm({ ...form, accountType: e.target.value })}
                className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1"
              >
                <option value="Activo">Activo</option>
                <option value="Pasivo">Pasivo</option>
                <option value="Patrimonio">Patrimonio</option>
                <option value="Ingreso">Ingreso</option>
                <option value="Gasto">Gasto</option>
              </select>
            </div>
          </div>
          <div className="flex gap-2.5 pt-2">
            <Button onClick={save}>Guardar Cuenta</Button>
            <Button variant="outline" onClick={() => setShowForm(false)}>
              Cancelar
            </Button>
          </div>
        </div>
      )}

      {/* Inline Edit Dialog */}
      {editingAccount && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => setEditingAccount(null)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-md shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-lg font-bold text-foreground">
              Editar Cuenta {editingAccount.accountNumber}
            </h3>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Nuevo Nombre
              </label>
              <Input
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
                className="mt-1"
                autoFocus
              />
            </div>
            <div className="flex justify-end gap-2.5 pt-2">
              <Button variant="outline" onClick={() => setEditingAccount(null)}>
                Cancelar
              </Button>
              <Button onClick={saveEdit}>Actualizar</Button>
            </div>
          </div>
        </div>
      )}

      <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
        {loading ? (
          <div className="flex items-center justify-center py-16 space-y-2">
            <Loader2 size={28} className="animate-spin text-[#C69C4B] mr-2" />
            <span className="text-sm text-muted-foreground">Cargando árbol de cuentas...</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/40 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3.5 w-48">Código</th>
                  <th className="text-left p-3.5">Nombre de Cuenta</th>
                  <th className="text-left p-3.5 w-32">Tipo</th>
                  <th className="text-right p-3.5 w-44">Saldo Actual</th>
                  <th className="text-right p-3.5 w-32">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {accounts.map((a) => (
                  <AccountRow
                    key={a.id}
                    account={a}
                    depth={0}
                    onEdit={handleEditClick}
                    onDelete={(acc) => setDeleteTarget(acc)}
                  />
                ))}
                {accounts.length === 0 && (
                  <tr>
                    <td colSpan={5} className="p-8 text-center text-muted-foreground">
                      No hay cuentas contables registradas en el catálogo.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <ConfirmDialog
        isOpen={!!deleteTarget}
        title="Eliminar Cuenta Contable"
        description={`¿Está seguro de que desea eliminar la cuenta ${deleteTarget?.accountNumber} - ${deleteTarget?.accountName}?`}
        confirmText="Eliminar Cuenta"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}

