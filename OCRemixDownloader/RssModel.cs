using System.Collections.Generic;
using System.Xml.Serialization;

namespace OCRemixDownloader
{
    /// <summary>
    /// Model for RSS 2.0 for XML deserialization
    /// </summary>
    [XmlRoot("rss")]
    public class RssRoot
    {
        [XmlElement("channel")]
        public RssChannel? Channel { get; set; }
    }

    public class RssChannel
    {
        [XmlElement("title")]
        public string? Title { get; set; }

        [XmlElement("link")]
        public string? Link { get; set; }

        [XmlElement("description")]
        public string? Description { get; set; }

        [XmlElement("item")]
        public List<RssItem>? Items { get; set; }
    }

    public class RssItem
    {
        [XmlElement("title")]
        public string? Title { get; set; }

        [XmlElement("link")]
        public string? Link { get; set; }

        [XmlElement("description")]
        public string? Description { get; set; }
    }
}
