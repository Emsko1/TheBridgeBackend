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
        var items = await _db.Listings.ToListAsync();
        
        // Force update if old description exists (checking for the old Evil Spirit description)
        if (items.Count > 0 && items.Any(i => i.Title == "2012 Honda Accord (Evil Spirit)" && (string.IsNullOrEmpty(i.Description) || !i.Description.StartsWith("Honda Accord Evil Spirit"))))
        {
            _db.Listings.RemoveRange(items);
            await _db.SaveChangesAsync();
            items.Clear();
        }

        if (items.Count == 0)
        {
          // seed sample if empty
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
            },
            new Listing { 
              Id=Guid.NewGuid(), 
              SellerId=Guid.NewGuid(), 
              Type="Car", 
              Title="2015 Toyota Highlander Limited", 
              Price=32000000, 
              Year=2015, 
              Location="Port Harcourt", 
              Description="3 rows seater, thumbstart, beige leather interior, foreign used, DVD headrest.", 
              Photos = new List<string> { "https://images.unsplash.com/photo-1609521263047-f8f205293f24?auto=format&fit=crop&w=800&q=80" } 
            },
            new Listing { 
              Id=Guid.NewGuid(), 
              SellerId=Guid.NewGuid(), 
              Type="Car", 
              Title="2010 Toyota Camry (Muscle)", 
              Price=4500000, 
              Year=2010, 
              Location="Ibadan", 
              Description="Registered Nigerian used Toyota Camry (Muscle). Sound engine, chilling AC, papers are complete. Minor scratch on bumper.", 
              Photos = new List<string> { "https://images.unsplash.com/photo-1621007947382-bb3c3968e3bb?auto=format&fit=crop&w=800&q=80" } 
            },
            new Listing { 
              Id=Guid.NewGuid(), 
              SellerId=Guid.NewGuid(), 
              Type="Car", 
              Title="2012 Honda Accord (Evil Spirit)", 
              Price=3800000, 
              Year=2012, 
              Location="Lagos", 
              Description="Honda Accord Evil Spirit. V4 engine, leather seat, alloy rim, sharp transmission. First body, no accident history.", 
              Photos = new List<string> { "https://images.unsplash.com/photo-1590362891991-f776e747a588?auto=format&fit=crop&w=800&q=80" } 
            }
          };
          
          // Save seed data to DB so it persists
          _db.Listings.AddRange(items);
          await _db.SaveChangesAsync();
        }
        return Ok(items);
      } catch (Exception ex) {
          return StatusCode(500, new { message = "Failed to load listings", error = ex.Message, stack = ex.StackTrace });
      }
    }

    [HttpGet("debug-photos")]
    [AllowAnonymous]
    public async Task<IActionResult> DebugPhotos() {
        var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Id\", \"Photos\" FROM \"Listings\"";
        var result = new List<object>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            result.Add(new {
                Id = reader.GetGuid(0),
                PhotosRaw = reader.GetValue(1).ToString()
            });
        }
        return Ok(result);
    }

    [HttpPost("reset-data")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetData() {
        await _db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Listings\"");
        return Ok(new { message = "Listings table truncated" });
    }

    [HttpPost("fix-photos")]
    [AllowAnonymous]
    public async Task<IActionResult> FixPhotos() {
        var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();
        
        var listingsToUpdate = new List<(Guid Id, string NewPhotos)>();

        using (var command = connection.CreateCommand()) {
            command.CommandText = "SELECT \"Id\", \"Photos\" FROM \"Listings\"";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) {
                var id = reader.GetGuid(0);
                var raw = reader.GetValue(1).ToString();
                
                if (string.IsNullOrEmpty(raw) || raw.StartsWith("[")) continue;

                if (raw.StartsWith("{") && raw.EndsWith("}")) {
                    var content = raw.Substring(1, raw.Length - 2);
                    // Handle empty case
                    if (string.IsNullOrWhiteSpace(content)) {
                        listingsToUpdate.Add((id, "[]"));
                        continue;
                    }
                    
                    var items = content.Split(',');
                    var jsonItems = items.Select(i => {
                        var trimmed = i.Trim();
                        // If it's already properly quoted (Postgres text array with quotes), keep it
                        if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) return trimmed;
                        // Otherwise wrap in quotes
                        return $"\"{trimmed}\"";
                    });
                    var newJson = $"[{string.Join(",", jsonItems)}]";
                    listingsToUpdate.Add((id, newJson));
                }
            }
        }

        foreach(var item in listingsToUpdate) {
            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = "UPDATE \"Listings\" SET \"Photos\" = @p1 WHERE \"Id\" = @p2";
            var p1 = updateCmd.CreateParameter(); p1.ParameterName = "@p1"; p1.Value = item.NewPhotos; updateCmd.Parameters.Add(p1);
            var p2 = updateCmd.CreateParameter(); p2.ParameterName = "@p2"; p2.Value = item.Id; updateCmd.Parameters.Add(p2);
            await updateCmd.ExecuteNonQueryAsync();
        }

        return Ok(new { message = $"Fixed {listingsToUpdate.Count} listings" });
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
        var localListings = await _db.Listings.ToListAsync();
        var externalListings = await _externalProvider.GetListingsAsync();
        
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
      var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == id);
      if (listing == null)
        return NotFound(new { message = "Listing not found" });
      
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
      if (listing.Photos == null)
        listing.Photos = new List<string>();
      
      _db.Listings.Add(listing);
      await _db.SaveChangesAsync();
      return CreatedAtAction(nameof(GetById), new { id = listing.Id }, listing);
    }
  }
}
