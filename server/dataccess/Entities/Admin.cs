using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JerneIF25.DataAccess.Entities;

[Table("admins")] 
public class Admin
{
    [Key]
    [Column("id")]
    public long id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = null!;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Column("created_at")]
    public DateTime created_at { get; set; } = DateTime.UtcNow;
}