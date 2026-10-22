using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DynamicIsland;

/// <summary>Waktu sejak terakhir kali keyboard/mouse dipakai.</summary>
internal static class IdleTime
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    public static TimeSpan Get()
    {
        var li = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref li)) return TimeSpan.Zero;
        uint now = unchecked((uint)Environment.TickCount);
        return TimeSpan.FromMilliseconds(unchecked(now - li.dwTime));
    }
}

// Pengingat istirahat: setelah N menit terus-menerus memakai komputer, island menampilkan pengingat.
// Jika tidak ada aktivitas selama 3 menit atau lebih, hitungan dianggap sudah istirahat dan dimulai ulang.
public partial class MainWindow
{
    private readonly DispatcherTimer _breakTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private DateTime _breakStart = DateTime.Now;      // awal pemakaian terus-menerus
    private DateTime _breakLastTick = DateTime.Now;
    private static readonly TimeSpan BreakIdleReset = TimeSpan.FromMinutes(3);

    private void InitBreakReminder()
    {
        _breakStart = _breakLastTick = DateTime.Now;
        _breakTimer.Tick += (_, _) => BreakTick();
        _breakTimer.Start();
    }

    private void BreakTick()
    {
        var now = DateTime.Now;
        if (now - _breakLastTick > TimeSpan.FromMinutes(2)) _breakStart = now;   // PC habis tidur/sleep
        _breakLastTick = now;

        if (IdleTime.Get() >= BreakIdleReset) { _breakStart = now; return; }     // sedang menjauh dari layar

        int minutes = IslandPrefs.BreakMinutes;
        if (minutes <= 0) return;
        if (now - _breakStart < TimeSpan.FromMinutes(minutes)) return;

        // Jangan menyela saat island sedang dipakai; coba lagi di putaran berikutnya
        if (_state == IslandState.Expanded && Island.IsMouseOver) return;

        _breakStart = now;
        ShowBreakToast(minutes);
    }

    private void ShowBreakToast(int minutes)
    {
        System.Media.SystemSounds.Asterisk.Play();
        ShowToast("clock", $"Sudah {BreakDuration(minutes)} menatap layar. Istirahat sebentar ya!", null, 12000);
    }

    private static string BreakDuration(int m)
    {
        if (m < 60) return $"{m} menit";
        int h = m / 60, r = m % 60;
        return r == 0 ? $"{h} jam" : r == 30 ? $"{h},5 jam" : $"{h} jam {r} menit";
    }

    // ---------------------------------------------------------------- menu klik kanan
    private void SyncBreakMenu()
    {
        foreach (var o in BreakMenu.Items)
            if (o is MenuItem mi && int.TryParse(mi.Tag as string, out int m))
                mi.IsChecked = m == IslandPrefs.BreakMinutes;
    }

    private void BreakInterval_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mi && int.TryParse(mi.Tag as string, out int m))
        {
            IslandPrefs.BreakMinutes = m;
            _breakStart = DateTime.Now;
            SyncBreakMenu();
        }
    }

    private void BreakTest_Click(object sender, RoutedEventArgs e)
        => ShowBreakToast(IslandPrefs.BreakMinutes > 0 ? IslandPrefs.BreakMinutes : 60);
}
