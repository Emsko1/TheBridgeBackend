using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

namespace Bridge.Backend.Services {
  public interface IExternalListingProvider {
    Task<List<Models.Listing>> GetListingsAsync();
  }

  public class ExternalListingProvider : IExternalListingProvider {
    private readonly HttpClient _httpClient;
    private static readonly List<Models.Listing> MockExternalListings = new() {
      new Models.Listing {
        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        SellerId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Type = "Car",
        Title = "Lexus RX 350 2017",
        Description = "Direct Belgium Lexus RX 350. Foreign used, Full option, Thumbstart, Reverse camera, Navigation system, Cream leather interior.",
        Price = 28000000,
        Year = 2017,
        Location = "Lagos • Ikeja",
        Status = "Active",
        Source = "Cars45",
        Photos = new List<string> {
          "https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?auto=format&fit=crop&w=800&q=80"
        }
      },
      new Models.Listing {
        Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        SellerId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
        Type = "Car",
        Title = "Toyota Highlander 2015",
        Description = "Registered Nigerian used Toyota Highlander. First body, Sound engine and gear, AC chilling. Low mileage.",
        Price = 14500000,
        Year = 2015,
        Location = "Abuja • Gwarinpa",
        Status = "Active",
        Source = "Cars45",
        Photos = new List<string> {
          "https://images.unsplash.com/photo-1609521263047-f8f205293f24?auto=format&fit=crop&w=800&q=80"
        }
      },
      new Models.Listing {
        Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
        SellerId = Guid.Parse("00000000-0000-0000-0000-000000000003"),
        Type = "Car",
        Title = "Honda Accord 2018",
        Description = "Updated 2018 Honda Accord Sport. Foreign used, Keyless entry, Push to start, Back up camera, Bluetooth, Alloy wheels.",
        Price = 11000000,
        Year = 2018,
        Location = "Lagos • Surulere",
        Status = "Active",
        Source = "Cars45",
        Photos = new List<string> {
          "https://images.unsplash.com/photo-1590362891991-f776e747a588?auto=format&fit=crop&w=800&q=80"
        }
      },
      new Models.Listing {
        Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
        SellerId = Guid.Parse("00000000-0000-0000-0000-000000000004"),
        Type = "Car",
        Title = "Mercedes-Benz GLE 450 2020",
        Description = "Super Clean Mercedes-Benz GLE 450 4Matic. Panoramic roof, 360 camera, Ambient lighting, Digital dashboard. Buy and drive.",
        Price = 65000000,
        Year = 2020,
        Location = "Lagos • Lekki Phase 1",
        Status = "Active",
        Source = "Cars45",
        Photos = new List<string> {
          "https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?auto=format&fit=crop&w=800&q=80"
        }
      }
    };

    public ExternalListingProvider(HttpClient httpClient) {
      _httpClient = httpClient;
    }

    public async Task<List<Models.Listing>> GetListingsAsync() {
      try {
        // Premium marketplace listings provider
        // In production, integrate with real marketplace APIs
        return await Task.FromResult(MockExternalListings);
      } catch (Exception ex) {
        Console.WriteLine($"Error fetching external listings: {ex.Message}");
        return new List<Models.Listing>();
      }
    }
  }
}
