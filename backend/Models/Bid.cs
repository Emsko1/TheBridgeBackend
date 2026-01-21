using System;

namespace Bridge.Backend.Models {
  public class Bid {
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid BidderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected
  }
}
