using System;
using System.Collections.Generic;

namespace JerneIF25.DataAccess.Entities;

public partial class boards
{
    public long id { get; set; }

    public long game_id { get; set; }

    public long player_id { get; set; }

    public List<short> numbers { get; set; } = null!;

    public decimal price_dkk { get; set; }

    public DateTime purchased_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool is_deleted { get; set; }

    public virtual games game { get; set; } = null!;

    public virtual players player { get; set; } = null!;
}
