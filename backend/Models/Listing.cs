using System;
using System.Collections.Generic;

namespace Bridge.Backend.Models {
  public class Listing {
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string Type { get; set; } // Car | SparePart
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int? Year { get; set; }
    public string Location { get; set; }
    public List<string> Photos { get; set; }
    public string Status { get; set; } = "Active";

    // Tender / Auction Fields
    public DateTime? SaleStartTime { get; set; } // Appointed Hour
    public DateTime? SaleEndTime { get; set; }
    public bool IsTender { get; set; }
    public decimal? MinimumBid { get; set; }
    
    public string? Source { get; set; } // "Local" or "Cars45"
  }
}
