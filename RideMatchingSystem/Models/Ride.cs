using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideMatchingSystem.Models;

[Table("Rides")]
public class Ride
{
    [Key]
    public int RideId { get; set; }

    public int RiderId { get; set; }

    public int? DriverId { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal PickupLatitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal PickupLongitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal DropoffLatitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal DropoffLongitude { get; set; }

    public RideStatus Status { get; set; } = RideStatus.Searching;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? OfferedAt { get; set; }

    public DateTime? OfferExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? ArrivedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public Rider? Rider { get; set; }

    public Driver? Driver { get; set; }
}