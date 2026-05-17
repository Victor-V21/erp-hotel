using AutoMapper;
using hotel_erp.Application.DTOs;
using hotel_erp.Domain.Entities;
using hotel_erp.Domain.Enums;

namespace hotel_erp.Application.Mappings
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
            CreateMap<Customer, CustomerDto>();
            CreateMap<Guest, GuestDto>()
                .ForMember(d => d.GuestRTN, o => o.MapFrom(s => s.RTN));

            // Invoice
            CreateMap<CAI, CAIDto>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.IsExpiringSoon, o => o.MapFrom(s => s.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));
            CreateMap<Invoice, InvoiceDto>()
                .ForMember(d => d.CAINumber, o => o.MapFrom(s => s.CAI.CAINumber))
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId))
                .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.DocumentType.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            // Tax Configuration
            CreateMap<TaxConfiguration, TaxConfigurationDto>();

            // Business Settings
            CreateMap<BusinessSettings, BusinessSettingsDto>();

            // Discount
            CreateMap<Discount, DiscountDto>()
                .ForMember(d => d.DiscountType, o => o.MapFrom(s => s.DiscountType.ToString()));

            // Cash
            CreateMap<CashRegister, CashRegisterDto>();
            CreateMap<CashMovement, CashMovementDto>()
                .ForMember(d => d.CashRegisterName, o => o.MapFrom(s => s.CashRegister.Name))
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.FirstName + " " + s.User.LastName))
                .ForMember(d => d.MovementType, o => o.MapFrom(s => s.MovementType.ToString()));
        }
    }
}
