namespace Rxintel.Generic.Dtos.Dtos
{
    public class TermsAndConditionsDto
    {
        public Guid Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public Guid? UpdatedByAdminId { get; set; }

        public string? UpdatedByAdminName { get; set; }
    }

    public class TermsAndConditionsUpdateDto
    {
        public string Content { get; set; } = string.Empty;
    }
}
