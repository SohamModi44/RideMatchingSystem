using System.ComponentModel.DataAnnotations;

namespace RideMatchingSystem.DTOs;

public class UpdateLocationRequest
{
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Range(-180, 180)]
    public decimal Longitude { get; set; }
}