
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Dtos.Auth;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Dtos.Cash;
using hotel_erp.Api.Dtos.Accounting;
using hotel_erp.Api.Dtos.Payments;
using AutoMapper;
using CustomerDto = hotel_erp.Api.Dtos.Customer.CustomerDto;
using CreateCustomerRequest = hotel_erp.Api.Dtos.Customer.CreateCustomerRequest;
using UpdateCustomerRequest = hotel_erp.Api.Dtos.Customer.UpdateCustomerRequest;
using GuestDto = hotel_erp.Api.Dtos.Customer.GuestDto;
using CreateGuestRequest = hotel_erp.Api.Dtos.Customer.CreateGuestRequest;
using UpdateGuestRequest = hotel_erp.Api.Dtos.Customer.UpdateGuestRequest;
using UpdateClassificationRequest = hotel_erp.Api.Dtos.Customer.UpdateClassificationRequest;
using DiscountDto = hotel_erp.Api.Dtos.Discount.DiscountDto;
using CreateDiscountRequest = hotel_erp.Api.Dtos.Discount.CreateDiscountRequest;
using UpdateDiscountRequest = hotel_erp.Api.Dtos.Discount.UpdateDiscountRequest;

namespace hotel_erp.Api.Dtos.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User
            CreateMap<User, UserDto>()
                .ForMember(d => d.Roles, o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name)));
            CreateMap<Role, RoleDto>()
                .ForMember(d => d.IsSystem, o => o.MapFrom(s => s.SystemKey != null))
                .ForMember(d => d.Permissions, o => o.MapFrom(s => s.RolePermissions.Select(rp => rp.Permission.Name)));
            CreateMap<Permission, PermissionDto>();

            // Room
            CreateMap<RoomType, RoomTypeDto>();
            CreateMap<Room, RoomDto>()
                .ForMember(d => d.RoomTypeName, o => o.MapFrom(s => s.RoomType.Name))
                .ForMember(d => d.PricePerNight, o => o.MapFrom(s => s.RoomType.PricePerNight))
                .ForMember(d => d.Capacity, o => o.MapFrom(s => s.RoomType.Capacity))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            // Reservation
            CreateMap<Reservation, ReservationDto>()
                .ForMember(d => d.GuestName, o => o.MapFrom(s => s.Guest.FirstName + " " + s.Guest.LastName))
                .ForMember(d => d.RoomNumber, o => o.MapFrom(s => s.Room.RoomNumber))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            // Folio
            CreateMap<Folio, FolioDto>()
                .ForMember(d => d.GuestName, o => o.MapFrom(s => s.Guest.FirstName + " " + s.Guest.LastName))
                .ForMember(d => d.RoomNumber, o => o.MapFrom(s => s.Room.RoomNumber))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.FolioItems))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
            CreateMap<FolioItem, FolioItemDto>();

            // Customer
            CreateMap<global::hotel_erp.Api.Database.Entities.Customer, CustomerDto>()
                .ForMember(d => d.TaxpayerType, o => o.MapFrom(s => s.TaxpayerType.ToString()));
            CreateMap<Guest, GuestDto>()
                .ForMember(d => d.GuestRTN, o => o.MapFrom(s => s.RTN))
                .ForMember(d => d.TaxpayerType, o => o.MapFrom(s => s.TaxpayerType.ToString()));

            // Invoice
            CreateMap<CAI, CAIDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.IsExpiringSoon, o => o.MapFrom(s => s.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));
            CreateMap<DocumentAuthorization, DocumentAuthorizationDto>()
                .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.DocumentType.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.IsExpiringSoon, o => o.MapFrom(s => s.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))))
                .ForMember(d => d.HasAttachment, o => o.MapFrom(s => !string.IsNullOrEmpty(s.AttachmentPath)));
            CreateMap<Invoice, InvoiceDto>()
                .ForMember(d => d.CAINumber, o => o.MapFrom(s => s.CAINumberSnapshot ?? s.CAI.CAINumber))
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.InvoiceItems))
                .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.DocumentType.ToString()))
                .ForMember(d => d.TaxpayerType, o => o.MapFrom(s => s.TaxpayerType.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PaymentMethod, o => o.MapFrom(s =>
                    s.PaymentApplications.Any(application => application.Payment.Status != PaymentStatus.Anulado)
                        ? s.PaymentApplications
                            .Where(application => application.Payment.Status != PaymentStatus.Anulado)
                            .Select(application => application.Payment.Method)
                            .Distinct()
                            .Count() == 1
                                ? s.PaymentApplications
                                    .Where(application => application.Payment.Status != PaymentStatus.Anulado)
                                    .Select(application => application.Payment.Method.ToString())
                                    .First()
                                : "Mixto"
                        : s.PaymentMethod))
                .ForMember(d => d.PaidAmount, o => o.MapFrom(s => s.PaymentApplications
                    .Where(application => application.Payment.Status != PaymentStatus.Anulado)
                    .Sum(application => application.Amount)))
                .ForMember(d => d.CreditedAmount, o => o.MapFrom(s => s.CreditNotes
                    .Where(note => note.DocumentType == InvoiceDocumentType.NotaCredito)
                    .Sum(note => note.TotalAmount)))
                .ForMember(d => d.BalanceDue, o => o.MapFrom(s =>
                    s.DocumentType == InvoiceDocumentType.NotaCredito
                        ? 0m
                        : Math.Max(
                            0m,
                            s.TotalAmount
                                - s.CreditNotes
                                    .Where(note => note.DocumentType == InvoiceDocumentType.NotaCredito)
                                    .Sum(note => note.TotalAmount)
                                - s.PaymentApplications
                                    .Where(application => application.Payment.Status != PaymentStatus.Anulado)
                                    .Sum(application => application.Amount))));
            CreateMap<InvoiceItem, InvoiceItemDto>();

            // Tax Configuration
            CreateMap<TaxConfiguration, TaxConfigurationDto>();

            // Business Settings
            CreateMap<BusinessSettings, BusinessSettingsDto>()
                .ForMember(destination => destination.FiscalProfileStatus, options => options.MapFrom(source => source.FiscalProfileStatus.ToString()));

            // Discount
            CreateMap<global::hotel_erp.Api.Database.Entities.Discount, DiscountDto>()
                .ForMember(d => d.DiscountType, o => o.MapFrom(s => s.DiscountType.ToString()));

            // Supplier
            CreateMap<Supplier, SupplierDto>();

            // PurchaseInvoice
            CreateMap<PurchaseInvoice, PurchaseInvoiceDto>()
                .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier.Name))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
            CreateMap<PurchaseInvoiceItem, PurchaseInvoiceItemDto>();

            // Cash
            CreateMap<CashRegister, CashRegisterDto>();
            CreateMap<CashMovement, CashMovementDto>()
                .ForMember(d => d.CashRegisterName, o => o.MapFrom(s => s.CashRegister.Name))
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.FirstName + " " + s.User.LastName))
                .ForMember(d => d.MovementType, o => o.MapFrom(s => s.MovementType.ToString()));

            // Payments
            CreateMap<PaymentApplication, PaymentApplicationDto>()
                .ForMember(destination => destination.CorrelativeNumber,
                    options => options.MapFrom(source => source.Invoice.CorrelativeNumber));
            CreateMap<Payment, PaymentDto>()
                .ForMember(destination => destination.RecordedByUserName,
                    options => options.MapFrom(source => source.RecordedByUser.FirstName + " " + source.RecordedByUser.LastName))
                .ForMember(destination => destination.CashRegisterName,
                    options => options.MapFrom(source => source.CashRegisterId.HasValue ? source.CashRegister.Name : null))
                .ForMember(destination => destination.Method,
                    options => options.MapFrom(source => source.Method.ToString()))
                .ForMember(destination => destination.Status,
                    options => options.MapFrom(source => source.Status.ToString()))
                .ForMember(destination => destination.RefundedAmount,
                    options => options.MapFrom(source => source.Refunds
                        .Where(refund => refund.Status == RefundStatus.Confirmado)
                        .Sum(refund => refund.Amount)))
                .ForMember(destination => destination.CardSettledAmount,
                    options => options.MapFrom(source => source.CardSettlementApplications
                        .Where(application => application.CardSettlement.Status == CardSettlementStatus.Confirmado)
                        .Sum(application => application.Amount)));
            CreateMap<RefundApplication, RefundApplicationDto>()
                .ForMember(destination => destination.CreditNoteCorrelativeNumber,
                    options => options.MapFrom(source => source.CreditNote.CorrelativeNumber));
            CreateMap<Refund, RefundDto>()
                .ForMember(destination => destination.PaymentNumber,
                    options => options.MapFrom(source => source.Payment.PaymentNumber))
                .ForMember(destination => destination.RecordedByUserName,
                    options => options.MapFrom(source => source.RecordedByUser.FirstName + " " + source.RecordedByUser.LastName))
                .ForMember(destination => destination.CashRegisterName,
                    options => options.MapFrom(source => source.CashRegisterId.HasValue ? source.CashRegister!.Name : null))
                .ForMember(destination => destination.Method,
                    options => options.MapFrom(source => source.Method.ToString()))
                .ForMember(destination => destination.Status,
                    options => options.MapFrom(source => source.Status.ToString()));
            CreateMap<CardSettlementApplication, CardSettlementApplicationDto>()
                .ForMember(destination => destination.PaymentNumber,
                    options => options.MapFrom(source => source.Payment.PaymentNumber))
                .ForMember(destination => destination.PaymentDate,
                    options => options.MapFrom(source => source.Payment.PaymentDate))
                .ForMember(destination => destination.PaymentReference,
                    options => options.MapFrom(source => source.Payment.ExternalReference));
            CreateMap<CardSettlement, CardSettlementDto>()
                .ForMember(destination => destination.RecordedByUserName,
                    options => options.MapFrom(source => source.RecordedByUser.FirstName + " " + source.RecordedByUser.LastName))
                .ForMember(destination => destination.Status,
                    options => options.MapFrom(source => source.Status.ToString()));

            // Accounting
            CreateMap<AccountingAccount, AccountingAccountDto>()
                .ForMember(d => d.AccountType, o => o.MapFrom(s => s.AccountType.ToString()));
            CreateMap<AccountingEntry, AccountingEntryDto>()
                .ForMember(d => d.EntryType, o => o.MapFrom(s => s.EntryType.ToString()))
                .ForMember(d => d.TotalDebit, o => o.MapFrom(s => s.EntryItems.Sum(i => i.Debit)))
                .ForMember(d => d.TotalCredit, o => o.MapFrom(s => s.EntryItems.Sum(i => i.Credit)))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.EntryItems));
            CreateMap<EntryItem, EntryItemDto>()
                .ForMember(d => d.AccountNumber, o => o.MapFrom(s => s.Account.AccountNumber))
                .ForMember(d => d.AccountName, o => o.MapFrom(s => s.Account.AccountName));
        }
    }
}
