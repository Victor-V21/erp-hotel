import React, { useState } from 'react'
import { AlertTriangle, Loader2, X } from 'lucide-react'
import { Button } from './button'

interface ConfirmDialogProps {
  isOpen: boolean
  title: string
  description: string | React.ReactNode
  confirmText?: string
  cancelText?: string
  variant?: 'destructive' | 'default'
  onConfirm: () => Promise<void> | void
  onCancel: () => void
}

export function ConfirmDialog({
  isOpen,
  title,
  description,
  confirmText = 'Confirmar',
  cancelText = 'Cancelar',
  variant = 'destructive',
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const [loading, setLoading] = useState(false)

  if (!isOpen) return null

  const handleConfirm = async () => {
    try {
      setLoading(true)
      await onConfirm()
    } finally {
      setLoading(false)
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in duration-200"
      onClick={onCancel}
    >
      <div
        className="relative w-full max-w-md bg-card border border-border rounded-xl p-6 shadow-2xl space-y-4 animate-in zoom-in-95 duration-150"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
      >
        <button
          type="button"
          onClick={onCancel}
          className="absolute right-4 top-4 p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent transition-colors cursor-pointer"
        >
          <X size={18} />
        </button>

        <div className="flex items-start gap-3.5">
          <div
            className={`p-2.5 rounded-full shrink-0 ${
              variant === 'destructive'
                ? 'bg-destructive/10 text-destructive'
                : 'bg-primary/10 text-primary'
            }`}
          >
            <AlertTriangle size={22} className={variant === 'destructive' ? 'text-destructive' : 'text-[#C69C4B]'} />
          </div>
          <div className="space-y-1 pr-6">
            <h3 id="confirm-dialog-title" className="text-lg font-semibold text-foreground">
              {title}
            </h3>
            <div className="text-sm text-muted-foreground leading-relaxed">
              {description}
            </div>
          </div>
        </div>

        <div className="flex items-center justify-end gap-2.5 pt-3 border-t border-border">
          <Button
            type="button"
            variant="outline"
            onClick={onCancel}
            disabled={loading}
            className="cursor-pointer"
          >
            {cancelText}
          </Button>
          <Button
            type="button"
            variant={variant === 'destructive' ? 'destructive' : 'default'}
            onClick={handleConfirm}
            disabled={loading}
            className="cursor-pointer"
          >
            {loading && <Loader2 size={16} className="animate-spin mr-1.5" />}
            {confirmText}
          </Button>
        </div>
      </div>
    </div>
  )
}
