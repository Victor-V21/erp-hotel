import React from 'react'
import { AlertCircle, CheckCircle2, AlertTriangle, Info, X } from 'lucide-react'
import { cn } from '@/lib/utils'

export type AlertVariant = 'error' | 'success' | 'warning' | 'info'

interface InlineAlertProps {
  variant?: AlertVariant
  title?: string
  message: string | React.ReactNode
  onClose?: () => void
  className?: string
}

const variantStyles: Record<AlertVariant, { container: string; icon: React.ComponentType<{ className?: string; size?: number }> }> = {
  error: {
    container: 'bg-red-50 dark:bg-red-950/40 border-red-200 dark:border-red-800 text-red-900 dark:text-red-200',
    icon: AlertCircle,
  },
  success: {
    container: 'bg-emerald-50 dark:bg-emerald-950/40 border-emerald-200 dark:border-emerald-800 text-emerald-900 dark:text-emerald-200',
    icon: CheckCircle2,
  },
  warning: {
    container: 'bg-amber-50 dark:bg-amber-950/40 border-amber-200 dark:border-amber-800 text-amber-900 dark:text-amber-200',
    icon: AlertTriangle,
  },
  info: {
    container: 'bg-sky-50 dark:bg-sky-950/40 border-sky-200 dark:border-sky-800 text-sky-900 dark:text-sky-200',
    icon: Info,
  },
}

export function InlineAlert({
  variant = 'info',
  title,
  message,
  onClose,
  className,
}: InlineAlertProps) {
  const { container, icon: Icon } = variantStyles[variant]

  return (
    <div
      role="alert"
      className={cn(
        'relative flex items-start gap-3 p-3.5 rounded-lg border text-sm transition-all duration-200 shadow-sm animate-in fade-in-50',
        container,
        className
      )}
    >
      <Icon size={18} className="shrink-0 mt-0.5" />
      <div className="flex-1 space-y-0.5">
        {title && <p className="font-semibold leading-tight">{title}</p>}
        <div className="leading-relaxed opacity-95 text-xs sm:text-sm">{message}</div>
      </div>
      {onClose && (
        <button
          type="button"
          onClick={onClose}
          className="p-1 -mr-1 -mt-1 rounded-md opacity-70 hover:opacity-100 hover:bg-black/5 dark:hover:bg-white/5 transition-opacity cursor-pointer"
          aria-label="Cerrar notificación"
        >
          <X size={14} />
        </button>
      )}
    </div>
  )
}
