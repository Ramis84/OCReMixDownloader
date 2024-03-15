using System.Collections.Generic;

namespace OCReMixDownloader;

public class Parameters
{
    public string? ConfigPath { get; set; }
    public string? OutputPath { get; set; }
    public int? From { get; set; }
    public int? To { get; set; }
    public int Threads { get; set; } = 1;
    public bool IncludeTorrents { get; set; }
    public bool IgnoreHashErrors { get; set; }
    public SortedSet<int> Songs { get; set; } = [];
}