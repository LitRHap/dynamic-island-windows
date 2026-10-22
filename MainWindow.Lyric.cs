using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DynamicIsland;

// Lirik di dalam pemutar musik (panel di sisi kanan halaman Media).
// Mandiri: lagu baru dideteksi sendiri lewat timer, jadi tidak perlu mengubah MainWindow.xaml.cs.
// Memakai anggota dari MainWindow.xaml.cs: _session, _playing, _title, _state, _page, IslandState.
public partial class MainWindow
{
    private readonly LyricsService _lyricsSvc = new();
    private LyricsResult? _lyr;
    private CancellationTokenSource? _lyrCts;
    private string _lyrKey = "";
    private int _lyrIdx = -2;
    private string _lyrNow = "";             // baris saat ini (kosong jika lirik mati)
    private readonly DispatcherTimer _lyrTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };

    // + = lirik muncul lebih cepat, - = lebih lambat (milidetik). Sesuaikan jika terasa tidak pas.
    private const int LyricOffsetMs = 0;

    // Dipanggil dari XAML (Window Loaded="OnLoadedExtra")
    private void OnLoadedExtra(object sender, RoutedEventArgs e)
    {
        LyricsItem.IsChecked = IslandPrefs.LyricsEnabled;
        SyncBreakMenu();
        _lyrTimer.Tick += (_, _) => LyricsTick();
        _lyrTimer.Start();
        InitBreakReminder();
    }

    // ---------------------------------------------------------------- menu on/off
    private void LyricsToggle_Click(object sender, RoutedEventArgs e)
    {
        IslandPrefs.LyricsEnabled = LyricsItem.IsChecked;
        if (!IslandPrefs.LyricsEnabled) ClearLyrics();
        else { _lyrKey = ""; LyricsTick(); }          // nyalakan: muat ulang lirik lagu yang sedang diputar
    }

    // ---------------------------------------------------------------- deteksi lagu
    private void LyricsTick()
    {
        if (!IslandPrefs.LyricsEnabled) return;

        string title = _title ?? "";
        string artist = ArtistText.Text ?? "";
        string key = artist + "|" + title;

        if (title.Length == 0) { if (_lyrKey.Length > 0) ClearLyrics(); return; }
        if (key != _lyrKey) LoadLyrics(artist, title, "");
        UpdateLyricLine();
    }

    private void ClearLyrics()
    {
        _lyrCts?.Cancel();
        _lyr = null;
        _lyrKey = "";
        _lyrIdx = -2;
        _lyrNow = "";
        LyricNow.Text = "";
        LyricNext.Text = "";
        SetLyricsVisible(false);
    }

    private void SetLyricsVisible(bool on)
        => LyricPanel.Visibility = on && IslandPrefs.LyricsEnabled ? Visibility.Visible : Visibility.Collapsed;

    // Boleh juga dipanggil dari RefreshMediaAsync (aman: lagu yang sama diabaikan)
    private async void LoadLyrics(string artist, string title, string album)
    {
        if (!IslandPrefs.LyricsEnabled) return;
        string key = artist + "|" + title;
        if (key == _lyrKey) return;

        _lyrKey = key;
        _lyr = null;
        _lyrIdx = -2;
        _lyrNow = "";
        LyricNow.Text = "";
        LyricNext.Text = "";
        SetLyricsVisible(false);

        _lyrCts?.Cancel();
        _lyrCts = new CancellationTokenSource();
        var ct = _lyrCts.Token;
        try
        {
            await Task.Delay(600, ct);               // beri waktu pemutar memperbarui durasi lagu baru
            string alb = album;
            if (string.IsNullOrEmpty(alb) && _session != null)
            {
                try { var p = await _session.TryGetMediaPropertiesAsync(); alb = p.AlbumTitle ?? ""; } catch { }
            }
            var res = await _lyricsSvc.GetAsync(artist, title, alb, CurrentDuration(), ct);
            if (ct.IsCancellationRequested) return;

            _lyr = res;
            _lyrIdx = -2;
            SetLyricsVisible(res != null);           // tidak ada lirik -> panel disembunyikan
            UpdateLyricLine();
        }
        catch (OperationCanceledException) { }
        catch { }
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

    // ---------------------------------------------------------------- baris lirik
    private void UpdateLyricLine()
    {
        if (_lyr == null || !IslandPrefs.LyricsEnabled) return;
        bool visible = _state == IslandState.Expanded && _page == "media";
        if (!_playing && !visible) return;           // tidak ada yang perlu diperbarui

        var pos = PlaybackPosition();
        if (pos is null) return;

        var lines = _lyr.Lines;
        int i = LyricsService.IndexAt(lines, pos.Value + TimeSpan.FromMilliseconds(LyricOffsetMs));
        if (i == _lyrIdx) return;
        _lyrIdx = i;

        string now = i >= 0 ? lines[i].Text : "";
        string next = i + 1 < lines.Count ? lines[i + 1].Text : "";
        _lyrNow = now;
        SetLyricTexts(string.IsNullOrWhiteSpace(now) ? "♪" : now, next);
    }

    private void SetLyricTexts(string now, string next)
    {
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
