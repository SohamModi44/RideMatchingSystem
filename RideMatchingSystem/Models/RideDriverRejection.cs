using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RideMatchingSystem.Models;

[Table("RideDriverRejections")]
public class RideDriverRejection
{
    [Key]
    public int Id { get; set; }

    public int RideId { get; set; }

    public int DriverId { get; set; }

    public DateTime RejectedAt { get; set; } = DateTime.UtcNow;
}