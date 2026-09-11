import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React, { Suspense, useEffect, type ReactNode } from 'react'
import AppLayout from '@/components/layout/AppLayout'
import ProtectedRoute from '@/components/layout/ProtectedRoute'
import { refreshBrowserSession } from '@/lib/sessionRefresh'
import { useAuthStore } from '@/store/authStore'
import type { AuthResponse } from '@/types'

// Lazy loaded pages
const LoginPage = React.lazy(() => import('@/pages/auth/LoginPage'))
const ChangePasswordPage = React.lazy(() => import('@/pages/auth/ChangePasswordPage'))
const DashboardPage = React.lazy(() => import('@/pages/dashboard/DashboardPage'))
const RoomsPage = React.lazy(() => import('@/pages/rooms/RoomsPage'))
const RoomTypesPage = React.lazy(() => import('@/pages/rooms/RoomTypesPage'))
const FloorMapPage = React.lazy(() => import('@/pages/rooms/FloorMapPage'))
const ReservationsPage = React.lazy(() => import('@/pages/reservations/ReservationsPage'))
const CheckInPage = React.lazy(() => import('@/pages/reservations/CheckInPage'))
const CheckOutPage = React.lazy(() => import('@/pages/reservations/CheckOutPage'))
const CustomersPage = React.lazy(() => import('@/pages/customers/CustomersPage'))
const InventoryPage = React.lazy(() => import('@/pages/inventory/InventoryPage'))
const FoliosPage = React.lazy(() => import('@/pages/folios/FoliosPage'))
const GuestsPage = React.lazy(() => import('@/pages/guests/GuestsPage'))
const InvoicesPage = React.lazy(() => import('@/pages/invoices/InvoicesPage'))
const InvoiceEditPage = React.lazy(() => import('@/pages/invoices/InvoiceEditPage'))
const AuthorizationsPage = React.lazy(() => import('@/pages/invoices/AuthorizationsPage'))
const CashRegistersPage = React.lazy(() => import('@/pages/cash/CashRegistersPage'))
const CardSettlementsPage = React.lazy(() => import('@/pages/cash/CardSettlementsPage'))
const SettingsPage = React.lazy(() => import('@/pages/settings/SettingsPage'))
const DiscountsPage = React.lazy(() => import('@/pages/discounts/DiscountsPage'))
const BackupsPage = React.lazy(() => import('@/pages/backups/BackupsPage'))
const ChartOfAccountsPage = React.lazy(() => import('@/pages/accounting/ChartOfAccountsPage'))
const JournalEntriesPage = React.lazy(() => import('@/pages/accounting/JournalEntriesPage'))
const TrialBalancePage = React.lazy(() => import('@/pages/accounting/TrialBalancePage'))
const ReportsPage = React.lazy(() => import('@/pages/reports/ReportsPage'))
const UsersPage = React.lazy(() => import('@/pages/users/UsersPage'))
const AuditLogsPage = React.lazy(() => import('@/pages/admin/AuditLogsPage'))
const NotFoundPage = React.lazy(() => import('@/pages/common/NotFoundPage'))

const queryClient = new QueryClient()

const PageLoader = () => (
  <div className="flex h-full items-center justify-center p-8">
    <div className="flex flex-col items-center gap-2 text-muted-foreground">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-r-transparent"></div>
      <p className="text-sm">Cargando módulo...</p>
    </div>
  </div>
)

let sessionInitialization: Promise<AuthResponse> | null = null

function requestInitialSession(): Promise<AuthResponse> {
  sessionInitialization ??= refreshBrowserSession()
  return sessionInitialization
}

function SessionBootstrap({ children }: { children: ReactNode }) {
  const isInitialized = useAuthStore((state) => state.isInitialized)
  const setAuth = useAuthStore((state) => state.setAuth)
  const finishInitialization = useAuthStore((state) => state.finishInitialization)

  useEffect(() => {
    if (isInitialized) return

    let active = true
    const initialize = async () => {
      try {
        const data = await requestInitialSession()
        if (!active) return
        if (data.success && data.user && data.accessToken) {
          setAuth(data.user, data.accessToken)
        } else {
          finishInitialization()
        }
      } catch {
        if (active) finishInitialization()
      }
    }

    void initialize()
    return () => {
      active = false
    }
  }, [finishInitialization, isInitialized, setAuth])

  return isInitialized ? <>{children}</> : <PageLoader />
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <SessionBootstrap>
          <Suspense fallback={<PageLoader />}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route
              path="/change-password"
              element={
                <ProtectedRoute allowPasswordChange>
                  <ChangePasswordPage />
                </ProtectedRoute>
              }
            />
            <Route
              element={
                <ProtectedRoute>
                  <AppLayout />
                </ProtectedRoute>
              }
            >
              <Route path="/dashboard" element={<DashboardPage />} />
              
              {/* Operaciones & Habitaciones */}
              <Route path="/rooms" element={<ProtectedRoute requiredPermission="manage_reservations"><RoomsPage /></ProtectedRoute>} />
              <Route path="/rooms/types" element={<ProtectedRoute requiredPermission="manage_reservations"><RoomTypesPage /></ProtectedRoute>} />
              <Route path="/rooms/map" element={<ProtectedRoute requiredPermission="manage_reservations"><FloorMapPage /></ProtectedRoute>} />
              <Route path="/reservations" element={<ProtectedRoute requiredPermission="manage_reservations"><ReservationsPage /></ProtectedRoute>} />
              <Route path="/checkin" element={<ProtectedRoute requiredPermission="manage_reservations"><CheckInPage /></ProtectedRoute>} />
              <Route path="/checkout" element={<ProtectedRoute requiredPermission="manage_reservations"><CheckOutPage /></ProtectedRoute>} />
              <Route path="/guests" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcion', 'Contador']}><GuestsPage /></ProtectedRoute>} />
              
              {/* Facturación & Clientes */}
              <Route path="/folios" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcion', 'Caja', 'Contador']}><FoliosPage /></ProtectedRoute>} />
              <Route path="/customers" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcion', 'Contador']}><CustomersPage /></ProtectedRoute>} />
              <Route path="/invoices" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcion', 'Caja', 'Contador']}><InvoicesPage /></ProtectedRoute>} />
              <Route path="/invoices/:id/edit" element={<ProtectedRoute requiredPermission="create_invoices"><InvoiceEditPage /></ProtectedRoute>} />
              <Route path="/authorizations" element={<ProtectedRoute requiredPermission="manage_taxes"><AuthorizationsPage /></ProtectedRoute>} />
              <Route path="/cai" element={<Navigate to="/authorizations" replace />} />
              <Route path="/document-authorizations" element={<Navigate to="/authorizations" replace />} />
              
              {/* Caja e Inventario */}
              <Route path="/cash" element={<ProtectedRoute requiredPermission="manage_cash"><CashRegistersPage /></ProtectedRoute>} />
              <Route path="/card-settlements" element={<ProtectedRoute requiredPermission="manage_accounting"><CardSettlementsPage /></ProtectedRoute>} />
              <Route path="/inventory" element={<ProtectedRoute requiredPermission="manage_inventory"><InventoryPage /></ProtectedRoute>} />
              
              {/* Contabilidad & Reportes */}
              <Route path="/reports" element={<ProtectedRoute requiredPermission="view_reports"><ReportsPage /></ProtectedRoute>} />
              <Route path="/accounting/chart-of-accounts" element={<ProtectedRoute requiredPermission="manage_accounting"><ChartOfAccountsPage /></ProtectedRoute>} />
              <Route path="/accounting/journal-entries" element={<ProtectedRoute requiredPermission="manage_accounting"><JournalEntriesPage /></ProtectedRoute>} />
              <Route path="/accounting/trial-balance" element={<ProtectedRoute requiredPermission="manage_accounting"><TrialBalancePage /></ProtectedRoute>} />
              
              {/* Administración & Sistema */}
              <Route path="/users" element={<ProtectedRoute requiredPermission="manage_users"><UsersPage /></ProtectedRoute>} />
              <Route path="/audit-logs" element={<ProtectedRoute requiredPermission="view_audit"><AuditLogsPage /></ProtectedRoute>} />
              <Route path="/discounts" element={<ProtectedRoute requiredPermission="manage_settings"><DiscountsPage /></ProtectedRoute>} />
              <Route path="/backups" element={<ProtectedRoute requiredPermission="manage_backups"><BackupsPage /></ProtectedRoute>} />
              <Route path="/settings" element={<ProtectedRoute requiredPermission="manage_settings"><SettingsPage /></ProtectedRoute>} />
              
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
          </Suspense>
        </SessionBootstrap>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
