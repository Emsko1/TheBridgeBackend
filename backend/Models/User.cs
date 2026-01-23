using System;

namespace Bridge.Backend.Models {
  public class User {
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; } // Buyer|Seller|Admin
    public string? KycStatus { get; set; } // None|Submitted|Verified|Rejected
    public string? PasswordHash { get; set; }
    
    // KYB/KYS Fields
    public string? BusinessName { get; set; }
    public string? RegistrationNumber { get; set; }
    public List<string>? DocumentUrls { get; set; }
    public string? RejectionReason { get; set; }

    // OTP Verification
    public string? OtpCode { get; set; }
    public DateTimeOffset? OtpExpiration { get; set; }
    public bool IsEmailVerified { get; set; } = false;
  }
}
