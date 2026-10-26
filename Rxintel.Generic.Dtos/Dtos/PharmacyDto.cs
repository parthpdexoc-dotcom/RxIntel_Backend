namespace Rxintel.Generic.Dtos.Dtos
{
    public class PharmacyDto : PharmacyUserDto
    {
        public string DEA { get; set; }

        public DateTime? DeaExpirationDate { get; set; }

        public string? BusinessLegalName { get; set; }

        public string BusinessTradeName { get; set; }

        public string? NPI { get; set; }

        public string? PharmacyAddress { get; set; }

        public string? StreetAddress { get; set; }

        public string? City { get; set; }

        public Guid? StateId { get; set; }

        public string? StateName { get; set; }

        public Guid? PharmacySoftwareId { get; set; }

        public string? ZipCode { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Fax { get; set; }

        public string? StateBoardOfPharmacyLicenseNumber { get; set; }

        public DateTime? BopLicenseExpirationDate { get; set; }

        public string? GlobalLocationNumber { get; set; }

        public string? PharmacyBillingSoftwareName { get; set; }

        public string? PharmacyBillingSoftwarePhone { get; set; }

        public string? PharmacyBillingSoftwareEmail { get; set; }

        public string? Email { get; set; }

        public bool HasDeaFile { get; set; }

        public string? DeaFileName { get; set; }

        public bool HasStateBoardFile { get; set; }

        public string? StateBoardFileName { get; set; }

        public bool AcceptedTermsAndConditions { get; set; }
    }
}
