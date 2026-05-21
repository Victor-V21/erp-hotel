import { useState, useEffect } from 'react'
import api from '@/lib/axios'
import { Button } from '@/components/ui/button'
import { Download, RotateCw } from 'lucide-react'

interface BackupLog {
  id: string
  startedAt: string
  completedAt: string | null
  fileName: string
  sizeBytes: number
  sha256Hash: string | null
  status: string
  uploadedAt: string | null
  uploadAttempts: number
  errorMessage: string | null
}

export default function BackupsPage() {
  const [logs, setLogs] = useState<BackupLog[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => { loadLogs() }, [])

  const loadLogs = async () => {
    const { data } = await api.get<BackupLog[]>('/backup/logs')
    setLogs(data)
  }

  const createBackup = async () => {
    setLoading(true)
    setError('')
    setSuccess('')
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
      setSuccess(`Respaldo "${fileName}" descargado exitosamente`)
      loadLogs()
    } catch (e: any) {
      setError(e.response?.data?.message || e.message || 'Error al crear respaldo')
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
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Respaldo de Base de Datos</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={loadLogs}><RotateCw size={16} className="mr-1" /> Actualizar</Button>
          <Button onClick={createBackup} disabled={loading}>
            <Download size={16} className="mr-1" />
            {loading ? 'Respaldando...' : 'Realizar Respaldo Ahora'}
          </Button>
        </div>
      </div>

      {error && <div className="p-3 bg-red-50 border border-red-200 rounded text-sm text-red-800">{error}</div>}
      {success && <div className="p-3 bg-green-50 border border-green-200 rounded text-sm text-green-800">{success}</div>}

      <div className="border border-border rounded-lg overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted"><tr>
            <th className="text-left p-3">Archivo</th>
            <th className="text-left p-3">Fecha</th>
            <th className="text-right p-3">Tamaño</th>
            <th className="text-left p-3">Estado</th>
            <th className="text-left p-3">Subido a Drive</th>
            <th className="text-left p-3">Error</th>
          </tr></thead>
          <tbody>
            {logs.map(log => (
              <tr key={log.id} className="border-t border-border">
                <td className="p-3 font-mono text-xs">{log.fileName}</td>
                <td className="p-3">{new Date(log.startedAt).toLocaleString()}</td>
                <td className="p-3 text-right">{formatSize(log.sizeBytes)}</td>
                <td className="p-3">
                  <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                    log.status === 'CreatedLocal' || log.status === 'UploadedToDrive' ? 'bg-green-100 text-green-800' :
                    log.status === 'Failed' || log.status === 'UploadFailed' ? 'bg-red-100 text-red-800' :
                    'bg-yellow-100 text-yellow-800'
                  }`}>{log.status}</span>
                </td>
                <td className="p-3">{log.uploadedAt ? new Date(log.uploadedAt).toLocaleString() : '-'}</td>
                <td className="p-3 text-red-600 text-xs">{log.errorMessage || '-'}</td>
              </tr>
            ))}
            {logs.length === 0 && <tr><td colSpan={6} className="p-6 text-center text-muted-foreground">No hay respaldos registrados</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
