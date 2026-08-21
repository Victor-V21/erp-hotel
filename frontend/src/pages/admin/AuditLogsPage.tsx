import { useState, useEffect, useCallback, useMemo } from 'react'
import api from '@/lib/axios'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { Pagination } from '@/components/ui/Pagination'
import {
  ShieldCheck,
  Search,
  RefreshCw,
  Clock,
  User,
  Activity,
  FileCode,
  CheckCircle2,
  AlertTriangle,
  Loader2,
  X,
  Hash,
  Filter,
} from 'lucide-react'

interface AuditLog {
  id: string
  userId: string | null
  userName: string | null
  userFullName: string
  action: string
  entityName: string | null
  entityId: string | null
  correlativeNumber: string | null
  paymentMethod: string | null
  previousHash: string | null
  hash: string | null
  changes: string | null
  timestamp: string
  hondurasTimestamp: string
}

interface IntegrityResult {
  isValid: boolean
  totalRecordsVerified: number
  verifiedAt: string
  errorMessage?: string | null
  brokenLogId?: string | null
}

export default function AuditLogsPage() {
  const [logs, setLogs] = useState<AuditLog[]>([])
  const [loading, setLoading] = useState(true)
  const [verifying, setVerifying] = useState(false)
  const [integrityResult, setIntegrityResult] = useState<IntegrityResult | null>(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedAction, setSelectedAction] = useState('')
  const [inspectLog, setInspectLog] = useState<AuditLog | null>(null)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)

  // Pagination
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)

  const loadLogs = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await api.get<AuditLog[]>('/audit-logs')
      setLogs(data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al consultar el registro de auditoría.' })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadLogs()
  }, [loadLogs])

  const verifyIntegrity = async () => {
    setVerifying(true)
    try {
      const { data } = await api.get<IntegrityResult>('/audit-logs/verify-integrity')
      setIntegrityResult(data)
      if (data.isValid) {
        setAlertInfo({
          variant: 'success',
          message: `Cadena criptográfica íntegra. Se verificaron ${data.totalRecordsVerified} transacciones con hash SHA-256 sin discrepancias.`,
        })
      } else {
        setAlertInfo({
          variant: 'error',
          message: `¡Alerta de Integridad! ${data.errorMessage || 'Se detectó una discrepancia en la cadena de firmas.'}`,
        })
      }
    } catch {
      setAlertInfo({ variant: 'error', message: 'No se pudo completar la verificación criptográfica.' })
    } finally {
      setVerifying(false)
    }
  }

  // Filter list
  const filteredLogs = useMemo(() => {
    return logs.filter((log) => {
      const matchesSearch =
        searchTerm === '' ||
        log.action.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (log.entityName && log.entityName.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (log.correlativeNumber && log.correlativeNumber.toLowerCase().includes(searchTerm.toLowerCase())) ||
        log.userFullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (log.userName && log.userName.toLowerCase().includes(searchTerm.toLowerCase())) ||
        (log.hash && log.hash.toLowerCase().includes(searchTerm.toLowerCase()))

      const matchesAction = selectedAction === '' || log.action === selectedAction
      return matchesSearch && matchesAction
    })
  }, [logs, searchTerm, selectedAction])

  const distinctActions = useMemo(() => {
    return Array.from(new Set(logs.map((l) => l.action))).filter(Boolean)
  }, [logs])

  const totalPages = Math.ceil(filteredLogs.length / pageSize) || 1
  const paginatedLogs = useMemo(() => {
    const start = (currentPage - 1) * pageSize
    return filteredLogs.slice(start, start + pageSize)
  }, [filteredLogs, currentPage, pageSize])

  const formatJson = (raw: string | null) => {
    if (!raw) return 'Sin cambios detallados'
    try {
      const parsed = JSON.parse(raw)
      return JSON.stringify(parsed, null, 2)
    } catch {
      return raw
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <ShieldCheck className="text-[#C69C4B]" size={24} />
            Bitácora de Auditoría & Trazabilidad
          </h1>
          <p className="text-sm text-muted-foreground">
            Registro inmutable de transacciones, eventos del sistema y verificación de integridad criptográfica SHA-256.
          </p>
        </div>
        <div className="flex items-center gap-2.5">
          <Button variant="outline" onClick={loadLogs} disabled={loading} className="gap-1.5">
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            Actualizar
          </Button>
          <Button
            onClick={verifyIntegrity}
            disabled={verifying}
            className="gap-1.5 bg-[#C69C4B] hover:bg-[#b0883b] text-white"
          >
            {verifying ? <Loader2 size={14} className="animate-spin" /> : <Hash size={14} />}
            Verificar Integridad SHA-256
          </Button>
        </div>
      </div>

      {alertInfo && (
        <InlineAlert variant={alertInfo.variant} message={alertInfo.message} onClose={() => setAlertInfo(null)} />
      )}

      {/* Integrity Status Card */}
      {integrityResult && (
        <div
          className={`p-4 rounded-xl border flex items-center justify-between gap-4 shadow-xs animate-in fade-in ${
            integrityResult.isValid
              ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-900 dark:text-emerald-200'
              : 'border-red-500/30 bg-red-500/10 text-red-900 dark:text-red-200'
          }`}
        >
          <div className="flex items-center gap-3">
            {integrityResult.isValid ? (
              <CheckCircle2 size={24} className="text-emerald-600 dark:text-emerald-400 shrink-0" />
            ) : (
              <AlertTriangle size={24} className="text-red-600 dark:text-red-400 shrink-0" />
            )}
            <div>
              <p className="font-bold text-sm">
                {integrityResult.isValid
                  ? 'Cadena Criptográfica Íntegra y Válida'
                  : '¡Advertencia! Posible Alteración Detectada'}
              </p>
              <p className="text-xs opacity-90">
                {integrityResult.isValid
                  ? `${integrityResult.totalRecordsVerified} registros verificados secuencialmente. Ningún dato ha sido alterado fuera del flujo oficial.`
                  : integrityResult.errorMessage}
              </p>
            </div>
          </div>
          <span className="text-[11px] font-mono opacity-80 shrink-0">
            {new Date(integrityResult.verifiedAt).toLocaleTimeString()}
          </span>
        </div>
      )}

      {/* Filter and Search Bar */}
      <div className="grid grid-cols-1 sm:grid-cols-12 gap-3 p-4 rounded-xl border border-border bg-card shadow-xs">
        <div className="sm:col-span-8 relative">
          <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Buscar por usuario, acción, entidad, correlativo o hash SHA-256..."
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value)
              setCurrentPage(1)
            }}
            className="pl-8 text-xs h-9"
          />
        </div>
        <div className="sm:col-span-4">
          <select
            value={selectedAction}
            onChange={(e) => {
              setSelectedAction(e.target.value)
              setCurrentPage(1)
            }}
            className="w-full border border-input rounded-md px-3 py-2 text-xs bg-background h-9"
          >
            <option value="">Todas las acciones ({distinctActions.length})</option>
            {distinctActions.map((act) => (
              <option key={act} value={act}>
                {act}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Audit Logs Table */}
      <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
        <div className="p-3.5 border-b border-border bg-muted/20 flex items-center justify-between">
          <h3 className="font-semibold text-xs text-foreground uppercase tracking-wider">
            Eventos Registrados
          </h3>
          <span className="text-xs text-muted-foreground">{filteredLogs.length} registro(s)</span>
        </div>

        {loading ? (
          <div className="flex items-center justify-center py-16 space-y-2">
            <Loader2 size={26} className="animate-spin text-[#C69C4B] mr-2" />
            <span className="text-xs text-muted-foreground">Cargando eventos de auditoría...</span>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3.5 w-40">Fecha & Hora</th>
                  <th className="text-left p-3.5 w-44">Usuario</th>
                  <th className="text-left p-3.5">Acción</th>
                  <th className="text-left p-3.5">Entidad / Módulo</th>
                  <th className="text-left p-3.5">Referencia</th>
                  <th className="text-left p-3.5 w-32">Hash SHA-256</th>
                  <th className="text-right p-3.5 w-20">Detalle</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {paginatedLogs.map((log) => (
                  <tr key={log.id} className="hover:bg-accent/20 transition-colors">
                    <td className="p-3.5 font-mono text-muted-foreground whitespace-nowrap">
                      {new Date(log.timestamp).toLocaleString('es-HN', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                        second: '2-digit',
                      })}
                    </td>
                    <td className="p-3.5 font-medium text-foreground">
                      <div className="flex items-center gap-1.5">
                        <User size={13} className="text-[#C69C4B]" />
                        <span>{log.userFullName}</span>
                      </div>
                      {log.userName && (
                        <span className="text-[10px] text-muted-foreground">@{log.userName}</span>
                      )}
                    </td>
                    <td className="p-3.5">
                      <span className="px-2 py-0.5 rounded-md text-[11px] font-bold bg-primary/10 text-foreground border border-primary/20">
                        {log.action}
                      </span>
                    </td>
                    <td className="p-3.5 text-foreground font-medium">{log.entityName || '—'}</td>
                    <td className="p-3.5 font-mono text-muted-foreground">
                      {log.correlativeNumber || log.paymentMethod || '—'}
                    </td>
                    <td className="p-3.5 font-mono text-[10px] text-muted-foreground truncate max-w-[120px]" title={log.hash || ''}>
                      {log.hash ? `${log.hash.slice(0, 10)}...` : '—'}
                    </td>
                    <td className="p-3.5 text-right">
                      {log.changes ? (
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => setInspectLog(log)}
                          className="h-7 text-xs gap-1"
                        >
                          <FileCode size={12} /> Ver
                        </Button>
                      ) : (
                        <span className="text-muted-foreground text-[11px]">—</span>
                      )}
                    </td>
                  </tr>
                ))}
                {paginatedLogs.length === 0 && (
                  <tr>
                    <td colSpan={7} className="p-8 text-center text-muted-foreground">
                      No se encontraron registros de auditoría con los filtros actuales.
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
          totalItems={filteredLogs.length}
          pageSize={pageSize}
          onPageChange={setCurrentPage}
          onPageSizeChange={(size) => {
            setPageSize(size)
            setCurrentPage(1)
          }}
        />
      </div>

      {/* INSPECT LOG JSON MODAL */}
      {inspectLog && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in"
          onClick={() => setInspectLog(null)}
        >
          <div
            className="bg-card border border-border rounded-xl p-6 w-full max-w-2xl shadow-2xl space-y-4 animate-in zoom-in-95 max-h-[85vh] overflow-y-auto"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between border-b border-border pb-3">
              <div className="flex items-center gap-2">
                <FileCode className="text-[#C69C4B]" size={22} />
                <h3 className="text-lg font-bold text-foreground">
                  Inspección de Transacción ({inspectLog.action})
                </h3>
              </div>
              <button
                onClick={() => setInspectLog(null)}
                className="p-1 rounded-md text-muted-foreground hover:text-foreground hover:bg-accent cursor-pointer"
              >
                <X size={18} />
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div className="grid grid-cols-2 gap-2 p-3 bg-muted/20 rounded-lg border border-border">
                <div>
                  <strong>Usuario:</strong> {inspectLog.userFullName} ({inspectLog.userName || 'Sistema'})
                </div>
                <div>
                  <strong>Fecha:</strong> {new Date(inspectLog.timestamp).toLocaleString('es-HN')}
                </div>
                <div>
                  <strong>Entidad:</strong> {inspectLog.entityName}
                </div>
                <div>
                  <strong>Referencia:</strong> {inspectLog.correlativeNumber || 'N/A'}
                </div>
                <div className="col-span-2 truncate font-mono text-[11px]">
                  <strong>Hash:</strong> {inspectLog.hash}
                </div>
                <div className="col-span-2 truncate font-mono text-[11px] text-muted-foreground">
                  <strong>Hash Previo:</strong> {inspectLog.previousHash || 'Génesis'}
                </div>
              </div>

              <div>
                <p className="font-semibold text-muted-foreground mb-1.5">Contenido / Cambios Serializados:</p>
                <pre className="p-3.5 rounded-lg bg-black/80 text-emerald-400 font-mono text-xs overflow-x-auto max-h-60 leading-relaxed border border-border">
                  {formatJson(inspectLog.changes)}
                </pre>
              </div>
            </div>

            <div className="flex justify-end pt-2">
              <Button onClick={() => setInspectLog(null)}>Cerrar</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
