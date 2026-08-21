using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/document-authorizations")]
    [Authorize]
    public class DocumentAuthorizationsController : ControllerBase
    {
        private readonly IDocumentAuthorizationRepository _repo;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public DocumentAuthorizationsController(IDocumentAuthorizationRepository repo, IMapper mapper, IWebHostEnvironment env)
        {
            _repo = repo;
            _mapper = mapper;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentAuthorizationDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<DocumentAuthorizationDto>>(await _repo.GetAllAsync()));

        [HttpGet("active/{documentType}")]
        public async Task<ActionResult<DocumentAuthorizationDto>> GetActive(string documentType)
        {
            if (!Enum.TryParse<InvoiceDocumentType>(documentType, out var type))
                return BadRequest("Tipo de documento fiscal inválido");

            var authorization = await _repo.GetActiveAsync(type);
            if (authorization == null) return NotFound($"No hay autorización activa para {documentType}");
            return Ok(_mapper.Map<DocumentAuthorizationDto>(authorization));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentAuthorizationDto>> GetById(Guid id)
        {
            var authorization = await _repo.GetByIdAsync(id);
            if (authorization == null) return NotFound();
            return Ok(_mapper.Map<DocumentAuthorizationDto>(authorization));
        }

        [HttpPost]
        public async Task<ActionResult<DocumentAuthorizationDto>> Create([FromBody] CreateDocumentAuthorizationRequest request)
        {
            if (!Enum.TryParse<InvoiceDocumentType>(request.DocumentType, out var type)
                || type is not (InvoiceDocumentType.Factura or InvoiceDocumentType.NotaCredito or InvoiceDocumentType.NotaDebito))
                return BadRequest("Tipo de documento fiscal inválido");

            var existing = await _repo.GetByCAIAsync(type, request.CAINumber);
            if (existing != null) return BadRequest("Ya existe esa autorización para el tipo de documento indicado");

            var authorization = new DocumentAuthorization
            {
                DocumentType = type,
                CAINumber = request.CAINumber,
                IssueDate = request.IssueDate,
                DueDate = request.DueDate,
                InitialRange = request.InitialRange,
                FinalRange = request.FinalRange,
                CurrentCorrelative = request.InitialRange,
                Status = CAIStatus.Activo
            };

            await _repo.AddAsync(authorization);
            return CreatedAtAction(nameof(GetById), new { id = authorization.Id }, _mapper.Map<DocumentAuthorizationDto>(authorization));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("with-file")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<DocumentAuthorizationDto>> CreateWithFile([FromForm] CreateDocumentAuthorizationRequest request, IFormFile? file)
        {
            if (!Enum.TryParse<InvoiceDocumentType>(request.DocumentType, out var type)
                || type is not (InvoiceDocumentType.Factura or InvoiceDocumentType.NotaCredito or InvoiceDocumentType.NotaDebito))
                return BadRequest("Tipo de documento fiscal inválido");

            var existing = await _repo.GetByCAIAsync(type, request.CAINumber);
            if (existing != null) return BadRequest("Ya existe esa autorización para el tipo de documento indicado");

            var authorization = new DocumentAuthorization
            {
                DocumentType = type,
                CAINumber = request.CAINumber,
                IssueDate = request.IssueDate,
                DueDate = request.DueDate,
                InitialRange = request.InitialRange,
                FinalRange = request.FinalRange,
                CurrentCorrelative = request.InitialRange,
                Status = CAIStatus.Activo
            };

            if (file != null && file.Length > 0)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".pdf" || (file.ContentType != "application/pdf" && file.ContentType != "application/octet-stream"))
                    return BadRequest("Solo se permiten archivos PDF");

                var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads", "authorizations");
                Directory.CreateDirectory(uploadDir);

                var fileName = $"autorizacion_{request.CAINumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                authorization.AttachmentPath = filePath;
            }

            await _repo.AddAsync(authorization);
            return CreatedAtAction(nameof(GetById), new { id = authorization.Id }, _mapper.Map<DocumentAuthorizationDto>(authorization));
        }

        [HttpGet("{id}/file")]
        public async Task<ActionResult> GetFile(Guid id)
        {
            var authorization = await _repo.GetByIdAsync(id);
            if (authorization == null) return NotFound();
            if (string.IsNullOrEmpty(authorization.AttachmentPath) || !System.IO.File.Exists(authorization.AttachmentPath))
                return NotFound("No hay archivo adjunto para esta autorización");

            var ext = Path.GetExtension(authorization.AttachmentPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };

            return PhysicalFile(authorization.AttachmentPath, contentType, $"autorizacion_{authorization.CAINumber}.pdf");
        }
    }
}

