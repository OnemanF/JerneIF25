using api.DTOs.Players;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class PlayersService : IPlayersService
{
    private readonly ApplicationDbContext _db;

    public PlayersService(ApplicationDbContext db) => _db = db;

    public async Task<PlayerResponse> CreateAsync(CreatePlayerRequest dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Trim().Length < 2)
            throw new ArgumentException("Name must be at least 2 characters.", nameof(dto.Name));

        var p = new players
        {
            name = dto.Name.Trim(),
            phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone!.Trim(),
            email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email!.Trim(),
            is_active = dto.IsActive ?? false,
            created_at = DateTime.UtcNow
        };

        _db.players.Add(p);
        await _db.SaveChangesAsync(ct);

        return Map(p);
    }

    public async Task<PlayerResponse> UpdateAsync(long id, UpdatePlayerRequest dto, CancellationToken ct = default)
    {
        var p = await _db.players.FirstOrDefaultAsync(x => x.id == id && !x.is_deleted, ct);
        if (p is null) throw new KeyNotFoundException("Player not found.");

        p.name = dto.Name.Trim();
        p.phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone!.Trim();
        p.email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email!.Trim();
        p.is_active = dto.IsActive;
        p.member_expires_at = dto.MemberExpiresAt;
        p.updated_at = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
    {
        var p = await _db.players.FirstOrDefaultAsync(x => x.id == id && !x.is_deleted, ct);
        if (p is null) throw new KeyNotFoundException("Player not found.");
        p.is_deleted = true;
        p.updated_at = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static PlayerResponse Map(players p) => new()
    {
        Id = p.id,
        Name = p.name,
        Phone = p.phone,
        Email = p.email,
        IsActive = p.is_active,
        MemberExpiresAt = p.member_expires_at,
        CreatedAt = p.created_at,
        UpdatedAt = p.updated_at
    };
}
