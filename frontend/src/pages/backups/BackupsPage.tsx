import { useState, useEffect, useCallback } from 'react'
import api from '@/lib/axios'
import type { BackupLog } from '@/types'
import { Button } from '@/components/ui/button'
import { Download, RotateCw, Loader2, Database } from 'lucide-react'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { getApiErrorMessage } from '@/lib/errors'

export default function BackupsPage() {
  const [logs, setLogs] = useState<BackupLog[]>([])
  const [loading, setLoading] = useState(false)
  const [fetching, setFetching] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const loadLogs = useCallback(async () => {
    setFetching(true)
    setError(null)
    try {
      const { data } = await api.get<BackupLog[]>('/backup/logs')
      setLogs(data)
    } catch (error: unknown) {
      console.error('Error cargando historial de respaldos', error)
      setError(getApiErrorMessage(error, 'No fue posible cargar el historial de respaldos.'))
    } finally {
      setFetching(false)
    }
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => void loadLogs(), 0)
    return () => window.clearTimeout(timer)
  }, [loadLogs])

  const createBackup = async () => {
    setLoading(true)
    setError(null)
    setSuccess(null)
    try {
      const response = await api.post('/backup/manual', {}, { responseType: 'blob' })
      const disposition = response.headers['content-disposition']
      let fileName = `hotel_erp_${new Date().toISOString().slice(0, 10)}.dump`
      if (disposition) {
        const match = disposition.match(/filename=(.+)/)
        if (match) fileName = match[1]
      }
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.setAttribute('download', fileName)
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)
      setSuccess(`Respaldo "${fileName}" generado y descargado exitosamente.`)
      loadLogs()
    } catch (error: unknown) {
      setError(getApiErrorMessage(error, 'Error al crear el respaldo de base de datos.'))
    } finally {
      setLoading(false)
    }
  }

  const formatSize = (bytes: number) => {
    if (bytes === 0) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <Database className="text-[#C69C4B]" size={24} />
            Respaldo de Base de Datos
          </h1>
          <p className="text-sm text-muted-foreground">
            Copias de seguridad de PostgreSQL y sincronización en la nube.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={loadLogs} disabled={fetching}>
            <RotateCw size={15} className={fetching ? 'animate-spin mr-1.5' : 'mr-1.5'} />
            Actualizar
          </Button>
          <Button onClick={createBackup} disabled={loading}>
            {loading ? (
              <Loader2 size={16} className="animate-spin mr-1.5" />
            ) : (
              <Download size={16} className="mr-1.5" />
            )}
            {loading ? 'Generando dump...' : 'Realizar Respaldo Ahora'}
          </Button>
        </div>
      </div>

      {error && (
        <InlineAlert
          variant="error"
          title="Error de respaldo"
          message={error}
          onClose={() => setError(null)}
        />
      )}

      {success && (
        <InlineAlert
          variant="success"
          title="Respaldo exitoso"
          message={success}
          onClose={() => setSuccess(null)}
        />
      )}

      <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
        <div className="p-4 border-b border-border/80 bg-muted/20 flex items-center justify-between">
          <h3 className="font-semibold text-sm text-foreground">Historial de Respaldos</h3>
          <span className="text-xs text-muted-foreground">{logs.length} registro(s)</span>
        </div>

        {fetching ? (
          <div className="flex items-center justify-center py-12 space-y-2">
            <Loader2 size={24} className="animate-spin text-[#C69C4B] mr-2" />
            <span className="text-sm text-muted-foreground">Cargando registros...</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/40 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3.5">Archivo</th>
                  <th className="text-left p-3.5">Fecha y Hora</th>
                  <th className="text-right p-3.5">Tamaño</th>
                  <th className="text-left p-3.5">Estado</th>
                  <th className="text-left p-3.5">Subido a Drive</th>
                  <th className="text-left p-3.5">Detalles</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {logs.map((log) => (
                  <tr key={log.id} className="hover:bg-accent/30 transition-colors">
                    <td className="p-3.5 font-mono text-xs text-foreground font-medium">{log.fileName}</td>
                    <td className="p-3.5 text-xs text-muted-foreground">
                      {new Date(log.startedAt).toLocaleString('es-HN')}
                    </td>
                    <td className="p-3.5 text-right font-mono text-xs">{formatSize(log.sizeBytes)}</td>
                    <td className="p-3.5">
                      <span
                        className={`px-2.5 py-0.5 rounded-full text-xs font-medium ${
                          log.status === 'CreatedLocal' || log.status === 'UploadedToDrive'
                            ? 'bg-emerald-100 dark:bg-emerald-950/40 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800'
                            : log.status === 'Failed' || log.status === 'UploadFailed'
                            ? 'bg-red-100 dark:bg-red-950/40 text-red-800 dark:text-red-300 border border-red-300 dark:border-red-800'
                            : 'bg-amber-100 dark:bg-amber-950/40 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-800'
                        }`}
                      >
                        {log.status}
                      </span>
                    </td>
                    <td className="p-3.5 text-xs text-muted-foreground">
                      {log.uploadedAt ? new Date(log.uploadedAt).toLocaleString('es-HN') : '—'}
                    </td>
                    <td className="p-3.5 text-red-600 dark:text-red-400 text-xs truncate max-w-xs">
                      {log.errorMessage || '—'}
                    </td>
                  </tr>
                ))}
                {logs.length === 0 && (
                  <tr>
                    <td colSpan={6} className="p-8 text-center text-muted-foreground">
                      No hay respaldos registrados en el sistema.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
