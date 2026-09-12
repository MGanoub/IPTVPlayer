using System.Text.Json.Serialization;

namespace IpTvApp
{
    public class XtreamCategory
    {
        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; }

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; }
    }
}