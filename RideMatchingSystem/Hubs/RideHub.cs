using Microsoft.AspNetCore.SignalR;

namespace RideMatchingSystem.Hubs;

public class RideHub : Hub
{
    public static string RideGroup(int rideId)
        => $"ride-{rideId}";

    public static string DriverGroup(int driverId)
        => $"driver-{driverId}";

    public async Task JoinRide(int rideId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RideGroup(rideId));
    }

    public async Task JoinDriver(int driverId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            DriverGroup(driverId));
    }

    public async Task LeaveRide(int rideId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RideGroup(rideId));
    }

    public async Task LeaveDriver(int driverId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            DriverGroup(driverId));
    }
}