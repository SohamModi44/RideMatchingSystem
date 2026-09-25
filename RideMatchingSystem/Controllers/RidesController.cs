using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.DTOs;
using RideMatchingSystem.Hubs;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Controllers;

[ApiController]
[Route("api/rides")]
public class RidesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<RideHub> _hub;

    public RidesController(
        AppDbContext db,
        IHubContext<RideHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRide(
        CreateRideRequest request)
    {
        var riderExists =
            await _db.Riders.AnyAsync(
                x => x.RiderId == request.RiderId);

        if (!riderExists)
        {
            return BadRequest(new
            {
                message = "Invalid rider."
            });
        }

        var activeRideExists =
            await _db.Rides.AnyAsync(
                x =>
                    x.RiderId == request.RiderId &&
                    (
                        x.Status == RideStatus.Searching ||
                        x.Status == RideStatus.DriverOffered ||
                        x.Status == RideStatus.DriverAssigned ||
                        x.Status == RideStatus.DriverArrived ||
                        x.Status == RideStatus.InProgress
                    ));

        if (activeRideExists)
        {
            return Conflict(new
            {
                message =
                    "Rider already has an active ride."
            });
        }

        var ride = new Ride
        {
            RiderId = request.RiderId,
            PickupLatitude = request.PickupLatitude,
            PickupLongitude = request.PickupLongitude,
            DropoffLatitude = request.DropoffLatitude,
            DropoffLongitude = request.DropoffLongitude,
            Status = RideStatus.Searching,
            CreatedAt = DateTime.UtcNow
        };

        _db.Rides.Add(ride);

        await _db.SaveChangesAsync();

        await _hub.Clients
            .Group(RideHub.RideGroup(ride.RideId))
            .SendAsync(
                "RideCreated",
                new
                {
                    rideId = ride.RideId
                });

        return Accepted(
            $"/api/rides/{ride.RideId}",
            new
            {
                rideId = ride.RideId,
                status = ride.Status.ToString(),
                message =
                    "Ride created. Searching for nearby drivers."
            });
    }

    [HttpGet("{rideId}")]
    public async Task<IActionResult> GetRide(
        int rideId)
    {
        var ride = await _db.Rides
            .Include(x => x.Driver)
            .FirstOrDefaultAsync(
                x => x.RideId == rideId);

        if (ride == null)
        {
            return NotFound(new
            {
                message = "Ride not found."
            });
        }

        return Ok(ToResponse(ride));
    }

    [HttpPost("{rideId}/accept")]
    public async Task<IActionResult> AcceptRide(
        int rideId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var ride = await _db.Rides
            .FirstOrDefaultAsync(
                x => x.RideId == rideId);

        if (ride == null)
            return NotFound();

        if (ride.Status != RideStatus.DriverOffered)
        {
            return BadRequest(new
            {
                message =
                    "Ride is no longer available for acceptance."
            });
        }

        if (!ride.DriverId.HasValue)
        {
            return BadRequest(new
            {
                message = "No driver assigned to this offer."
            });
        }

        var driver = await _db.Drivers
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM Drivers
                WHERE DriverId = {ride.DriverId.Value}
                FOR UPDATE
                """)
            .FirstOrDefaultAsync();

        if (driver == null ||
            driver.Status != DriverStatus.Offered)
        {
            return BadRequest(new
            {
                message =
                    "Driver is no longer available."
            });
        }

        if (ride.OfferExpiresAt.HasValue &&
            ride.OfferExpiresAt.Value < DateTime.UtcNow)
        {
            driver.Status = DriverStatus.Available;

            ride.DriverId = null;
            ride.Status = RideStatus.Searching;
            ride.OfferExpiresAt = null;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return BadRequest(new
            {
                message =
                    "Ride offer has expired."
            });
        }

        driver.Status = DriverStatus.Busy;

        ride.Status = RideStatus.DriverAssigned;
        ride.AcceptedAt = DateTime.UtcNow;
        ride.OfferExpiresAt = null;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await NotifyRideStatus(
            ride.RideId,
            ride.Status);

        return Ok(new
        {
            rideId,
            status = ride.Status.ToString(),
            driverId = driver.DriverId,
            message = "Ride accepted."
        });
    }

    [HttpPost("{rideId}/reject")]
    public async Task<IActionResult> RejectRide(
        int rideId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var ride = await _db.Rides
            .FirstOrDefaultAsync(
                x => x.RideId == rideId);

        if (ride == null)
            return NotFound();

        if (ride.Status != RideStatus.DriverOffered ||
            !ride.DriverId.HasValue)
        {
            return BadRequest(new
            {
                message =
                    "This ride is not currently offered."
            });
        }

        var driverId = ride.DriverId.Value;

        var driver = await _db.Drivers
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM Drivers
                WHERE DriverId = {driverId}
                FOR UPDATE
                """)
            .FirstOrDefaultAsync();

        if (driver != null &&
            driver.Status == DriverStatus.Offered)
        {
            driver.Status = DriverStatus.Available;
        }

        var rejection = new RideDriverRejection
        {
            RideId = ride.RideId,
            DriverId = driverId,
            RejectedAt = DateTime.UtcNow
        };

        _db.RideDriverRejections.Add(rejection);

        ride.DriverId = null;
        ride.Status = RideStatus.Searching;
        ride.OfferedAt = null;
        ride.OfferExpiresAt = null;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await NotifyRideStatus(
            ride.RideId,
            ride.Status);

        return Ok(new
        {
            rideId,
            status = ride.Status.ToString(),
            message =
                "Ride rejected. Searching for another driver."
        });
    }

    [HttpPost("{rideId}/arrived")]
    public async Task<IActionResult> DriverArrived(
        int rideId)
    {
        var ride = await _db.Rides
            .Include(x => x.Driver)
            .FirstOrDefaultAsync(
                x => x.RideId == rideId);

        if (ride == null)
            return NotFound();

        if (ride.Status != RideStatus.DriverAssigned)
        {
            return BadRequest(new
            {
                message =
                    "Driver cannot mark arrival at this stage."
            });
        }

        ride.Status = RideStatus.DriverArrived;
        ride.ArrivedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await NotifyRideStatus(
            ride.RideId,
            ride.Status);

        return Ok(new
        {
            rideId,
            status = ride.Status.ToString()
        });
    }

    [HttpPost("{rideId}/start")]
    public async Task<IActionResult> StartRide(
        int rideId)
    {
        var ride = await _db.Rides
            .FirstOrDefaultAsync(
                x => x.RideId == rideId);

        if (ride == null)
            return NotFound();

        if (ride.Status != RideStatus.DriverArrived)
        {
            return BadRequest(new
            {
                message =
                    "Driver must arrive before starting the ride."
            });
        }

        ride.Status = RideStatus.InProgress;
        ride.StartedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await NotifyRideStatus(
            ride.RideId,
            ride.Status);

        return Ok(new
        {
            rideId,
            status = ride.Status.ToString()
        });
    }

    [HttpPost("{rideId}/complete")]
    public async Task<IActionResult> CompleteRide(
        int rideId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var ride = await _db.Rides
            .FirstOrDefaultAsync(
                x => x.RideId == rideId);

        if (ride == null)
            return NotFound();

        if (ride.Status != RideStatus.InProgress)
        {
            return BadRequest(new
            {
                message =
                    "Only an in-progress ride can be completed."
            });
        }

        if (!ride.DriverId.HasValue)
        {
            return BadRequest(new
            {
                message = "Ride has no driver."
            });
        }

        var driver = await _db.Drivers
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM Drivers
                WHERE DriverId = {ride.DriverId.Value}
                FOR UPDATE
                """)
            .FirstOrDefaultAsync();

        if (driver == null)
        {
            return BadRequest(new
            {
                message = "Driver not found."
            });
        }

        ride.Status = RideStatus.Completed;
        ride.CompletedAt = DateTime.UtcNow;

        // Driver becomes available for next rider.
        driver.Status = DriverStatus.Available;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await NotifyRideStatus(
            ride.RideId,
            ride.Status);

        await _hub.Clients
            .Group(RideHub.DriverGroup(driver.DriverId))
            .SendAsync(
                "DriverStatusChanged",
                new
                {
                    driverId = driver.DriverId,
                    status = driver.Status.ToString()
                });

        return Ok(new
        {
            rideId,
            status = ride.Status.ToString(),
            driverStatus = driver.Status.ToString(),
            message =
                "Ride completed. Driver is available for the next ride."
        });
    }

    [HttpPost("{rideId}/cancel")]
    public async Task<IActionResult> CancelRide(
        int rideId,
        CancelRideRequest request)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var ride = await _db.Rides
            .FirstOrDefaultAsync(
                x => x.RideId == rideId);

        if (ride == null)
            return NotFound();

        if (ride.Status == RideStatus.Completed ||
            ride.Status == RideStatus.Cancelled)
        {
            return BadRequest(new
            {
                message =
                    "Ride is already finished."
            });
        }

        if (ride.DriverId.HasValue)
        {
            var driver = await _db.Drivers
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM Drivers
                    WHERE DriverId = {ride.DriverId.Value}
                    FOR UPDATE
                    """)
                .FirstOrDefaultAsync();

            if (driver != null &&
                (driver.Status == DriverStatus.Offered ||
                 driver.Status == DriverStatus.Busy))
            {
                driver.Status =
                    DriverStatus.Available;
            }
        }

        ride.Status = RideStatus.Cancelled;
        ride.CancelledAt = DateTime.UtcNow;
        ride.CancellationReason =
            request.Reason;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await NotifyRideStatus(
            ride.RideId,
            ride.Status);

        return Ok(new
        {
            rideId,
            status = ride.Status.ToString(),
            message = "Ride cancelled."
        });
    }

    private async Task NotifyRideStatus(
        int rideId,
        RideStatus status)
    {
        await _hub.Clients
            .Group(RideHub.RideGroup(rideId))
            .SendAsync(
                "RideStatusChanged",
                new
                {
                    rideId,
                    status = status.ToString()
                });
    }

    private static RideResponse ToResponse(
        Ride ride)
    {
        return new RideResponse
        {
            RideId = ride.RideId,
            Status =
                (RideStatusResponse)ride.Status,
            StatusText =
                ride.Status.ToString(),

            PickupLatitude =
                ride.PickupLatitude,

            PickupLongitude =
                ride.PickupLongitude,

            DropoffLatitude =
                ride.DropoffLatitude,

            DropoffLongitude =
                ride.DropoffLongitude,

            DriverId =
                ride.DriverId,

            DriverName =
                ride.Driver?.FullName,

            DriverPhone =
                ride.Driver?.PhoneNumber,

            VehicleNumber =
                ride.Driver?.VehicleNumber,

            DriverLatitude =
                ride.Driver?.CurrentLatitude,

            DriverLongitude =
                ride.Driver?.CurrentLongitude,

            CreatedAt =
                ride.CreatedAt
        };
    }
}