using System;
using System.Collections.Generic;

namespace OCRemixDownloader
{
    public class SettingsModel
    {
        public int? NextDownloadNumber { get; set; }
        public DateTime? LastTorrentDate { get; set; }
        public List<string>? LastTorrentFiles { get; set; }
    }
}
