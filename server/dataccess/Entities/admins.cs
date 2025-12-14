using System;
using System.Collections.Generic;

namespace JerneIF25.DataAccess.Entities;

public partial class admins
{
    public long id { get; set; }

    public string email { get; set; } = null!;

    public string password_hash { get; set; } = null!;

    public DateTime created_at { get; set; }
}
