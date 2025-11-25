using System.ComponentModel.DataAnnotations;

namespace api.Models;

public record CreatePlayerDto(string Name, string? Phone, string? Email);