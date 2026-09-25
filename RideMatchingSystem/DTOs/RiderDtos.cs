using System.ComponentModel.DataAnnotations;

namespace RideMatchingSystem.DTOs;

public class CreateRiderRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}