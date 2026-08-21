import { useState, useEffect, useRef, useCallback } from 'react'
import axios from 'axios'
import api from '@/lib/axios'
import type { BusinessSettings } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'

const PAPER_PRESETS = [
  { label: '58mm', width: 34 }, { label: '76mm', width: 40 },
  { label: '80mm', width: 46 }, { label: '80mm+', width: 56 },
]

const defaults: BusinessSettings = {
  businessName: '', rtn: '', address: '', phone: '', email: '', footer: '',
  isvRate: 0.15, touristTaxRate: 0.04, printPrinterName: '',
  printWidth: 46, printLogoHeight: 40, printFontSize: 'condensed', printLineSpacing: 1,
  showLogo: true, showHeader: true, showFiscal: true, showGuest: true,
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

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message || error.message
  }
  return error instanceof Error ? error.message : 'Error desconocido'
}

export default function SettingsPage() {
  const [s, setS] = useState<BusinessSettings>(defaults)
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
  const [customSlider, setCustomSlider] = useState(false)
  const [observedWidth, setObservedWidth] = useState(46)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  const fetchSettings = useCallback(async () => {
    try {
      const { data } = await api.get<BusinessSettings>('/settings/business')
      const isvRate = data.isvRate > 1 ? data.isvRate / 100 : data.isvRate
      const touristTaxRate = data.touristTaxRate > 1 ? data.touristTaxRate / 100 : data.touristTaxRate
      setS(prev => ({ ...prev, ...data, isvRate: isvRate ?? 0.15, touristTaxRate: touristTaxRate ?? 0.04, printWidth: data.printWidth ?? 46, printLogoHeight: data.printLogoHeight ?? 40, printFontSize: data.printFontSize ?? 'condensed', printLineSpacing: data.printLineSpacing ?? 1, marginLeft: data.marginLeft ?? 0 }))
      setLogoPreview(data.logoBase64 || '')
      setCustomSlider(!PAPER_PRESETS.some(p => p.width === (data.printWidth ?? 46)))
    } catch { }
  }, [])

  const loadPreview = useCallback(async () => {
    setLoadingPreview(true)
    try {
      const { data } = await api.get('/print/test-preview', { params: { width: s.printWidth } })
      setTestPreview(data.text); setTestLogo(data.logoBase64 || null)
    } catch { }
    finally { setLoadingPreview(false) }
  }, [s.printWidth])

  useEffect(() => { fetchSettings(); api.get<string[]>('/print/printers').then(({ data }) => setPrinters(data)).catch(() => {}) }, [fetchSettings])

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
    try { 
      await api.put('/settings/business', { ...s, logoBase64: logoPreview || null })
      setSaveSuccess(true)
      setTimeout(() => setSaveSuccess(false), 3000)
    }
    finally { setSaving(false) }
  }

  const presetActive = PAPER_PRESETS.find(p => p.width === s.printWidth)
  const w = s.printWidth || 46

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold">Configuración de Impresión</h1>

      <div className="space-y-4">
        {/* Datos del negocio */}
        <div className="border border-border rounded-lg p-4 bg-card space-y-3">
          <h3 className="font-medium">Datos del Negocio</h3>
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2">
              <label className="text-sm font-medium">Nombre</label>
              <Input value={s.businessName} onChange={e => setS({...s, businessName: e.target.value})} />
            </div>
            <div>
              <label className="text-sm font-medium">RTN</label>
              <Input value={s.rtn} onChange={e => setS({...s, rtn: e.target.value})} maxLength={14} />
            </div>
            <div>
              <label className="text-sm font-medium">Teléfono</label>
              <Input value={s.phone} onChange={e => setS({...s, phone: e.target.value})} />
            </div>
            <div>
              <label className="text-sm font-medium">Email</label>
              <Input value={s.email} onChange={e => setS({...s, email: e.target.value})} />
            </div>
            <div className="col-span-2">
              <label className="text-sm font-medium">Dirección</label>
              <Input value={s.address} onChange={e => setS({...s, address: e.target.value})} />
            </div>
            <div className="col-span-2">
              <label className="text-sm font-medium">Pie de Página</label>
              <Input value={s.footer} onChange={e => setS({...s, footer: e.target.value})} />
            </div>
            <div>
              <label className="text-sm font-medium">ISV (%)</label>
              <Input type="number" step="0.01" min={0} max={100} value={s.isvRate * 100} onChange={e => setS({...s, isvRate: +e.target.value / 100})} />
            </div>
            <div>
              <label className="text-sm font-medium">Tasa Turística (%)</label>
              <Input type="number" step="0.01" min={0} max={100} value={s.touristTaxRate * 100} onChange={e => setS({...s, touristTaxRate: +e.target.value / 100})} />
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
          <h3 className="font-medium">Impresora Termica</h3>
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
                className={`px-3 py-1 text-xs rounded border cursor-pointer transition-colors ${customSlider ? 'bg-primary text-primary-foreground border-primary' : 'bg-background text-muted-foreground border-border hover:bg-accent'}`}>Custom</button>
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

          <div className="grid grid-cols-2 gap-3">
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
              <label className="text-sm font-medium">Alineacion Encabezado</label>
              <select value={s.headerAlign} onChange={e => setS({...s, headerAlign: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1">
                <option value="center">Centrado</option>
                <option value="left">Izquierda</option>
              </select>
            </div>
            <div>
              <label className="text-sm font-medium">Caracter Separador</label>
              <select value={s.separatorChar} onChange={e => setS({...s, separatorChar: e.target.value})} className="border border-input rounded-md px-3 py-2 text-sm w-full bg-background mt-1">
                <option value="-">Linea (-)</option>
                <option value="=">Doble (=)</option>
                <option value="_">Bajo (_)</option>
              </select>
            </div>
            <div className="col-span-2">
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
    </div>
  )
}
