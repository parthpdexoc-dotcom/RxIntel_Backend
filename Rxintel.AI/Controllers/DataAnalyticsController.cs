using Microsoft.AspNetCore.Mvc;
using Rxintel.Generic.Api;
using Rxintel.Generic.Dtos.Dtos;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataAnalyticsController : BaseController
    {
        private readonly IDataAnalyticsService _dataAnalyticsService;

        public DataAnalyticsController(IDataAnalyticsService dataAnalyticsService)
        {
            _dataAnalyticsService = dataAnalyticsService;
        }

        /// <summary>
        /// Data and Analytics report — aggregated by NDC.
        /// SplitDates off: StartDate/EndDate filter billing by BillDate; purchases are included for matching NDCs.
        /// SplitDates on: Ordered range filters Purchase.InvoiceDate; Billing range filters Billing.BillDate.
        /// AdvancedView: per-insurance actual billed/net quantities (no parent/child family rollup).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<DataAnalyticsResponseDto>> Get([FromQuery] DataAnalyticsQueryParam param)
        {
            var dateError = ValidateDateParams(param);
            if (dateError != null)
                return BadRequest(new { message = dateError });

            if (param.PharmacyId == Guid.Empty)
                return BadRequest(new { message = "PharmacyId is required." });

            var result = await _dataAnalyticsService.GetAnalyticsAsync(param);
            return Ok(result);
        }

        [HttpGet("export")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportGet([FromQuery] DataAnalyticsQueryParam param)
        {
            return await HandleExportAsync(param);
        }

        [HttpPost("export")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportPost([FromBody] DataAnalyticsQueryParam param)
        {
            return await HandleExportAsync(param);
        }

        private async Task<IActionResult> HandleExportAsync(DataAnalyticsQueryParam param)
        {
            var dateError = ValidateDateParams(param);
            if (dateError != null)
                return BadRequest(new { message = dateError });

            if (param.PharmacyId == Guid.Empty)
                return BadRequest(new { message = "PharmacyId is required." });

            var bytes = await _dataAnalyticsService.ExportAnalyticsAsync(param);
            var fileName = $"DataAnalytics_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static string? ValidateDateParams(DataAnalyticsQueryParam param)
        {
            if (param.SplitDates)
            {
                if (param.BillingStartDate == null || param.BillingEndDate == null
                    || param.OrderedStartDate == null || param.OrderedEndDate == null)
                    return "Billing and ordered date ranges are required when SplitDates is enabled.";

                if (param.BillingEndDate < param.BillingStartDate)
                    return "Billing end date must be on or after billing start date.";

                if (param.OrderedEndDate < param.OrderedStartDate)
                    return "Ordered end date must be on or after ordered start date.";

                return null;
            }

            if (param.EndDate < param.StartDate)
                return "EndDate must be on or after StartDate.";

            return null;
        }
    }
}