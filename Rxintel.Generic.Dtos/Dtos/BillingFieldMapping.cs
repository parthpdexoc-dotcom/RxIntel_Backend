using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class BillingFieldMappingDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "PharmacySoftwareId is required")]
        public Guid PharmacySoftwareId { get; set; }

        [MaxLength(200, ErrorMessage = "Source Field Name cannot exceed 200 characters")]
        public string? SourceFieldName { get; set; }

        [Required(ErrorMessage = "Destination Field Name is required")]
        [MaxLength(100, ErrorMessage = "Destination Field Name cannot exceed 100 characters")]
        public string DestinationFieldName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }

    /// <summary>CSV upload response: CSV headers + suggested rows as main DTO.</summary>
    public class BillingFieldMappingCsvPreviewDto
    {
        public string? Message { get; set; }
        public IEnumerable<string> DestinationColumns { get; set; } = new List<string>();
        public IEnumerable<BillingFieldMappingDto> Rows { get; set; } = new List<BillingFieldMappingDto>();
    }

}
