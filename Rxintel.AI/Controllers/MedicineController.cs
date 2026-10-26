using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Services.Interfaces;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicineController : BaseController
    {
        private readonly IMedicineService _medicineMasterService;
        public MedicineController(IMedicineService medicineMasterService)
        {
            _medicineMasterService = medicineMasterService;
        }
        [HttpGet]
        public async Task<PagedList<MedicineDto>> GetAll([FromQuery] PageParam pageParam)
        {
            return await _medicineMasterService.GetAll(pageParam);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<MedicineDto>> GetById(Guid id)
        {
            var result = await _medicineMasterService.GetByIdAsync(id);

            if (result == null)
                return NotFound();
             
            return Ok(result);
        }
        [HttpGet("dropdown")]
        public async Task<PagedList<MedicineDto>> MedicineDropdownById(
            [FromQuery] Guid? Id,
            [FromQuery] PageParam pageParam)
        {
            return await _medicineMasterService.MedicineDropdownById(Id, pageParam);
        }

        [HttpGet("lookup")]
        public async Task<ActionResult<Rxintel.Generic.Services.Helpers.MedicineMasterLookupResult>> Lookup(
            [FromQuery] string drugName,
            [FromQuery] string? dosageForm = null,
            [FromQuery] string? strength = null,
            [FromQuery] string? packageSize = null)
        {
            var result = await _medicineMasterService.LookupClinicalAsync(drugName, dosageForm, strength, packageSize);
            if (result == null) return NotFound();
            return Ok(result);
        }
        [HttpPost("upsert")]
        public async Task<SuccessResult<MedicineDto>> Upsert([FromBody] MedicineDto medicineDto)
        {
            return await _medicineMasterService.UpsertAsync(medicineDto);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<SuccessResult<MedicineDto>>> DeleteById(Guid id)
        {
            var result = await _medicineMasterService.DeleteAsync(id);
            if (!result.IsSuccessStatusCode)
                return BadRequest(result);
            return Ok(result);
        }
    }
}