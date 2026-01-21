using System;

namespace Bridge.Backend.Models {
  public class EscrowTransaction {
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public string Currency { get; set; } = "NGN";
    public string Status { get; set; } = "PendingPayment"; // PendingPayment, FundsHeld, Released, InDispute, Refunded
    public string PaymentProvider { get; set; }
    public string ProviderReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  }
}
