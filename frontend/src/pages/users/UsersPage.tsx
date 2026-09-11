import { useState, useEffect, useCallback } from 'react'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/errors'
import type { User, Role, CreateUserRequest, UpdateUserRequest } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { InlineAlert } from '@/components/ui/InlineAlert'
import {
  Users,
  UserPlus,
  Search,
  KeyRound,
  Edit3,
  UserX,
  UserCheck,
  Eye,
  EyeOff,
  RefreshCw,
  Loader2,
  CheckCircle2,
} from 'lucide-react'

const roleBadgeColors: Record<string, string> = {
  Admin: 'bg-amber-100 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-800',
  Recepcion: 'bg-blue-100 dark:bg-blue-950/40 text-blue-800 dark:text-blue-300 border border-blue-300 dark:border-blue-800',
  Caja: 'bg-cyan-100 dark:bg-cyan-950/40 text-cyan-800 dark:text-cyan-300 border border-cyan-300 dark:border-cyan-800',
  Contador: 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800',
}

const isStrongPassword = (value: string) =>
  value.length >= 12 && /[A-Z]/.test(value) && /[a-z]/.test(value) && /\d/.test(value)

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [loading, setLoading] = useState(true)
  const [searchTerm, setSearchTerm] = useState('')
  const [roleFilter, setRoleFilter] = useState('')

  // Modals & form state
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [createForm, setCreateForm] = useState<CreateUserRequest>({
    username: '',
    password: '',
    email: '',
    firstName: '',
    lastName: '',
    roles: ['Recepcion'],
  })
  const [showPassword, setShowPassword] = useState(false)

  const [editingUser, setEditingUser] = useState<User | null>(null)
  const [editForm, setEditForm] = useState<UpdateUserRequest>({
    firstName: '',
    lastName: '',
    email: '',
    isActive: true,
    roles: [],
  })

  const [resettingUser, setResettingUser] = useState<User | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [showResetPassword, setShowResetPassword] = useState(false)

  const [deactivateTarget, setDeactivateTarget] = useState<User | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [actionLoading, setActionLoading] = useState(false)

  const loadData = useCallback(async () => {
    setLoading(true)
    try {
      const [usersRes, rolesRes] = await Promise.all([
        api.get<User[]>('/users'),
        api.get<Role[]>('/roles'),
      ])
      setUsers(usersRes.data)
      setRoles(rolesRes.data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al consultar la lista de usuarios y roles.' })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => void loadData(), 0)
    return () => window.clearTimeout(timer)
  }, [loadData])

  // Handle Create User
  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!createForm.username || !createForm.password || !createForm.email || !createForm.firstName || !createForm.lastName) {
      setAlertInfo({ variant: 'error', message: 'Todos los campos obligatorios deben ser completados.' })
      return
    }
    if (!isStrongPassword(createForm.password)) {
      setAlertInfo({ variant: 'error', message: 'La contraseña debe tener al menos 12 caracteres, mayúscula, minúscula y número.' })
      return
    }

    setActionLoading(true)
    try {
      await api.post('/users', createForm)
      setAlertInfo({ variant: 'success', message: `Usuario @${createForm.username} creado exitosamente con sus roles asignados.` })
      setShowCreateModal(false)
      setCreateForm({
        username: '',
        password: '',
        email: '',
        firstName: '',
        lastName: '',
        roles: ['Recepcion'],
      })
      loadData()
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al crear el nuevo usuario.') })
    } finally {
      setActionLoading(false)
    }
  }

  // Handle Edit User
  const openEditModal = (user: User) => {
    setEditingUser(user)
    setEditForm({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      isActive: user.isActive,
      roles: user.roles || [],
    })
  }

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!editingUser) return

    setActionLoading(true)
    try {
      await api.put(`/users/${editingUser.id}`, editForm)
      setAlertInfo({ variant: 'success', message: `Datos del usuario @${editingUser.username} actualizados correctamente.` })
      setEditingUser(null)
      loadData()
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al actualizar el usuario.') })
    } finally {
      setActionLoading(false)
    }
  }

  // Handle Password Reset
  const handleResetPasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!resettingUser) return
    if (!isStrongPassword(newPassword)) {
      setAlertInfo({ variant: 'error', message: 'La nueva contraseña debe tener al menos 12 caracteres, mayúscula, minúscula y número.' })
      return
    }

    setActionLoading(true)
    try {
      await api.post(`/users/${resettingUser.id}/reset-password`, { newPassword })
      setAlertInfo({ variant: 'success', message: `Contraseña restablecida exitosamente para @${resettingUser.username}.` })
      setResettingUser(null)
      setNewPassword('')
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al restablecer la contraseña.') })
    } finally {
      setActionLoading(false)
    }
  }

  // Handle Deactivate User
  const handleDeactivateConfirm = async () => {
    if (!deactivateTarget) return
    try {
      await api.delete(`/users/${deactivateTarget.id}`)
      setAlertInfo({ variant: 'success', message: `Usuario @${deactivateTarget.username} desactivado del sistema.` })
      setDeactivateTarget(null)
      loadData()
    } catch (error: unknown) {
      setAlertInfo({ variant: 'error', message: getApiErrorMessage(error, 'Error al desactivar el usuario.') })
    }
  }

  const toggleRoleSelection = (roleName: string, isCreate: boolean) => {
    if (isCreate) {
      const current = createForm.roles || []
      const updated = current.includes(roleName)
        ? current.filter((r) => r !== roleName)
        : [...current, roleName]
      setCreateForm({ ...createForm, roles: updated })
    } else {
      const current = editForm.roles || []
      const updated = current.includes(roleName)
        ? current.filter((r) => r !== roleName)
        : [...current, roleName]
      setEditForm({ ...editForm, roles: updated })
    }
  }

  const filteredUsers = users.filter((u) => {
    const matchesSearch =
      u.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
      u.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      `${u.firstName} ${u.lastName}`.toLowerCase().includes(searchTerm.toLowerCase())
    const matchesRole = roleFilter ? u.roles.includes(roleFilter) : true
    return matchesSearch && matchesRole
  })

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <Users className="text-[#C69C4B]" size={24} />
            Gestión de Usuarios y Roles
          </h1>
          <p className="text-sm text-muted-foreground">
            Administración de cuentas de acceso del personal del Hotel Maya Central y asignación de roles.
          </p>
        </div>
        <div className="flex items-center gap-2.5">
          <Button variant="outline" onClick={loadData} disabled={loading} className="gap-1.5">
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            Actualizar
          </Button>
          <Button onClick={() => setShowCreateModal(true)} className="gap-1.5">
            <UserPlus size={16} />
            Crear Usuario
          </Button>
        </div>
      </div>

      {alertInfo && (
        <InlineAlert
          variant={alertInfo.variant}
          message={alertInfo.message}
          onClose={() => setAlertInfo(null)}
        />
      )}

      {/* Filters Bar */}
      <div className="p-4 rounded-xl border border-border bg-card shadow-xs flex flex-wrap gap-3 items-center">
        <div className="relative flex-1 min-w-[240px]">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por nombre, usuario o email..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-8 text-sm"
          />
        </div>
        <div className="w-56">
          <select
            value={roleFilter}
            onChange={(e) => setRoleFilter(e.target.value)}
            className="w-full border border-input rounded-md px-3 py-2 text-sm bg-background"
          >
            <option value="">Todos los roles</option>
            {roles.map((r) => (
              <option key={r.id} value={r.name}>
                Rol: {r.name}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Users Table */}
      <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
        <div className="p-4 border-b border-border/80 bg-muted/20 flex items-center justify-between">
          <h3 className="font-semibold text-sm text-foreground">Usuarios Registrados</h3>
          <span className="text-xs text-muted-foreground">{filteredUsers.length} usuario(s)</span>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-16 space-y-2">
            <Loader2 size={26} className="animate-spin text-[#C69C4B] mr-2" />
            <span className="text-sm text-muted-foreground">Cargando usuarios...</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/40 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3.5">Usuario</th>
                  <th className="text-left p-3.5">Nombre Completo</th>
                  <th className="text-left p-3.5">Correo Electrónico</th>
                  <th className="text-left p-3.5">Roles Asignados</th>
                  <th className="text-left p-3.5">Estado</th>
                  <th className="text-right p-3.5">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {filteredUsers.map((u) => (
                  <tr key={u.id} className="hover:bg-accent/30 transition-colors">
                    <td className="p-3.5">
                      <div className="flex items-center gap-2">
                        <div className="w-8 h-8 rounded-full bg-primary/10 text-[#C69C4B] flex items-center justify-center font-bold text-xs">
                          {u.username.slice(0, 2).toUpperCase()}
                        </div>
                        <div>
                          <p className="font-semibold text-foreground text-xs leading-none">@{u.username}</p>
                        </div>
                      </div>
                    </td>
                    <td className="p-3.5 text-xs text-foreground font-medium">
                      {u.firstName} {u.lastName}
                    </td>
                    <td className="p-3.5 text-xs text-muted-foreground">{u.email}</td>
                    <td className="p-3.5">
                      <div className="flex flex-wrap gap-1.5">
                        {u.roles && u.roles.length > 0 ? (
                          u.roles.map((r) => (
                            <span
                              key={r}
                              className={`px-2 py-0.5 rounded-md text-[11px] font-semibold ${
                                roleBadgeColors[r] || 'bg-muted text-muted-foreground border border-border'
                              }`}
                            >
                              {r}
                            </span>
                          ))
                        ) : (
                          <span className="text-xs text-muted-foreground italic">Sin roles</span>
                        )}
                      </div>
                    </td>
                    <td className="p-3.5">
                      <span
                        className={`px-2.5 py-0.5 rounded-full text-[11px] font-semibold flex items-center w-fit gap-1 ${
                          u.isActive
                            ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800'
                            : 'bg-neutral-200 dark:bg-neutral-800 text-neutral-600 dark:text-neutral-400 border border-border'
                        }`}
                      >
                        {u.isActive ? <UserCheck size={11} /> : <UserX size={11} />}
                        {u.isActive ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="p-3.5 text-right space-x-1.5">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => openEditModal(u)}
                        className="h-7 w-7 p-0"
                        title="Editar usuario y roles"
                      >
                        <Edit3 size={13} />
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          setResettingUser(u)
                          setNewPassword('')
                        }}
                        className="h-7 w-7 p-0 text-amber-600 hover:text-amber-700 dark:text-amber-400"
                        title="Restablecer contraseña"
                      >
                        <KeyRound size={13} />
                      </Button>
                      {u.isActive && (
                        <Button
                          size="sm"
                          variant="destructive"
                          onClick={() => setDeactivateTarget(u)}
                          className="h-7 w-7 p-0"
                          title="Desactivar usuario"
                        >
                          <UserX size={13} />
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
                {filteredUsers.length === 0 && (
                  <tr>
                    <td colSpan={6} className="p-8 text-center text-muted-foreground">
                      No se encontraron usuarios con los criterios de búsqueda.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* CREATE USER MODAL */}
      {showCreateModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => setShowCreateModal(false)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-lg shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center gap-2 border-b border-border pb-3">
              <UserPlus className="text-[#C69C4B]" size={20} />
              <h3 className="text-lg font-bold text-foreground">Crear Nuevo Usuario</h3>
            </div>

            <form onSubmit={handleCreateSubmit} className="space-y-3.5">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Nombre *
                  </label>
                  <Input
                    required
                    value={createForm.firstName}
                    onChange={(e) => setCreateForm({ ...createForm, firstName: e.target.value })}
                    placeholder="Ej. Juan"
                    className="mt-1"
                  />
                </div>
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Apellido *
                  </label>
                  <Input
                    required
                    value={createForm.lastName}
                    onChange={(e) => setCreateForm({ ...createForm, lastName: e.target.value })}
                    placeholder="Ej. Pérez"
                    className="mt-1"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Nombre de Usuario *
                  </label>
                  <Input
                    required
                    value={createForm.username}
                    onChange={(e) => setCreateForm({ ...createForm, username: e.target.value })}
                    placeholder="Ej. jperez"
                    className="mt-1"
                  />
                </div>
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Correo Electrónico *
                  </label>
                  <Input
                    type="email"
                    required
                    value={createForm.email}
                    onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                    placeholder="juan@hotelmayacentral.com"
                    className="mt-1"
                  />
                </div>
              </div>

              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Contraseña Inicial * (12 caracteres, mayúscula, minúscula y número)
                </label>
                <div className="relative mt-1">
                  <Input
                    type={showPassword ? 'text' : 'password'}
                    required
                    minLength={12}
                    value={createForm.password}
                    onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
                    placeholder="••••••••"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground cursor-pointer"
                  >
                    {showPassword ? <EyeOff size={15} /> : <Eye size={15} />}
                  </button>
                </div>
              </div>

              {/* Roles Selection */}
              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground block mb-1.5">
                  Roles y Permisos del Sistema
                </label>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-2 p-3 bg-muted/20 border border-border rounded-lg">
                  {roles.map((role) => {
                    const isSelected = createForm.roles?.includes(role.name)
                    return (
                      <button
                        type="button"
                        key={role.id}
                        onClick={() => toggleRoleSelection(role.name, true)}
                        className={`p-2 rounded-md text-xs font-semibold text-left transition-all border flex items-center justify-between cursor-pointer ${
                          isSelected
                            ? 'bg-[#C69C4B]/15 border-[#C69C4B] text-foreground'
                            : 'bg-card border-border text-muted-foreground hover:text-foreground'
                        }`}
                      >
                        <span>{role.name}</span>
                        {isSelected && <CheckCircle2 size={13} className="text-[#C69C4B]" />}
                      </button>
                    )
                  })}
                </div>
              </div>

              <div className="flex justify-end gap-2.5 pt-3 border-t border-border">
                <Button type="button" variant="outline" onClick={() => setShowCreateModal(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={actionLoading} className="gap-1.5">
                  {actionLoading && <Loader2 size={14} className="animate-spin" />}
                  Crear Usuario
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* EDIT USER MODAL */}
      {editingUser && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => setEditingUser(null)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-lg shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center gap-2 border-b border-border pb-3">
              <Edit3 className="text-[#C69C4B]" size={20} />
              <h3 className="text-lg font-bold text-foreground">
                Editar Usuario @{editingUser.username}
              </h3>
            </div>

            <form onSubmit={handleEditSubmit} className="space-y-3.5">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Nombre
                  </label>
                  <Input
                    value={editForm.firstName || ''}
                    onChange={(e) => setEditForm({ ...editForm, firstName: e.target.value })}
                    className="mt-1"
                  />
                </div>
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Apellido
                  </label>
                  <Input
                    value={editForm.lastName || ''}
                    onChange={(e) => setEditForm({ ...editForm, lastName: e.target.value })}
                    className="mt-1"
                  />
                </div>
              </div>

              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Correo Electrónico
                </label>
                <Input
                  type="email"
                  value={editForm.email || ''}
                  onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
                  className="mt-1"
                />
              </div>

              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  id="isActiveToggle"
                  checked={editForm.isActive ?? true}
                  onChange={(e) => setEditForm({ ...editForm, isActive: e.target.checked })}
                  className="h-4 w-4 rounded border-border text-[#C69C4B] focus:ring-[#C69C4B]"
                />
                <label htmlFor="isActiveToggle" className="text-xs font-medium text-foreground cursor-pointer">
                  Cuenta Activa y Habilitada para Iniciar Sesión
                </label>
              </div>

              {/* Roles Selection */}
              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground block mb-1.5">
                  Roles Asignados
                </label>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-2 p-3 bg-muted/20 border border-border rounded-lg">
                  {roles.map((role) => {
                    const isSelected = editForm.roles?.includes(role.name)
                    return (
                      <button
                        type="button"
                        key={role.id}
                        onClick={() => toggleRoleSelection(role.name, false)}
                        className={`p-2 rounded-md text-xs font-semibold text-left transition-all border flex items-center justify-between cursor-pointer ${
                          isSelected
                            ? 'bg-[#C69C4B]/15 border-[#C69C4B] text-foreground'
                            : 'bg-card border-border text-muted-foreground hover:text-foreground'
                        }`}
                      >
                        <span>{role.name}</span>
                        {isSelected && <CheckCircle2 size={13} className="text-[#C69C4B]" />}
                      </button>
                    )
                  })}
                </div>
              </div>

              <div className="flex justify-end gap-2.5 pt-3 border-t border-border">
                <Button type="button" variant="outline" onClick={() => setEditingUser(null)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={actionLoading} className="gap-1.5">
                  {actionLoading && <Loader2 size={14} className="animate-spin" />}
                  Guardar Cambios
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* RESET PASSWORD MODAL */}
      {resettingUser && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => setResettingUser(null)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-md shadow-2xl space-y-4 animate-in zoom-in-95"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center gap-2 border-b border-border pb-3">
              <KeyRound className="text-amber-500" size={20} />
              <h3 className="text-lg font-bold text-foreground">
                Restablecer Contraseña
              </h3>
            </div>

            <p className="text-xs text-muted-foreground">
              Establecer nueva contraseña para el usuario <strong className="text-foreground">@{resettingUser.username}</strong> ({resettingUser.firstName} {resettingUser.lastName}). Se cerrarán todas las sesiones abiertas de este usuario.
            </p>

            <form onSubmit={handleResetPasswordSubmit} className="space-y-3.5">
              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Nueva Contraseña * (12 caracteres, mayúscula, minúscula y número)
                </label>
                <div className="relative mt-1">
                  <Input
                    type={showResetPassword ? 'text' : 'password'}
                    required
                    minLength={12}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="••••••••"
                    autoFocus
                  />
                  <button
                    type="button"
                    onClick={() => setShowResetPassword(!showResetPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground cursor-pointer"
                  >
                    {showResetPassword ? <EyeOff size={15} /> : <Eye size={15} />}
                  </button>
                </div>
              </div>

              <div className="flex justify-end gap-2.5 pt-3 border-t border-border">
                <Button type="button" variant="outline" onClick={() => setResettingUser(null)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={actionLoading} className="gap-1.5 bg-amber-600 hover:bg-amber-700 text-white">
                  {actionLoading && <Loader2 size={14} className="animate-spin" />}
                  Confirmar Contraseña
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* DEACTIVATE CONFIRM DIALOG */}
      <ConfirmDialog
        isOpen={!!deactivateTarget}
        title="Desactivar Usuario"
        description={`¿Está seguro de que desea desactivar al usuario @${deactivateTarget?.username} (${deactivateTarget?.firstName} ${deactivateTarget?.lastName})? El usuario no podrá iniciar sesión en Hotel Maya Central.`}
        confirmText="Desactivar Usuario"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleDeactivateConfirm}
        onCancel={() => setDeactivateTarget(null)}
      />
    </div>
  )
}
