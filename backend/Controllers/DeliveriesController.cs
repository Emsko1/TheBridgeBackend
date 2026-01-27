using Microsoft.AspNetCore.Mvc;
using Bridge.Backend.Data;
using Bridge.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Bridge.Backend.Controllers {
  [ApiController]
  [Route("api/[controller]")]
  public class DeliveriesController : ControllerBase {
    private readonly BridgeDbContext _db;

    public DeliveriesController(BridgeDbContext db) {
      _db = db;
    }

    [HttpGet("my-deliveries")]
    [Authorize]
    public async Task<IActionResult> GetMyDeliveries() {
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
      if (userIdClaim == null) return Unauthorized();
      var userId = Guid.Parse(userIdClaim.Value);

      var deliveries = await _db.Deliveries
        .Where(d => d.BuyerId == userId || d.SellerId == userId)
        .OrderByDescending(d => d.CreatedAt)
        .ToListAsync();
      
      return Ok(deliveries);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateDelivery([FromBody] Delivery delivery) {
        // Validation logic
        delivery.Id = Guid.NewGuid();
        delivery.CreatedAt = DateTime.UtcNow;
        delivery.Status = "Pending";
        
        _db.Deliveries.Add(delivery);
        await _db.SaveChangesAsync();
        
        return Ok(delivery);
    }

    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status) {
        var delivery = await _db.Deliveries.FindAsync(id);
        if (delivery == null) return NotFound();
        
        delivery.Status = status;
        await _db.SaveChangesAsync();
        return Ok(delivery);
    }
  }
}
