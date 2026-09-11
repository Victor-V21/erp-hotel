import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Hotel, Eye, EyeOff } from 'lucide-react'
import axios from 'axios'
import api from '@/lib/axios'
import type { LoginRequest, AuthResponse } from '@/types'
import { getApiErrorMessage } from '@/lib/errors'

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const setAuth = useAuthStore((s) => s.setAuth)
  const [form, setForm] = useState<LoginRequest>({ username: '', password: '' })
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')

    try {
      const { data } = await api.post<AuthResponse>('/auth/login', form)
      if (data.success && data.user && data.accessToken) {
        setAuth(data.user, data.accessToken)
        navigate(data.user.mustChangePassword ? '/change-password' : '/dashboard', { replace: true })
      } else {
        setError(data.message || 'Error al iniciar sesión')
      }
    } catch (err) {
      if (axios.isAxiosError(err)) {
        setError(getApiErrorMessage(err, 'Credenciales inválidas'))
      } else {
        setError('Credenciales inválidas')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/30">
      <div className="w-full max-w-md p-8 space-y-6 bg-card rounded-xl shadow-lg border border-border">
        <div className="text-center space-y-2">
          <div className="flex justify-center">
            <div className="p-3 rounded-full bg-primary/10">
              <Hotel className="w-8 h-8 text-primary" />
            </div>
          </div>
          <h1 className="text-2xl font-bold text-foreground">Hotel Maya Central</h1>
          <p className="text-sm text-muted-foreground">
            Sistema de Gestión Hotelera y Facturación SAR
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {location.state?.passwordChanged && (
            <div role="status" className="rounded-md border border-emerald-600/25 bg-emerald-600/10 p-3 text-sm text-emerald-800 dark:text-emerald-300">
              Contraseña actualizada. Inicie sesión nuevamente para continuar.
            </div>
          )}
          <div className="space-y-2">
            <label className="text-sm font-medium">Usuario</label>
            <Input
              type="text"
              placeholder="Ingrese su usuario"
              value={form.username}
              onChange={(e) => setForm({ ...form, username: e.target.value })}
              required
            />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">Contraseña</label>
            <div className="relative">
              <Input
                type={showPassword ? 'text' : 'password'}
                placeholder="Ingrese su contraseña"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                required
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground cursor-pointer"
              >
                {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>

          {error && (
            <div className="p-3 rounded-md bg-destructive/10 text-destructive text-sm">
              {error}
            </div>
          )}

          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? 'Iniciando sesión...' : 'Iniciar Sesión'}
          </Button>
        </form>

        <p className="text-xs text-center text-muted-foreground">
          © {new Date().getFullYear()} Hotel Maya Central · Honduras
        </p>
      </div>
    </div>
  )
}
