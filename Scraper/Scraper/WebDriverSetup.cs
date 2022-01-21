using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.Generic;

namespace Scraper
{
    public class WebDriverSetup
    {
        public readonly string _url;
        private readonly ChromeDriver _driver;
        
        public WebDriverSetup()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            _url = config["Urls:MainSite"];
            _driver = new ChromeDriver
            {
                Url = _url
            };
        }
        public ChromeDriver SetupDriver()
        {
            var cookies = new List<Cookie>
            {
                new Cookie("isAgeVerified", "1", "/"),
                new Cookie("cookieConsent", "[]", "/"),
                new Cookie("ai_user", "nl+1f|2022-01-11T10:28:55.407Z", "/"),
                new Cookie("ai_session", "4Act/|1641896935493|1641898794861", "/")
            };

            foreach(var cookie in cookies)
            {
                _driver.Manage().Cookies.AddCookie(cookie);
            }

            _driver.Manage().Window.Maximize();
            return _driver;
        }
    }
}
