using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Bridge.Backend.Models;

namespace Bridge.Backend.Hubs {
  public class MarketplaceHub : Hub {
    // thread-safe dictionary to map connectionId to userId
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _onlineUsers = new();

    public async Task NotifyMatch(string message) {
      await Clients.All.SendAsync("Matched", message);
    }

    // Broadcast new listing to all connected clients
    public async Task BroadcastListingCreated(Listing listing) {
      await Clients.AllExcept(Context.ConnectionId).SendAsync("ListingCreated", listing);
    }

    // Broadcast listing update to all connected clients
    public async Task BroadcastListingUpdated(Listing listing) {
      await Clients.AllExcept(Context.ConnectionId).SendAsync("ListingUpdated", listing);
    }

    // Broadcast listing deletion to all connected clients
    public async Task BroadcastListingDeleted(string listingId) {
      await Clients.AllExcept(Context.ConnectionId).SendAsync("ListingDeleted", listingId);
    }

    public async Task BroadcastBidPlaced(Bid bid) {
        await Clients.All.SendAsync("BidPlaced", bid);
    }

    // Called when a user connects
    public override async Task OnConnectedAsync() {
      // Try to get userId from claims
      var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
      
      if (!string.IsNullOrEmpty(userId)) {
        _onlineUsers.TryAdd(Context.ConnectionId, userId);
        await Clients.All.SendAsync("UserStatusChanged", userId, true); // true = online
      }

      await base.OnConnectedAsync();
      await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
    }

    // Called when a user disconnects
    public override async Task OnDisconnectedAsync(Exception exception) {
      if (_onlineUsers.TryRemove(Context.ConnectionId, out var userId)) {
         // Check if user has other connections? For simplicity, we assume one connection per user for now or broadcast anyway
         // A better approach would be to count connections per user
         await Clients.All.SendAsync("UserStatusChanged", userId, false); // false = offline
      }

      await base.OnDisconnectedAsync(exception);
      await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
    }
  }
}
