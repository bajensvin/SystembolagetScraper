using HtmlAgilityPack;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Scraper
{
    class Program
    {
        public static void Main()
        {
            var setup = new WebDriverSetup();
            var driver = setup.SetupDriver();
            driver.Navigate().GoToUrl($"{driver.Url}/nytt");
            var doc = new HtmlDocument();
            doc.LoadHtml(driver.PageSource);

            var upcomingReleasesContainer = doc.DocumentNode.SelectNodes("/html/body/div[1]/div[2]/main/div[2]/div/div/div[1]/div/div[1]/div/div/div");

            var datePattern = new Regex(@"\d+\/\d+");
            var wordPattern = new Regex(@"[a-zA-ZåäöÅÄÖ ]");
            var upcomingReleasesNodes = upcomingReleasesContainer.Select(x => x.SelectNodes(".//a[contains(@href, 'sok')]")).First();
            
            var upcomingLaunches = upcomingReleasesNodes.Select(x => new UpcomingLaunch { Date = datePattern.Match(x.InnerText).Value, Link = new Uri(HttpUtility.HtmlDecode(x.GetAttributeValue("href", string.Empty))), Type = HttpUtility.HtmlDecode(x.InnerText.Substring(x.InnerText.IndexOf(' ') + 1)) }).ToList();

            //foreach (var link in upcomingLaunches.Select(y => y.Link))
            //{
            //    driver.Navigate().GoToUrl(new Uri(""));
            //}

            driver.Quit();
        }
    }

    class UpcomingLaunch
    {
        public string Type { get; set; }
        public string Date { get; set; }
        public Uri Link { get; set; }
    }

}
