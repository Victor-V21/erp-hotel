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
}

export interface Customer {
  id: string
  rtn?: string
  name: string
  address?: string
  phone?: string
  email?: string
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
