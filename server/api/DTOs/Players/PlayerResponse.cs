namespace api.DTOs.Players;

public sealed class PlayerResponse
{
    public long   Id               { get; set; }
    public string Name             { get; set; } = "";
    public string? Phone           { get; set; }
    public string? Email           { get; set; }
    public bool   IsActive         { get; set; }
    public DateOnly? MemberExpiresAt { get; set; }
    public DateTime CreatedAt      { get; set; }
    public DateTime? UpdatedAt     { get; set; }
}