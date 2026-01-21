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
    public AuthController(BridgeDbContext db, IConfiguration cfg){ _db = db; _cfg = cfg; }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] User user){
      // Validate required fields
      if(string.IsNullOrWhiteSpace(user?.Email) || string.IsNullOrWhiteSpace(user?.PasswordHash) || string.IsNullOrWhiteSpace(user?.Name)) {
        return BadRequest(new { message = "Name, Email and password are required" });
      }

      // Check if user already exists
      var exists = await _db.Users.AnyAsync(u => u.Email == user.Email);
      if(exists) return BadRequest(new { message = "User with this email already exists" });

      try {
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
        
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();
        
        return Ok(new { 
          message = "Registration successful",
          user = new { 
            id = newUser.Id, 
            email = newUser.Email, 
            name = newUser.Name, 
            role = newUser.Role,
            kycStatus = newUser.KycStatus
          } 
        });
      } catch(Exception ex) {
        Console.WriteLine($"Register error: {ex.Message}");
        return BadRequest(new { message = "Registration failed: " + ex.Message });
      }
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
      var token = GenerateJwt(user);
      return Ok(new { token, user = new { user.Id, user.Email, user.Name, user.Role } });
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
