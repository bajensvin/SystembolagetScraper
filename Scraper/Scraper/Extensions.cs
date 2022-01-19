using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http;
using System.Text;

namespace Scraper
{
    public static class Extensions
    {
        public static StringContent AsJsonContent(this object source)
        {
            return new StringContent(
                JsonConvert.SerializeObject(source, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                Encoding.UTF8,
                "application/json");
        }
    }
}
