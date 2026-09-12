using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IpTvApp
{
    public class Channel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string IconUrl { get; set; }
        public string CategoryId { get; set; }
    }

    // Matches the raw shape returned by player_api.php?action=get_live_streams
    public class XtreamChannel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("stream_id")]
        public int StreamId { get; set; }

        [JsonPropertyName("stream_icon")]
        public string IconUrl { get; set; }

        [JsonPropertyName("category_id")]
        public string CategoryId { get; set; }
    }

    public static class XtreamApiClient
    {
        public static List<Channel> Parse(string json, PlaylistProfile profile)
        {
            List<XtreamChannel> xtreamChannels =
                JsonSerializer.Deserialize<List<XtreamChannel>>(json);

            return xtreamChannels
                .Select(xc => new Channel
                {
                    Name = xc.Name,
                    Url = profile.BuildStreamUrl(xc.StreamId),
                    IconUrl = xc.IconUrl,
                    CategoryId = xc.CategoryId
                })
                .ToList();
        }
    }
}