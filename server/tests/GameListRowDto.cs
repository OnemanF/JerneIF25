using System;
using System.Collections.Generic;

namespace tests;

public sealed class GameListRowDto
{
    public long id { get; set; }
    public DateOnly week_start { get; set; }
    public string status { get; set; } = "";
    public List<short>? winning { get; set; }
    public decimal revenueDkk { get; set; }
}