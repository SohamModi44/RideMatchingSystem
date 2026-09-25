using System.ComponentModel.DataAnnotations;

namespace RideMatchingSystem.DTOs;

public class CreateDriverRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string VehicleNumber { get; set; } = string.Empty;
}

public class UpdateDriverLocationRequest
{
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }
}