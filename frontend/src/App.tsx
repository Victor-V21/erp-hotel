import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import AppLayout from '@/components/layout/AppLayout'
import ProtectedRoute from '@/components/layout/ProtectedRoute'
import LoginPage from '@/pages/auth/LoginPage'
import DashboardPage from '@/pages/dashboard/DashboardPage'
import RoomsPage from '@/pages/rooms/RoomsPage'
import RoomTypesPage from '@/pages/rooms/RoomTypesPage'
import ReservationsPage from '@/pages/reservations/ReservationsPage'
import CheckInPage from '@/pages/reservations/CheckInPage'
import CheckOutPage from '@/pages/reservations/CheckOutPage'
import FoliosPage from '@/pages/folios/FoliosPage'
import GuestsPage from '@/pages/guests/GuestsPage'
import InvoicesPage from '@/pages/invoices/InvoicesPage'
import InvoiceEditPage from '@/pages/invoices/InvoiceEditPage'
import AuthorizationsPage from '@/pages/invoices/AuthorizationsPage'
import CashRegistersPage from '@/pages/cash/CashRegistersPage'
import SettingsPage from '@/pages/settings/SettingsPage'
import DiscountsPage from '@/pages/discounts/DiscountsPage'
import BackupsPage from '@/pages/backups/BackupsPage'

const queryClient = new QueryClient()

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
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
            <Route path="/rooms" element={<RoomsPage />} />
            <Route path="/rooms/types" element={<RoomTypesPage />} />
            <Route path="/reservations" element={<ReservationsPage />} />
            <Route path="/folios" element={<FoliosPage />} />
            <Route path="/checkin" element={<CheckInPage />} />
            <Route path="/checkout" element={<CheckOutPage />} />
            <Route path="/guests" element={<GuestsPage />} />
            <Route path="/invoices" element={<InvoicesPage />} />
            <Route path="/invoices/:id/edit" element={<InvoiceEditPage />} />
            <Route path="/authorizations" element={<AuthorizationsPage />} />
            <Route path="/cai" element={<Navigate to="/authorizations" replace />} />
            <Route path="/document-authorizations" element={<Navigate to="/authorizations" replace />} />
            <Route path="/cash" element={<CashRegistersPage />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/discounts" element={<DiscountsPage />} />
            <Route path="/backups" element={<BackupsPage />} />
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
