using Microsoft.AspNetCore.Mvc;
using Bridge.Backend.Data;
using Bridge.Backend.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Bridge.Backend.Controllers {
  [ApiController]
  [Route("api/[controller]")]
  public class BidsController : ControllerBase {
    private readonly BridgeDbContext _db;

    public BidsController(BridgeDbContext db) {
      _db = db;
    }

    [HttpGet("listing/{listingId}")]
    public async Task<IActionResult> GetBidsForListing(Guid listingId) {
      var bids = await _db.Bids
        .Where(b => b.ListingId == listingId)
        .OrderByDescending(b => b.Amount)
        .ToListAsync();
      return Ok(bids);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PlaceBid([FromBody] Bid bid) {
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
      if (userIdClaim == null) return Unauthorized();
      var userId = Guid.Parse(userIdClaim.Value);

      var listing = await _db.Listings.FindAsync(bid.ListingId);
      if (listing == null) return NotFound("Listing not found");

      if (listing.IsTender) {
        if (listing.SaleStartTime > DateTime.UtcNow) return BadRequest("Tender has not started yet");
        if (listing.SaleEndTime < DateTime.UtcNow) return BadRequest("Tender has ended");
      }

      if (listing.MinimumBid.HasValue && bid.Amount < listing.MinimumBid.Value) {
        return BadRequest($"Bid must be at least {listing.MinimumBid}");
      }

      bid.Id = Guid.NewGuid();
      bid.BidderId = userId;
      bid.Timestamp = DateTime.UtcNow;
      bid.Status = "Pending";

      _db.Bids.Add(bid);
      await _db.SaveChangesAsync();

      return Ok(bid);
    }

    [HttpPost("accept/{bidId}")]
    [Authorize]
    public async Task<IActionResult> AcceptBid(Guid bidId) {
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
      if (userIdClaim == null) return Unauthorized();
      var sellerId = Guid.Parse(userIdClaim.Value);

      var bid = await _db.Bids.FindAsync(bidId);
      if (bid == null) return NotFound("Bid not found");

      var listing = await _db.Listings.FindAsync(bid.ListingId);
      if (listing == null) return NotFound("Listing not found");

      if (listing.SellerId != sellerId) return Forbid("Only the seller can accept bids");

      bid.Status = "Accepted";
      listing.Status = "Sold"; // Or "PendingPayment"

      // Reject other bids?
      var otherBids = await _db.Bids.Where(b => b.ListingId == listing.Id && b.Id != bidId).ToListAsync();
      foreach(var other in otherBids) {
        other.Status = "Rejected";
      }

      await _db.SaveChangesAsync();
      return Ok(new { message = "Bid accepted", bidId = bid.Id });
    }
  }
}
