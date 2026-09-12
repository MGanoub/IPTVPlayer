# IpTvApp

A desktop IPTV player for Windows, built with WPF and LibVLCSharp. Connects to Xtream Codes–based providers, browses live channels by category, and plays streams directly — all with a local SQLite cache so your channel list survives a restart without hitting the network.

This project was built as a practice exercise in C#, WPF, and EF Core — the architecture reflects that: it favors clarity over cleverness, and a few design decisions are called out below specifically because they were practicing moments.

<img width="1252" height="721" alt="image" src="https://github.com/user-attachments/assets/1c5c8689-30c3-4cf1-8cb3-927d3bc12c9c" />


## Features

- **Multiple provider profiles** — save server URL, username, and password per provider, switch between them from a dropdown
- **Xtream Codes API integration** — authenticates and pulls channels via `player_api.php`, not raw M3U export (more reliable, avoids slow/timeout-prone playlist export endpoints on some providers)
- **Category browsing** — channels are grouped using the provider's real `get_live_categories` data, with a search box to filter categories (built to comfortably handle playlists in the 10,000+ channel range)
- **Local caching via SQLite + EF Core** — channels and profiles persist to a local database; reopening the app shows your last-loaded channel list instantly, no re-fetch required
- **Playback via LibVLCSharp** — handles HLS, MPEG-TS, and other live-stream formats VLC supports natively
- **Playback controls** — play/pause, stop, volume
- **Dark UI** — custom WPF styles and control templates (no default Windows chrome)

## Tech stack

| Layer | Choice |
|---|---|
| UI framework | WPF (.NET 10) |
| Video playback | LibVLCSharp / LibVLC |
| Data storage | SQLite via Entity Framework Core |
| Provider protocol | Xtream Codes JSON API |

## Getting started

### Prerequisites

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An IPTV subscription using an Xtream Codes–compatible panel (server URL, username, password)

### Setup

```bash
git clone https://github.com/MGanoub/IpTvApp.git
cd IpTvApp
dotnet restore
dotnet ef database update
dotnet run
```

`dotnet ef database update` creates the local SQLite database (`%AppData%\IpTvApp\iptv.db`) on first run. If `dotnet-ef` isn't installed:

```bash
dotnet tool install --global dotnet-ef
```

### Using the app

1. Click **New** next to the Profile dropdown.
2. Expand **Profile details** and enter your provider's server URL (just the base, e.g. `http://example.com:8080`, no path), username, and password.
3. Click **Save Profile**, then **Load Playlist**.
4. Browse categories on the left, click a channel to play it.

## Architecture notes

A few decisions worth explaining, since they weren't the first thing tried:

- **Channels belong to a profile via a foreign key** (`Channel.PlaylistProfileId`), not a join table. Each channel is provider-specific (its stream URL is built from that provider's credentials), so this is a genuine one-to-many relationship, not many-to-many.
- **Category browsing is two-level (category list → channel list) rather than one grouped list.** An earlier version tried grouping a single `ListBox` by category, which works fine for a few hundred items but caused serious memory/performance problems at the scale this app actually needs to support (playlists with 10,000+ channels are common with IPTV providers). Splitting into "pick a category, then see its channels" keeps both lists small regardless of total channel count.

## Known limitations

- **Credentials are stored in plaintext** in the local SQLite database. Fine for personal use on a trusted machine; not suitable as-is if this were ever multi-user or handling more sensitive credentials.
- Tested primarily against one Xtream Codes provider — panel implementations vary, so stream URL patterns (`.ts` vs `.m3u8`, port differences between the API and stream server) may need adjustment for other providers.
- No EPG (program guide) support yet.
- No favorites yet.

## Roadmap

- [ ] Favorites (star a channel, view a dedicated Favorites list)
- [ ] Resume-to-live behavior after pausing a live stream
- [ ] Encrypt stored credentials

## Disclaimer

This is a generic IPTV client. It does not provide, bundle, or endorse any specific content or provider. You are responsible for only using it with IPTV services you're legitimately authorized to access.

## License

All rights reserved. This code is not licensed for reuse, modification, or distribution without prior written permission.
