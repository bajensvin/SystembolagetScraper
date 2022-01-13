using System;

namespace Scraper.Models
{
    public class UpcomingLaunch
    {
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public Uri Link { get; set; }
    }
}
