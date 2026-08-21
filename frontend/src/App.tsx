import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import React, { Suspense } from 'react'
import AppLayout from '@/components/layout/AppLayout'
import ProtectedRoute from '@/components/layout/ProtectedRoute'

// Lazy loaded pages
const LoginPage = React.lazy(() => import('@/pages/auth/LoginPage'))
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

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Suspense fallback={<PageLoader />}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route
              element={
                <ProtectedRoute>
                  <AppLayout />
                </ProtectedRoute>
              }
            >
              <Route path="/dashboard" element={<DashboardPage />} />
              
              {/* Operaciones & Habitaciones */}
              <Route path="/rooms" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista']}><RoomsPage /></ProtectedRoute>} />
              <Route path="/rooms/types" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista']}><RoomTypesPage /></ProtectedRoute>} />
              <Route path="/rooms/map" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista']}><FloorMapPage /></ProtectedRoute>} />
              <Route path="/reservations" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista']}><ReservationsPage /></ProtectedRoute>} />
              <Route path="/checkin" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista']}><CheckInPage /></ProtectedRoute>} />
              <Route path="/checkout" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista']}><CheckOutPage /></ProtectedRoute>} />
              <Route path="/guests" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista']}><GuestsPage /></ProtectedRoute>} />
              
              {/* Facturación & Clientes */}
              <Route path="/folios" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista', 'Contador']}><FoliosPage /></ProtectedRoute>} />
              <Route path="/customers" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista', 'Contador']}><CustomersPage /></ProtectedRoute>} />
              <Route path="/invoices" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista', 'Contador']}><InvoicesPage /></ProtectedRoute>} />
              <Route path="/invoices/:id/edit" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista', 'Contador']}><InvoiceEditPage /></ProtectedRoute>} />
              <Route path="/authorizations" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista', 'Contador']}><AuthorizationsPage /></ProtectedRoute>} />
              <Route path="/cai" element={<Navigate to="/authorizations" replace />} />
              <Route path="/document-authorizations" element={<Navigate to="/authorizations" replace />} />
              
              {/* Caja e Inventario */}
              <Route path="/cash" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista', 'Contador']}><CashRegistersPage /></ProtectedRoute>} />
              <Route path="/inventory" element={<ProtectedRoute allowedRoles={['Admin']}><InventoryPage /></ProtectedRoute>} />
              
              {/* Contabilidad & Reportes */}
              <Route path="/reports" element={<ProtectedRoute allowedRoles={['Admin', 'Contador']}><ReportsPage /></ProtectedRoute>} />
              <Route path="/accounting/chart-of-accounts" element={<ProtectedRoute allowedRoles={['Admin', 'Contador']}><ChartOfAccountsPage /></ProtectedRoute>} />
              <Route path="/accounting/journal-entries" element={<ProtectedRoute allowedRoles={['Admin', 'Contador']}><JournalEntriesPage /></ProtectedRoute>} />
              <Route path="/accounting/trial-balance" element={<ProtectedRoute allowedRoles={['Admin', 'Contador']}><TrialBalancePage /></ProtectedRoute>} />
              
              {/* Administración & Sistema */}
              <Route path="/users" element={<ProtectedRoute allowedRoles={['Admin']}><UsersPage /></ProtectedRoute>} />
              <Route path="/audit-logs" element={<ProtectedRoute allowedRoles={['Admin']}><AuditLogsPage /></ProtectedRoute>} />
              <Route path="/discounts" element={<ProtectedRoute allowedRoles={['Admin', 'Recepcionista']}><DiscountsPage /></ProtectedRoute>} />
              <Route path="/backups" element={<ProtectedRoute allowedRoles={['Admin']}><BackupsPage /></ProtectedRoute>} />
              <Route path="/settings" element={<ProtectedRoute allowedRoles={['Admin']}><SettingsPage /></ProtectedRoute>} />
              
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </Suspense>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
