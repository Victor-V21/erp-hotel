import { Navigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { ShieldAlert, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { ReactNode } from 'react'

interface Props {
  children: ReactNode
  allowedRoles?: string[]
  requiredPermission?: string | string[]
}

export default function ProtectedRoute({ children, allowedRoles, requiredPermission }: Props) {
  const { isAuthenticated, hasRole, hasPermission } = useAuthStore()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (allowedRoles && allowedRoles.length > 0 && !hasRole(allowedRoles)) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] text-center p-6 space-y-4">
        <div className="p-4 rounded-full bg-destructive/10 text-destructive">
          <ShieldAlert className="w-12 h-12 text-[#C69C4B]" />
        </div>
        <div className="space-y-1">
          <h2 className="text-2xl font-bold text-foreground">Acceso Restringido</h2>
          <p className="text-sm text-muted-foreground max-w-md">
            Su usuario no dispone de los permisos ni el rol necesario ({allowedRoles.join(', ')}) para acceder a este módulo.
          </p>
        </div>
        <Button onClick={() => window.location.href = '/dashboard'} variant="outline" className="gap-2">
          <ArrowLeft size={16} /> Volver al Dashboard
        </Button>
      </div>
    )
  }

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh] text-center p-6 space-y-4">
        <div className="p-4 rounded-full bg-destructive/10 text-destructive">
          <ShieldAlert className="w-12 h-12 text-[#C69C4B]" />
        </div>
        <div className="space-y-1">
          <h2 className="text-2xl font-bold text-foreground">Permiso Insuficiente</h2>
          <p className="text-sm text-muted-foreground max-w-md">
            Se requiere una autorización especial del sistema para realizar acciones en esta vista.
          </p>
        </div>
        <Button onClick={() => window.location.href = '/dashboard'} variant="outline" className="gap-2">
          <ArrowLeft size={16} /> Volver al Dashboard
        </Button>
      </div>
    )
  }

  return <>{children}</>
}

