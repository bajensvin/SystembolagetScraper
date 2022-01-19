using System;

namespace Scraper.Models
{
    public class Beer
    {
        public int Id { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string ReleaseType { get; set; }
        public Uri DetailPageLink { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public decimal Price { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }
}
