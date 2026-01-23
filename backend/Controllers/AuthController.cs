using Microsoft.AspNetCore.Mvc;
using Bridge.Backend.Data;
using Bridge.Backend.Models;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

namespace Bridge.Backend.Controllers {
  [ApiController]
  [Route("api/[controller]")]
  public class AuthController : ControllerBase {
    private readonly BridgeDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly Bridge.Backend.Services.IEmailService _emailService;

    public AuthController(BridgeDbContext db, IConfiguration cfg, Bridge.Backend.Services.IEmailService emailService)
    { 
        _db = db; 
        _cfg = cfg; 
        _emailService = emailService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] User user){
      try {
        // Validate required fields
        if(string.IsNullOrWhiteSpace(user?.Email) || string.IsNullOrWhiteSpace(user?.PasswordHash) || string.IsNullOrWhiteSpace(user?.Name)) {
          return BadRequest(new { message = "Name, Email and password are required" });
        }

        // Check if user already exists
        var exists = await _db.Users.AnyAsync(u => u.Email == user.Email);
        if(exists) return BadRequest(new { message = "User with this email already exists" });

        // Hash password
        var pwHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        
        // Create new user
        var newUser = new User {
          Id = Guid.NewGuid(),
          Name = user.Name,
          Email = user.Email,
          PasswordHash = pwHash,
          Role = user.Role ?? "Buyer", // Allow role selection or default to Buyer
          KycStatus = "Submitted", // Default to Submitted if docs provided, else None
          BusinessName = user.BusinessName,
          RegistrationNumber = user.RegistrationNumber,
          DocumentUrls = user.DocumentUrls ?? new List<string>()
        };

        if (newUser.DocumentUrls.Count == 0 && string.IsNullOrEmpty(newUser.BusinessName)) {
            newUser.KycStatus = "None";
        }
        
        // Check if email verification is enabled
        var enableEmailVerification = _cfg.GetValue<bool>("EmailSettings:EnableEmailVerification", true);

        if (enableEmailVerification)
        {
            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            newUser.OtpCode = otp;
            newUser.OtpExpiration = DateTimeOffset.UtcNow.AddMinutes(10);
            newUser.IsEmailVerified = false;

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            // Send OTP Email
            try {
                await _emailService.SendEmailAsync(newUser.Email, "Verify your email", $"Your verification code is: <b>{otp}</b>. It expires in 10 minutes.");
            } catch (Exception emailEx) {
                // Rollback user creation
                _db.Users.Remove(newUser);
                await _db.SaveChangesAsync();
                return BadRequest(new { message = "Registration failed: Could not send verification email. Please check your email address or try again later. " + emailEx.Message });
            }
            
            return Ok(new { 
              message = "Registration successful. Please check your email for verification code.",
              user = new { 
                id = newUser.Id, 
                email = newUser.Email, 
                name = newUser.Name, 
                role = newUser.Role,
                kycStatus = newUser.KycStatus,
                isEmailVerified = newUser.IsEmailVerified
              } 
            });
        }
        else
        {
            // Bypass verification
            newUser.IsEmailVerified = true;
            newUser.OtpCode = null;
            newUser.OtpExpiration = null;

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            return Ok(new { 
              message = "Registration successful.",
              user = new { 
                id = newUser.Id, 
                email = newUser.Email, 
                name = newUser.Name, 
                role = newUser.Role,
                kycStatus = newUser.KycStatus,
                isEmailVerified = newUser.IsEmailVerified
              } 
            });
        }
      } catch(Exception ex) {
        Console.WriteLine($"Register error: {ex.Message}");
        return BadRequest(new { message = "Registration failed: " + ex.Message });
      }
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request) {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return BadRequest(new { message = "User not found" });

        if (user.IsEmailVerified) return BadRequest(new { message = "Email already verified" });

        if (user.OtpCode != request.Otp || user.OtpExpiration < DateTimeOffset.UtcNow) {
            return BadRequest(new { message = "Invalid or expired OTP" });
        }

        user.IsEmailVerified = true;
        user.OtpCode = null;
        user.OtpExpiration = null;
        await _db.SaveChangesAsync();

        var token = GenerateJwt(user);
        return Ok(new { 
            message = "Email verified successfully", 
            token, 
            user = new { user.Id, user.Email, user.Name, user.Role, user.IsEmailVerified } 
        });
    }

    public class VerifyEmailRequest {
        public string Email { get; set; }
        public string Otp { get; set; }
    }

    [HttpPost("kyc/status/{userId}")]
    [Authorize] // Should be Admin only in real app
    public async Task<IActionResult> UpdateKycStatus(Guid userId, [FromBody] User statusUpdate) {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });

        user.KycStatus = statusUpdate.KycStatus;
        if (!string.IsNullOrEmpty(statusUpdate.RejectionReason)) {
            user.RejectionReason = statusUpdate.RejectionReason;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"KYC status updated to {user.KycStatus}" });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] User login){
      if(string.IsNullOrEmpty(login.Email) || string.IsNullOrEmpty(login.PasswordHash)) return BadRequest("Email and password required");
      var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == login.Email);
      if(user == null) return Unauthorized();
      if(!BCrypt.Net.BCrypt.Verify(login.PasswordHash, user.PasswordHash)) return Unauthorized();
      
      if(!user.IsEmailVerified) {
          return Unauthorized(new { message = "Email not verified. Please verify your email." });
      }

      var token = GenerateJwt(user);
      return Ok(new { token, user = new { user.Id, user.Email, user.Name, user.Role, user.IsEmailVerified } });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser() {
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
      if (userIdClaim == null) return Unauthorized();
      
      var userId = Guid.Parse(userIdClaim.Value);
      var user = await _db.Users.FindAsync(userId);
      
      if (user == null) return NotFound("User not found");
      
      return Ok(new {
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        user.KycStatus,
        user.BusinessName,
        user.RegistrationNumber,
        user.RejectionReason
      });
    }

    private string GenerateJwt(User user){
      var key = _cfg["Jwt:Key"] ?? "replace_with_long_secret_key_change_in_production";
      var tokenHandler = new JwtSecurityTokenHandler();
      var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
      var tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity(new[] {
          new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
          new Claim(ClaimTypes.Email, user.Email ?? ""),
          new Claim(ClaimTypes.Role, user.Role ?? "Buyer")
        }),
        Expires = DateTime.UtcNow.AddDays(7),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
      };
      var token = tokenHandler.CreateToken(tokenDescriptor);
      return tokenHandler.WriteToken(token);
    }
  }
}
