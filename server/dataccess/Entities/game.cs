using System;
using System.Collections.Generic;

namespace JerneIF25.DataAccess.Entities;

public partial class game
{
    public long id { get; set; }

    public DateOnly week_start { get; set; }

    public string status { get; set; } = null!;

    public List<short>? winning_nums { get; set; }

    public DateTime? published_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool is_deleted { get; set; }

    public virtual ICollection<board> boards { get; set; } = new List<board>();
}
