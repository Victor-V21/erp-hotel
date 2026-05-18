using hotel_erp.Domain.Entities;
using hotel_erp.Domain.Enums;

namespace hotel_erp.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId);
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId);
    }

    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByNameAsync(string name);
        Task<IEnumerable<Role>> GetAllAsync();
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId);
    }

    public interface IPermissionRepository
    {
        Task<Permission?> GetByIdAsync(Guid id);
        Task<Permission?> GetByNameAsync(string name);
        Task<IEnumerable<Permission>> GetAllAsync();
        Task AddAsync(Permission permission);
        Task DeleteAsync(Guid id);
    }

    public interface IRoomTypeRepository
    {
        Task<RoomType?> GetByIdAsync(Guid id);
        Task<RoomType?> GetByNameAsync(string name);
        Task<IEnumerable<RoomType>> GetAllAsync();
        Task AddAsync(RoomType roomType);
        Task UpdateAsync(RoomType roomType);
        Task DeleteAsync(Guid id);
    }

    public interface IRoomRepository
    {
        Task<Room?> GetByIdAsync(Guid id);
        Task<Room?> GetByRoomNumberAsync(string roomNumber);
        Task<IEnumerable<Room>> GetAllAsync();
        Task<IEnumerable<Room>> GetByStatusAsync(string status);
        Task<IEnumerable<Room>> GetAvailableAsync(DateOnly checkIn, DateOnly checkOut);
        Task AddAsync(Room room);
        Task UpdateAsync(Room room);
        Task DeleteAsync(Guid id);
    }

    public interface IGuestRepository
    {
        Task<Guest?> GetByIdAsync(Guid id);
        Task<Guest?> GetByDocumentNumberAsync(string documentNumber);
        Task<IEnumerable<Guest>> GetAllAsync();
        Task<IEnumerable<Guest>> SearchAsync(string term);
        Task AddAsync(Guest guest);
        Task UpdateAsync(Guest guest);
        Task DeleteAsync(Guid id);
    }

    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(Guid id);
        Task<Customer?> GetByRTNAsync(string rtn);
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<IEnumerable<Customer>> SearchAsync(string term);
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(Guid id);
    }

    public interface IReservationRepository
    {
        Task<Reservation?> GetByIdAsync(Guid id);
        Task<IEnumerable<Reservation>> GetAllAsync();
        Task<IEnumerable<Reservation>> GetByGuestAsync(Guid guestId);
        Task<IEnumerable<Reservation>> GetByRoomAsync(Guid roomId);
        Task<IEnumerable<Reservation>> GetByDateRangeAsync(DateOnly start, DateOnly end);
        Task<IEnumerable<Reservation>> GetByStatusAsync(string status);
        Task AddAsync(Reservation reservation);
        Task UpdateAsync(Reservation reservation);
        Task DeleteAsync(Guid id);
    }

    public interface IFolioRepository
    {
        Task<Folio?> GetByIdAsync(Guid id);
        Task<Folio?> GetByReservationAsync(Guid reservationId);
        Task<IEnumerable<Folio>> GetAllAsync();
        Task AddAsync(Folio folio);
        Task UpdateAsync(Folio folio);
    }

    public interface ICAIRepository
    {
        Task<CAI?> GetByIdAsync(Guid id);
        Task<CAI?> GetByCAINumberAsync(string caiNumber);
        Task<IEnumerable<CAI>> GetAllAsync();
        Task<CAI?> GetActiveCAIAsync();
        Task AddAsync(CAI cai);
        Task UpdateAsync(CAI cai);
        Task DeleteAsync(Guid id);
    }

    public interface IDocumentAuthorizationRepository
    {
        Task<DocumentAuthorization?> GetByIdAsync(Guid id);
        Task<DocumentAuthorization?> GetActiveAsync(InvoiceDocumentType documentType);
        Task<DocumentAuthorization?> GetByCAIAsync(InvoiceDocumentType documentType, string caiNumber);
        Task<IEnumerable<DocumentAuthorization>> GetAllAsync();
        Task AddAsync(DocumentAuthorization authorization);
        Task UpdateAsync(DocumentAuthorization authorization);
        Task DeleteAsync(Guid id);
    }

    public interface IFiscalAuthorizationService
    {
        Task<FiscalCorrelativeResult> GetNextCorrelativeAsync(InvoiceDocumentType documentType, Guid? authorizationId = null);
    }

    public record FiscalCorrelativeResult(
        Guid AuthorizationId,
        string CorrelativeNumber,
        string CAINumber,
        string InitialRange,
        string FinalRange,
        DateTime DueDate);

    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(Guid id);
        Task<Invoice?> GetByCorrelativeAsync(string correlative);
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime start, DateTime end);
        Task<IEnumerable<Invoice>> GetByCustomerAsync(Guid customerId);
        Task<IEnumerable<Invoice>> GetByGuestDocumentAsync(string documentNumber);
        Task AddAsync(Invoice invoice);
        void DeleteInvoiceItems(Guid invoiceId);
        Task UpdateAsync(Invoice invoice);
        Task<string> GetNextCorrelativeAsync(Guid caiId);
    }

    public interface ICashRegisterRepository
    {
        Task<CashRegister?> GetByIdAsync(Guid id);
        Task<IEnumerable<CashRegister>> GetAllAsync();
        Task AddAsync(CashRegister cashRegister);
        Task UpdateAsync(CashRegister cashRegister);
    }

    public interface ICashMovementRepository
    {
        Task<CashMovement?> GetByIdAsync(Guid id);
        Task<IEnumerable<CashMovement>> GetByRegisterAsync(Guid cashRegisterId);
        Task<IEnumerable<CashMovement>> GetByDateRangeAsync(DateTime start, DateTime end);
        Task AddAsync(CashMovement cashMovement);
    }

    public interface IDiscountRepository
    {
        Task<Discount?> GetByIdAsync(Guid id);
        Task<IEnumerable<Discount>> GetAllAsync();
        Task<IEnumerable<Discount>> GetActiveAsync();
        Task AddAsync(Discount discount);
        Task UpdateAsync(Discount discount);
        Task DeleteAsync(Guid id);
    }

    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog auditLog);
        Task<IEnumerable<AuditLog>> GetByUserAsync(Guid userId);
        Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime start, DateTime end);
    }

    public interface IAccountingService
    {
        Task CreateInvoiceEntryAsync(Invoice invoice);
    }
}
