export interface User {
  id: string
  username: string
  email: string
  firstName: string
  lastName: string
  isActive: boolean
  roles: string[]
  permissions: string[]
}

export interface AuthResponse {
  success: boolean
  message?: string
  accessToken?: string
  refreshToken?: string
  expiresAt?: string
  user?: User
}

export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  password: string
  email: string
  firstName: string
  lastName: string
}

export interface RoomType {
  id: string
  name: string
  description?: string
  pricePerNight: number
  capacity: number
}

export interface Room {
  id: string
  roomNumber: string
  floor: number
  roomTypeId: string
  roomTypeName: string
  status: string
  observations?: string
  pricePerNight: number
  capacity: number
}

export interface Reservation {
  id: string
  guestId: string
  guestName: string
  roomId: string
  roomNumber: string
  checkInDate: string
  checkOutDate: string
  adults: number
  children: number
  paymentMethod?: string
  advancePayment: number
  status: string
  notes?: string
}

export interface Guest {
  id: string
  firstName: string
  lastName: string
  fullName: string
  email?: string
  phone?: string
  dateOfBirth?: string
  nationality?: string
  documentType?: string
  documentNumber?: string
  origin?: string
  hasVehicle: boolean
  vehiclePlate?: string
  company?: string
  guestRTN?: string
  preferences?: string
  classification?: string
  taxpayerType?: string
  exonerationOrderNumber?: string
  sefinExonerationCertificateNumber?: string
  sagRegistryNumber?: string
}

export interface Customer {
  id: string
  rtn?: string
  name: string
  address?: string
  phone?: string
  email?: string
  taxpayerType: string
  exonerationOrderNumber?: string
  sefinExonerationCertificateNumber?: string
  sagRegistryNumber?: string
  isIsvExempt: boolean
  isTouristTaxExempt: boolean
  exonerationValidFrom?: string
  exonerationValidTo?: string
}

export interface Category {
  id: string
  name: string
  description?: string
}

export interface Product {
  id: string
  name: string
  description?: string
  sku?: string
  categoryId?: string
  categoryName?: string
  unitPrice: number
  currentStock: number
  minStockLevel: number
  isActive: boolean
}

export interface InventoryMovement {
  id: string
  productId: string
  productName: string
  movementType: string
  quantity: number
  movementDate: string
  unitPrice?: number
  totalValue?: number
  newStock: number
  referenceId?: string
  userName?: string
}

export interface CAI {
  id: string
  caiNumber: string
  issueDate: string
  dueDate: string
  initialRange: string
  finalRange: string
  currentCorrelative: string
  status: string
  isExpiringSoon: boolean
}

export interface Invoice {
  id: string
  caiId: string
  documentAuthorizationId?: string
  caiNumber: string
  caiNumberSnapshot?: string
  authorizationRangeSnapshot?: string
  authorizationDueDateSnapshot?: string
  correlativeNumber: string
  invoiceDate: string
  customerId: string
  customerName: string
  rtnCliente?: string
  customerAddress?: string
  subTotal: number
  isvAmount: number
  isv15Amount: number
  isv18Amount: number
  touristTaxAmount: number
  discountsAmount: number
  totalAmount: number
  taxableAmount: number
  exemptAmount: number
  exoneratedAmount: number
  taxpayerType: string
  exonerationOrderNumber?: string
  sefinExonerationCertificateNumber?: string
  sagRegistryNumber?: string
  isIsvExempt: boolean
  isTouristTaxExempt: boolean
  originalInvoiceId?: string
  originalCorrelativeNumber?: string
  reason?: string
  documentType: string
  status: string
  items: InvoiceItem[]
}

export interface InvoiceItem {
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
  isExempt: boolean
  isvRate: number
  isTouristTaxable: boolean
  discountPercentage: number
}

export interface CashRegister {
  id: string
  name: string
  description?: string
  isActive: boolean
}

export interface CashMovement {
  id: string
  cashRegisterId: string
  cashRegisterName: string
  userId: string
  userName: string
  movementType: string
  amount: number
  description?: string
  movementDate: string
  balanceAfter: number
}

export interface DocumentAuthorization {
  id: string
  documentType: string
  caiNumber: string
  issueDate: string
  dueDate: string
  initialRange: string
  finalRange: string
  currentCorrelative: string
  status: string
  isExpiringSoon: boolean
  attachmentPath?: string
}

export interface BusinessSettings {
  businessName: string
  rtn: string
  address: string
  phone: string
  email: string
  logoBase64?: string
  footer: string
  isvRate: number
  touristTaxRate: number
  printPrinterName?: string
  printWidth: number
  printLogoHeight: number
  printFontSize: string
  printLineSpacing: number
  showLogo: boolean
  showHeader: boolean
  showFiscal: boolean
  showGuest: boolean
  showItems: boolean
  showTotals: boolean
  showPayment: boolean
  showFooter: boolean
  headerAlign: string
  separatorChar: string
  marginLeft: number
}

export interface InvoicePrintData {
  header: { logo: string; hotel: string; rtn: string; direccion: string; telefono: string }
  tipo: string
  factura: { cai: string; correlativo: string; rango: string; fecha: string; fechaLimite: string }
  cliente: { nombre: string; rtn: string; checkIn?: string; checkOut?: string }
  items: { desc: string; cant: number; precio: number; total: number }[]
  totales: { subtotal: number; isv: number; tasaTuristico: number; descuentos?: number; total: number }
  pago: { metodo: string; efectivoRecibido?: number; cambio?: number; letras: string }
  footer: string
}

export interface AccountingAccount {
  id: string
  accountNumber: string
  accountName: string
  accountType: string
  parentAccountId?: string
  isActive: boolean
  balance: number
  children: AccountingAccount[]
}

export interface AccountingEntry {
  id: string
  transactionDate: string
  description: string
  entryType: string
  referenceId?: string
  totalDebit: number
  totalCredit: number
  items: EntryItem[]
}

export interface EntryItem {
  id: string
  accountId: string
  accountNumber: string
  accountName: string
  debit: number
  credit: number
  description?: string
}

export interface DashboardStat {
  title: string
  value: string
  trend?: string
}

export interface DashboardAlert {
  title: string
  description: string
  severity: string
}

export interface DashboardInvoice {
  correlativeNumber: string
  customerName: string
  totalAmount: number
  status: string
  invoiceDate: string
}

export interface DashboardReservation {
  guestName: string
  roomNumber: string
  status: string
  checkInDate: string
  checkOutDate: string
}

export interface DashboardCash {
  registerName: string
  balance: number
}

export interface DashboardSummary {
  stats: DashboardStat[]
  alerts: DashboardAlert[]
  recentInvoices: DashboardInvoice[]
  upcomingReservations: DashboardReservation[]
  cash?: DashboardCash | null
  occupiedRooms: number
  freeRooms: number
  pendingCheckIns: number
  pendingCheckOuts: number
}

export interface TrialBalanceItem {
  accountId: string
  accountNumber: string
  accountName: string
  accountType: string
  previousBalance: number
  debit: number
  credit: number
  balance: number
}

export interface AccountMovement {
  date: string
  description: string
  debit: number
  credit: number
  balance: number
  referenceType: string
}

export interface CreateAccountRequest {
  accountNumber: string
  accountName: string
  accountType: string
  parentAccountId?: string
}

export interface Discount {
  id: string
  name: string
  description?: string
  discountType: string
  value: number
  isActive: boolean
  applicableTo?: string
  requiresDocument: boolean
  minAge?: number
  priority: number
}

export interface FolioItem {
  id: string
  description: string
  quantity: number
  unitPrice: number
  lineTotal: number
  isExempt: boolean
  isvRate: number
  isTouristTaxable: boolean
  discountPercentage: number
}

export interface Folio {
  id: string
  reservationId: string
  guestId: string
  guestName: string
  roomId: string
  roomNumber: string
  openingDate: string
  closingDate: string | null
  totalAmount: number
  status: string
  items: FolioItem[]
}

export interface BackupLog {
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

export interface Role {
  id: string
  name: string
  description?: string
  permissions?: string[]
}

export interface CreateUserRequest {
  username: string
  password: string
  email: string
  firstName: string
  lastName: string
  roles?: string[]
}

export interface UpdateUserRequest {
  firstName?: string
  lastName?: string
  email?: string
  isActive?: boolean
  roles?: string[]
}

