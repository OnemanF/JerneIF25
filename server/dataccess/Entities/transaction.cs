using System;
using System.Collections.Generic;

namespace JerneIF25.DataAccess.Entities;

public partial class transaction
{
    public long id { get; set; }

    public long player_id { get; set; }

    public string status { get; set; } = null!;

    public decimal amount_dkk { get; set; }

    public string? mobilepay_ref { get; set; }

    public string? note { get; set; }

    public DateTime requested_at { get; set; }

    public DateTime? decided_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool is_deleted { get; set; }

    public virtual player player { get; set; } = null!;
}
