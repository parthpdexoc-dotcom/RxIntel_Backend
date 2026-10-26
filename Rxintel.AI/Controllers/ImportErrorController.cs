using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Common.Helpers;
using Rxintel.Generic.Common.Implementations.Paginations;
using Rxintel.Generic.Common.Implementations.Responses;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Enums;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportErrorController : BaseController
{
    private readonly IImportErrorService _importErrorService;

    public ImportErrorController(IImportErrorService importErrorService)
    {
        _importErrorService = importErrorService;
    }

    /// <summary>Main grid � failed import files.</summary>
    [HttpGet]
    public async Task<PagedList<ImportErrorFileDto>> GetFiles([FromQuery] ImportType importType,[FromQuery] PageParam pageParam)
    {
        return await _importErrorService.GetFiles(importType, pageParam);
    }

    /// <summary>Inner grid � error rows for one file.</summary>
    [HttpGet("{fileId:guid}/details")]
    public async Task<PagedList<ImportErrorDetailDto>> GetDetailsByFileId(Guid fileId,[FromQuery] PageParam pageParam)
    {
        return await _importErrorService.GetDetailsByFileId(fileId, pageParam);
    }

    /// <summary>View modal � all error messages for one inner-grid row.</summary>
    [HttpGet("details/{detailId:guid}")]
    public async Task<ActionResult<SuccessResult<ImportErrorDetailDto>>> GetDetailById(Guid detailId)
    {
        var result = await _importErrorService.GetDetailById(detailId);

        if (!result.IsSuccessStatusCode)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCount([FromQuery] ImportType importType)
    {
        return Ok(await _importErrorService.GetCount(importType));
    }

    [HttpGet("unread-counts")]
    public async Task<ActionResult<ImportErrorUnreadCountDto>> GetUnreadCounts()
    {
        return Ok(await _importErrorService.GetUnreadCounts());
    }

    [HttpPut("mark-viewed")]
    public async Task<IActionResult> MarkAsViewed([FromQuery] ImportType importType)
    {
        await _importErrorService.MarkAsViewed(importType);
        return Ok();
    }

    [HttpGet("{fileId:guid}/download")]
    public async Task<IActionResult> DownloadErrors(Guid fileId)
    {
        var result = await _importErrorService.DownloadErrorsCsv(fileId);

        if (!result.IsSuccessStatusCode || result.Data == null)
            return BadRequest(result);

        return File(
            result.Data.Content,
            ImportFileHelper.GetErrorDownloadContentType(result.Data.FileName),
            result.Data.FileName);
    }

    [HttpDelete("{fileId:guid}")]
    public async Task<ActionResult<SuccessResult<ImportErrorFileDto>>> DeleteFile(Guid fileId)
    {
        var result = await _importErrorService.DeleteFile(fileId);

        if (!result.IsSuccessStatusCode)
            return BadRequest(result);

        return Ok(result);
    }
}
