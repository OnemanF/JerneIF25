using System;
using System.Collections.Generic;

namespace JerneIF25.DataAccess.Entities;

public partial class player_credentials
{
    public long player_id { get; set; }

    public string email { get; set; } = null!;

    public string password_hash { get; set; } = null!;

    public DateTime created_at { get; set; }

    public virtual players player { get; set; } = null!;
}
