using System;
using System.Collections.Generic;

namespace JerneIF25.DataAccess.Entities;

public partial class player
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

    public virtual ICollection<board_subscription> board_subscriptions { get; set; } = new List<board_subscription>();

    public virtual ICollection<board> boards { get; set; } = new List<board>();

    public virtual ICollection<transaction> transactions { get; set; } = new List<transaction>();
}
