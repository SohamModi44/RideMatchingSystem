using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.Hubs;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Services;

public class RideMatchingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RideMatchingWorker> _logger;

    public RideMatchingWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RideMatchingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var intervalSeconds =
            _configuration.GetValue<int>(
                "RideMatching:MatchingIntervalSeconds", 2);

        _logger.LogInformation(
            "Ride matching worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var matchingService =
                    scope.ServiceProvider
                        .GetRequiredService<RideMatchingService>();

                await matchingService.MatchPendingRidesAsync(
                    stoppingToken);

                await ExpireOffersAsync(
                    scope.ServiceProvider,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in ride matching worker.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(intervalSeconds),
                stoppingToken);
        }
    }

    private static async Task ExpireOffersAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var db =
            services.GetRequiredService<AppDbContext>();

        var hub =
            services.GetRequiredService<
                IHubContext<RideHub>>();

        var expiredRides =
            await db.Rides
                .Where(x =>
                    x.Status == RideStatus.DriverOffered &&
                    x.OfferExpiresAt != null &&
                    x.OfferExpiresAt <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

        foreach (var ride in expiredRides)
        {
            if (ride.DriverId.HasValue)
            {
                var driver =
                    await db.Drivers.FindAsync(
                        new object[]
                        {
                            ride.DriverId.Value
                        },
                        cancellationToken);

                if (driver != null &&
                    driver.Status == DriverStatus.Offered)
                {
                    driver.Status =
                        DriverStatus.Available;
                }
            }

            ride.DriverId = null;
            ride.Status = RideStatus.Searching;
            ride.OfferedAt = null;
            ride.OfferExpiresAt = null;
        }

        if (expiredRides.Count == 0)
            return;

        await db.SaveChangesAsync(
            cancellationToken);

        foreach (var ride in expiredRides)
        {
            await hub.Clients
                .Group(
                    RideHub.RideGroup(
                        ride.RideId))
                .SendAsync(
                    "RideStatusChanged",
                    new
                    {
                        rideId = ride.RideId,
                        status = ride.Status.ToString()
                    },
                    cancellationToken);
        }
    }
}