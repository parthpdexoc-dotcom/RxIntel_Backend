namespace Rxintel.Generic.Dtos.Dtos
{
    public class MedicineDto
    {
        public Guid Id { get; set; }

        public string? NDC { get; set; }

        public string? Brand { get; set; }

        public string? Strength { get; set; }

        /// <summary>Strength unit labels (e.g. mg/mL, g/mL). Comma-separated when multiple.</summary>
        public string? StrengthUnit { get; set; }

        public string? PackageSize { get; set; }

        /// <summary>Dosage form labels (e.g. TABLET, SPRAY). Comma-separated when multiple.</summary>
        public string? DosageForm { get; set; }

        public string? ProductNDC { get; set; }

        public string? Suffix { get; set; }

        public string? Generic { get; set; }

        public string? Route { get; set; }

        public string? Labeler { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
