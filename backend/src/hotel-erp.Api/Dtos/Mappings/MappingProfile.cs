
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Dtos.Auth;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Dtos.Cash;
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
                .ForMember(d => d.IsExpiringSoon, o => o.MapFrom(s => s.DueDate <= DateTime.UtcNow.AddDays(30)))
                .ForMember(d => d.AttachmentPath, o => o.MapFrom(s => s.AttachmentPath));
            CreateMap<Invoice, InvoiceDto>()
                .ForMember(d => d.CAINumber, o => o.MapFrom(s => s.CAI.CAINumber))
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId))
                .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.DocumentType.ToString()))
                .ForMember(d => d.TaxpayerType, o => o.MapFrom(s => s.TaxpayerType.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            // Tax Configuration
            CreateMap<TaxConfiguration, TaxConfigurationDto>();

            // Business Settings
            CreateMap<BusinessSettings, BusinessSettingsDto>();

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
        }
    }
}




