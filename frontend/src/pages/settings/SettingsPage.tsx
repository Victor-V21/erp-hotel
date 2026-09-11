import { useState, useEffect, useRef, useCallback } from 'react'
import axios from 'axios'
import api from '@/lib/axios'
import type { BusinessSettings } from '@/types'
import { useAuthStore } from '@/store/authStore'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { Archive, CheckCircle2, Loader2, ShieldAlert } from 'lucide-react'

const PAPER_PRESETS = [
  { label: '58mm', width: 34 }, { label: '76mm', width: 40 },
  { label: '80mm', width: 46 }, { label: '80mm+', width: 56 },
]

const defaults: BusinessSettings = {
  businessName: '', rtn: '', address: '', phone: '', email: '', footer: '',
  isvRate: 0, touristTaxRate: 0, fiscalProfileStatus: 'Borrador', fiscalProfileVersion: 1,
  printPrinterName: '',
  printWidth: 46, printLogoHeight: 40, printFontSize: 'condensed', printLineSpacing: 1,
  showLogo: true, showHeader: true, showFiscal: false, showGuest: true,
  showItems: true, showTotals: true, showPayment: true, showFooter: true,
  headerAlign: 'center', separatorChar: '-', marginLeft: 0
}

type SectionKey = 'showLogo' | 'showHeader' | 'showFiscal' | 'showGuest' | 'showItems' | 'showTotals' | 'showPayment' | 'showFooter'

const SECTIONS: { key: SectionKey; label: string }[] = [
  { key: 'showLogo', label: 'Logo' },
  { key: 'showHeader', label: 'Encabezado' },
  { key: 'showFiscal', label: 'Info. Fiscal' },
  { key: 'showGuest', label: 'Huésped' },
  { key: 'showItems', label: 'Items' },
  { key: 'showTotals', label: 'Totales' },
  { key: 'showPayment', label: 'Pago' },
  { key: 'showFooter', label: 'Pie' },
]

type ApiErrorBody = { message?: string; detail?: string; errors?: string[] | Record<string, string[]> }

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<ApiErrorBody>(error)) {
    const data = error.response?.data
    if (Array.isArray(data?.errors) && data.errors.length) {
      return `${data.message || data.detail || 'Revise los datos'}: ${data.errors.join(' ')}`
    }
    if (data?.errors && !Array.isArray(data.errors)) {
      const validationMessages = Object.values(data.errors).flat()
      if (validationMessages.length) return validationMessages.join(' ')
    }
    return data?.message || data?.detail || error.message
  }
  return error instanceof Error ? error.message : 'Error desconocido'
}

const todayInHonduras = () => {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: 'America/Tegucigalpa',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(new Date())
  const value = Object.fromEntries(parts.map(part => [part.type, part.value]))
  return `${value.year}-${value.month}-${value.day}`
}

const fiscalSignature = (settings: BusinessSettings) => JSON.stringify([
  settings.businessName.trim(),
  settings.rtn.trim(),
  settings.address.trim(),
  settings.isvRate,
  settings.touristTaxRate,
])

export default function SettingsPage() {
  const hasPermission = useAuthStore(state => state.hasPermission)
  const [s, setS] = useState<BusinessSettings>(defaults)
  const [savedFiscalSignature, setSavedFiscalSignature] = useState<string | null>(null)
  const [logoPreview, setLogoPreview] = useState('')
  const [printers, setPrinters] = useState<string[]>([])
  const [printerTest, setPrinterTest] = useState<{success: boolean; message: string} | null>(null)
  const [testing, setTesting] = useState(false)
  const [testPreview, setTestPreview] = useState<string | null>(null)
  const [testLogo, setTestLogo] = useState<string | null>(null)
  const [loadingPreview, setLoadingPreview] = useState(false)
  const [printResult, setPrintResult] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [saveSuccess, setSaveSuccess] = useState(false)
  const [operationAlert, setOperationAlert] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [validFrom, setValidFrom] = useState(todayInHonduras())
  const [validUntil, setValidUntil] = useState('')
  const [approvalNote, setApprovalNote] = useState('')
  const [retirementReason, setRetirementReason] = useState('')
  const [fiscalActionLoading, setFiscalActionLoading] = useState(false)
  const [confirmRetirement, setConfirmRetirement] = useState(false)
  const [customSlider, setCustomSlider] = useState(false)
  const [observedWidth, setObservedWidth] = useState(46)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  const fetchSettings = useCallback(async () => {
    try {
      const { data } = await api.get<BusinessSettings>('/settings/business')
      const isvRate = data.isvRate > 1 ? data.isvRate / 100 : data.isvRate
      const touristTaxRate = data.touristTaxRate > 1 ? data.touristTaxRate / 100 : data.touristTaxRate
      const normalizedSettings = { ...defaults, ...data, isvRate, touristTaxRate }
      setS(normalizedSettings)
      setSavedFiscalSignature(fiscalSignature(normalizedSettings))
      setLogoPreview(data.logoBase64 || '')
      setValidFrom(data.fiscalValidFrom || todayInHonduras())
      setValidUntil(data.fiscalValidUntil || '')
      setApprovalNote(data.fiscalApprovalNote || '')
      setRetirementReason(data.fiscalRetirementReason || '')
      setCustomSlider(!PAPER_PRESETS.some(p => p.width === (data.printWidth ?? 46)))
    } catch (error: unknown) {
      setPrintResult(`Error al cargar configuración: ${getErrorMessage(error)}`)
    }
  }, [])

  const loadPreview = useCallback(async () => {
    setLoadingPreview(true)
    try {
      const { data } = await api.get('/print/test-preview', { params: { width: s.printWidth } })
      setTestPreview(data.text); setTestLogo(data.logoBase64 || null)
    } catch (error: unknown) {
      setPrintResult(`Error al generar vista previa: ${getErrorMessage(error)}`)
    }
    finally { setLoadingPreview(false) }
  }, [s.printWidth])

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void fetchSettings()
      void api.get<string[]>('/print/printers')
        .then(({ data }) => setPrinters(data))
        .catch((error: unknown) => setPrintResult(`Error al consultar impresoras: ${getErrorMessage(error)}`))
    }, 0)
    return () => window.clearTimeout(timer)
  }, [fetchSettings])

  // Auto-preview con debounce de 500ms cuando cambian parámetros
  useEffect(() => {
    if (timerRef.current) clearTimeout(timerRef.current)
    timerRef.current = setTimeout(loadPreview, 500)
    return () => { if (timerRef.current) clearTimeout(timerRef.current) }
  }, [s.printWidth, s.printFontSize, s.printLineSpacing, s.showLogo, s.showHeader, s.showFiscal, s.showGuest, s.showItems, s.showTotals, s.showPayment, s.showFooter, s.headerAlign, s.separatorChar, s.marginLeft, loadPreview])

  const toggleSection = (key: SectionKey) => setS(prev => ({ ...prev, [key]: !prev[key] }))

  const handleLogo = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]; if (!file) return
    const reader = new FileReader()
    reader.onload = () => setLogoPreview(reader.result as string)
    reader.readAsDataURL(file)
  }

  const testPrinter = async () => {
    if (!s.printPrinterName) return
    setTesting(true); setPrinterTest(null)
    try { const { data } = await api.get('/print/test', { params: { name: s.printPrinterName } }); setPrinterTest(data) }
    catch { setPrinterTest({ success: false, message: 'Error de conexión' }) }
    finally { setTesting(false) }
  }

  const printRuler = async () => {
    setPrintResult(null)
    try { const { data } = await api.post('/print/test-ruler'); setPrintResult(data.message) }
    catch (e: unknown) { setPrintResult('Error: ' + getErrorMessage(e)) }
  }

  const printTest = async () => {
    setPrintResult(null)
    try { const { data } = await api.post(`/print/test-print?width=${s.printWidth}`); setPrintResult(data.message || 'OK') }
    catch (e: unknown) { setPrintResult('Error: ' + getErrorMessage(e)) }
  }

  const save = async () => {
    setSaving(true)
    setSaveSuccess(false)
    setOperationAlert(null)
    try {
      const { data } = await api.put<BusinessSettings>('/settings/business', { ...s, logoBase64: logoPreview || null })
      setS(prev => ({ ...prev, ...data }))
      setSavedFiscalSignature(fiscalSignature(data))
      setSaveSuccess(true)
      setTimeout(() => setSaveSuccess(false), 3000)
    }
    catch (error: unknown) {
      setOperationAlert({ variant: 'error', message: `No se pudo guardar: ${getErrorMessage(error)}` })
    }
    finally { setSaving(false) }
  }

  const approveFiscalProfile = async () => {
    if (approvalNote.trim().length < 10) {
      setOperationAlert({ variant: 'error', message: 'La nota de aprobación debe explicar la revisión realizada (mínimo 10 caracteres).' })
      return
    }

    setFiscalActionLoading(true)
    setOperationAlert(null)
    try {
      const { data } = await api.post<BusinessSettings>('/settings/business/approve', {
        validFrom,
        validUntil: validUntil || null,
        approvalNote: approvalNote.trim(),
      })
      setS(prev => ({ ...prev, ...data }))
      setOperationAlert({ variant: 'success', message: `Perfil fiscal v${data.fiscalProfileVersion} aprobado. La emisión fiscal está habilitada durante su vigencia.` })
    } catch (error: unknown) {
      setOperationAlert({ variant: 'error', message: `No se pudo aprobar el perfil: ${getErrorMessage(error)}` })
    } finally {
      setFiscalActionLoading(false)
    }
  }

  const retireFiscalProfile = async () => {
    if (retirementReason.trim().length < 10) {
      setConfirmRetirement(false)
      setOperationAlert({ variant: 'error', message: 'Indique un motivo de retiro de al menos 10 caracteres.' })
      return
    }

    setOperationAlert(null)
    try {
      const { data } = await api.post<BusinessSettings>('/settings/business/retire', {
        reason: retirementReason.trim(),
      })
      setS(prev => ({ ...prev, ...data }))
      setConfirmRetirement(false)
      setOperationAlert({ variant: 'success', message: `Perfil fiscal v${data.fiscalProfileVersion} retirado. La emisión fiscal quedó bloqueada.` })
    } catch (error: unknown) {
      setConfirmRetirement(false)
      setOperationAlert({ variant: 'error', message: `No se pudo retirar el perfil: ${getErrorMessage(error)}` })
    }
  }

  const presetActive = PAPER_PRESETS.find(p => p.width === s.printWidth)
  const w = s.printWidth || 46
  const canManageTaxes = hasPermission('manage_taxes')
  const fiscalApproved = s.fiscalProfileStatus === 'Aprobado'
  const fiscalHasUnsavedChanges = savedFiscalSignature !== fiscalSignature(s)
  const fiscalMissingFields = [
    !s.businessName.trim() && 'nombre del negocio',
    !/^\d{14}$/.test(s.rtn.trim()) && 'RTN de 14 dígitos',
    !s.address.trim() && 'dirección',
  ].filter(Boolean) as string[]
  const fiscalStatusDescription = fiscalApproved
    ? 'El perfil está aprobado; la emisión se habilita únicamente durante la vigencia indicada.'
    : s.fiscalProfileStatus === 'Retirado'
      ? 'La emisión fiscal está bloqueada hasta guardar y aprobar una nueva versión.'
      : 'La emisión fiscal está bloqueada hasta completar y aprobar este perfil.'

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Configuración del sistema</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Datos fiscales del hotel, habilitación de emisión y ajustes de impresión local.
        </p>
      </div>

      {operationAlert && (
        <InlineAlert
          variant={operationAlert.variant}
          message={operationAlert.message}
          onClose={() => setOperationAlert(null)}
        />
      )}

      <section className="border border-border rounded-xl bg-card overflow-hidden" aria-labelledby="fiscal-profile-title">
        <div className="p-4 sm:p-5 border-b border-border bg-muted/20">
          <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
            <div className="flex items-start gap-3">
              <div className={`p-2.5 rounded-lg ${fiscalApproved ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' : 'bg-amber-500/10 text-amber-700 dark:text-amber-400'}`}>
                {fiscalApproved ? <CheckCircle2 size={20} /> : <ShieldAlert size={20} />}
              </div>
              <div>
                <h2 id="fiscal-profile-title" className="font-semibold text-foreground">Perfil fiscal SAR</h2>
                <p className="text-sm text-muted-foreground mt-0.5">{fiscalStatusDescription}</p>
              </div>
            </div>
            <span className={`self-start rounded-full px-2.5 py-1 text-xs font-semibold ${fiscalApproved ? 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300' : s.fiscalProfileStatus === 'Retirado' ? 'bg-destructive/10 text-destructive' : 'bg-amber-500/10 text-amber-700 dark:text-amber-300'}`}>
              {s.fiscalProfileStatus} · v{s.fiscalProfileVersion}
            </span>
          </div>
        </div>

        <div className="p-4 sm:p-5 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label htmlFor="fiscal-valid-from" className="text-sm font-medium">Vigente desde</label>
              <Input
                id="fiscal-valid-from"
                type="date"
                value={validFrom}
                onChange={event => setValidFrom(event.target.value)}
                disabled={!canManageTaxes || fiscalActionLoading}
                className="mt-1"
              />
            </div>
            <div>
              <label htmlFor="fiscal-valid-until" className="text-sm font-medium">Vigente hasta</label>
              <Input
                id="fiscal-valid-until"
                type="date"
                value={validUntil}
                min={validFrom}
                onChange={event => setValidUntil(event.target.value)}
                disabled={!canManageTaxes || fiscalActionLoading}
                className="mt-1"
              />
              <p className="text-xs text-muted-foreground mt-1">Déjelo vacío si el perfil no tiene fecha final.</p>
            </div>
          </div>

          <div>
            <label htmlFor="fiscal-approval-note" className="text-sm font-medium">Nota de revisión y aprobación</label>
            <textarea
              id="fiscal-approval-note"
              value={approvalNote}
              onChange={event => setApprovalNote(event.target.value)}
              disabled={!canManageTaxes || fiscalActionLoading}
              rows={3}
              maxLength={500}
              placeholder="Documentos revisados, responsable y alcance de la validación..."
              className="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-xs outline-none transition-colors focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/30 disabled:cursor-not-allowed disabled:opacity-50"
            />
          </div>

          <div className="flex flex-col sm:flex-row gap-2 sm:items-center">
            <Button
              type="button"
              onClick={approveFiscalProfile}
              disabled={!canManageTaxes || fiscalActionLoading || saving || fiscalHasUnsavedChanges || fiscalMissingFields.length > 0}
              className="gap-2"
            >
              {fiscalActionLoading ? <Loader2 size={16} className="animate-spin" /> : <CheckCircle2 size={16} />}
              {fiscalApproved ? 'Renovar aprobación' : 'Aprobar perfil fiscal'}
            </Button>
            {!canManageTaxes && (
              <p className="text-xs text-muted-foreground">Su usuario puede consultar el estado, pero no aprobarlo.</p>
            )}
            {canManageTaxes && fiscalHasUnsavedChanges && (
              <p className="text-xs text-amber-700 dark:text-amber-300">Guarde los datos del negocio antes de aprobar esta versión.</p>
            )}
            {canManageTaxes && !fiscalHasUnsavedChanges && fiscalMissingFields.length > 0 && (
              <p className="text-xs text-amber-700 dark:text-amber-300">Complete {fiscalMissingFields.join(', ')} para habilitar la aprobación.</p>
            )}
          </div>

          {fiscalApproved && canManageTaxes && (
            <div className="pt-4 border-t border-border space-y-3">
              <div>
                <label htmlFor="fiscal-retirement-reason" className="text-sm font-medium">Motivo para retirar la versión activa</label>
                <Input
                  id="fiscal-retirement-reason"
                  value={retirementReason}
                  onChange={event => setRetirementReason(event.target.value)}
                  maxLength={500}
                  placeholder="Ej.: sustitución por nueva información fiscal aprobada"
                  className="mt-1"
                />
              </div>
              <Button
                type="button"
                variant="outline"
                onClick={() => setConfirmRetirement(true)}
                className="gap-2 text-destructive hover:text-destructive"
              >
                <Archive size={16} /> Retirar perfil fiscal
              </Button>
            </div>
          )}
        </div>
      </section>

      <div className="space-y-4">
        {/* Datos del negocio */}
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">Datos del Negocio</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="sm:col-span-2">
              <label htmlFor="business-name" className="text-sm font-medium">Nombre</label>
              <Input id="business-name" autoComplete="organization" value={s.businessName} onChange={e => setS({...s, businessName: e.target.value})} />
            </div>
            <div>
              <label htmlFor="business-rtn" className="text-sm font-medium">RTN</label>
              <Input id="business-rtn" inputMode="numeric" value={s.rtn} onChange={e => setS({...s, rtn: e.target.value})} maxLength={14} />
            </div>
            <div>
              <label htmlFor="business-phone" className="text-sm font-medium">Teléfono</label>
              <Input id="business-phone" type="tel" autoComplete="tel" value={s.phone} onChange={e => setS({...s, phone: e.target.value})} />
            </div>
            <div>
              <label htmlFor="business-email" className="text-sm font-medium">Email</label>
              <Input id="business-email" type="email" autoComplete="email" value={s.email} onChange={e => setS({...s, email: e.target.value})} />
            </div>
            <div className="sm:col-span-2">
              <label htmlFor="business-address" className="text-sm font-medium">Dirección</label>
              <Input id="business-address" autoComplete="street-address" value={s.address} onChange={e => setS({...s, address: e.target.value})} />
            </div>
            <div className="sm:col-span-2">
              <label htmlFor="print-footer" className="text-sm font-medium">Pie de Página</label>
              <Input id="print-footer" value={s.footer} onChange={e => setS({...s, footer: e.target.value})} />
            </div>
            <div>
              <label htmlFor="business-isv-rate" className="text-sm font-medium">ISV (%)</label>
              <Input id="business-isv-rate" type="number" step="0.01" min={0} max={100} value={s.isvRate * 100} onChange={e => setS({...s, isvRate: +e.target.value / 100})} />
            </div>
            <div>
              <label htmlFor="business-tourist-rate" className="text-sm font-medium">Tasa Turística (%)</label>
              <Input id="business-tourist-rate" type="number" step="0.01" min={0} max={100} value={s.touristTaxRate * 100} onChange={e => setS({...s, touristTaxRate: +e.target.value / 100})} />
            </div>
          </div>
        </div>

        {/* Logo */}
        <div className="border border-border rounded-lg p-4 bg-card space-y-2">
          <h3 className="font-medium">Logo</h3>
          <input type="file" ref={fileRef} accept="image/*" onChange={handleLogo} className="text-sm" />
          {logoPreview && (
            <div className="mt-2">
              <img src={logoPreview} className="object-contain border rounded" style={{maxHeight:`${s.printLogoHeight * 2}px`}} alt="" />
              <div className="flex items-center gap-2 mt-1">
                <span className="text-xs">Alto:</span>
                <input type="range" min={20} max={200} value={s.printLogoHeight} onChange={e => setS({...s, printLogoHeight: +e.target.value})} className="flex-1" />
                <span className="text-xs font-mono">{s.printLogoHeight}px</span>
              </div>
              <Button size="sm" variant="outline" className="mt-1" onClick={() => { setLogoPreview(''); if (fileRef.current) fileRef.current.value = '' }}>Eliminar</Button>
            </div>
          )}
        </div>

        {/* Impresora */}
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">Impresora térmica</h3>
          <div>
            <label className="text-sm font-medium">Dispositivo</label>
            <select value={s.printPrinterName || ''} onChange={e => { setS({...s, printPrinterName: e.target.value}); setPrinterTest(null) }}
              className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1">
              <option value="">-- Seleccione --</option>
              {printers.map(p => <option key={p} value={p}>{p}</option>)}
            </select>
            <div className="flex gap-2 mt-2 flex-wrap">
              <Button size="sm" variant="outline" onClick={testPrinter} disabled={!s.printPrinterName || testing}>{testing ? 'Probando...' : 'Probar'}</Button>
              <Button size="sm" variant="outline" onClick={printRuler} disabled={!s.printPrinterName}>Imprimir Regla</Button>
              <Button size="sm" variant="outline" onClick={() => api.get<string[]>('/print/printers').then(({ data }) => setPrinters(data)).catch(() => {})}>Recargar</Button>
            </div>
            <div className="flex items-center gap-2 mt-2">
              <span className="text-sm">Ancho observado:</span>
              <Input
                type="number"
                min={28}
                max={100}
                value={observedWidth}
                onChange={e => setObservedWidth(parseInt(e.target.value) || 46)}
                className="w-20 h-8 text-sm"
              />
              <Button size="sm" variant="outline" onClick={() => {
                setS(prev => ({ ...prev, printWidth: observedWidth }))
                setCustomSlider(true)
              }}>Aplicar</Button>
            </div>
            {printerTest && (
              <div className={`mt-1 p-2 rounded text-sm ${printerTest.success ? 'bg-green-50 dark:bg-green-900/20 text-green-800 dark:text-green-300' : 'bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-300'}`}>
                {printerTest.success ? 'OK: ' : 'Error: '}{printerTest.message}
              </div>
            )}
          </div>
        </div>

        {/* Layout */}
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">Layout</h3>

          <div>
            <label className="text-sm font-medium">Ancho de Papel</label>
            <div className="flex gap-1 flex-wrap mt-1">
              {PAPER_PRESETS.map(p => (
                <button key={p.label} onClick={() => { setCustomSlider(false); setS({...s, printWidth: p.width}) }}
                  className={`px-3 py-1 text-xs rounded border cursor-pointer transition-colors ${!customSlider && presetActive?.width === p.width ? 'bg-primary text-primary-foreground border-primary' : 'bg-background text-muted-foreground border-border hover:bg-accent'}`}>{p.label}</button>
              ))}
              <button onClick={() => setCustomSlider(true)}
                className={`px-3 py-1 text-xs rounded border cursor-pointer transition-colors ${customSlider ? 'bg-primary text-primary-foreground border-primary' : 'bg-background text-muted-foreground border-border hover:bg-accent'}`}>Personalizado</button>
            </div>
            {customSlider && (
              <div className="flex items-center gap-3 mt-2">
                <input type="range" min={28} max={72} value={s.printWidth} onChange={e => setS({...s, printWidth: +e.target.value})} className="flex-1 accent-primary cursor-pointer" />
                <span className="text-sm font-mono w-8 text-right">{s.printWidth}</span>
              </div>
            )}
            <p className="text-xs text-muted-foreground mt-1">
              Imprima la regla, observe el último número visible en el papel e ingrese ese valor en "Ancho observado".
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="text-sm font-medium">Fuente</label>
              <select value={s.printFontSize} onChange={e => setS({...s, printFontSize: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1">
                <option value="condensed">Condensada</option>
                <option value="normal">Normal</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">Interlineado</label>
              <select value={s.printLineSpacing} onChange={e => setS({...s, printLineSpacing: +e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1">
                <option value={0}>Compacto</option>
                <option value={1}>Normal</option>
                <option value={2}>Espaciado</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">Alineación del encabezado</label>
              <select value={s.headerAlign} onChange={e => setS({...s, headerAlign: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1">
                <option value="center">Centrado</option>
                <option value="left">Izquierda</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">Carácter separador</label>
              <select value={s.separatorChar} onChange={e => setS({...s, separatorChar: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1">
                <option value="-">Línea (-)</option>
                <option value="=">Doble (=)</option>
                <option value="_">Bajo (_)</option>
              </select>
            </div>
            <div className="sm:col-span-2">
              <label className="text-sm font-medium">Margen Izquierdo (espacios)</label>
              <input type="range" min={0} max={8} value={s.marginLeft} onChange={e => setS({...s, marginLeft: +e.target.value})} className="w-full accent-primary cursor-pointer" />
              <span className="text-xs text-muted-foreground">{s.marginLeft} espacio(s)</span>
            </div>
          </div>
        </div>

        {/* Secciones visibles */}
        <div className="border border-border rounded-lg p-4 bg-card space-y-2">
          <h3 className="font-medium">Secciones Visibles</h3>
          <div className="flex flex-wrap gap-2">
            {SECTIONS.map(sec => (
              <button key={sec.key} onClick={() => toggleSection(sec.key)}
                className={`px-3 py-1.5 text-xs rounded border cursor-pointer transition-colors ${s[sec.key] ? 'bg-primary text-primary-foreground border-primary' : 'bg-background text-muted-foreground border-border hover:bg-accent'}`}>
                {sec.label}
              </button>
            ))}
          </div>
        </div>

        {/* Preview */}
        <div className="border border-border rounded-lg p-4 bg-card space-y-2">
          <div className="flex justify-between items-center">
            <h3 className="font-medium">Vista Previa (auto-actualizada)</h3>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" onClick={printTest} disabled={!s.printPrinterName}>Imprimir Prueba</Button>
            </div>
          </div>
          {loadingPreview && <p className="text-xs text-muted-foreground">Cargando...</p>}
          {testPreview && (
            <div className="border border-border rounded-lg p-2" style={{background:'#fff', color:'#000', maxWidth:`${Math.min(w * 6.5, 550)}px`, margin:'0 auto'}}>
              {testLogo && s.showLogo && <div className="text-center mb-1"><img src={testLogo} className="mx-auto" style={{maxHeight:`${Math.min(s.printLogoHeight * 2, 400)}px`}} alt="" /></div>}
              <pre className="font-mono whitespace-pre-wrap" style={{margin:0, fontSize:'10px', lineHeight:'1.15'}}>{testPreview}</pre>
            </div>
          )}
          {printResult && (
            <div className={`p-2 rounded text-sm ${printResult.includes('Error') ? 'bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-300' : 'bg-green-50 dark:bg-green-900/20 text-green-800 dark:text-green-300'}`}>{printResult}</div>
          )}
        </div>

        <Button onClick={save} disabled={saving} className="w-full">{saving ? 'Guardando...' : 'Guardar Configuración'}</Button>
        {saveSuccess && (
          <InlineAlert
            variant="success"
            message="Configuración guardada exitosamente"
            onClose={() => setSaveSuccess(false)}
          />
        )}
      </div>

      <ConfirmDialog
        isOpen={confirmRetirement}
        title="Retirar perfil fiscal"
        description="La emisión de nuevas facturas y notas fiscales quedará bloqueada de inmediato. La versión y su historial se conservarán en auditoría."
        confirmText="Retirar y bloquear emisión"
        onConfirm={retireFiscalProfile}
        onCancel={() => setConfirmRetirement(false)}
      />
    </div>
  )
}
