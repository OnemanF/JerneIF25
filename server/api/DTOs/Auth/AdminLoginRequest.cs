namespace api.DTOs.Auth;

public sealed record AdminLoginRequest(string Email, string Password);