import { useEffect, useState, useCallback, useMemo } from 'react'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/errors'
import type { Customer } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { Pagination } from '@/components/ui/Pagination'
import { useAuthStore } from '@/store/authStore'
import { Building2, Plus, Search, Edit2, Trash2, Loader2, RefreshCw, X } from 'lucide-react'

const defaultForm = {
  rtn: '',
  name: '',
  address: '',
  phone: '',
  email: '',
  taxpayerType: 'Gravado',
  exonerationOrderNumber: '',
  sefinExonerationCertificateNumber: '',
  sagRegistryNumber: '',
  isIsvExempt: false,
  isTouristTaxExempt: false,
  exonerationValidFrom: '',
  exonerationValidTo: '',
}

function sanitize(form: typeof defaultForm) {
  return {
    rtn: form.rtn || null,
    name: form.name.trim(),
    address: form.address || null,
    phone: form.phone || null,
    email: form.email || null,
    taxpayerType: form.taxpayerType,
    exonerationOrderNumber: form.exonerationOrderNumber || null,
    sefinExonerationCertificateNumber: form.sefinExonerationCertificateNumber || null,
    sagRegistryNumber: form.sagRegistryNumber || null,
    isIsvExempt: form.isIsvExempt,
    isTouristTaxExempt: form.isTouristTaxExempt,
    exonerationValidFrom: form.exonerationValidFrom || null,
    exonerationValidTo: form.exonerationValidTo || null,
  }
}

export default function CustomersPage() {
  const canManageReservations = useAuthStore((state) => state.hasPermission('manage_reservations'))
  const [customers, setCustomers] = useState<Customer[]>([])
  const [search, setSearch] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(defaultForm)
  const [loading, setLoading] = useState(true)
  const [deleteTarget, setDeleteTarget] = useState<Customer | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [actionLoading, setActionLoading] = useState(false)

  // Pagination
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await api.get<Customer[]>('/customers', { params: { search: search || undefined } })
      setCustomers(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al consultar la cartera de clientes.' })
    } finally {
      setLoading(false)
    }
  }, [search])

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0)
    return () => window.clearTimeout(timer)
  }, [load])

  const startCreate = () => {
    setEditingId(null)
    setForm(defaultForm)
    setShowForm(true)
  }

  const startEdit = (customer: Customer) => {
    setEditingId(customer.id)
    setForm({
      rtn: customer.rtn || '',
      name: customer.name,
      address: customer.address || '',
      phone: customer.phone || '',
      email: customer.email || '',
      taxpayerType: customer.taxpayerType || 'Gravado',
      exonerationOrderNumber: customer.exonerationOrderNumber || '',
      sefinExonerationCertificateNumber: customer.sefinExonerationCertificateNumber || '',
      sagRegistryNumber: customer.sagRegistryNumber || '',
      isIsvExempt: customer.isIsvExempt,
      isTouristTaxExempt: customer.isTouristTaxExempt,
      exonerationValidFrom: customer.exonerationValidFrom || '',
      exonerationValidTo: customer.exonerationValidTo || '',
    })
    setShowForm(true)
  }

  const save = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.name.trim()) {
      setAlertInfo({ variant: 'error', message: 'La razón social o nombre del cliente es obligatorio.' })
      return
    }

    if (form.rtn && form.rtn.length !== 14) {
      setAlertInfo({ variant: 'error', message: 'El RTN debe contener exactamente 14 dígitos numéricos.' })
      return
    }

    setActionLoading(true)
    try {
      if (editingId) {
        await api.put(`/customers/${editingId}`, sanitize(form))
        setAlertInfo({ variant: 'success', message: 'Cliente actualizado correctamente.' })
      } else {
        await api.post('/customers', sanitize(form))
        setAlertInfo({ variant: 'success', message: 'Cliente registrado exitosamente.' })
      }
      setShowForm(false)
      setEditingId(null)
      setForm(defaultForm)
      load()
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al guardar el cliente.') })
    } finally {
      setActionLoading(false)
    }
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    setActionLoading(true)
    try {
      await api.delete(`/customers/${deleteTarget.id}`)
      setAlertInfo({ variant: 'success', message: 'Cliente eliminado correctamente.' })
      setDeleteTarget(null)
      load()
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se puede eliminar un cliente con facturas asociadas.') })
    } finally {
      setActionLoading(false)
    }
  }

  const filteredCustomers = useMemo(() => {
    return customers.filter((c) => {
      const q = search.toLowerCase()
      return (
        c.name.toLowerCase().includes(q) ||
        (c.rtn && c.rtn.includes(q)) ||
        (c.phone && c.phone.includes(q)) ||
        (c.email && c.email.toLowerCase().includes(q))
      )
    })
  }, [customers, search])

  const totalPages = Math.ceil(filteredCustomers.length / pageSize) || 1
  const paginatedCustomers = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredCustomers.slice(start, start + pageSize)
  }, [filteredCustomers, currentPage, pageSize])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <Building2 className="text-[#C69C4B]" size={24} />
            Directorio de Clientes & Empresas
          </h1>
          <p className="text-sm text-muted-foreground">
            Registro fiscal de personas naturales, jurídicas con RTN y acreditaciones de exoneración SAR.
          </p>
        </div>
        <div className="flex items-center gap-2.5">
          <Button variant="outline" onClick={load} disabled={loading} className="gap-1.5">
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            Actualizar
          </Button>
          {canManageReservations && (
            <Button onClick={startCreate} className="gap-1.5">
              <Plus size={16} />
              Nuevo Cliente / Empresa
            </Button>
          )}
        </div>
      </div>

      {alertInfo && (
        <InlineAlert variant={alertInfo.variant} message={alertInfo.message} onClose={() => setAlertInfo(null)} />
      )}

      {/* Form Card */}
      {showForm && canManageReservations && (
        <form onSubmit={save} className="border border-border rounded-xl p-5 bg-card space-y-4 shadow-sm animate-in fade-in">
          <div className="flex items-center justify-between border-b border-border pb-3">
            <h3 className="font-bold text-sm text-foreground">
              {editingId ? 'Editar Cliente / Empresa' : 'Registrar Nuevo Cliente / Empresa'}
            </h3>
            <button
              type="button"
              onClick={() => setShowForm(false)}
              className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent cursor-pointer"
            >
              <X size={16} />
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5 text-xs">
            <div>
              <label className="text-muted-foreground font-semibold">Nombre / Razón Social *</label>
              <Input
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="Ej. Distribuidora del Norte S.A. de C.V."
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">RTN Fiscal (14 Dígitos)</label>
              <Input
                value={form.rtn}
                onChange={(e) => setForm({ ...form, rtn: e.target.value.replace(/\D/g, '').slice(0, 14) })}
                placeholder="08011990123456"
                maxLength={14}
                className="text-xs mt-1 font-mono"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Correo Electrónico</label>
              <Input
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                placeholder="contabilidad@empresa.hn"
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Teléfono de Contacto</label>
              <Input
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                placeholder="+504 2222-0000"
                className="text-xs mt-1 font-mono"
              />
            </div>
            <div className="md:col-span-2">
              <label className="text-muted-foreground font-semibold">Dirección Fiscal Completa</label>
              <Input
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
                placeholder="Ej. Colonia Palmira, Avenida República de Chile, Tegucigalpa"
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Régimen Contribuyente SAR</label>
              <select
                value={form.taxpayerType}
                onChange={(e) => setForm({ ...form, taxpayerType: e.target.value })}
                className="w-full border border-input rounded-md px-3 py-2 text-xs bg-background mt-1"
              >
                <option value="ConsumidorFinal">Consumidor Final</option>
                <option value="Gravado">Gravado / Empresa</option>
                <option value="Exonerado">Exonerado Diplomático / SEFIN</option>
              </select>
            </div>
            <div className="flex items-center gap-6 pt-5">
              <label className="flex items-center gap-2 text-xs text-muted-foreground cursor-pointer">
                <input
                  type="checkbox"
                  checked={form.isIsvExempt}
                  onChange={(e) => setForm({ ...form, isIsvExempt: e.target.checked })}
                  className="rounded border-border"
                />
                Exento de ISV (15%)
              </label>
              <label className="flex items-center gap-2 text-xs text-muted-foreground cursor-pointer">
                <input
                  type="checkbox"
                  checked={form.isTouristTaxExempt}
                  onChange={(e) => setForm({ ...form, isTouristTaxExempt: e.target.checked })}
                  className="rounded border-border"
                />
                Exento de Tasa Turística (4%)
              </label>
            </div>

            {form.taxpayerType === 'Exonerado' && (
              <>
                <div>
                  <label className="text-muted-foreground font-semibold">No. Orden de Compra Exenta</label>
                  <Input
                    value={form.exonerationOrderNumber}
                    onChange={(e) => setForm({ ...form, exonerationOrderNumber: e.target.value })}
                    placeholder="Ej. OC-EX-2026-001"
                    className="text-xs mt-1 font-mono"
                  />
                </div>
                <div>
                  <label className="text-muted-foreground font-semibold">Constancia de Registro SEFIN</label>
                  <Input
                    value={form.sefinExonerationCertificateNumber}
                    onChange={(e) => setForm({ ...form, sefinExonerationCertificateNumber: e.target.value })}
                    placeholder="Ej. SEFIN-DGR-2026-1234"
                    className="text-xs mt-1 font-mono"
                  />
                </div>
                <div>
                  <label className="text-muted-foreground font-semibold">Registro SAG (Agropecuario)</label>
                  <Input
                    value={form.sagRegistryNumber}
                    onChange={(e) => setForm({ ...form, sagRegistryNumber: e.target.value })}
                    placeholder="Opcional"
                    className="text-xs mt-1 font-mono"
                  />
                </div>
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <label className="text-muted-foreground font-semibold">Vigente Desde</label>
                    <Input
                      type="date"
                      value={form.exonerationValidFrom}
                      onChange={(e) => setForm({ ...form, exonerationValidFrom: e.target.value })}
                      className="text-xs mt-1"
                    />
                  </div>
                  <div>
                    <label className="text-muted-foreground font-semibold">Vigente Hasta</label>
                    <Input
                      type="date"
                      value={form.exonerationValidTo}
                      onChange={(e) => setForm({ ...form, exonerationValidTo: e.target.value })}
                      className="text-xs mt-1"
                    />
                  </div>
                </div>
              </>
            )}
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t border-border">
            <Button type="button" variant="outline" onClick={() => setShowForm(false)}>
              Cancelar
            </Button>
            <Button type="submit" disabled={actionLoading}>
              {actionLoading && <Loader2 size={14} className="animate-spin mr-1" />}
              {editingId ? 'Actualizar Cliente' : 'Guardar Cliente'}
            </Button>
          </div>
        </form>
      )}

      {/* Search Bar */}
      <div className="p-4 rounded-xl border border-border bg-card shadow-xs">
        <div className="relative">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por razón social, RTN, teléfono o correo electrónico..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setCurrentPage(1)
            }}
            className="pl-8 text-xs h-9"
          />
        </div>
      </div>

      {/* Customers Table */}
      <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
        <div className="p-3.5 border-b border-border bg-muted/20 flex items-center justify-between">
          <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider">
            Listado de Clientes y Empresas
          </h3>
          <span className="text-xs text-muted-foreground">{filteredCustomers.length} cliente(s)</span>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-16 space-y-2">
            <Loader2 size={26} className="animate-spin text-[#C69C4B] mr-2" />
            <span className="text-xs text-muted-foreground">Cargando clientes...</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3.5">Cliente / Razón Social</th>
                  <th className="text-left p-3.5 w-36">RTN Fiscal</th>
                  <th className="text-left p-3.5 w-28">Régimen</th>
                  <th className="text-left p-3.5 w-28">Teléfono</th>
                  <th className="text-left p-3.5">Correo</th>
                  <th className="text-right p-3.5 w-24">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {paginatedCustomers.map((customer) => (
                  <tr key={customer.id} className="hover:bg-accent/20 transition-colors">
                    <td className="p-3.5 font-bold text-foreground">{customer.name}</td>
                    <td className="p-3.5 font-mono text-muted-foreground font-semibold">
                      {customer.rtn || '—'}
                    </td>
                    <td className="p-3.5">
                      <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-primary/10 text-foreground border border-primary/20">
                        {customer.taxpayerType}
                      </span>
                    </td>
                    <td className="p-3.5 font-mono text-muted-foreground">{customer.phone || '—'}</td>
                    <td className="p-3.5 text-muted-foreground">{customer.email || '—'}</td>
                    <td className="p-3.5 text-right space-x-1">
                      {canManageReservations ? (
                        <>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => startEdit(customer)}
                            className="h-7 w-7 p-0"
                            title="Editar cliente"
                          >
                            <Edit2 size={12} />
                          </Button>
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => setDeleteTarget(customer)}
                            className="h-7 w-7 p-0"
                            title="Eliminar"
                          >
                            <Trash2 size={12} />
                          </Button>
                        </>
                      ) : (
                        <span className="text-[10px] text-muted-foreground">Solo lectura</span>
                      )}
                    </td>
                  </tr>
                ))}
                {paginatedCustomers.length === 0 && (
                  <tr>
                    <td colSpan={6} className="p-8 text-center text-muted-foreground">
                      No se encontraron clientes registrados con los filtros seleccionados.
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
          totalItems={filteredCustomers.length}
          pageSize={pageSize}
          onPageChange={setCurrentPage}
          onPageSizeChange={(size) => {
            setPageSize(size)
            setCurrentPage(1)
          }}
        />
      </div>

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={!!deleteTarget}
        title="Eliminar Cliente"
        description={`¿Está seguro de que desea eliminar al cliente "${deleteTarget?.name}"? Esta acción es irreversible.`}
        confirmText="Eliminar Cliente"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}
