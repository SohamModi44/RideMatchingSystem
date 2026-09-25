using System.ComponentModel.DataAnnotations;

namespace RideMatchingSystem.DTOs;

public class CreateRideRequest
{
    [Required]
    public int RiderId { get; set; }

    [Range(-90, 90)]
    public decimal PickupLatitude { get; set; }

    [Range(-180, 180)]
    public decimal PickupLongitude { get; set; }

    [Range(-90, 90)]
    public decimal DropoffLatitude { get; set; }

    [Range(-180, 180)]
    public decimal DropoffLongitude { get; set; }
}

public class CancelRideRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class RideResponse
{
    public int RideId { get; set; }

    public RideStatusResponse Status { get; set; }

    public string StatusText { get; set; } = string.Empty;

    public decimal PickupLatitude { get; set; }

    public decimal PickupLongitude { get; set; }

    public decimal DropoffLatitude { get; set; }

    public decimal DropoffLongitude { get; set; }

    public int? DriverId { get; set; }

    public string? DriverName { get; set; }

    public string? DriverPhone { get; set; }

    public string? VehicleNumber { get; set; }

    public decimal? DriverLatitude { get; set; }

    public decimal? DriverLongitude { get; set; }

    public DateTime CreatedAt { get; set; }
}

public enum RideStatusResponse
{
    Searching = 0,
    DriverOffered = 1,
    DriverAssigned = 2,
    DriverArrived = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6,
    NoDriverFound = 7
}