using Newtonsoft.Json;

namespace fbognini.WebFramework.Api
{
    public class LinksResult
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Next { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Prev { get; set; }
    }
}
