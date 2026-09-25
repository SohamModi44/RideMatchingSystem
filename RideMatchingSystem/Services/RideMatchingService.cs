using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.Hubs;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Services;

public class RideMatchingService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<RideHub> _hub;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RideMatchingService> _logger;

    public RideMatchingService(
        AppDbContext db,
        IHubContext<RideHub> hub,
        IConfiguration configuration,
        ILogger<RideMatchingService> logger)
    {
        _db = db;
        _hub = hub;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task MatchPendingRidesAsync(
        CancellationToken cancellationToken)
    {
        var maximumRides =
            _configuration.GetValue<int>(
                "RideMatching:MaximumRidesPerCycle", 20);

        var rides = await _db.Rides
            .Where(x => x.Status == RideStatus.Searching)
            .OrderBy(x => x.CreatedAt)
            .Take(maximumRides)
            .ToListAsync(cancellationToken);

        foreach (var ride in rides)
        {
            await TryOfferDriverAsync(ride.RideId, cancellationToken);
        }
    }

    private async Task TryOfferDriverAsync(
        int rideId,
        CancellationToken cancellationToken)
    {
        var timeoutSeconds =
            _configuration.GetValue<int>(
                "RideMatching:DriverLocationTimeoutSeconds", 30);

        var maximumDistance =
            _configuration.GetValue<double>(
                "RideMatching:MaximumDriverSearchDistanceKm", 10);

        var offerTimeout =
            _configuration.GetValue<int>(
                "RideMatching:DriverOfferTimeoutSeconds", 15);

        var ride = await _db.Rides
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.RideId == rideId &&
                     x.Status == RideStatus.Searching,
                cancellationToken);

        if (ride == null)
            return;

        var minimumLocationTime =
            DateTime.UtcNow.AddSeconds(-timeoutSeconds);

        var rejectedDriverIds = await _db.RideDriverRejections
            .Where(x => x.RideId == rideId)
            .Select(x => x.DriverId)
            .ToListAsync(cancellationToken);

        var drivers = await _db.Drivers
            .Where(x =>
                x.Status == DriverStatus.Available &&
                x.LastLocationUpdatedAt >= minimumLocationTime &&
                !rejectedDriverIds.Contains(x.DriverId))
            .ToListAsync(cancellationToken);

        var candidates = drivers
            .Select(driver => new
            {
                Driver = driver,
                Distance = CalculateDistance(
                    (double)driver.CurrentLatitude,
                    (double)driver.CurrentLongitude,
                    (double)ride.PickupLatitude,
                    (double)ride.PickupLongitude)
            })
            .Where(x => x.Distance <= maximumDistance)
            .OrderBy(x => x.Distance)
            .ToList();

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var offered = await TryCreateOfferAsync(
                rideId,
                candidate.Driver.DriverId,
                candidate.Distance,
                offerTimeout,
                cancellationToken);

            if (offered)
                return;
        }

        await MarkNoDriverFoundIfRequiredAsync(
            rideId,
            cancellationToken);
    }

    private async Task<bool> TryCreateOfferAsync(
        int rideId,
        int driverId,
        double distance,
        int offerTimeout,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var driver = await _db.Drivers
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM Drivers
                    WHERE DriverId = {driverId}
                    FOR UPDATE
                    """)
                .FirstOrDefaultAsync(cancellationToken);

            if (driver == null ||
                driver.Status != DriverStatus.Available)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var ride = await _db.Rides
                .FirstOrDefaultAsync(
                    x => x.RideId == rideId,
                    cancellationToken);

            if (ride == null ||
                ride.Status != RideStatus.Searching)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            driver.Status = DriverStatus.Offered;

            ride.DriverId = driver.DriverId;
            ride.Status = RideStatus.DriverOffered;
            ride.OfferedAt = DateTime.UtcNow;
            ride.OfferExpiresAt =
                DateTime.UtcNow.AddSeconds(offerTimeout);

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            await _hub.Clients
                .Group(RideHub.DriverGroup(driver.DriverId))
                .SendAsync(
                    "RideRequestReceived",
                    new
                    {
                        rideId = ride.RideId,
                        pickupLatitude = ride.PickupLatitude,
                        pickupLongitude = ride.PickupLongitude,
                        dropoffLatitude = ride.DropoffLatitude,
                        dropoffLongitude = ride.DropoffLongitude,
                        distanceKm = Math.Round(distance, 2),
                        expiresAt = ride.OfferExpiresAt
                    },
                    cancellationToken);

            await _hub.Clients
                .Group(RideHub.RideGroup(ride.RideId))
                .SendAsync(
                    "RideStatusChanged",
                    new
                    {
                        rideId = ride.RideId,
                        status = ride.Status.ToString()
                    },
                    cancellationToken);

            _logger.LogInformation(
                "Ride {RideId} offered to Driver {DriverId}",
                ride.RideId,
                driver.DriverId);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task MarkNoDriverFoundIfRequiredAsync(
        int rideId,
        CancellationToken cancellationToken)
    {
        var ride = await _db.Rides
            .FirstOrDefaultAsync(
                x => x.RideId == rideId,
                cancellationToken);

        if (ride == null ||
            ride.Status != RideStatus.Searching)
        {
            return;
        }

        var createdAgo =
            DateTime.UtcNow - ride.CreatedAt;

        if (createdAgo.TotalSeconds < 20)
            return;

        ride.Status = RideStatus.NoDriverFound;

        await _db.SaveChangesAsync(cancellationToken);

        await _hub.Clients
            .Group(RideHub.RideGroup(ride.RideId))
            .SendAsync(
                "RideStatusChanged",
                new
                {
                    rideId = ride.RideId,
                    status = ride.Status.ToString()
                },
                cancellationToken);
    }

    public static double CalculateDistance(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadiusKm = 6371;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) *
            Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) *
            Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        var c =
            2 *
            Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double ToRadians(double value)
    {
        return value * Math.PI / 180;
    }
}