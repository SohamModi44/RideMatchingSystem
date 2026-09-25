using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideMatchingSystem.Data;
using RideMatchingSystem.DTOs;
using RideMatchingSystem.Models;

namespace RideMatchingSystem.Controllers;

[ApiController]
[Route("api/riders")]
public class RidersController : ControllerBase
{
    private readonly AppDbContext _db;

    public RidersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRiders()
    {
        var riders = await _db.Riders
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.RiderId,
                x.FullName,
                x.PhoneNumber,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(riders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRider(
        CreateRiderRequest request)
    {
        var rider = new Rider
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Riders.Add(rider);

        await _db.SaveChangesAsync();

        return Created(
            $"/api/riders/{rider.RiderId}",
            new
            {
                rider.RiderId,
                rider.FullName,
                rider.PhoneNumber
            });
    }
}