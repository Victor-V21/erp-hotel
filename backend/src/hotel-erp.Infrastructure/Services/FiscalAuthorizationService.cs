using hotel_erp.Application.Interfaces;
using hotel_erp.Application.Services;
using hotel_erp.Domain.Enums;
using hotel_erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Infrastructure.Services
{
    public class FiscalAuthorizationService : IFiscalAuthorizationService
    {
        private readonly ApplicationDbContext _context;

        public FiscalAuthorizationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FiscalCorrelativeResult> GetNextCorrelativeAsync(InvoiceDocumentType documentType, Guid? authorizationId = null)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var query = _context.DocumentAuthorizations.Where(a => a.DocumentType == documentType && a.Status == CAIStatus.Activo);
            var authorization = authorizationId.HasValue
                ? await query.FirstOrDefaultAsync(a => a.Id == authorizationId.Value)
                : await query.OrderBy(a => a.DueDate).FirstOrDefaultAsync();

            if (authorization == null)
                throw new InvalidOperationException($"No hay autorización fiscal activa para {documentType}");

            var now = HondurasTime.Now;
            if (authorization.DueDate <= now)
            {
                authorization.Status = CAIStatus.Vencido;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                throw new InvalidOperationException($"La autorización fiscal para {documentType} está vencida");
            }

            var parts = authorization.CurrentCorrelative.Split('-');
            if (parts.Length != 4)
                throw new InvalidOperationException("Formato de correlativo inválido");

            var sequential = int.Parse(parts[3]) + 1;
            var finalSeq = int.Parse(authorization.FinalRange.Split('-').Last());
            if (sequential > finalSeq)
            {
                authorization.Status = CAIStatus.Agotado;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                throw new InvalidOperationException($"La autorización fiscal para {documentType} agotó su rango");
            }

            var correlative = $"{parts[0]}-{parts[1]}-{parts[2]}-{sequential:D8}";
            authorization.CurrentCorrelative = correlative;
            _context.DocumentAuthorizations.Update(authorization);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new FiscalCorrelativeResult(
                authorization.Id,
                correlative,
                authorization.CAINumber,
                authorization.InitialRange,
                authorization.FinalRange,
                authorization.DueDate);
        }
    }
}
