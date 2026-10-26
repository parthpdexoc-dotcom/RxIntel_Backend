using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/sig-mapping")]
    [Route("api/erx-sig-mapping")]
    [Route("api/erx-audit/sig-mappings")]
    public class ErxSigMappingController : BaseController
    {
        private readonly IErxSigMappingService _sigMappingService;
        private readonly ITokenParser _tokenParser;

        public ErxSigMappingController(
            IErxSigMappingService sigMappingService,
            ITokenParser tokenParser)
        {
            _sigMappingService = sigMappingService;
            _tokenParser = tokenParser;
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<ErxSigMappingDto>>> GetSigMappings(
            [FromQuery] PageParam pageParam,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? category = null)
        {
            var mappings = await _sigMappingService.GetAllMappingsAsync(pageParam, searchTerm, category);
            return Ok(mappings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ErxSigMappingDto>> GetSigMappingById(int id)
        {
            var mapping = await _sigMappingService.GetByIdAsync(id);
            if (mapping == null) return NotFound();
            return Ok(mapping);
        }

        [HttpPost]
        public async Task<ActionResult<SuccessResult<ErxSigMappingDto>>> CreateSigMapping([FromBody] CreateErxSigMappingDto dto)
        {
            if (!_tokenParser.IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new SuccessResult<ErxSigMappingDto>
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Access denied. Only system administrators can create SIG mappings."
                });
            }

            var result = await _sigMappingService.CreateMappingAsync(dto);
            if (!result.IsSuccessStatusCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SuccessResult<ErxSigMappingDto>>> UpdateSigMapping(int id, [FromBody] UpdateErxSigMappingDto dto)
        {
            if (!_tokenParser.IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new SuccessResult<ErxSigMappingDto>
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Access denied. Only system administrators can update SIG mappings."
                });
            }

            var result = await _sigMappingService.UpdateMappingAsync(id, dto);
            if (!result.IsSuccessStatusCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<SuccessResult<string>>> DeleteSigMapping(int id)
        {
            if (!_tokenParser.IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new SuccessResult<string>
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Access denied. Only system administrators can delete SIG mappings."
                });
            }

            var result = await _sigMappingService.DeleteMappingAsync(id);
            if (!result.IsSuccessStatusCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPut("{id}/toggle")]
        public async Task<ActionResult<SuccessResult<string>>> ToggleSigMappingActive(int id)
        {
            if (!_tokenParser.IsAdmin())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new SuccessResult<string>
                {
                    Status = StatusCodes.Status403Forbidden,
                    Message = "Access denied. Only system administrators can toggle SIG mappings."
                });
            }

            var result = await _sigMappingService.ToggleActiveAsync(id);
            if (!result.IsSuccessStatusCode)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("test-comparison")]
        public async Task<ActionResult<TestSigComparisonResponseDto>> TestSigComparison([FromBody] TestSigComparisonRequestDto dto)
        {
            var result = await _sigMappingService.TestComparisonAsync(dto);
            return Ok(result);
        }
    }
}
