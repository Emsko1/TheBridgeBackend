using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Bridge.Backend.Models
{
    public class RegisterDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; }

        public string Role { get; set; } = "Buyer"; // Default to Buyer

        // For Sellers
        public string? BusinessName { get; set; }

        public string? RegistrationNumber { get; set; }

        public List<string>? DocumentUrls { get; set; }
    }

    public class LoginDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
