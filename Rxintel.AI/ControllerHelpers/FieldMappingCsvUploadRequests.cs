using Microsoft.AspNetCore.Http;

namespace Rxintel.AI.ControllerHelpers
{
    public sealed class BillingFieldMappingCsvUploadRequest
    {
        public IFormFile File { get; set; } = default!;
        public Guid PharmacySoftwareId { get; set; }
    }

    public sealed class WholesalerFieldMappingCsvUploadRequest
    {
        public IFormFile File { get; set; } = default!;
        public Guid WholesalerId { get; set; }
    }

    public sealed class PurchaseCsvUploadRequest
    {
        public IFormFile? File { get; set; }
        public Guid PharmacyId { get; set; }
        public Guid WholesalerId { get; set; }
    }

    public sealed class BillingCsvUploadRequest
    {
        public IFormFile? File { get; set; }
        public Guid PharmacyId { get; set; }
        public Guid PharmacySoftwareId { get; set; }
    }
}
