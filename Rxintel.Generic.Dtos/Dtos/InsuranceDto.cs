namespace Rxintel.Generic.Dtos.Dtos
{
    public class InsuranceDto
    {
        public Guid Id { get; set; }

        public string? InsuranceName { get; set; }

        public string? BINNumber { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        public List<InsuranceGroupDto> InsuranceGroups { get; set; } = new();
    }
}
