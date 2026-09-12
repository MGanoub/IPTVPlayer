using System.IO;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace IpTvApp;

public class AppDbContext : DbContext
{
    public DbSet<PlaylistProfile> Profiles { get; set; }
    public DbSet<Channel> Channels { get; set; }
    
    static AppDbContext()
    {
        Batteries.Init();
    }

    private static readonly string DbPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IpTvApp", "iptv.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        options.UseSqlite($"Data Source={DbPath}");
    }
}