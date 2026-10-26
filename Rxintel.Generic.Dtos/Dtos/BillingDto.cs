namespace Rxintel.Generic.Dtos.Dtos
{
    public class BillingDto
    {
        public Guid Id { get; set; }

        public Guid PharmacyId { get; set; }

        public Guid InsuranceId { get; set; }

        public string? NDC { get; set; }

        public decimal QuantityBilled { get; set; }

        public string? RXNO { get; set; }

        public DateTime BillDate { get; set; }

        public string? DrugName { get; set; }

        public string? DrugForm { get; set; }

        public string? PackageSize { get; set; }

        public string? DrugTherapy { get; set; }

        public int? Days { get; set; }

        public decimal PrimaryInsurancePaidAmount { get; set; }

        public string? PrimaryInsuranceBinNo { get; set; }

        public decimal? SecondaryInsurancePaidAmount { get; set; }

        public Guid? SecondaryInsuranceId { get; set; }

        public string? SecondaryInsurance { get; set; }

        public string? SecondaryInsuranceBinNo { get; set; }

        public decimal? TotalInsurancePaidAmount { get; set; }

        public Guid? ImportErrorFileId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
