var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string ValidUsername = "testuser";
const string ValidPassword = "testpass";

app.MapGet("/get.php", (HttpRequest request) =>
{
    string? username = request.Query["username"];
    string? password = request.Query["password"];

    if (username != ValidUsername || password != ValidPassword)
    {
        return Results.Text("Access denied", statusCode: 403);
    }

    string playlist = """
                      #EXTM3U
                      #EXTINF:-1,Big Buck Bunny (Test)
                      https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8
                      #EXTINF:-1,Sintel (Test)
                      https://bitdash-a.akamaihd.net/content/sintel/hls/playlist.m3u8
                      #EXTINF:-1,Tears of Steel (Test)
                      https://test-streams.mux.dev/tos_ismc/main.m3u8
                      """;

    return Results.Text(playlist, "audio/x-mpegurl");
});

app.Run("http://localhost:5000");