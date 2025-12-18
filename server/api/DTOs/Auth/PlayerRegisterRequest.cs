namespace api.DTOs.Auth;

public sealed record PlayerRegisterRequest(string Name, string Email, string Password, string? Phone);