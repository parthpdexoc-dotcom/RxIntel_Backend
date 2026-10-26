using System.Text.Json.Serialization;
using Rxintel.Generic.Enums;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class PharmacyUserDto
    {
        public Guid Id { get; set; }

        public Guid PharmacyId { get; set; }

        public string? Name { get; set; }

        public string? UserEmail { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Password { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? ConfirmPassword { get; set; }

        public PharmacyUserRole? Role { get; set; }

        public string? PrimaryContactName { get; set; }

        public string? PrimaryContactTitle { get; set; }

        public string? PrimaryContactPhone { get; set; }

        public string? PrimaryWholesalerName { get; set; }

        public string? PrimaryWholesalerContactName { get; set; }

        public string? PrimaryWholesalerPhone { get; set; }

        public string? PrimaryWholesalerEmail { get; set; }

        public bool IsActive { get; set; } = true;

        [JsonPropertyName("profileImage")]
        public string? ProfileImage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
    }
}
