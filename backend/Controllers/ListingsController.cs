using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Bridge.Backend.Models;
using Bridge.Backend.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Bridge.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Bridge.Backend.Controllers {
  [ApiController]
  [Route("api/[controller]")]
  public class ListingsController : ControllerBase {
    private readonly BridgeDbContext _db;
    private readonly IExternalListingProvider _externalProvider;

    public ListingsController(BridgeDbContext db, IExternalListingProvider externalProvider){ 
      _db = db;
      _externalProvider = externalProvider;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Listing>>> GetAll()
    {
      try {
        var items = await _db.Listings.AsNoTracking().ToListAsync(); // AsNoTracking to modify in memory without EF issues
        
        // Transform photo URLs
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        foreach (var item in items) {
            item.Photos = item.Photos.Select(p => GetFullUrl(p, baseUrl)).ToList();
        }

        if (items.Count == 0)
        {
          // seed sample if empty
          items = new List<Listing> {
            new Listing { 
              Id=Guid.NewGuid(), 
              SellerId=Guid.NewGuid(), 
              Type="Car", 
              Title="2015 Toyota Camry SE", 
              Price=17500000, 
              Year=2015, 
              Location="Lagos", 
              Description="Foreign used 2015 Toyota Camry SE. Clean title, alloy wheels, reverse camera, leather seats, chilling AC. Buy and drive.", 
              Photos = new List<string> { "https://images.unsplash.com/photo-1621007947382-bb3c3968e3bb?auto=format&fit=crop&w=800&q=80" } 
            },
            new Listing { 
              Id=Guid.NewGuid(), 
              SellerId=Guid.NewGuid(), 
              Type="Car", 
              Title="2016 Lexus RX 350 F-Sport", 
              Price=45000000, 
              Year=2016, 
              Location="Abuja", 
              Description="Full option Lexus RX 350 F-Sport. Panoramic roof, thumbstart, accident free, low mileage, 360 camera.", 
              Photos = new List<string> { "https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?auto=format&fit=crop&w=800&q=80" } 
            },
            new Listing { 
              Id=Guid.NewGuid(), 
              SellerId=Guid.NewGuid(), 
              Type="Car", 
              Title="2014 Mercedes-Benz GLK 350 4Matic", 
              Price=22000000, 
              Year=2014, 
              Location="Lekki", 
              Description="Super clean foreign used GLK 350. Pristine condition, black leather interior, navigation system, power boot.", 
              Photos = new List<string> { "https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?auto=format&fit=crop&w=800&q=80" } 
            }
          };
          
          _db.Listings.AddRange(items);
          await _db.SaveChangesAsync();
        }
        return Ok(items);
      } catch (Exception ex) {
          return StatusCode(500, new { message = "Failed to load listings", error = ex.Message, stack = ex.StackTrace });
      }
    }

    [HttpGet("external")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Listing>>> GetExternalListings()
    {
      try {
        var externalListings = await _externalProvider.GetListingsAsync();
        return Ok(externalListings);
      } catch (Exception ex) {
        return BadRequest(new { message = "Failed to fetch external listings", error = ex.Message });
      }
    }

    [HttpGet("marketplace")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Listing>>> GetMarketplaceListings()
    {
      try {
        // Combine local and external listings
        var localListings = await _db.Listings.AsNoTracking().ToListAsync();
        var externalListings = await _externalProvider.GetListingsAsync();
        
        // Transform photo URLs for local listings
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        foreach (var item in localListings) {
            item.Photos = item.Photos.Select(p => GetFullUrl(p, baseUrl)).ToList();
        }
        
        var combined = new List<Listing>();
        combined.AddRange(localListings);
        combined.AddRange(externalListings);
        
        return Ok(combined);
      } catch (Exception ex) {
        return BadRequest(new { message = "Failed to fetch marketplace listings", error = ex.Message });
      }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Listing>> GetById(Guid id)
    {
      var listing = await _db.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
      if (listing == null)
        return NotFound(new { message = "Listing not found" });
      
      var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
      listing.Photos = listing.Photos.Select(p => GetFullUrl(p, baseUrl)).ToList();

      return Ok(listing);
    }
    
    [HttpPost]
    public async Task<ActionResult<Listing>> Create([FromBody] Listing listing) {
      // Get the authenticated user's ID from the JWT token
      var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
      if (userIdClaim == null)
        return Unauthorized(new { message = "User ID not found in token" });
      
      // Validation for Tender
      if (listing.IsTender) {
        if (listing.SaleStartTime == null || listing.SaleEndTime == null)
            return BadRequest(new { message = "Start and End time are required for Tenders" });
        if (listing.SaleEndTime <= listing.SaleStartTime)
            return BadRequest(new { message = "End time must be after Start time" });
      }

      listing.Id = Guid.NewGuid();
      listing.SellerId = Guid.Parse(userIdClaim.Value);
      listing.Status = listing.Status ?? "Active";
      
      // Handle Photos: Save Base64 to Disk
      if (listing.Photos != null && listing.Photos.Count > 0) {
          var processedPhotos = new List<string>();
          var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
          var imagesPath = Path.Combine(wwwrootPath, "images", "listings");
          
          if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

          foreach (var photo in listing.Photos) {
              if (photo.StartsWith("data:image")) {
                  try {
                      var base64Data = photo.Split(',')[1];
                      var bytes = Convert.FromBase64String(base64Data);
                      var fileName = $"{Guid.NewGuid()}.jpg";
                      var filePath = Path.Combine(imagesPath, fileName);
                      
                      await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                      processedPhotos.Add($"/images/listings/{fileName}"); // Save relative path
                  } catch (Exception ex) {
                      Console.WriteLine($"Failed to save image: {ex.Message}");
                      // If fail, just skip or keep original
                      processedPhotos.Add(photo); 
                  }
              } else {
                  processedPhotos.Add(photo);
              }
          }
          listing.Photos = processedPhotos;
      } else {
          listing.Photos = new List<string>();
      }
      
      _db.Listings.Add(listing);
      await _db.SaveChangesAsync();
      
      // Return with full URLs for immediate display
      var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
      listing.Photos = listing.Photos.Select(p => GetFullUrl(p, baseUrl)).ToList();

      return CreatedAtAction(nameof(GetById), new { id = listing.Id }, listing);
    }

    private string GetFullUrl(string path, string baseUrl) {
        if (string.IsNullOrEmpty(path)) return path;
        if (path.StartsWith("http") || path.StartsWith("data:")) return path;
        if (path.StartsWith("/")) return $"{baseUrl}{path}";
        return $"{baseUrl}/{path}";
    }
  }
}
