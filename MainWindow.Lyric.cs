using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DynamicIsland;

// Penghubung lirik <-> pemutar musik. Memakai nama anggota dari MainWindow versi percakapan ini:
//   _session, _playing, _title, _tlTotal, LyricPrev/LyricNow/LyricNext (lihat PageLyrics.xaml.txt)
public partial class MainWindow
{
    private readonly LyricsService _lyricsSvc = new();
    private LyricsResult? _lyr;
    private CancellationTokenSource? _lyrCts;
    private string _lyrKey = "";
    private int _lyrIdx = -2;
    private string _lyrNow = "";             // baris saat ini (dipakai juga oleh teks di pill kecil)

    // + = lirik muncul lebih cepat, - = lebih lambat (milidetik). Sesuaikan jika terasa tidak pas.
    private const int LyricOffsetMs = 0;

    // Panggil saat lagu berganti (di RefreshMediaAsync, setelah _title terisi)
    private async void LoadLyrics(string artist, string title, string album)
    {
        string key = artist + "|" + title;
        if (key == _lyrKey) return;
        _lyrKey = key;
        _lyr = null;
        _lyrIdx = -2;
        _lyrNow = "";
        SetLyricTexts("", "Mencari lirik…", "");

        _lyrCts?.Cancel();
        _lyrCts = new CancellationTokenSource();
        var ct = _lyrCts.Token;
        try
        {
            await Task.Delay(600, ct);               // beri waktu pemutar memperbarui durasi lagu baru
            var res = await _lyricsSvc.GetAsync(artist, title, album, CurrentDuration(), ct);
            if (ct.IsCancellationRequested) return;
            _lyr = res;
            if (res == null) SetLyricTexts("", "Lirik tidak ditemukan", "");
        }
        catch (OperationCanceledException) { }
        catch { if (!ct.IsCancellationRequested) SetLyricTexts("", "Lirik tidak ditemukan", ""); }
    }

    private TimeSpan CurrentDuration()
    {
        if (_session == null) return TimeSpan.Zero;
        try
        {
            var tl = _session.GetTimelineProperties();
            var d = tl.EndTime - tl.StartTime;
            return d.TotalSeconds >= 1 ? d : TimeSpan.Zero;
        }
        catch { return TimeSpan.Zero; }
    }

    private TimeSpan? PlaybackPosition()
    {
        if (_session == null) return null;
        try
        {
            var tl = _session.GetTimelineProperties();
            var pos = tl.Position;
            if (_playing) pos += DateTimeOffset.Now - tl.LastUpdatedTime;
            return pos;
        }
        catch { return null; }
    }

    // Panggil dari OnTick (tiap 250 ms)
    private void UpdateLyricLine()
    {
        if (_lyr == null) return;
        bool visible = _state == IslandState.Expanded && _page == "lyrics";
        if (!_playing && !visible) return;           // tidak ada yang perlu diperbarui

        var pos = PlaybackPosition();
        if (pos is null) return;

        var lines = _lyr.Lines;
        int i = LyricsService.IndexAt(lines, pos.Value + TimeSpan.FromMilliseconds(LyricOffsetMs));
        if (i == _lyrIdx) return;
        _lyrIdx = i;

        string prev = i > 0 ? lines[i - 1].Text : "";
        string now = i >= 0 ? lines[i].Text : "";
        string next = i + 1 < lines.Count ? lines[i + 1].Text : "";
        _lyrNow = now;
        SetLyricTexts(prev, string.IsNullOrWhiteSpace(now) ? "♪" : now, next);
    }

    private void SetLyricTexts(string prev, string now, string next)
    {
        LyricPrev.Text = prev;
        LyricNext.Text = next;
        if (LyricNow.Text == now) return;

        LyricNow.Text = now;
        LyricNow.BeginAnimation(OpacityProperty, new DoubleAnimation(0.3, 1, TimeSpan.FromMilliseconds(280)));
        if (LyricNow.RenderTransform is TranslateTransform t)
            t.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(320))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
    }
}
