using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.DTOs;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Services;

public class DriverLocationService
{
    private readonly AppDbContext _db;

    public DriverLocationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> UpdateLocationAsync(
        int driverId,
        UpdateDriverLocationRequest request)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(x => x.DriverId == driverId);

        if (driver == null)
            return false;

        driver.CurrentLatitude = request.Latitude;
        driver.CurrentLongitude = request.Longitude;
        driver.LastLocationUpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> GoOnlineAsync(int driverId)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(x => x.DriverId == driverId);

        if (driver == null)
            return false;

        if (driver.LastLocationUpdatedAt == default)
            return false;

        driver.Status = DriverStatus.Available;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> GoOfflineAsync(int driverId)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(x => x.DriverId == driverId);

        if (driver == null)
            return false;

        if (driver.Status == DriverStatus.Busy ||
            driver.Status == DriverStatus.Offered)
        {
            return false;
        }

        driver.Status = DriverStatus.Offline;

        await _db.SaveChangesAsync();

        return true;
    }
}