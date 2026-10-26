namespace Rxintel.Generic.Dtos.Dtos
{
    public class WholesalerDto
    {
        public Guid Id { get; set; }

        public string WholesalerName { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public bool Vawd { get; set; }

        public DateTime? VawdDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
