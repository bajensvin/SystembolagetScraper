using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            
            var upcomingLaunches = upcomingReleasesNodes.Select(x => new UpcomingLaunch { Date = DateTime.Parse(datePattern.Match(x.InnerText).Value, CultureInfo.CurrentCulture), Link = new Uri(HttpUtility.HtmlDecode(x.GetAttributeValue("href", string.Empty))), Type = HttpUtility.HtmlDecode(x.InnerText.Substring(x.InnerText.IndexOf(' ') + 1)) }).ToList();

            foreach (var launch in upcomingLaunches)
            {
                driver.Navigate().GoToUrl(launch.Link);

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/div[2]/main/div[2]/div/div/div/div[2]/div[4]")));

                var showMoreButtonSelector = By.XPath("//button[text()='Visa fler ']");

                if (driver.FindElements(showMoreButtonSelector).Count > 0)
                {
                    var showMoreButton = driver.FindElement(showMoreButtonSelector);

                    while (showMoreButton.Displayed)
                    {
                        try
                        {
                            showMoreButton.Click();
                            showMoreButton = driver.FindElement(showMoreButtonSelector);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                }

                var beerListDoc = new HtmlDocument();
                beerListDoc.LoadHtml(driver.PageSource);
                var beerlist = beerListDoc.DocumentNode.SelectNodes("/html/body/div[1]/div[2]/main/div[2]/div/div/div/div[2]/div[4]");
                var beersNodes = beerlist.Select(x => x.SelectNodes(".//a[contains(@href, 'produkt/ol')]")).First();

                var beers = new List<Beer>();

                foreach (var beer in beersNodes)
                {
                    beers.Add(new Beer
                    {
                        Name = beer.SelectSingleNode("//div/div/div[3]/div/div/div/div/h3").InnerText,
                        Type = beer.SelectSingleNode("//div/div/div[3]/div/div/div/h4").InnerText,
                        Description = beer.SelectSingleNode("//div/div/div[3]/div/div/div[2]/div[1]").InnerText,
                        Country = beer.SelectSingleNode("//div/div/div[3]/div/div/div/div[1]/span[2]").InnerText,
                        Size = beer.SelectSingleNode("//div/div/div[3]/div[2]/div[1]/div[2]").InnerText
                        //Price = Convert.ToDecimal(beer.SelectSingleNode("//div/div/div[3]/div[2]/div[3]/span[1]"), CultureInfo.CurrentCulture)
                        //Need to get info about the date for the release this item is included in
                    });
                }

                //Save beer entries to db
            }

            driver.Quit();
        }
    }

    class UpcomingLaunch
    {
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public Uri Link { get; set; }
    }

    class Beer
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public decimal Price { get; set; }
        public string Size { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }

}
