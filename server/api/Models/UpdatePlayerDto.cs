using System.ComponentModel.DataAnnotations;

namespace api.Models;

public record UpdatePlayerDto(string Name, string? Phone, string? Email, bool IsActive, DateOnly? MemberExpiresAt);