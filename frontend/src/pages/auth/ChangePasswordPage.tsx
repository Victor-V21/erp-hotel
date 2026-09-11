import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Check, Eye, EyeOff, Hotel, KeyRound, LogOut, ShieldCheck } from 'lucide-react'
import api from '@/lib/axios'
import { getApiErrorMessage } from '@/lib/errors'
import { useAuthStore } from '@/store/authStore'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const passwordRequirements = [
  { label: 'Al menos 12 caracteres', test: (value: string) => value.length >= 12 },
  { label: 'Una letra mayúscula', test: (value: string) => /[A-Z]/.test(value) },
  { label: 'Una letra minúscula', test: (value: string) => /[a-z]/.test(value) },
  { label: 'Un número', test: (value: string) => /\d/.test(value) },
] as const

export default function ChangePasswordPage() {
  const navigate = useNavigate()
  const logout = useAuthStore((state) => state.logout)
  const user = useAuthStore((state) => state.user)
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [showPasswords, setShowPasswords] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  const requirementsMet = passwordRequirements.every((requirement) => requirement.test(newPassword))
  const passwordsMatch = newPassword.length > 0 && newPassword === confirmation
  const canSubmit = currentPassword.length > 0 && requirementsMet && passwordsMatch && !submitting

  const endSession = async () => {
    try {
      await api.post('/auth/logout')
    } finally {
      logout()
      navigate('/login', { replace: true })
    }
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    if (!canSubmit) return

    setSubmitting(true)
    setError('')
    try {
      await api.post('/auth/change-password', { currentPassword, newPassword })
      logout()
      navigate('/login', { replace: true, state: { passwordChanged: true } })
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, 'No fue posible actualizar la contraseña.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="min-h-screen bg-muted/30 px-4 py-8 sm:px-6 lg:grid lg:grid-cols-[minmax(20rem,0.8fr)_minmax(32rem,1.2fr)] lg:p-0">
      <section className="hidden border-r border-border bg-card px-10 py-12 lg:flex lg:flex-col lg:justify-between">
        <div className="flex items-center gap-3">
          <div className="rounded-lg bg-primary/10 p-2 text-primary">
            <Hotel aria-hidden="true" size={24} />
          </div>
          <div>
            <p className="font-bold text-foreground">Hotel Maya Central</p>
            <p className="text-xs text-muted-foreground">Sistema de gestión hotelera</p>
          </div>
        </div>

        <div className="max-w-md space-y-5">
          <ShieldCheck aria-hidden="true" className="text-primary" size={40} strokeWidth={1.5} />
          <h1 className="text-3xl font-bold leading-tight text-foreground">
            Proteja su cuenta antes de comenzar
          </h1>
          <p className="text-sm leading-6 text-muted-foreground">
            Esta contraseña temporal solo sirve para el primer acceso. El cambio cierra las sesiones anteriores y habilita los módulos asignados a su rol.
          </p>
        </div>

        <p className="text-xs text-muted-foreground">Facturación y operación local · Honduras</p>
      </section>

      <section className="flex min-h-[calc(100vh-4rem)] items-center justify-center lg:min-h-screen lg:px-10">
        <div className="w-full max-w-lg rounded-xl border border-border bg-card p-6 shadow-sm sm:p-8">
          <div className="mb-7 space-y-2">
            <div className="mb-4 inline-flex rounded-lg bg-primary/10 p-2 text-primary lg:hidden">
              <KeyRound aria-hidden="true" size={24} />
            </div>
            <p className="text-xs font-semibold uppercase tracking-wider text-primary">Primer acceso</p>
            <h2 className="text-2xl font-bold text-foreground">Cree una contraseña personal</h2>
            <p className="text-sm leading-6 text-muted-foreground">
              Sesión iniciada como <span className="font-semibold text-foreground">{user?.username}</span>. No podrá abrir otros módulos hasta completar este paso.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-5">
            <div className="space-y-2">
              <label htmlFor="current-password" className="text-sm font-medium text-foreground">Contraseña temporal</label>
              <Input
                id="current-password"
                type={showPasswords ? 'text' : 'password'}
                autoComplete="current-password"
                value={currentPassword}
                onChange={(event) => setCurrentPassword(event.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="new-password" className="text-sm font-medium text-foreground">Contraseña nueva</label>
              <Input
                id="new-password"
                type={showPasswords ? 'text' : 'password'}
                autoComplete="new-password"
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                minLength={12}
                required
                aria-describedby="password-requirements"
              />
              <ul id="password-requirements" className="grid gap-1.5 pt-1 text-xs text-muted-foreground sm:grid-cols-2">
                {passwordRequirements.map((requirement) => {
                  const met = requirement.test(newPassword)
                  return (
                    <li key={requirement.label} className="flex items-center gap-1.5">
                      <Check aria-hidden="true" size={14} className={met ? 'text-emerald-600' : 'text-muted-foreground/50'} />
                      <span className={met ? 'text-foreground' : undefined}>{requirement.label}</span>
                    </li>
                  )
                })}
              </ul>
            </div>

            <div className="space-y-2">
              <label htmlFor="confirm-password" className="text-sm font-medium text-foreground">Confirme la contraseña nueva</label>
              <Input
                id="confirm-password"
                type={showPasswords ? 'text' : 'password'}
                autoComplete="new-password"
                value={confirmation}
                onChange={(event) => setConfirmation(event.target.value)}
                minLength={12}
                required
                aria-invalid={confirmation.length > 0 && !passwordsMatch}
              />
              {confirmation.length > 0 && !passwordsMatch && (
                <p className="text-xs text-destructive">Las contraseñas no coinciden.</p>
              )}
            </div>

            <label className="flex w-fit cursor-pointer items-center gap-2 text-sm text-muted-foreground">
              <input
                type="checkbox"
                checked={showPasswords}
                onChange={(event) => setShowPasswords(event.target.checked)}
                className="h-4 w-4 accent-primary"
              />
              {showPasswords ? <EyeOff aria-hidden="true" size={16} /> : <Eye aria-hidden="true" size={16} />}
              Mostrar contraseñas
            </label>

            {error && (
              <div role="alert" className="rounded-md border border-destructive/25 bg-destructive/10 p-3 text-sm text-destructive">
                {error}
              </div>
            )}

            <div className="flex flex-col-reverse gap-3 pt-1 sm:flex-row sm:justify-between">
              <Button type="button" variant="ghost" onClick={endSession} className="gap-2">
                <LogOut aria-hidden="true" size={16} />
                Cerrar sesión
              </Button>
              <Button type="submit" disabled={!canSubmit} className="min-w-44">
                {submitting ? 'Actualizando...' : 'Actualizar contraseña'}
              </Button>
            </div>
          </form>
        </div>
      </section>
    </main>
  )
}
