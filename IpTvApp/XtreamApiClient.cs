using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using SQLitePCL;

namespace IpTvApp
{
    public class Channel
    {
        public int Id { get; set; }               // primary key
        public string Name { get; set; }
        public string Url { get; set; }
        public string IconUrl { get; set; }
        public string CategoryId { get; set; }
        
        public string Category { get; set; }
        
        // Foreign key: "this channel belongs to one profile"
        public int PlaylistProfileId { get; set; }
        public PlaylistProfile PlaylistProfile { get; set; }
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
        public static List<Channel> Parse(string channelsJson, string categoriesJson, PlaylistProfile profile)
        {
            List<XtreamChannel> xtreamChannels =
                JsonSerializer.Deserialize<List<XtreamChannel>>(channelsJson);
            
            List<XtreamCategory> xtreamCategories =
                JsonSerializer.Deserialize<List<XtreamCategory>>(categoriesJson);
            
            Dictionary<string, string> categoryNames = xtreamCategories
                .ToDictionary(c => c.CategoryId, c => c.CategoryName);

            return xtreamChannels
                .Select(xc => new Channel
                {
                    Name = xc.Name,
                    Category = categoryNames.GetValueOrDefault(xc.CategoryId, "Uncategorized"),
                    Url = profile.BuildStreamUrl(xc.StreamId),
                    IconUrl = xc.IconUrl,
                    CategoryId = xc.CategoryId
                })
                .ToList();
        }
    }
}