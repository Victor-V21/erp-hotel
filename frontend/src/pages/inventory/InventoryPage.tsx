import { useEffect, useMemo, useState, useCallback } from 'react'
import api from '@/lib/axios'
import type { Category, InventoryMovement, Product } from '@/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { InlineAlert } from '@/components/ui/InlineAlert'
import { ConfirmDialog } from '@/components/ui/ConfirmDialog'
import { Pagination } from '@/components/ui/Pagination'
import { Boxes, Plus, Search, Edit2, Trash2, ArrowUpRight, ArrowDownLeft, RefreshCw, Loader2, AlertTriangle, X } from 'lucide-react'

const movementTypes = ['Entrada', 'Salida', 'Ajuste', 'Compra', 'Venta']

const categoryDefault = { name: '', description: '' }
const productDefault = {
  name: '',
  description: '',
  sku: '',
  categoryId: '',
  unitPrice: 0,
  currentStock: 0,
  minStockLevel: 0,
  isActive: true,
}
const movementDefault = {
  productId: '',
  movementType: 'Entrada',
  quantity: 1,
  unitPrice: '',
  referenceId: '',
  targetStock: '',
}

export default function InventoryPage() {
  const [tab, setTab] = useState<'products' | 'categories' | 'movements'>('products')
  const [categories, setCategories] = useState<Category[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [movements, setMovements] = useState<InventoryMovement[]>([])
  const [search, setSearch] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [categoryForm, setCategoryForm] = useState(categoryDefault)
  const [productForm, setProductForm] = useState(productDefault)
  const [movementForm, setMovementForm] = useState(movementDefault)
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null)
  const [editingProductId, setEditingProductId] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [alertInfo, setAlertInfo] = useState<{ variant: 'error' | 'success' | 'info'; message: string } | null>(null)
  const [deleteProductTarget, setDeleteProductTarget] = useState<Product | null>(null)
  const [deleteCategoryTarget, setDeleteCategoryTarget] = useState<Category | null>(null)

  // Pagination for products
  const [productPage, setProductPage] = useState(1)
  const [productPageSize, setProductPageSize] = useState(15)

  // Pagination for movements
  const [movPage, setMovPage] = useState(1)
  const [movPageSize, setMovPageSize] = useState(15)

  const loadAll = useCallback(async () => {
    setLoading(true)
    try {
      const [categoriesRes, productsRes, movementsRes] = await Promise.all([
        api.get<Category[]>('/inventory/categories'),
        api.get<Product[]>('/inventory/products'),
        api.get<InventoryMovement[]>('/inventory/movements'),
      ])
      setCategories(categoriesRes.data)
      setProducts(productsRes.data)
      setMovements(movementsRes.data)
    } catch {
      setAlertInfo({ variant: 'error', message: 'Error al cargar el inventario hotelero.' })
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadAll()
  }, [loadAll])

  const filteredProducts = useMemo(() => {
    return products.filter((p) => {
      const q = search.toLowerCase()
      const matchesSearch =
        q === '' ||
        p.name.toLowerCase().includes(q) ||
        (p.sku && p.sku.toLowerCase().includes(q)) ||
        (p.description && p.description.toLowerCase().includes(q))
      const matchesCat = categoryFilter === '' || p.categoryId === categoryFilter
      return matchesSearch && matchesCat
    })
  }, [products, search, categoryFilter])

  const saveCategory = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!categoryForm.name.trim()) return
    setActionLoading(true)
    try {
      if (editingCategoryId) {
        await api.put(`/inventory/categories/${editingCategoryId}`, categoryForm)
        setAlertInfo({ variant: 'success', message: 'Categoría actualizada correctamente.' })
      } else {
        await api.post('/inventory/categories', categoryForm)
        setAlertInfo({ variant: 'success', message: 'Categoría creada exitosamente.' })
      }
      setCategoryForm(categoryDefault)
      setEditingCategoryId(null)
      loadAll()
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'Error al guardar la categoría.' })
    } finally {
      setActionLoading(false)
    }
  }

  const editCategory = (category: Category) => {
    setEditingCategoryId(category.id)
    setCategoryForm({ name: category.name, description: category.description || '' })
    setTab('categories')
  }

  const handleDeleteCategory = async () => {
    if (!deleteCategoryTarget) return
    setActionLoading(true)
    try {
      await api.delete(`/inventory/categories/${deleteCategoryTarget.id}`)
      setAlertInfo({ variant: 'success', message: 'Categoría eliminada.' })
      setDeleteCategoryTarget(null)
      loadAll()
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'No se puede eliminar una categoría con productos asignados.' })
    } finally {
      setActionLoading(false)
    }
  }

  const saveProduct = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!productForm.name.trim()) return
    setActionLoading(true)
    try {
      const payload = {
        ...productForm,
        categoryId: productForm.categoryId || null,
        description: productForm.description || null,
        sku: productForm.sku || null,
      }
      if (editingProductId) {
        await api.put(`/inventory/products/${editingProductId}`, payload)
        setAlertInfo({ variant: 'success', message: 'Producto actualizado con éxito.' })
      } else {
        await api.post('/inventory/products', payload)
        setAlertInfo({ variant: 'success', message: 'Producto registrado en inventario.' })
      }
      setProductForm(productDefault)
      setEditingProductId(null)
      loadAll()
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'Error al guardar el producto.' })
    } finally {
      setActionLoading(false)
    }
  }

  const editProduct = (product: Product) => {
    setEditingProductId(product.id)
    setProductForm({
      name: product.name,
      description: product.description || '',
      sku: product.sku || '',
      categoryId: product.categoryId || '',
      unitPrice: product.unitPrice,
      currentStock: product.currentStock,
      minStockLevel: product.minStockLevel,
      isActive: product.isActive,
    })
    setTab('products')
  }

  const handleDeleteProduct = async () => {
    if (!deleteProductTarget) return
    setActionLoading(true)
    try {
      await api.delete(`/inventory/products/${deleteProductTarget.id}`)
      setAlertInfo({ variant: 'success', message: 'Producto eliminado del inventario.' })
      setDeleteProductTarget(null)
      loadAll()
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'No se puede eliminar un producto con movimientos registrados.' })
    } finally {
      setActionLoading(false)
    }
  }

  const recordMovement = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!movementForm.productId) return
    setActionLoading(true)
    try {
      await api.post('/inventory/movements', {
        productId: movementForm.productId,
        movementType: movementForm.movementType,
        quantity: Number(movementForm.quantity),
        unitPrice: movementForm.unitPrice ? Number(movementForm.unitPrice) : null,
        referenceId: movementForm.referenceId || null,
        targetStock: movementForm.targetStock ? Number(movementForm.targetStock) : null,
      })
      setMovementForm(movementDefault)
      setAlertInfo({ variant: 'success', message: 'Movimiento de inventario registrado correctamente.' })
      loadAll()
    } catch (err: any) {
      setAlertInfo({ variant: 'error', message: err.response?.data?.message || 'Error al registrar movimiento.' })
    } finally {
      setActionLoading(false)
    }
  }

  const lowStockCount = useMemo(
    () => products.filter((p) => p.isActive && p.currentStock <= p.minStockLevel).length,
    [products]
  )

  const productTotalPages = Math.ceil(filteredProducts.length / productPageSize) || 1
  const paginatedProducts = useMemo(() => {
    const start = (productPage - 1) * productPageSize
    return filteredProducts.slice(start, start + productPageSize)
  }, [filteredProducts, productPage, productPageSize])

  const movTotalPages = Math.ceil(movements.length / movPageSize) || 1
  const paginatedMovements = useMemo(() => {
    const start = (movPage - 1) * movPageSize
    return movements.slice(start, start + movPageSize)
  }, [movements, movPage, movPageSize])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground tracking-tight flex items-center gap-2.5">
            <Boxes className="text-[#C69C4B]" size={24} />
            Inventario & Suministros
          </h1>
          <p className="text-sm text-muted-foreground">
            Control de existencias de minibar, restaurante, lencería y amenidades hoteleras.
          </p>
        </div>
        <Button variant="outline" onClick={loadAll} disabled={loading} className="gap-1.5 self-start sm:self-auto">
          <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
          Actualizar
        </Button>
      </div>

      {alertInfo && (
        <InlineAlert variant={alertInfo.variant} message={alertInfo.message} onClose={() => setAlertInfo(null)} />
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="border border-border rounded-xl p-4 bg-card shadow-xs">
          <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Total Productos Activos</p>
          <p className="text-2xl font-bold text-foreground mt-1">{products.filter((p) => p.isActive).length}</p>
        </div>
        <div className="border border-border rounded-xl p-4 bg-card shadow-xs">
          <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Categorías de Almacén</p>
          <p className="text-2xl font-bold text-foreground mt-1">{categories.length}</p>
        </div>
        <div className="border border-border rounded-xl p-4 bg-card shadow-xs">
          <p className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">Alertas de Stock Bajo</p>
          <div className="flex items-center gap-2 mt-1">
            <p className={`text-2xl font-bold ${lowStockCount > 0 ? 'text-amber-500' : 'text-emerald-500'}`}>
              {lowStockCount}
            </p>
            {lowStockCount > 0 && (
              <span className="text-[11px] text-amber-600 dark:text-amber-400 flex items-center gap-1 font-semibold">
                <AlertTriangle size={13} /> Reabastecer
              </span>
            )}
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-border pb-2">
        <Button variant={tab === 'products' ? 'default' : 'outline'} size="sm" onClick={() => setTab('products')}>
          Productos ({products.length})
        </Button>
        <Button variant={tab === 'categories' ? 'default' : 'outline'} size="sm" onClick={() => setTab('categories')}>
          Categorías ({categories.length})
        </Button>
        <Button variant={tab === 'movements' ? 'default' : 'outline'} size="sm" onClick={() => setTab('movements')}>
          Kardex / Movimientos ({movements.length})
        </Button>
      </div>

      {/* TAB 1: PRODUCTS */}
      {tab === 'products' && (
        <div className="grid gap-6 xl:grid-cols-[400px_1fr] animate-in fade-in">
          {/* Form */}
          <form onSubmit={saveProduct} className="border border-border rounded-xl p-5 bg-card space-y-3.5 shadow-xs h-fit">
            <h3 className="font-bold text-sm text-foreground flex items-center justify-between">
              <span>{editingProductId ? 'Editar Producto' : 'Nuevo Producto'}</span>
              {editingProductId && (
                <button
                  type="button"
                  onClick={() => {
                    setEditingProductId(null)
                    setProductForm(productDefault)
                  }}
                  className="text-xs text-muted-foreground hover:text-foreground"
                >
                  Cancelar
                </button>
              )}
            </h3>

            <div>
              <label className="text-xs font-semibold text-muted-foreground">Nombre del Producto *</label>
              <Input
                required
                placeholder="Ej. Agua Mineral 500ml / Toalla de Baño"
                value={productForm.name}
                onChange={(e) => setProductForm({ ...productForm, name: e.target.value })}
                className="text-xs mt-1"
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="text-xs font-semibold text-muted-foreground">Código SKU</label>
                <Input
                  placeholder="AGUA-500"
                  value={productForm.sku}
                  onChange={(e) => setProductForm({ ...productForm, sku: e.target.value })}
                  className="text-xs mt-1 font-mono uppercase"
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-muted-foreground">Categoría</label>
                <select
                  value={productForm.categoryId}
                  onChange={(e) => setProductForm({ ...productForm, categoryId: e.target.value })}
                  className="w-full border border-input rounded-md px-2.5 py-2 text-xs bg-background mt-1"
                >
                  <option value="">Sin categoría</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label className="text-xs font-semibold text-muted-foreground">Descripción Opcional</label>
              <Input
                placeholder="Detalle o presentación"
                value={productForm.description}
                onChange={(e) => setProductForm({ ...productForm, description: e.target.value })}
                className="text-xs mt-1"
              />
            </div>

            <div className="grid grid-cols-3 gap-2">
              <div>
                <label className="text-xs font-semibold text-muted-foreground">Precio Venta (L)</label>
                <Input
                  type="number"
                  step="0.01"
                  value={productForm.unitPrice}
                  onChange={(e) => setProductForm({ ...productForm, unitPrice: Number(e.target.value) })}
                  className="text-xs mt-1 font-mono font-bold"
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-muted-foreground">Stock Actual</label>
                <Input
                  type="number"
                  value={productForm.currentStock}
                  onChange={(e) => setProductForm({ ...productForm, currentStock: Number(e.target.value) })}
                  className="text-xs mt-1 font-mono"
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-muted-foreground">Stock Mínimo</label>
                <Input
                  type="number"
                  value={productForm.minStockLevel}
                  onChange={(e) => setProductForm({ ...productForm, minStockLevel: Number(e.target.value) })}
                  className="text-xs mt-1 font-mono"
                />
              </div>
            </div>

            <div className="flex items-center justify-between pt-2 border-t border-border">
              <label className="flex items-center gap-2 text-xs text-muted-foreground cursor-pointer">
                <input
                  type="checkbox"
                  checked={productForm.isActive}
                  onChange={(e) => setProductForm({ ...productForm, isActive: e.target.checked })}
                  className="rounded border-border"
                />
                Producto Activo en Venta
              </label>

              <div className="flex gap-2">
                <Button type="submit" disabled={actionLoading} size="sm">
                  {actionLoading && <Loader2 size={13} className="animate-spin mr-1" />}
                  {editingProductId ? 'Actualizar' : 'Guardar Producto'}
                </Button>
              </div>
            </div>
          </form>

          {/* Table */}
          <div className="space-y-3">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 p-3 rounded-xl border border-border bg-card shadow-xs">
              <div className="relative">
                <Search size={14} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <Input
                  placeholder="Buscar por nombre o SKU..."
                  value={search}
                  onChange={(e) => {
                    setSearch(e.target.value)
                    setProductPage(1)
                  }}
                  className="pl-8 text-xs h-8"
                />
              </div>
              <select
                value={categoryFilter}
                onChange={(e) => {
                  setCategoryFilter(e.target.value)
                  setProductPage(1)
                }}
                className="border border-input rounded-md px-2.5 py-1 text-xs bg-background h-8"
              >
                <option value="">Todas las Categorías</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
              <table className="w-full text-xs">
                <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                  <tr>
                    <th className="text-left p-3">Producto</th>
                    <th className="text-left p-3 w-28">SKU</th>
                    <th className="text-left p-3 w-28">Categoría</th>
                    <th className="text-right p-3 w-20">Stock</th>
                    <th className="text-right p-3 w-24">Precio</th>
                    <th className="text-right p-3 w-20">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border/60">
                  {paginatedProducts.map((p) => {
                    const isLow = p.currentStock <= p.minStockLevel
                    return (
                      <tr key={p.id} className="hover:bg-accent/20 transition-colors">
                        <td className="p-3 font-medium text-foreground">
                          {p.name}
                          {!p.isActive && (
                            <span className="ml-2 text-[10px] text-muted-foreground font-normal">(Inactivo)</span>
                          )}
                        </td>
                        <td className="p-3 font-mono text-muted-foreground">{p.sku || '—'}</td>
                        <td className="p-3 text-muted-foreground">{p.categoryName || '—'}</td>
                        <td className="p-3 text-right font-mono font-bold">
                          <span className={isLow ? 'text-amber-600 dark:text-amber-400' : 'text-foreground'}>
                            {p.currentStock}
                          </span>
                        </td>
                        <td className="p-3 text-right font-mono font-bold text-foreground">
                          L {p.unitPrice.toFixed(2)}
                        </td>
                        <td className="p-3 text-right space-x-1">
                          <Button size="sm" variant="outline" onClick={() => editProduct(p)} className="h-6 w-6 p-0">
                            <Edit2 size={11} />
                          </Button>
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => setDeleteProductTarget(p)}
                            className="h-6 w-6 p-0"
                          >
                            <Trash2 size={11} />
                          </Button>
                        </td>
                      </tr>
                    )
                  })}
                  {paginatedProducts.length === 0 && (
                    <tr>
                      <td colSpan={6} className="p-8 text-center text-muted-foreground">
                        No se encontraron productos en el inventario.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>

              <Pagination
                currentPage={productPage}
                totalPages={productTotalPages}
                totalItems={filteredProducts.length}
                pageSize={productPageSize}
                onPageChange={setProductPage}
                onPageSizeChange={(size) => {
                  setProductPageSize(size)
                  setProductPage(1)
                }}
              />
            </div>
          </div>
        </div>
      )}

      {/* TAB 2: CATEGORIES */}
      {tab === 'categories' && (
        <div className="grid gap-6 lg:grid-cols-[360px_1fr] animate-in fade-in">
          <form onSubmit={saveCategory} className="border border-border rounded-xl p-5 bg-card space-y-3.5 shadow-xs h-fit">
            <h3 className="font-bold text-sm text-foreground">
              {editingCategoryId ? 'Editar Categoría' : 'Nueva Categoría'}
            </h3>
            <div>
              <label className="text-xs font-semibold text-muted-foreground">Nombre *</label>
              <Input
                required
                placeholder="Ej. Minibar, Lencería, Aseo"
                value={categoryForm.name}
                onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })}
                className="text-xs mt-1"
              />
            </div>
            <div>
              <label className="text-xs font-semibold text-muted-foreground">Descripción</label>
              <Input
                placeholder="Opcional"
                value={categoryForm.description}
                onChange={(e) => setCategoryForm({ ...categoryForm, description: e.target.value })}
                className="text-xs mt-1"
              />
            </div>
            <div className="flex justify-end gap-2 pt-2 border-t border-border">
              <Button type="submit" size="sm" disabled={actionLoading}>
                {editingCategoryId ? 'Actualizar' : 'Crear Categoría'}
              </Button>
            </div>
          </form>

          <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs">
            <table className="w-full text-xs">
              <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3">Nombre</th>
                  <th className="text-left p-3">Descripción</th>
                  <th className="text-right p-3 w-20">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {categories.map((c) => (
                  <tr key={c.id} className="hover:bg-accent/20">
                    <td className="p-3 font-bold text-foreground">{c.name}</td>
                    <td className="p-3 text-muted-foreground">{c.description || '—'}</td>
                    <td className="p-3 text-right space-x-1">
                      <Button size="sm" variant="outline" onClick={() => editCategory(c)} className="h-6 w-6 p-0">
                        <Edit2 size={11} />
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={() => setDeleteCategoryTarget(c)}
                        className="h-6 w-6 p-0"
                      >
                        <Trash2 size={11} />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 3: MOVEMENTS / KARDEX */}
      {tab === 'movements' && (
        <div className="grid gap-6 lg:grid-cols-[380px_1fr] animate-in fade-in">
          <form onSubmit={recordMovement} className="border border-border rounded-xl p-5 bg-card space-y-3.5 shadow-xs h-fit">
            <h3 className="font-bold text-sm text-foreground">Registrar Movimiento de Kardex</h3>

            <div>
              <label className="text-xs font-semibold text-muted-foreground">Producto *</label>
              <select
                required
                value={movementForm.productId}
                onChange={(e) => setMovementForm({ ...movementForm, productId: e.target.value })}
                className="w-full border border-input rounded-md px-2.5 py-2 text-xs bg-background mt-1"
              >
                <option value="">-- Seleccione Producto --</option>
                {products.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} (Stock: {p.currentStock})
                  </option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="text-xs font-semibold text-muted-foreground">Tipo de Movimiento</label>
                <select
                  value={movementForm.movementType}
                  onChange={(e) => setMovementForm({ ...movementForm, movementType: e.target.value })}
                  className="w-full border border-input rounded-md px-2.5 py-2 text-xs bg-background mt-1"
                >
                  {movementTypes.map((t) => (
                    <option key={t} value={t}>
                      {t}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs font-semibold text-muted-foreground">Cantidad *</label>
                <Input
                  type="number"
                  min="1"
                  required
                  value={movementForm.quantity}
                  onChange={(e) => setMovementForm({ ...movementForm, quantity: Number(e.target.value) })}
                  className="text-xs mt-1 font-mono"
                />
              </div>
            </div>

            <div>
              <label className="text-xs font-semibold text-muted-foreground">Referencia / Motivo</label>
              <Input
                placeholder="Ej. Factura Compra #1234 / Merma"
                value={movementForm.referenceId}
                onChange={(e) => setMovementForm({ ...movementForm, referenceId: e.target.value })}
                className="text-xs mt-1"
              />
            </div>

            <div className="flex justify-end pt-2 border-t border-border">
              <Button type="submit" size="sm" disabled={actionLoading}>
                Guardar Movimiento
              </Button>
            </div>
          </form>

          <div className="border border-border rounded-xl bg-card overflow-hidden shadow-xs flex flex-col">
            <table className="w-full text-xs">
              <thead className="bg-muted/40 font-semibold text-muted-foreground uppercase tracking-wider">
                <tr>
                  <th className="text-left p-3">Fecha</th>
                  <th className="text-left p-3">Producto</th>
                  <th className="text-left p-3">Tipo</th>
                  <th className="text-right p-3">Cant.</th>
                  <th className="text-left p-3">Referencia</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/60">
                {paginatedMovements.map((m) => (
                  <tr key={m.id} className="hover:bg-accent/20">
                    <td className="p-3 font-mono text-muted-foreground">
                      {new Date(m.movementDate).toLocaleDateString('es-HN')}
                    </td>
                    <td className="p-3 font-bold text-foreground">{m.productName || '—'}</td>
                    <td className="p-3">
                      <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-primary/10 text-foreground">
                        {m.movementType}
                      </span>
                    </td>
                    <td className="p-3 text-right font-mono font-bold">{m.quantity}</td>
                    <td className="p-3 text-muted-foreground">{m.referenceId || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <Pagination
              currentPage={movPage}
              totalPages={movTotalPages}
              totalItems={movements.length}
              pageSize={movPageSize}
              onPageChange={setMovPage}
              onPageSizeChange={(size) => {
                setMovPageSize(size)
                setMovPage(1)
              }}
            />
          </div>
        </div>
      )}

      {/* Delete Dialogs */}
      <ConfirmDialog
        isOpen={!!deleteProductTarget}
        title="Eliminar Producto"
        description={`¿Está seguro de que desea eliminar el producto "${deleteProductTarget?.name}"?`}
        confirmText="Eliminar"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleDeleteProduct}
        onCancel={() => setDeleteProductTarget(null)}
      />

      <ConfirmDialog
        isOpen={!!deleteCategoryTarget}
        title="Eliminar Categoría"
        description={`¿Está seguro de que desea eliminar la categoría "${deleteCategoryTarget?.name}"?`}
        confirmText="Eliminar"
        cancelText="Cancelar"
        variant="destructive"
        onConfirm={handleDeleteCategory}
        onCancel={() => setDeleteCategoryTarget(null)}
      />
    </div>
  )
}
