using System;
using System.Collections.Generic;

namespace JerneIF25.DataAccess.Entities;

public partial class board_subscription
{
    public long id { get; set; }

    public long player_id { get; set; }

    public List<short> numbers { get; set; } = null!;

    public int remaining_weeks { get; set; }

    public bool is_active { get; set; }

    public DateTime started_at { get; set; }

    public DateTime? canceled_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool is_deleted { get; set; }

    public virtual player player { get; set; } = null!;
}
