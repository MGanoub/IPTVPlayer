using System;

namespace IpTvApp
{
    public class PlaylistProfile
    {
        public int Id { get; set; }              // primary key, EF convention
        public string Name { get; set; }
        public string ServerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime? LastUpdated { get; set; }
        
        // Navigation property: "a profile has many channels"
        public List<Channel> Channels { get; set; } = new();

        public string BuildChannelsApiUrl()
        {
            return $"{ServerUrl}/player_api.php?username={Username}&password={Password}" +
                   $"&action=get_live_streams";
        }
        
        public string BuildCategoriesApiUrl()
        {
            return $"{ServerUrl}/player_api.php?username={Username}&password={Password}" +
                   $"&action=get_live_categories";
        }

        public string BuildStreamUrl(int streamId)
        {
            return $"{ServerUrl}/live/{Username}/{Password}/{streamId}.ts";
        }
    }
}