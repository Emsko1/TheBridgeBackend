using System;

namespace Bridge.Backend.Models {
  public class Delivery {
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    
    public string PickupAddress { get; set; }
    public string DeliveryAddress { get; set; }
    
    public string Status { get; set; } = "Pending"; // Pending, InTransit, Delivered, Cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedDeliveryTime { get; set; }
    
    public string? TrackingHistory { get; set; } // JSON or simple log
  }
}
