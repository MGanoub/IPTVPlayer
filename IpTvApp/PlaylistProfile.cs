using System;

namespace IpTvApp
{
    public class PlaylistProfile
    {
        public string Name { get; set; }
        public string ServerUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string BuildChannelsApiUrl()
        {
            return $"{ServerUrl}/player_api.php?username={Username}&password={Password}" +
                   $"&action=get_live_streams";
        }

        public string BuildStreamUrl(int streamId)
        {
            return $"{ServerUrl}/live/{Username}/{Password}/{streamId}.ts";
        }
    }
}