using AutoMapper;
using hotel_erp.Api.Dtos.Common;
using hotel_erp.Api.Services.Interfaces;
using hotel_erp.Api.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace hotel_erp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CAIController : ControllerBase
    {
        private readonly ICAIRepository _repo;
        private readonly IMapper _mapper;

        public CAIController(ICAIRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CAIDto>>> GetAll()
            => Ok(_mapper.Map<IEnumerable<CAIDto>>(await _repo.GetAllAsync()));

        [HttpGet("active")]
        public async Task<ActionResult<CAIDto>> GetActive()
        {
            var cai = await _repo.GetActiveCAIAsync();
            if (cai == null) return NotFound("No hay un CAI activo");
            return Ok(_mapper.Map<CAIDto>(cai));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CAIDto>> GetById(Guid id)
        {
            var cai = await _repo.GetByIdAsync(id);
            if (cai == null) return NotFound();
            return Ok(_mapper.Map<CAIDto>(cai));
        }

        [HttpPost]
        public async Task<ActionResult<CAIDto>> Create([FromBody] CreateCAIRequest request)
        {
            var existing = await _repo.GetByCAINumberAsync(request.CAINumber);
            if (existing != null) return BadRequest("El número de CAI ya existe");

            var activeCai = await _repo.GetActiveCAIAsync();
            if (activeCai != null) return BadRequest("Ya existe un CAI activo. Desactívelo antes de crear uno nuevo.");

            var cai = new CAI
            {
                CAINumber = request.CAINumber,
                IssueDate = request.IssueDate,
                DueDate = request.DueDate,
                InitialRange = request.InitialRange,
                FinalRange = request.FinalRange,
                CurrentCorrelative = request.InitialRange,
                Status = CAIStatus.Activo
            };

            await _repo.AddAsync(cai);
            return CreatedAtAction(nameof(GetById), new { id = cai.Id }, _mapper.Map<CAIDto>(cai));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}

