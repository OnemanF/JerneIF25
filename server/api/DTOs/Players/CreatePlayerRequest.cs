using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Players;

public sealed class CreatePlayerRequest
{
    [Required, MinLength(2)]
    public string Name { get; set; } = null!;

    [Phone]
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public bool? IsActive { get; set; }
}