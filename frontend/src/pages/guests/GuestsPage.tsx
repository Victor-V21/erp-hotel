import { useState, useEffect, useCallback, useMemo } from 'react'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/errors'
import type { Guest } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { Pagination } from '@/components/ui/Pagination'
import { useAuthStore } from '@/store/authStore'
import { Users, Plus, Search, Edit2, Trash2, Loader2, RefreshCw, X } from 'lucide-react'

function sanitize(g: typeof defaultForm) {
  return {
    ...g,
    email: g.email || null,
    phone: g.phone || null,
    dateOfBirth: g.dateOfBirth || null,
    documentNumber: g.documentNumber || null,
    nationality: g.nationality || null,
    origin: g.origin || null,
    vehiclePlate: g.vehiclePlate || null,
    company: g.company || null,
    guestRTN: g.guestRTN || null,
    preferences: g.preferences || null,
    taxpayerType: g.taxpayerType || null,
    exonerationOrderNumber: g.exonerationOrderNumber || null,
    sefinExonerationCertificateNumber: g.sefinExonerationCertificateNumber || null,
    sagRegistryNumber: g.sagRegistryNumber || null,
  }
}

const defaultForm = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  dateOfBirth: '',
  nationality: 'Hondureña',
  documentType: 'DNI',
  documentNumber: '',
  origin: '',
  hasVehicle: false,
  vehiclePlate: '',
  company: '',
  guestRTN: '',
  preferences: '',
  classification: 'Normal',
  taxpayerType: 'ConsumidorFinal',
  exonerationOrderNumber: '',
  sefinExonerationCertificateNumber: '',
  sagRegistryNumber: '',
}

export default function GuestsPage() {
  const canManageReservations = useAuthStore((state) => state.hasPermission('manage_reservations'))
  const [guests, setGuests] = useState<Guest[]>([])
  const [search, setSearch] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(defaultForm)
  const [loading, setLoading] = useState(true)
  const [deleteTarget, setDeleteTarget] = useState<Guest | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [actionLoading, setActionLoading] = useState(false)

  // Pagination
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(15)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await api.get<Guest[]>('/guests', { params: { search: search || undefined } })
      setGuests(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al consultar el directorio de huéspedes.' })
    } finally {
      setLoading(false)
    }
  }, [search])

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0)
    return () => window.clearTimeout(timer)
  }, [load])

  const save = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.firstName.trim() || !form.lastName.trim()) {
      setAlertInfo({ variant: 'error', message: 'El nombre y apellido son obligatorios.' })
      return
    }

    setActionLoading(true)
    try {
      if (editingId) {
        await api.put(`/guests/${editingId}`, sanitize(form))
        setAlertInfo({ variant: 'success', message: 'Huésped actualizado correctamente.' })
      } else {
        await api.post('/guests', sanitize(form))
        setAlertInfo({ variant: 'success', message: 'Huésped registrado exitosamente.' })
      }
      setShowForm(false)
      setEditingId(null)
      setForm(defaultForm)
      load()
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al guardar el huésped.') })
    } finally {
      setActionLoading(false)
    }
  }

  const startEdit = (g: Guest) => {
    setEditingId(g.id)
    setForm({
      firstName: g.firstName,
      lastName: g.lastName,
      email: g.email || '',
      phone: g.phone || '',
      dateOfBirth: g.dateOfBirth || '',
      nationality: g.nationality || 'Hondureña',
      documentType: g.documentType || 'DNI',
      documentNumber: g.documentNumber || '',
      origin: g.origin || '',
      hasVehicle: g.hasVehicle,
      vehiclePlate: g.vehiclePlate || '',
      company: g.company || '',
      guestRTN: g.guestRTN || '',
      preferences: g.preferences || '',
      classification: g.classification || 'Normal',
      taxpayerType: g.taxpayerType || 'ConsumidorFinal',
      exonerationOrderNumber: g.exonerationOrderNumber || '',
      sefinExonerationCertificateNumber: g.sefinExonerationCertificateNumber || '',
      sagRegistryNumber: g.sagRegistryNumber || '',
    })
    setShowForm(true)
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    setActionLoading(true)
    try {
      await api.delete(`/guests/${deleteTarget.id}`)
      setAlertInfo({ variant: 'success', message: 'Ficha de huésped eliminada correctamente.' })
      setDeleteTarget(null)
      load()
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'No se puede eliminar un huésped con reservaciones activas.') })
    } finally {
      setActionLoading(false)
    }
  }

  const filteredGuests = useMemo(() => {
    return guests.filter((g) => {
      const q = search.toLowerCase()
      return (
        g.fullName.toLowerCase().includes(q) ||
        (g.documentNumber && g.documentNumber.includes(q)) ||
        (g.phone && g.phone.includes(q)) ||
        (g.company && g.company.toLowerCase().includes(q))
      )
    })
  }, [guests, search])

  const totalPages = Math.ceil(filteredGuests.length / pageSize) || 1
  const paginatedGuests = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredGuests.slice(start, start + pageSize)
  }, [filteredGuests, currentPage, pageSize])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <Users className="text-[#C69C4B]" size={24} />
            Directorio de Huéspedes
          </h1>
          <p className="text-sm text-muted-foreground">
            Registro oficial de huéspedes, documentos de identidad nacional y perfiles de preferencia.
          </p>
        </div>
        <div className="flex items-center gap-2.5">
          <Button variant="outline" onClick={load} disabled={loading} className="gap-1.5">
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            Actualizar
          </Button>
          {canManageReservations && (
            <Button
              onClick={() => {
                setEditingId(null)
                setForm(defaultForm)
                setShowForm(true)
              }}
              className="gap-1.5"
            >
              <Plus size={16} />
              Nuevo Huésped
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
              {editingId ? 'Editar Información del Huésped' : 'Registrar Nuevo Huésped'}
            </h3>
            <button
              type="button"
              onClick={() => setShowForm(false)}
              className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent cursor-pointer"
            >
              <X size={16} />
            </button>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3.5 text-xs">
            <div>
              <label className="text-muted-foreground font-semibold">Nombres *</label>
              <Input
                required
                value={form.firstName}
                onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                placeholder="Ej. Juan Carlos"
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Apellidos *</label>
              <Input
                required
                value={form.lastName}
                onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                placeholder="Ej. Pérez Rodríguez"
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Tipo Documento</label>
              <select
                value={form.documentType}
                onChange={(e) => setForm({ ...form, documentType: e.target.value })}
                className="w-full border border-input rounded-md px-2.5 py-2 text-xs bg-background mt-1"
              >
                <option value="DNI">DNI (Identidad Nacional)</option>
                <option value="Pasaporte">Pasaporte</option>
                <option value="Licencia">Licencia de Conducir</option>
                <option value="CarnetResidencia">Carnet de Residencia</option>
              </select>
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Número de Documento</label>
              <Input
                value={form.documentNumber}
                onChange={(e) => setForm({ ...form, documentNumber: e.target.value })}
                placeholder="Ej. 0801-1990-12345"
                className="text-xs mt-1 font-mono"
              />
            </div>

            <div>
              <label className="text-muted-foreground font-semibold">Nacionalidad</label>
              <Input
                value={form.nationality}
                onChange={(e) => setForm({ ...form, nationality: e.target.value })}
                placeholder="Hondureña"
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Procedencia / Ciudad</label>
              <Input
                value={form.origin}
                onChange={(e) => setForm({ ...form, origin: e.target.value })}
                placeholder="Ej. San Pedro Sula"
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Teléfono Celular</label>
              <Input
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                placeholder="+504 9999-9999"
                className="text-xs mt-1 font-mono"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Correo Electrónico</label>
              <Input
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                placeholder="cliente@correo.com"
                className="text-xs mt-1"
              />
            </div>

            <div>
              <label className="text-muted-foreground font-semibold">Empresa / Convenio</label>
              <Input
                value={form.company}
                onChange={(e) => setForm({ ...form, company: e.target.value })}
                placeholder="Opcional"
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">RTN Huésped</label>
              <Input
                value={form.guestRTN}
                onChange={(e) => setForm({ ...form, guestRTN: e.target.value })}
                placeholder="14 dígitos"
                maxLength={14}
                className="text-xs mt-1 font-mono"
              />
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Categoría / Perfil</label>
              <select
                value={form.classification}
                onChange={(e) => setForm({ ...form, classification: e.target.value })}
                className="w-full border border-input rounded-md px-2.5 py-2 text-xs bg-background mt-1"
              >
                <option value="Normal">Normal</option>
                <option value="VIP">Huésped VIP</option>
                <option value="Frecuente">Cliente Frecuente</option>
                <option value="Corporativo">Corporativo</option>
              </select>
            </div>
            <div>
              <label className="text-muted-foreground font-semibold">Tipo Contribuyente</label>
              <select
                value={form.taxpayerType}
                onChange={(e) => setForm({ ...form, taxpayerType: e.target.value })}
                className="w-full border border-input rounded-md px-2.5 py-2 text-xs bg-background mt-1"
              >
                <option value="ConsumidorFinal">Consumidor Final</option>
                <option value="Gravado">Gravado / Empresa</option>
                <option value="Exonerado">Exonerado Diplomático/SEFIN</option>
              </select>
            </div>
          </div>

          <div className="flex items-center justify-between pt-2 border-t border-border">
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="hasVehicle"
                checked={form.hasVehicle}
                onChange={(e) => setForm({ ...form, hasVehicle: e.target.checked })}
                className="rounded border-border"
              />
              <label htmlFor="hasVehicle" className="text-xs text-foreground cursor-pointer">
                Registrar vehículo ({form.hasVehicle ? 'Placa requerida' : 'Sin vehículo'})
              </label>
              {form.hasVehicle && (
                <Input
                  value={form.vehiclePlate}
                  onChange={(e) => setForm({ ...form, vehiclePlate: e.target.value.toUpperCase() })}
                  placeholder="H AA 1234"
                  className="w-32 text-xs h-7 ml-2 font-mono uppercase"
                />
              )}
            </div>

            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={() => setShowForm(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={actionLoading}>
                {actionLoading && <Loader2 size={14} className="animate-spin mr-1" />}
                {editingId ? 'Actualizar Ficha' : 'Guardar Huésped'}
              </Button>
            </div>
          </div>
        </form>
      )}

      {/* Filter and Search Bar */}
      <div className="p-4 rounded-xl border border-border bg-card shadow-xs">
        <div className="relative">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por nombre completo, DNI, teléfono o empresa..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setCurrentPage(1)
            }}
            className="pl-8 text-xs h-9"
          />
        </div>
      </div>

      {/* Guests Table */}
      <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
        <div className="p-3.5 border-b border-border bg-muted/20 flex items-center justify-between">
          <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider">
            Listado de Huéspedes
          </h3>
          <span className="text-xs text-muted-foreground">{filteredGuests.length} huésped(es)</span>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-16 space-y-2">
            <Loader2 size={26} className="animate-spin text-[#C69C4B] mr-2" />
            <span className="text-xs text-muted-foreground">Cargando directorio...</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3.5">Huésped</th>
                  <th className="text-left p-3.5 w-32">Documento</th>
                  <th className="text-left p-3.5">Nacionalidad / Ciudad</th>
                  <th className="text-left p-3.5 w-28">Teléfono</th>
                  <th className="text-left p-3.5">Empresa</th>
                  <th className="text-left p-3.5 w-24">Perfil</th>
                  <th className="text-right p-3.5 w-24">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {paginatedGuests.map((g) => (
                  <tr key={g.id} className="hover:bg-accent/20 transition-colors">
                    <td className="p-3.5 font-bold text-foreground">{g.fullName}</td>
                    <td className="p-3.5 font-mono text-muted-foreground">
                      {g.documentNumber ? `${g.documentType}: ${g.documentNumber}` : '—'}
                    </td>
                    <td className="p-3.5 text-muted-foreground">
                      {g.nationality || '—'} {g.origin ? `(${g.origin})` : ''}
                    </td>
                    <td className="p-3.5 font-mono text-muted-foreground">{g.phone || '—'}</td>
                    <td className="p-3.5 text-muted-foreground">{g.company || '—'}</td>
                    <td className="p-3.5">
                      <span
                        className={`px-2 py-0.5 rounded-full text-[10px] font-semibold ${
                          g.classification === 'VIP'
                            ? 'bg-purple-100 dark:bg-purple-950/40 text-purple-800 dark:text-purple-300 border border-purple-300'
                            : g.classification === 'Frecuente'
                            ? 'bg-amber-100 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300 border border-amber-300'
                            : 'bg-muted text-muted-foreground'
                        }`}
                      >
                        {g.classification || 'Normal'}
                      </span>
                    </td>
                    <td className="p-3.5 text-right space-x-1">
                      {canManageReservations ? (
                        <>
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => startEdit(g)}
                            className="h-7 w-7 p-0"
                            title="Editar ficha"
                          >
                            <Edit2 size={12} />
                          </Button>
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => setDeleteTarget(g)}
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
                {paginatedGuests.length === 0 && (
                  <tr>
                    <td colSpan={7} className="p-8 text-center text-muted-foreground">
                      No se encontraron huéspedes con los criterios de búsqueda.
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
          totalItems={filteredGuests.length}
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
        title="Eliminar Registro de Huésped"
        description={`¿Está seguro de que desea eliminar la ficha del huésped "${deleteTarget?.fullName}"? Esta acción es irreversible.`}
        confirmText="Eliminar Huésped"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}
