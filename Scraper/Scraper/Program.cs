using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Scraper.Models;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Scraper
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static HttpClient _client;
        public Program()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
            _client = new HttpClient
            {
                BaseAddress = new Uri("")
            };
        }

        public static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task MainAsync()
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

            var beers = new List<Beer>();

            foreach (var launch in upcomingLaunches)
            {
                driver.Navigate().GoToUrl(launch.Link);

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("/html/body/div[1]/div[2]/main/div[2]/div/div/div/div[2]/div[4]")));

                var showMoreButtonSelector = By.XPath("//button[text()='Visa fler ']");

                if (driver.FindElements(showMoreButtonSelector).Count > 0)
                {
                    var showMoreButton = driver.FindElement(showMoreButtonSelector);

                    //Click show more until the button is no longer visible, meaning that the end of the list has been reached
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
                var beerNodes = beerlist.Select(x => x.SelectNodes(".//a[contains(@href, 'produkt/ol')]")).FirstOrDefault();

                if (beerNodes != null && beerNodes.Any())
                {
                    foreach (var beer in beerNodes)
                    {
                        try
                        {
                            beers.Add(new Beer
                            {
                                Name = beer.SelectSingleNode(".//div/div/div[3]/div/div/div/div/h3").InnerText,
                                Type = beer.SelectSingleNode(".//div/div/div[3]/div/div/div/h4").InnerText,
                                Description = beer.SelectSingleNode(".//div/div/div[3]/div/div/div[2]/div[1]").InnerText,
                                Country = beer.SelectSingleNode(".//div/div/div[3]/div/div/div/div[1]/span[2]").InnerText,
                                Size = beer.SelectSingleNode(".//div/div/div[3]/div[2]/div[1]/div[2]").InnerText,
                                ReleaseDate = launch.Date,
                                ReleaseType = launch.Type,
                                DetailPageLink = new Uri(HttpUtility.HtmlDecode($"{setup._url}{beer.GetAttributeValue("href", string.Empty)}")),
                                Price = decimal.Parse(beer.SelectSingleNode(".//div/div/div[3]/div[2]/div[3]/span[1]").InnerText.TrimEnd('*').Replace(':', ','), CultureInfo.CurrentCulture),
                                Id = int.Parse(beer.GetAttributeValue("href", string.Empty).Substring(beer.GetAttributeValue("href", string.Empty).LastIndexOf('-') + 1).Replace("/", ""))
                            });
                        }
                        catch(Exception e)
                        {
                            Logger.Error(e);
                            throw new Exception(e.StackTrace);
                        }
                    }
                }

                if (beers.Any())
                {
                    await PostBeerData(beers);
                }
                
            }
            driver.Quit();
        }
        private static async Task<HttpResponseMessage> PostBeerData(List<Beer> beers)
        {
            var response = await _client.PostAsync("/SaveBeerData", beers.AsJsonContent());
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}
