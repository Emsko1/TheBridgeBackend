using Microsoft.EntityFrameworkCore;
using Bridge.Backend.Models;

namespace Bridge.Backend.Data {
  public class BridgeDbContext : DbContext {
    public BridgeDbContext(DbContextOptions<BridgeDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Listing> Listings { get; set; }
    public DbSet<EscrowTransaction> EscrowTransactions { get; set; }
    public DbSet<Bid> Bids { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Listing>()
            .Property(e => e.Photos)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null)
            );
    }
  }
}
