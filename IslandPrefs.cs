using System.IO;
using System.Text.Json;

namespace DynamicIsland;

/// <summary>Pengaturan kecil yang diingat antar sesi: %AppData%\DynamicIsland\prefs.json</summary>
internal static class IslandPrefs
{
    private sealed class Data
    {
        public bool LyricsEnabled { get; set; } = true;
        public int BreakMinutes { get; set; } = 60;     // 0 = pengingat istirahat mati
    }

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DynamicIsland", "prefs.json");

    private static readonly Data _d = Load();

    public static bool LyricsEnabled { get => _d.LyricsEnabled; set { _d.LyricsEnabled = value; Save(); } }
    public static int BreakMinutes { get => _d.BreakMinutes; set { _d.BreakMinutes = value; Save(); } }

    private static Data Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Data>(File.ReadAllText(FilePath)) ?? new Data();
        }
        catch { }
        return new Data();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_d, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
