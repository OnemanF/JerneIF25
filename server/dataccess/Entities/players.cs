using System;
using System.Collections.Generic;

namespace JerneIF25.DataAccess.Entities;

public partial class players
{
    public long id { get; set; }

    public string name { get; set; } = null!;

    public string? phone { get; set; }

    public string? email { get; set; }

    public bool is_active { get; set; }

    public DateOnly? member_expires_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool is_deleted { get; set; }

    public virtual ICollection<board_subscriptions> board_subscriptions { get; set; } = new List<board_subscriptions>();

    public virtual ICollection<boards> boards { get; set; } = new List<boards>();

    public virtual player_credentials? player_credentials { get; set; }

    public virtual ICollection<transactions> transactions { get; set; } = new List<transactions>();
}
