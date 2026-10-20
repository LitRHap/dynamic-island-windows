using System.IO;
using System.Runtime.InteropServices;

namespace DynamicIsland;


internal sealed class DownloadWatcher : IDisposable
{
    private static readonly string[] TempExt = { ".crdownload", ".part", ".opdownload", ".download", ".partial", ".!ut", ".bc!" };
    private const int StallMs = 20000;       

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(ref Guid rfid, uint flags, IntPtr token, out IntPtr path);

    private readonly object _lock = new();
    private readonly Dictionary<string, (long Size, long LastGrow)> _tracked = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileSystemWatcher? _fsw;
    private readonly System.Threading.Timer _timer;
    private long _lastTick = Environment.TickCount64;

    public bool Active { get; private set; }
    public long Bytes { get; private set; }          
    public double Speed { get; private set; }        
    public event Action? Updated;
    public event Action<string>? Completed;          

    public DownloadWatcher()
    {
        string dir = GetDownloadsFolder();
        if (Directory.Exists(dir))
        {
            try
            {
                foreach (var f in Directory.EnumerateFiles(dir)) Track(f);   // unduhan yang sudah berjalan
                _fsw = new FileSystemWatcher(dir) { NotifyFilter = NotifyFilters.FileName, IncludeSubdirectories = false };
                _fsw.Created += (_, e) => Track(e.FullPath);
                _fsw.Deleted += (_, e) => Untrack(e.FullPath);
                _fsw.Renamed += OnRenamed;
                _fsw.Error += (_, _) => { };
                _fsw.EnableRaisingEvents = true;
            }
            catch { }
        }
        _timer = new System.Threading.Timer(_ => Tick(), null, 1000, 1000);
    }

    private static bool IsTemp(string path)
    {
        string ext = Path.GetExtension(path);
        foreach (var t in TempExt)
            if (ext.Equals(t, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void Track(string path)
    {
        if (!IsTemp(path)) return;
        long size = 0;
        try { size = new FileInfo(path).Length; } catch { }
        lock (_lock) _tracked[path] = (size, Environment.TickCount64);
    }

    private void Untrack(string path)
    {
        lock (_lock) _tracked.Remove(path);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        bool wasTemp = IsTemp(e.OldFullPath), isTemp = IsTemp(e.FullPath);
        if (wasTemp) Untrack(e.OldFullPath);
        if (isTemp) Track(e.FullPath);
        if (wasTemp && !isTemp) Completed?.Invoke(Path.GetFileName(e.FullPath));
    }

    private void Tick()
    {
        long now = Environment.TickCount64;
        double dt = Math.Max(0.2, (now - _lastTick) / 1000.0);
        _lastTick = now;

        long total = 0, grown = 0;
        bool any = false;
        lock (_lock)
        {
            foreach (var key in _tracked.Keys.ToList())
            {
                long size;
                try
                {
                    var fi = new FileInfo(key);
                    if (!fi.Exists) { _tracked.Remove(key); continue; }   
                    size = fi.Length;
                }
                catch { continue; }

                var (prev, lastGrow) = _tracked[key];
                if (size > prev) { grown += size - prev; lastGrow = now; }
                _tracked[key] = (size, lastGrow);

                if (now - lastGrow < StallMs) { any = true; total += size; }
            }
        }

        bool changed = any != Active;
        Active = any;
        Bytes = total;
        Speed = any ? Speed * 0.4 + (grown / dt) * 0.6 : 0;
        if (any || changed) Updated?.Invoke();
    }

    private static string GetDownloadsFolder()
    {
        try
        {
            var id = new Guid("374DE290-123F-4565-9164-39C4925E467B");   
            if (SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out var p) == 0)
            {
                try { return Marshal.PtrToStringUni(p) ?? ""; }
                finally { Marshal.FreeCoTaskMem(p); }
            }
        }
        catch { }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    public void Dispose()
    {
        _timer.Dispose();
        _fsw?.Dispose();
    }
}
