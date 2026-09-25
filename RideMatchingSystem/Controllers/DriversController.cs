using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.DTOs;
using RideMatchingSystem.Models;
using RideMatchingSystem.Services;

namespace RideMatchingSystem.Controllers;

[ApiController]
[Route("api/drivers")]
public class DriversController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly DriverLocationService _locationService;

    public DriversController(
        AppDbContext db,
        DriverLocationService locationService)
    {
        _db = db;
        _locationService = locationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDrivers()
    {
        var drivers = await _db.Drivers
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.DriverId,
                x.FullName,
                x.PhoneNumber,
                x.VehicleNumber,
                Status = x.Status.ToString(),
                x.CurrentLatitude,
                x.CurrentLongitude,
                x.LastLocationUpdatedAt
            })
            .ToListAsync();

        return Ok(drivers);
    }

    [HttpGet("{driverId}")]
    public async Task<IActionResult> GetDriver(int driverId)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(x =>
                x.DriverId == driverId);

        if (driver == null)
            return NotFound(new
            {
                message = "Driver not found."
            });

        return Ok(new
        {
            driver.DriverId,
            driver.FullName,
            driver.PhoneNumber,
            driver.VehicleNumber,
            Status = driver.Status.ToString(),
            driver.CurrentLatitude,
            driver.CurrentLongitude,
            driver.LastLocationUpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateDriver(
        CreateDriverRequest request)
    {
        var vehicleExists =
            await _db.Drivers.AnyAsync(
                x => x.VehicleNumber == request.VehicleNumber);

        if (vehicleExists)
        {
            return Conflict(new
            {
                message = "Vehicle number already exists."
            });
        }

        var driver = new Driver
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            VehicleNumber = request.VehicleNumber.Trim(),
            Status = DriverStatus.Offline,
            CreatedAt = DateTime.UtcNow
        };

        _db.Drivers.Add(driver);

        await _db.SaveChangesAsync();

        return Created(
            $"/api/drivers/{driver.DriverId}",
            new
            {
                driver.DriverId,
                driver.FullName,
                driver.VehicleNumber,
                status = driver.Status.ToString()
            });
    }

    [HttpPost("{driverId}/location")]
    public async Task<IActionResult> UpdateLocation(
        int driverId,
        UpdateDriverLocationRequest request)
    {
        var result =
            await _locationService.UpdateLocationAsync(
                driverId,
                request);

        if (!result)
        {
            return NotFound(new
            {
                message = "Driver not found."
            });
        }

        return Ok(new
        {
            message = "Location updated successfully.",
            latitude = request.Latitude,
            longitude = request.Longitude
        });
    }

    [HttpPost("{driverId}/online")]
    public async Task<IActionResult> GoOnline(
        int driverId)
    {
        var result =
            await _locationService.GoOnlineAsync(
                driverId);

        if (!result)
        {
            return BadRequest(new
            {
                message =
                    "Driver cannot go online. Send a valid GPS location first."
            });
        }

        return Ok(new
        {
            message = "Driver is now online.",
            status = DriverStatus.Available.ToString()
        });
    }

    [HttpPost("{driverId}/offline")]
    public async Task<IActionResult> GoOffline(
        int driverId)
    {
        var driver =
            await _db.Drivers
                .FirstOrDefaultAsync(
                    x => x.DriverId == driverId);

        if (driver == null)
        {
            return NotFound(new
            {
                message = "Driver not found."
            });
        }

        if (driver.Status == DriverStatus.Busy)
        {
            return BadRequest(new
            {
                message =
                    "Driver cannot go offline while a ride is in progress."
            });
        }

        if (driver.Status == DriverStatus.Offered)
        {
            return BadRequest(new
            {
                message =
                    "Please accept or reject the current ride request first."
            });
        }

        driver.Status = DriverStatus.Offline;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Driver is now offline.",
            status = driver.Status.ToString()
        });
    }
}