using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DynamicIsland;

public record LyricLine(TimeSpan Time, string Text);

public sealed record LyricsResult(IReadOnlyList<LyricLine> Lines);

/// <summary>
/// Mengambil lirik tersinkron (format LRC) dari LRCLIB (https://lrclib.net), layanan lirik gratis
/// berbasis komunitas tanpa API key. Hasil disimpan di cache lokal supaya satu lagu hanya diminta sekali.
/// Mandiri: tidak bergantung pada file lain di proyek.
/// </summary>
internal sealed class LyricsService
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var h = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        // LRCLIB meminta aplikasi memperkenalkan diri lewat User-Agent
        h.DefaultRequestHeaders.UserAgent.ParseAdd("DynamicIslandWindows/1.0 (https://github.com/LitRHap/dynamic-island-windows)");
        return h;
    }

    private static string CacheDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DynamicIsland", "lyrics");

    private readonly Dictionary<string, LyricsResult?> _mem = new();

    /// <returns>null jika lirik tersinkron tidak tersedia atau jaringan gagal.</returns>
    public async Task<LyricsResult?> GetAsync(string artist, string title, string album, TimeSpan duration,
                                              CancellationToken ct = default)
    {
        (artist, title) = Clean(artist, title);
        if (title.Length == 0) return null;

        string key = Hash($"{artist}|{title}|{(int)duration.TotalSeconds}");
        if (_mem.TryGetValue(key, out var cached)) return cached;

        if (TryReadCache(key, out var fromDisk))
        {
            _mem[key] = fromDisk;
            return fromDisk;
        }

        string? raw;
        try { raw = await FetchRawAsync(artist, title, album, duration, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return null; }                       // gagal jaringan: jangan di-cache, coba lagi nanti

        WriteCache(key, raw);                        // "tidak ada" juga disimpan agar tidak terus meminta
        var res = ToResult(raw);
        _mem[key] = res;
        return res;
    }

    // ---------------------------------------------------------------- jaringan
    private static async Task<string?> FetchRawAsync(string artist, string title, string album,
                                                     TimeSpan duration, CancellationToken ct)
    {
        string q = $"artist_name={Uri.EscapeDataString(artist)}&track_name={Uri.EscapeDataString(title)}";
        string exact = q;
        if (!string.IsNullOrWhiteSpace(album)) exact += "&album_name=" + Uri.EscapeDataString(album);
        if (duration > TimeSpan.Zero) exact += "&duration=" + (int)Math.Round(duration.TotalSeconds);

        using (var resp = await Http.GetAsync("https://lrclib.net/api/get?" + exact, ct))
        {
            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
                var s = SyncedOf(doc.RootElement);
                if (s != null) return s;
            }
            else if (resp.StatusCode != HttpStatusCode.NotFound)
                throw new HttpRequestException($"LRCLIB {(int)resp.StatusCode}");
        }

        // cadangan: pencarian yang lebih longgar
        using var sr = await Http.GetAsync("https://lrclib.net/api/search?" + q, ct);
        if (!sr.IsSuccessStatusCode)
        {
            if (sr.StatusCode == HttpStatusCode.NotFound) return null;
            throw new HttpRequestException($"LRCLIB {(int)sr.StatusCode}");
        }

        using var sdoc = JsonDocument.Parse(await sr.Content.ReadAsStringAsync(ct));
        if (sdoc.RootElement.ValueKind != JsonValueKind.Array) return null;

        string? best = null;
        double bestDiff = double.MaxValue;
        foreach (var el in sdoc.RootElement.EnumerateArray())
        {
            var synced = SyncedOf(el);
            if (synced == null) continue;
            double diff = 0;
            if (duration > TimeSpan.Zero && el.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number)
                diff = Math.Abs(d.GetDouble() - duration.TotalSeconds);
            if (diff < bestDiff) { bestDiff = diff; best = synced; }
        }
        return best != null && (duration <= TimeSpan.Zero || bestDiff <= 8) ? best : null;
    }

    private static string? SyncedOf(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        if (el.TryGetProperty("instrumental", out var ins) && ins.ValueKind == JsonValueKind.True) return null;
        if (el.TryGetProperty("syncedLyrics", out var s) && s.ValueKind == JsonValueKind.String)
        {
            var text = s.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        return null;
    }

    // ---------------------------------------------------------------- format LRC
    private static readonly Regex Stamp = new(@"\[(\d{1,3}):(\d{2})(?:[.:](\d{1,3}))?\]", RegexOptions.Compiled);

    public static List<LyricLine> ParseLrc(string lrc)
    {
        var lines = new List<LyricLine>();
        foreach (var raw in lrc.Replace("\r\n", "\n").Split('\n'))
        {
            var matches = Stamp.Matches(raw);
            if (matches.Count == 0) continue;                 // baris metadata seperti [ar:...] dilewati

            // semua cap waktu di awal baris, sisanya adalah teks
            int end = 0;
            var times = new List<TimeSpan>();
            foreach (Match m in matches)
            {
                if (m.Index < end || raw[end..m.Index].Trim().Length != 0) break;
                end = m.Index + m.Length;
                int min = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int sec = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                double frac = m.Groups[3].Success
                    ? double.Parse("0." + m.Groups[3].Value, CultureInfo.InvariantCulture) : 0;
                times.Add(TimeSpan.FromSeconds(min * 60 + sec + frac));
            }
            string text = raw[end..].Trim();
            foreach (var t in times) lines.Add(new LyricLine(t, text));
        }
        lines.Sort((a, b) => a.Time.CompareTo(b.Time));
        return lines;
    }

    /// <summary>Indeks baris yang sedang dinyanyikan pada posisi tertentu (-1 = belum mulai).</summary>
    public static int IndexAt(IReadOnlyList<LyricLine> lines, TimeSpan pos)
    {
        int lo = 0, hi = lines.Count - 1, ans = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (lines[mid].Time <= pos) { ans = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return ans;
    }

    private static LyricsResult? ToResult(string? raw)
    {
        if (raw == null) return null;
        var lines = ParseLrc(raw);
        return lines.Count == 0 ? null : new LyricsResult(lines);
    }

    // ---------------------------------------------------------------- membersihkan judul/artis
    private static readonly Regex Junk = new(
        @"\s*[\(\[][^)\]]*(official|video|audio|lyric|lirik|mv|hd|4k|visualizer|remaster|clip)[^)\]]*[\)\]]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Merapikan metadata, terutama dari YouTube ("Artis - Judul (Official Video)", saluran "… - Topic").</summary>
    public static (string Artist, string Title) Clean(string artist, string title)
    {
        artist = (artist ?? "").Trim();
        title = (title ?? "").Trim();

        title = Junk.Replace(title, "").Trim();
        artist = Regex.Replace(artist, @"\s*-\s*Topic$", "", RegexOptions.IgnoreCase).Trim();
        artist = Regex.Replace(artist, @"VEVO$", "", RegexOptions.IgnoreCase).Trim();

        bool channelLike = artist.Length == 0 ||
                           artist.Contains("official", StringComparison.OrdinalIgnoreCase);
        int dash = title.IndexOf(" - ", StringComparison.Ordinal);
        if (channelLike && dash > 0)
        {
            artist = title[..dash].Trim();
            title = title[(dash + 3)..].Trim();
        }
        return (artist, title);
    }

    // ---------------------------------------------------------------- cache di disk
    private static string Hash(string s) => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(s)));

    private static bool TryReadCache(string key, out LyricsResult? res)
    {
        res = null;
        try
        {
            string path = Path.Combine(CacheDir, key + ".lrc");
            if (!File.Exists(path)) return false;
            string text = File.ReadAllText(path, Encoding.UTF8);
            res = text == "#none" ? null : ToResult(text);
            return true;
        }
        catch { return false; }
    }

    private static void WriteCache(string key, string? raw)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            File.WriteAllText(Path.Combine(CacheDir, key + ".lrc"), raw ?? "#none", Encoding.UTF8);
        }
        catch { }
    }
}
