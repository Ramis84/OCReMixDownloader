namespace OCReMixDownloader
{
    public class Parameters
    {
        public string? ConfigPath { get; set; }
        public string? OutputPath { get; set; }
        public int Threads { get; set; } = 1;
        public bool IncludeTorrents { get; set; }
    }
}
