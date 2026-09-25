using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideMatchingSystem.Models;

[Table("Drivers")]
public class Driver
{
    [Key]
    public int DriverId { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string VehicleNumber { get; set; } = string.Empty;

    public DriverStatus Status { get; set; } = DriverStatus.Offline;

    [Column(TypeName = "decimal(10,7)")]
    public decimal CurrentLatitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal CurrentLongitude { get; set; }

    public DateTime LastLocationUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}