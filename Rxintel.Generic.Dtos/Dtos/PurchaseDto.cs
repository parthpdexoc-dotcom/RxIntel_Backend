namespace Rxintel.Generic.Dtos.Dtos
{
    public class PurchaseDto
    {
        public Guid Id { get; set; }

        public Guid PharmacyId { get; set; }

        public Guid WholesalerId { get; set; }

        public string? NDC { get; set; }

        public decimal QuantityPurchased { get; set; }

        /// <summary>Total units ordered (QuantityPurchased × PackageSize). Set automatically on save.</summary>
        public decimal TotalQuantityOrdered { get; set; }

        public string? PackageSize { get; set; }

        public string? Description { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public string? InvoiceNumber { get; set; }

        public Guid? ImportErrorFileId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
