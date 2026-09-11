using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Services;
using hotel_erp.Api.Database.Entities;
using hotel_erp.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace hotel_erp.Api.Services
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
            return await PostgresCorrelativeLock.ExecuteAsync(_context, $"auth:{documentType}", async () =>
            {
                var query = _context.DocumentAuthorizations.Where(a => a.DocumentType == documentType && a.Status == CAIStatus.Activo);
                var authorization = authorizationId.HasValue
                    ? await query.FirstOrDefaultAsync(a => a.Id == authorizationId.Value)
                    : await query.OrderBy(a => a.DueDate).FirstOrDefaultAsync();

                if (authorization == null)
                    throw new InvalidOperationException($"No hay autorización fiscal activa para {documentType}");

                var now = DateOnly.FromDateTime(HondurasTime.Now);
                if (authorization.DueDate < now)
                {
                    authorization.Status = CAIStatus.Vencido;
                    await _context.SaveChangesAsync();
                    throw new CorrelativeStateException($"La autorización fiscal para {documentType} está vencida");
                }

                var parts = authorization.CurrentCorrelative.Split('-');
                if (parts.Length != 4)
                    throw new InvalidOperationException("Formato de correlativo inválido");

                var hasIssuedDocuments = await _context.Invoices
                    .IgnoreQueryFilters()
                    .AnyAsync(invoice => invoice.DocumentAuthorizationId == authorization.Id);
                var sequential = !hasIssuedDocuments && authorization.CurrentCorrelative == authorization.InitialRange
                    ? int.Parse(parts[3])
                    : int.Parse(parts[3]) + 1;
                var finalSeq = int.Parse(authorization.FinalRange.Split('-').Last());
                if (sequential > finalSeq)
                {
                    authorization.Status = CAIStatus.Agotado;
                    await _context.SaveChangesAsync();
                    throw new CorrelativeStateException($"La autorización fiscal para {documentType} agotó su rango");
                }

                var correlative = $"{parts[0]}-{parts[1]}-{parts[2]}-{sequential:D8}";
                authorization.CurrentCorrelative = correlative;
                _context.DocumentAuthorizations.Update(authorization);
                await _context.SaveChangesAsync();

                return new FiscalCorrelativeResult(
                    authorization.Id,
                    correlative,
                    authorization.CAINumber,
                    authorization.InitialRange,
                    authorization.FinalRange,
                    authorization.DueDate);
            });
        }
    }
}
