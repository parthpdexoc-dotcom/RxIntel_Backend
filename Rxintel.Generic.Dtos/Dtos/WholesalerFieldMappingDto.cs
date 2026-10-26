using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class WholesalerFieldMappingDto
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "WholesalerId is required")]
        public Guid WholesalerId { get; set; }

        [MaxLength(200, ErrorMessage = "Source Column Name cannot exceed 200 characters")]
        public string? SourceColumnName { get; set; }

        [Required(ErrorMessage = "Destination Column Name is required")]
        [MaxLength(100, ErrorMessage = "Destination Column Name cannot exceed 100 characters")]
        public string DestinationColumnName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }

    /// <summary>CSV upload response: CSV headers + suggested rows as main DTO.</summary>
    public class WholesalerFieldMappingCsvPreviewDto
    {
        public string? Message { get; set; }
        public IEnumerable<string> DestinationColumns { get; set; } = new List<string>();
        public IEnumerable<WholesalerFieldMappingDto> Rows { get; set; } = new List<WholesalerFieldMappingDto>();
    }
}
