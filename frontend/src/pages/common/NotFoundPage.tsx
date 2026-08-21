import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Hotel, Home, ArrowLeft } from 'lucide-react'

export default function NotFoundPage() {
  const navigate = useNavigate()

  return (
    <div className="min-h-[70vh] flex flex-col items-center justify-center text-center p-6 space-y-5">
      <div className="p-4 rounded-2xl bg-primary/10 text-[#C69C4B] mb-2 animate-bounce">
        <Hotel size={48} />
      </div>

      <div className="space-y-2 max-w-md">
        <h1 className="text-4xl font-extrabold text-foreground tracking-tight">404</h1>
        <h2 className="text-xl font-bold text-foreground">Página No Encontrada</h2>
        <p className="text-sm text-muted-foreground">
          La ruta que intentas consultar no existe o fue reubicada en el sistema de Hotel Maya Central.
        </p>
      </div>

      <div className="flex gap-3 pt-2">
        <Button variant="outline" onClick={() => navigate(-1)} className="gap-2">
          <ArrowLeft size={16} /> Regresar
        </Button>
        <Button onClick={() => navigate('/dashboard')} className="gap-2 bg-[#C69C4B] hover:bg-[#b0883b] text-white">
          <Home size={16} /> Ir al Dashboard
        </Button>
      </div>
    </div>
  )
}
