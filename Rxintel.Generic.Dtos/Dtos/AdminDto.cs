using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Rxintel.Generic.Dtos.Dtos
{
    public class AdminDto 
    {
        public Guid Id { get; set; }
        public string? ConfirmPassword { get; set; }
   
        [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string? Name { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(50, ErrorMessage = "Email cannot exceed 50 characters")]
        public string? Email { get; set; }

        [MaxLength(50, ErrorMessage = "Password cannot exceed 50 characters")]
        public string? Password { get; set; }

        [MaxLength(50, ErrorMessage = "Role cannot exceed 50 characters")]
        public string? Role { get; set; }

        [JsonPropertyName("profileImage")]
        public string? ProfileImage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
    }
}