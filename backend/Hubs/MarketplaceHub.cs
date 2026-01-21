using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Bridge.Backend.Models;

namespace Bridge.Backend.Hubs {
  public class MarketplaceHub : Hub {
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

    // Called when a user connects
    public override async Task OnConnectedAsync() {
      await base.OnConnectedAsync();
      await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
    }

    // Called when a user disconnects
    public override async Task OnDisconnectedAsync(Exception exception) {
      await base.OnDisconnectedAsync(exception);
      await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
    }
  }
}
