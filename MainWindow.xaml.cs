using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Windows.Devices.Radios;
using Windows.Media.Control;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace DynamicIsland;

public record ShelfItem(string FullPath)
{
    public string Display => Path.GetFileName(FullPath.TrimEnd('\\', '/'));
}

public record NotifItem(string App, string Title, string Body)
{
    public string Header => string.IsNullOrWhiteSpace(Title) ? App : $"{App} · {Title}";
}

public partial class MainWindow : Window
{
    private const double WW = 480, WH = 230;    
    private const double CW = 170, CH = 32;     
    private const double TW = 290, TH = 56;     
    private const double EW = 410;              
    private const double MaxRadius = 26;
    private const byte GlassA = 0x40, GlassR = 34, GlassG = 40, GlassB = 62;
    private const double Jelly = 0.0;
    private static readonly bool AutoHideWhenCovered = true;
    private const double PeekPx = 3;


    private static readonly string[] PageOrder = { "calendar", "media", "lyrics", "stats", "control", "notif", "timer", "files" };
    private static readonly Dictionary<string, double> PageH = new()
    {
        ["calendar"] = 140, ["media"] = 92, ["lyrics"] = 92, ["stats"] = 120, ["control"] = 120, ["notif"] = 104, ["timer"] = 92, ["files"] = 92
    };

    private enum IslandState { Collapsed, Toast, Expanded }
    private IslandState _state = IslandState.Collapsed;
    private string _page = "calendar";      

    private IntPtr _hwnd;
    private double _dpi = 1;
    private int _winX;                
    private bool _dragging;
    private bool _covering;             

    private double _w, _h, _y, _vw, _vh, _vy;   
    private double _tw, _th, _ty;            
    private double _wob, _vwob;                
    private double _zeta = 0.5;                 
    private bool _springOn;
    private long _lastTicks;
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private Backdrop? _backdrop;
    private Point _dragStart;

    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _leaveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer _peekTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _sysTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _coverTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly DispatcherTimer _capTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };

    private (string Name, FrameworkElement Page, Button Tab)[] _pages =
        Array.Empty<(string Name, FrameworkElement Page, Button Tab)>();

    private readonly ObservableCollection<ShelfItem> _shelf = new();

    public MainWindow()
    {
        InitializeComponent();
        Left = 0; Top = 0;

        FileList.ItemsSource = _shelf;
        _shelf.CollectionChanged += (_, _) =>
            FilesHint.Visibility = _shelf.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            CompositionTarget.Rendering -= OnSpring;
            _backdrop?.Dispose();
            _dev?.Dispose();
            _caps?.Dispose();
            _dl?.Dispose();
            _btTimer.Stop();
        };
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        _dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        Native.MakeToolNoActivate(_hwnd);
        Native.ExcludeFromCapture(_hwnd);
        PlaceWindow();
    }

    private void PlaceWindow()
    {
        int pw = (int)Math.Round(WW * _dpi), ph = (int)Math.Round(WH * _dpi);
        _winX = (Native.ScreenWidthPx() - pw) / 2;
        Native.MoveResize(_hwnd, _winX, 0, pw, ph);
    }

    private void InitBackdrop()
    {
        _backdrop?.Dispose();
        _backdrop = new Backdrop((int)Math.Round(WW * _dpi), (int)Math.Round(WH * _dpi));
        BackdropImg.Source = _backdrop.Output;
        UpdateBackdrop();
    }

    private void UpdateBackdrop()
    {
        if (_backdrop == null) return;
        if (_ty < 0 && !_springOn) return;      
        _backdrop.Update(_winX, 0);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        PlaceWindow();
        TintRect.Fill = new SolidColorBrush(Color.FromArgb(GlassA, GlassR, GlassG, GlassB));

        _w = _tw = CW; _h = _th = CH; _y = _ty = 0;
        ApplyShape();
        InitBackdrop();

        _capTimer.Tick += (_, _) => UpdateBackdrop(); _capTimer.Start();
        _coverTimer.Tick += OnCover; _coverTimer.Start();
        _tick.Tick += OnTick; _tick.Start();
        _leaveTimer.Tick += (_, _) =>
        {
            _leaveTimer.Stop();
            if (!Island.IsMouseOver && !_dragging && !_typing && _state == IslandState.Expanded) SetState(IslandState.Collapsed);
        };
        _peekTimer.Tick += (_, _) =>
        {
            _peekTimer.Stop();
            if (!Island.IsMouseOver && !_typing && _state == IslandState.Expanded) SetState(IslandState.Collapsed);
        };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            if (_state == IslandState.Toast) SetState(IslandState.Collapsed);
        };
        _sysTimer.Tick += OnSys; _sysTimer.Start();

        _pages = new (string Name, FrameworkElement Page, Button Tab)[]
        {
            ("calendar", PageCalendar, TabCal), ("media", PageMedia, TabMedia), ("lyrics", PageLyrics, TabLyrics), ("stats", PageStats, TabStats), ("control", PageControl, TabCtrl),
            ("notif", PageNotif, TabNotif), ("timer", PageTimer, TabTimer), ("files", PageFiles, TabFiles)
        };
        NotifList.ItemsSource = _notifs;
        _statTimer.Tick += OnStatTick;
        _caps = new CapsLockWatcher();
        CapsBadge.Opacity = _caps.IsOn ? 1 : 0;
        _caps.Changed += on => Dispatcher.BeginInvoke(() => OnCapsChanged(on));
        _ = InitRadiosAsync();
        StartBluetoothWatch();
        StartDownloadWatch();

        InitCalendar();
        SetPage("calendar", false);
        AutoStartItem.IsChecked = Registry.CurrentUser.OpenSubKey(RunKey)?.GetValue("DynamicIsland") != null;
        UpdateCollapsedText();
        InitVolume();
        OnSys(null, EventArgs.Empty);
        await InitMediaAsync();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        _dpi = newDpi.DpiScaleX;
        base.OnDpiChanged(oldDpi, newDpi);
        PlaceWindow();
        InitBackdrop();
    }

    private void ApplyTargets(bool springy, bool kick = false)
    {
        (double w, double h) = _state switch
        {
            IslandState.Toast => (TW, TH),
            IslandState.Expanded => (EW, PageH[_page] + 64),
            _ => (CW, CH)
        };
        _tw = w;
        _th = h;
        bool hide = AutoHideWhenCovered && _covering && _state == IslandState.Collapsed;
        _ty = hide ? -(CH - PeekPx) : 0;
        _zeta = springy ? 0.8 : 0.92;
        if (kick) _vwob += (springy ? 150 : -110) * Jelly;   
        StartSpring();
    }

    private void StartSpring()
    {
        if (_springOn) return;
        _springOn = true;
        _lastTicks = _sw.ElapsedTicks;
        CompositionTarget.Rendering += OnSpring;
    }

    private static void Step(ref double pos, ref double vel, double target, double k, double zeta, double dt)
    {
        double c = 2 * zeta * Math.Sqrt(k);
        double a = -k * (pos - target) - c * vel;
        vel += a * dt;
        pos += vel * dt;
    }

    private void OnSpring(object? sender, EventArgs e)
    {
        long now = _sw.ElapsedTicks;
        double dt = Math.Min((now - _lastTicks) / (double)Stopwatch.Frequency, 0.05);
        _lastTicks = now;

        int steps = Math.Max(1, (int)Math.Ceiling(dt / (1.0 / 240)));
        double sdt = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            Step(ref _w, ref _vw, _tw, 330, _zeta, sdt);
            Step(ref _h, ref _vh, _th, 290, _zeta, sdt);   
            Step(ref _y, ref _vy, _ty, 300, 0.9, sdt);
            Step(ref _wob, ref _vwob, 0, 140, 0.14, sdt);  
        }
        ApplyShape();

        bool settled = Math.Abs(_w - _tw) < 0.1 && Math.Abs(_h - _th) < 0.1 && Math.Abs(_y - _ty) < 0.1
                       && Math.Abs(_vw) < 1 && Math.Abs(_vh) < 1 && Math.Abs(_vy) < 1
                       && Math.Abs(_wob) < 0.05 && Math.Abs(_vwob) < 0.5;
        if (settled)
        {
            _w = _tw; _h = _th; _y = _ty; _vw = _vh = _vy = 0; _wob = _vwob = 0;
            ApplyShape();
            CompositionTarget.Rendering -= OnSpring;  
            _springOn = false;
        }
    }

    private void ApplyShape()
    {
        double w = Math.Max(2, _w), h = Math.Max(2, _h);
        double r = Math.Min(MaxRadius, h / 2);

        double sb = Math.Clamp(_vw * 0.011 * Jelly + _wob * 0.5, -10, 12);
        double bb = Math.Clamp((_vh > 0 ? _vh * 0.014 : _vh * 0.008) * Jelly + _wob, -9, 16);

        double cx = WW / 2;
        var shape = BuildShape(cx, _y, w, h, r, sb, bb);
        Island.Clip = shape;
        Rim.Data = shape;
        Rim2.Data = shape;
        ShadowA.Data = shape; ShadowB.Data = shape; ShadowC.Data = shape;
        Rim2.Clip = shape;                      
        GlassPos.X = cx - w / 2;
        GlassPos.Y = _y;
        GlassLayers.Width = w;
        GlassLayers.Height = h;
        ContentPos.X = cx - w / 2;
        ContentPos.Y = _y;
        ContentHost.Width = w;
        ContentHost.Height = h;
        ExpandedView.Height = Math.Max(0, h - 28);   
    }

    private static Geometry BuildShape(double cx, double top, double w, double h, double r, double sb, double bb)
    {
        double L = cx - w / 2, R = cx + w / 2, T = top, B = top + h;
        double midY = (T + B - r) / 2;
        double third = (w - 2 * r) / 3;

        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            c.BeginFigure(new Point(L, T), true, true);
            c.LineTo(new Point(R, T), true, false);
            c.QuadraticBezierTo(new Point(R + 2 * sb, midY), new Point(R, B - r), true, false);
            c.QuadraticBezierTo(new Point(R, B), new Point(R - r, B), true, false);
            c.BezierTo(new Point(R - r - third, B + bb * 1.33), new Point(L + r + third, B + bb * 1.33),
                       new Point(L + r, B), true, false);
            c.QuadraticBezierTo(new Point(L, B), new Point(L, B - r), true, false);
            c.QuadraticBezierTo(new Point(L - 2 * sb, midY), new Point(L, T), true, false);
        }
        g.Freeze();
        return g;
    }

    private void OnCover(object? sender, EventArgs e)
    {
        if (_hwnd == IntPtr.Zero) return;
        int l = _winX + (int)Math.Round((WW - CW) / 2 * _dpi);
        int r = l + (int)Math.Round(CW * _dpi);
        bool now = Native.ForegroundCovers(_hwnd, l, 0, r, (int)Math.Round(CH * _dpi));
        if (now == _covering) return;
        _covering = now;
        if (_state == IslandState.Collapsed) ApplyTargets(false);
    }


    private static void FadeIn(FrameworkElement el, int delayMs = 0, double fromY = 8)
    {
        el.Visibility = Visibility.Visible;
        var delay = TimeSpan.FromMilliseconds(delayMs);
        el.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(240)) { BeginTime = delay });
        if (el.RenderTransform is TranslateTransform t)
        {
            t.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(fromY, 0, TimeSpan.FromMilliseconds(360))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                BeginTime = delay
            });
        }
    }

    private static void FadeOut(FrameworkElement el, double toY = 0)
    {
        if (el.Visibility != Visibility.Visible) return;
        var a = new DoubleAnimation(0, TimeSpan.FromMilliseconds(140));
        a.Completed += (_, _) =>
        {
            if (el.Opacity >= 0.01) return;
            el.Visibility = Visibility.Collapsed;
            if (el.RenderTransform is TranslateTransform t) { t.BeginAnimation(TranslateTransform.YProperty, null); t.Y = 0; }
        };
        el.BeginAnimation(OpacityProperty, a);
        if (toY != 0 && el.RenderTransform is TranslateTransform tt)
            tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, toY, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            });
    }

    private void SetState(IslandState s)
    {
        if (s == _state) return;
        _state = s;
        ApplyTargets(s != IslandState.Collapsed, true);
        switch (s)
        {
            case IslandState.Collapsed:
                _statTimer.Stop();
                FadeOut(ToastView); FadeOut(ExpandedView); FadeIn(CollapsedView, 120);
                break;
            case IslandState.Toast:
                _statTimer.Stop();
                FadeOut(CollapsedView); FadeOut(ExpandedView); FadeIn(ToastView, 80);
                break;
            case IslandState.Expanded:
                SetPage(_page, false);
                FadeOut(CollapsedView); FadeOut(ToastView); FadeIn(ExpandedView, 80);
                UpdateProgress();
                OnPageShown(_page);
                break;
        }
    }

    private void SetPage(string page, bool animate)
    {
        int oldIdx = Array.IndexOf(PageOrder, _page), newIdx = Array.IndexOf(PageOrder, page);
        int dir = newIdx >= oldIdx ? 1 : -1;
        bool changed = page != _page;
        _page = page;

        foreach (var (name, el, tab) in _pages)
        {
            bool on = name == page;
            tab.Background = on ? new SolidColorBrush(Color.FromArgb(0x44, 255, 255, 255)) : Brushes.Transparent;
            if (animate)
            {
                if (on) FadeIn(el, 0, 14 * dir); else FadeOut(el, -14 * dir);
            }
            else
            {
                el.BeginAnimation(OpacityProperty, null);
                el.Opacity = on ? 1 : 0;
                el.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
                if (el.RenderTransform is TranslateTransform t) { t.BeginAnimation(TranslateTransform.YProperty, null); t.Y = 0; }
            }
        }

        if (animate && changed)
        {
            if (_state == IslandState.Expanded) ApplyTargets(true);  
            OnPageShown(page);
        }
    }

    private void OnPageShown(string page)
    {
        if (page == "stats") { _wifiTick = 0; _statTimer.Start(); OnStatTick(null, EventArgs.Empty); }
        else _statTimer.Stop();
        if (page == "control") _ = RefreshControlAsync();
        if (page == "notif") _ = RefreshNotifAsync();
    }

    private void OpenPage(string page)
    {
        if (_state == IslandState.Expanded) { if (page != _page) SetPage(page, true); }
        else { _page = page; SetState(IslandState.Expanded); }
    }

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        var page = (string)((Button)sender).Tag;
        if (page != _page) SetPage(page, true);
    }

    private DateTime _lastWheel = DateTime.MinValue;

    private void Island_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_state != IslandState.Expanded) return;
        if (WheelBelongsToEventList()) return;
        e.Handled = true;
        if ((DateTime.Now - _lastWheel).TotalMilliseconds < 300) return;
        int idx = Math.Clamp(Array.IndexOf(PageOrder, _page) + (e.Delta < 0 ? 1 : -1), 0, PageOrder.Length - 1);
        if (PageOrder[idx] == _page) return;
        _lastWheel = DateTime.Now;
        SetPage(PageOrder[idx], true);
    }

    private void Island_MouseEnter(object sender, MouseEventArgs e)
    {
        _leaveTimer.Stop(); _peekTimer.Stop();
        if (_state != IslandState.Expanded)
        {
            _page = "calendar";          
            GoToday();
            SetState(IslandState.Expanded);
        }
    }

    private void Island_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_dragging || _typing) return;
        _leaveTimer.Stop(); _leaveTimer.Start();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void ShowToast(string icon, string label, double? bar = null, int ms = 2000)
    {
        if (_state == IslandState.Expanded && (Island.IsMouseOver || _sliding)) return; // jangan ganggu saat dipakai
        ToastIcon.Kind = icon;
        ToastLabel.Text = label;
        ToastTrack.Visibility = bar.HasValue ? Visibility.Visible : Visibility.Collapsed;
        if (bar.HasValue)
        {
            ToastFill.BeginAnimation(WidthProperty,
                new DoubleAnimation(190 * Math.Clamp(bar.Value, 0, 1), TimeSpan.FromMilliseconds(160))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }
        if (_state != IslandState.Toast) SetState(IslandState.Toast);
        _toastTimer.Interval = TimeSpan.FromMilliseconds(ms);
        _toastTimer.Stop(); _toastTimer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        UpdateLyricLine();
        UpdateTimer();
        UpdateCollapsedText();
        CheckNewDay();
        if (_state == IslandState.Expanded && _page == "media") UpdateProgress();
    }

    private void UpdateCollapsedText()
    {
        TimerBadge.Opacity = _tRunning ? 1 : 0;
        DlBadge.Opacity = _dlActive && !_tRunning ? 1 : 0;
        if (_tRunning) CollapsedText.Text = MmSs(_tRemaining);
        else if (_dlActive) CollapsedText.Text = Rate(_dlSpeed);
        else if (_playing && _lyrNow.Length > 0) CollapsedText.Text = _lyrNow;
        else if (_playing && _title.Length > 0) CollapsedText.Text = _title;
        else CollapsedText.Text = DateTime.Now.ToString("HH:mm");
    }

    private static string MmSs(TimeSpan t)
    {
        int secs = (int)Math.Ceiling(Math.Max(0, t.TotalSeconds));
        return $"{secs / 60:00}:{secs % 60:00}";
    }

    private static string Fmt(TimeSpan t) => $"{(int)t.TotalMinutes}:{t.Seconds:00}";

    private GlobalSystemMediaTransportControlsSessionManager? _mgr;
    private GlobalSystemMediaTransportControlsSession? _session;
    private string _lastTrack = "", _title = "";
    private bool _playing, _eqOn;
    private TimeSpan _tlStart, _tlTotal;

    private async Task InitMediaAsync()
    {
        _mgr = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _mgr.CurrentSessionChanged += (_, _) => Dispatcher.Invoke(AttachSession);
        AttachSession();
    }

    private void AttachSession()
    {
        if (_session != null)
        {
            _session.MediaPropertiesChanged -= OnMediaChanged;
            _session.PlaybackInfoChanged -= OnPlaybackChanged;
        }
        _session = _mgr?.GetCurrentSession();
        if (_session != null)
        {
            _session.MediaPropertiesChanged += OnMediaChanged;
            _session.PlaybackInfoChanged += OnPlaybackChanged;
        }
        _ = RefreshMediaAsync();
        RefreshPlayback();
    }

    private void OnMediaChanged(GlobalSystemMediaTransportControlsSession s, object a) => _ = RefreshMediaAsync();
    private void OnPlaybackChanged(GlobalSystemMediaTransportControlsSession s, object a) => RefreshPlayback();

    private async Task RefreshMediaAsync()
    {
        if (_session == null)
        {
            Dispatcher.Invoke(() =>
            {
                _title = ""; TitleText.Text = "Tidak ada media"; ArtistText.Text = "";
                CoverBrush.ImageSource = null; UpdateCollapsedText();
            });
            return;
        }

        var props = await _session.TryGetMediaPropertiesAsync();
        BitmapImage? bmp = null;
        if (props.Thumbnail != null)
        {
            try
            {
                using var ras = await props.Thumbnail.OpenReadAsync();
                using var stream = ras.AsStream();
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
            }
            catch { /* cover tidak tersedia */ }
        }

        Dispatcher.Invoke(() =>
        {
            _title = props.Title ?? "";
            TitleText.Text = _title.Length == 0 ? "Tanpa judul" : _title;
            ArtistText.Text = props.Artist ?? "";
            CoverBrush.ImageSource = bmp;
            UpdateCollapsedText();

            LoadLyrics(props.Artist ?? "", _title, props.AlbumTitle ?? "");
            string key = _title + "|" + props.Artist;
            if (key != _lastTrack && _title.Length > 0)
            {
                _lastTrack = key;
                if (!Island.IsMouseOver)
                {
                    OpenPage("media");        
                    _peekTimer.Stop(); _peekTimer.Start();
                }
            }
        });
    }

    private void RefreshPlayback()
    {
        GlobalSystemMediaTransportControlsSessionPlaybackStatus? status = null;
        try { status = _session?.GetPlaybackInfo()?.PlaybackStatus; } catch { }
        Dispatcher.Invoke(() =>
        {
            _playing = status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            PlayGlyph.Kind = _playing ? "pause" : "play";
            SetEq(_playing);
            UpdateCollapsedText();
        });
    }

    private void SetEq(bool on)
    {
        if (on == _eqOn) return;
        _eqOn = on;
        var bars = new[] { Eq1, Eq2, Eq3, Eq4 };
        int[] ms = { 360, 520, 440, 600 };
        for (int i = 0; i < bars.Length; i++)
        {
            if (on)
            {
                var a = new DoubleAnimation(0.25, 1, TimeSpan.FromMilliseconds(ms[i]))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                Timeline.SetDesiredFrameRate(a, 30);
                bars[i].BeginAnimation(ScaleTransform.ScaleYProperty, a);
            }
            else
            {
                bars[i].BeginAnimation(ScaleTransform.ScaleYProperty, null);
                bars[i].ScaleY = 0.25;
            }
        }
        EqPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(on ? 1 : 0, TimeSpan.FromMilliseconds(250)));
    }

    private void UpdateProgress()
    {
        if (_session == null) { SetProgress(0, TimeSpan.Zero, TimeSpan.Zero); return; }
        try
        {
            var tl = _session.GetTimelineProperties();
            _tlStart = tl.StartTime;
            _tlTotal = tl.EndTime - tl.StartTime;
            if (_tlTotal.TotalSeconds < 1) { SetProgress(0, TimeSpan.Zero, TimeSpan.Zero); return; }

            var pos = tl.Position;
            if (_playing) pos += DateTimeOffset.Now - tl.LastUpdatedTime;
            if (pos < TimeSpan.Zero) pos = TimeSpan.Zero;
            if (pos > _tlTotal) pos = _tlTotal;
            SetProgress(pos.TotalSeconds / _tlTotal.TotalSeconds, pos, _tlTotal);
        }
        catch { SetProgress(0, TimeSpan.Zero, TimeSpan.Zero); }
    }

    private void SetProgress(double fraction, TimeSpan pos, TimeSpan total)
    {
        PosText.Text = Fmt(pos);
        DurText.Text = total > TimeSpan.Zero ? Fmt(total) : "--:--";
        double w = Math.Max(0, ProgTrack.ActualWidth * fraction);
        ProgFill.BeginAnimation(WidthProperty, new DoubleAnimation(w, TimeSpan.FromMilliseconds(250)));
    }

    private async void Prog_Down(object sender, MouseButtonEventArgs e)
    {
        if (_session == null || _tlTotal.TotalSeconds < 1 || ProgTrack.ActualWidth <= 0) return;
        double f = Math.Clamp(e.GetPosition(ProgTrack).X / ProgTrack.ActualWidth, 0, 1);
        var target = _tlStart + TimeSpan.FromSeconds(_tlTotal.TotalSeconds * f);
        await _session.TryChangePlaybackPositionAsync(target.Ticks);
    }

    private async void Play_Click(object s, RoutedEventArgs e) { if (_session != null) await _session.TryTogglePlayPauseAsync(); }
    private async void Next_Click(object s, RoutedEventArgs e) { if (_session != null) await _session.TrySkipNextAsync(); }
    private async void Prev_Click(object s, RoutedEventArgs e) { if (_session != null) await _session.TrySkipPreviousAsync(); }

    private bool _tRunning;
    private TimeSpan _tRemaining;
    private DateTime _tEnd;

    private void UpdateTimer()
    {
        if (_tRunning)
        {
            _tRemaining = _tEnd - DateTime.Now;
            if (_tRemaining <= TimeSpan.Zero)
            {
                _tRunning = false;
                _tRemaining = TimeSpan.Zero;
                System.Media.SystemSounds.Exclamation.Play();
                ShowToast("alarm", "Waktu habis!", null, 4000);
            }
        }
        TimerText.Text = MmSs(_tRemaining);
        TimerGlyph.Kind = _tRunning ? "pause" : "play";
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        int minutes = int.Parse((string)((Button)sender).Tag);
        _tRemaining = TimeSpan.FromMinutes(minutes);
        _tEnd = DateTime.Now + _tRemaining;
        _tRunning = true;
        UpdateTimer();
    }

    private void TimerPause_Click(object sender, RoutedEventArgs e)
    {
        if (_tRunning) _tRunning = false;
        else if (_tRemaining > TimeSpan.Zero) { _tEnd = DateTime.Now + _tRemaining; _tRunning = true; }
        UpdateTimer();
    }

    private void TimerReset_Click(object sender, RoutedEventArgs e)
    {
        _tRunning = false; _tRemaining = TimeSpan.Zero; UpdateTimer();
    }

    private void Island_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        _leaveTimer.Stop(); _peekTimer.Stop();
        OpenPage("files");
    }

    private void Island_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Island_DragLeave(object sender, DragEventArgs e)
    {
        if (_dragging) return;
        _leaveTimer.Stop(); _leaveTimer.Start();
    }

    private void Island_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            foreach (var f in files)
                if (!_shelf.Any(x => x.FullPath == f)) _shelf.Add(new ShelfItem(f));
        }
        e.Handled = true;
    }

    private void FileList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        => _dragStart = e.GetPosition(FileList);

    private void FileList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || FileList.SelectedItem is not ShelfItem item) return;
        var d = e.GetPosition(FileList) - _dragStart;
        if (Math.Abs(d.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(d.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        _dragging = true;
        try
        {
            DragDrop.DoDragDrop(FileList, new DataObject(DataFormats.FileDrop, new[] { item.FullPath }), DragDropEffects.Copy);
        }
        finally
        {
            _dragging = false;
            if (!Island.IsMouseOver) { _leaveTimer.Stop(); _leaveTimer.Start(); }
        }
    }

    private void FileList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FileList.SelectedItem is not ShelfItem item) return;
        try { Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true }); }
        catch { /* file sudah dipindah / dihapus */ }
    }

    private void RemoveShelf_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is ShelfItem item) _shelf.Remove(item);
    }

    private MMDeviceEnumerator? _enum;
    private MMDevice? _dev;
    private float _lastVol = -1;
    private bool _lastMute;

    private void InitVolume()
    {
        try
        {
            _enum = new MMDeviceEnumerator();
            _dev = _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _lastVol = _dev.AudioEndpointVolume.MasterVolumeLevelScalar;
            _lastMute = _dev.AudioEndpointVolume.Mute;
            _dev.AudioEndpointVolume.OnVolumeNotification += OnVolume;
        }
        catch { /* tidak ada perangkat audio */ }
    }

    private void OnVolume(AudioVolumeNotificationData data)
    {
        Dispatcher.BeginInvoke(() =>
        {
            float v = data.MasterVolume;
            if (Math.Abs(v - _lastVol) < 0.001f && data.Muted == _lastMute) return;
            _lastVol = v; _lastMute = data.Muted;
            UpdateVolumeUi(v, data.Muted);

            string icon = data.Muted || v <= 0 ? "volume-0" : v < 0.34 ? "volume-1" : v < 0.67 ? "volume-2" : "volume-3";
            string label = data.Muted ? "Senyap" : $"Volume {Math.Round(v * 100)}%";
            ShowToast(icon, label, data.Muted ? 0 : v, 1800);
        });
    }

    private bool? _ac;
    private bool _lowShown;

    private void OnSys(object? sender, EventArgs e)
    {
        Native.KeepTopmost(_hwnd);
        if (!Native.TryGetPower(out bool ac, out int pct, out bool hasBat)) return;

        BatteryGlyph.Visibility = hasBat ? Visibility.Visible : Visibility.Collapsed;
        if (!hasBat) { BatteryText.Text = ""; return; }
        BatteryGlyph.Kind = ac ? "bolt" : pct <= 20 ? "battery-low" : "battery";
        BatteryText.Text = pct + "%";

        if (_ac.HasValue && _ac.Value != ac)
            ShowToast(ac ? "bolt" : "battery", ac ? $"Mengisi daya · {pct}%" : $"Mode baterai · {pct}%", pct / 100.0, 2500);
        else if (!ac && pct <= 20 && !_lowShown)
        {
            _lowShown = true;
            ShowToast("battery-low", $"Baterai lemah · {pct}%", pct / 100.0, 3500);
        }
        if (ac || pct > 25) _lowShown = false;
        _ac = ac;
    }

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private void AutoStart_Click(object sender, RoutedEventArgs e)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (key == null) return;
        if (AutoStartItem.IsChecked) key.SetValue("DynamicIsland", $"\"{Environment.ProcessPath}\"");
        else key.DeleteValue("DynamicIsland", false);
    }

    private CapsLockWatcher? _caps;

    private void OnCapsChanged(bool on)
    {
        CapsBadge.BeginAnimation(OpacityProperty, new DoubleAnimation(on ? 1 : 0, TimeSpan.FromMilliseconds(200)));
        ShowToast("caps", on ? "Caps Lock aktif" : "Caps Lock mati", null, 1500);
    }

    private readonly SystemStats _stats = new();
    private readonly DispatcherTimer _statTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _statBusy;

    private async void OnStatTick(object? sender, EventArgs e)
    {
        if (_statBusy) return;
        _statBusy = true;
        if (_wifiTick++ % 10 == 0) _ = RefreshWifiAsync();       
        try
        {
            await Task.Run(() => _stats.Sample());   
            UpdateStatsUi();
        }
        catch { }
        finally { _statBusy = false; }
    }

    private void UpdateStatsUi()
    {
        SetBar(CpuFill, CpuTrack, _stats.Cpu / 100);
        CpuText.Text = $"{_stats.Cpu:0}%";

        SetBar(RamFill, RamTrack, _stats.RamPercent / 100);
        RamText.Text = $"{_stats.RamUsedGb:0.0} / {_stats.RamTotalGb:0.#} GB";

        if (_stats.Gpu >= 0) { SetBar(GpuFill, GpuTrack, _stats.Gpu / 100); GpuText.Text = $"{_stats.Gpu:0}%"; }
        else { SetBar(GpuFill, GpuTrack, 0); GpuText.Text = "N/A"; }

        NetDownText.Text = Rate(_stats.NetDown);
        NetUpText.Text = Rate(_stats.NetUp);
    }

    private static string Rate(double bytesPerSec)
        => bytesPerSec >= 1048576 ? $"{bytesPerSec / 1048576:0.0} MB/s" : $"{bytesPerSec / 1024:0} KB/s";

    private static void SetBar(Border fill, FrameworkElement track, double f, bool animate = true, bool warn = true)
    {
        f = Math.Clamp(f, 0, 1);
        if (track.ActualWidth <= 0) track.UpdateLayout();
        double w = track.ActualWidth * f;
        if (animate)
            fill.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(w, TimeSpan.FromMilliseconds(450))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        else { fill.BeginAnimation(FrameworkElement.WidthProperty, null); fill.Width = w; }
        if (warn) fill.Background = f > 0.85 ? new SolidColorBrush(Color.FromRgb(255, 159, 10)) : Brushes.White;
    }

    private bool _sliding;
    private Radio? _wifi, _bt;
    private int _briPending = -1;
    private bool _briBusy;

    private async Task RefreshControlAsync()
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);   // tunggu layout selesai
        try
        {
            if (_dev != null) UpdateVolumeUi(_dev.AudioEndpointVolume.MasterVolumeLevelScalar, _dev.AudioEndpointVolume.Mute);
        }
        catch { }
        UpdateRadioTiles();

        int? b = await Task.Run(Brightness.Get);
        if (b.HasValue) { BriRow.Visibility = Visibility.Visible; SetBar(BriFill, BriTrack, b.Value / 100.0, true, false); }
        else BriRow.Visibility = Visibility.Collapsed;     // PC desktop: tidak ada kecerahan internal
    }

    private void UpdateVolumeUi(float v, bool muted)
    {
        if (!_sliding) SetBar(VolFill, VolTrack, muted ? 0 : v, true, false);
        SetTile(TileMute, true, muted);
    }

    private static void SetTile(Button b, bool available, bool active)
    {
        b.IsEnabled = available;
        b.Opacity = available ? 1 : 0.35;
        b.Background = new SolidColorBrush(active ? Color.FromArgb(0xEE, 255, 255, 255) : Color.FromArgb(0x33, 255, 255, 255));
        b.Foreground = active ? new SolidColorBrush(Color.FromRgb(0x22, 0x26, 0x33)) : Brushes.White;
    }

    private void Slider_Down(object sender, MouseButtonEventArgs e)
    {
        var g = (Grid)sender;
        g.CaptureMouse();
        _sliding = true; _dragging = true;
        DragSlider(g, e.GetPosition(g).X);
        e.Handled = true;
    }

    private void Slider_Move(object sender, MouseEventArgs e)
    {
        var g = (Grid)sender;
        if (g.IsMouseCaptured) DragSlider(g, e.GetPosition(g).X);
    }

    private void Slider_Up(object sender, MouseButtonEventArgs e)
    {
        var g = (Grid)sender;
        g.ReleaseMouseCapture();
        _sliding = false; _dragging = false;
        if (!Island.IsMouseOver) { _leaveTimer.Stop(); _leaveTimer.Start(); }
    }

    private void DragSlider(Grid g, double x)
    {
        double f = Math.Clamp(x / Math.Max(1, g.ActualWidth), 0, 1);
        if ((string)g.Tag == "vol")
        {
            try
            {
                if (_dev != null)
                {
                    _dev.AudioEndpointVolume.MasterVolumeLevelScalar = (float)f;
                    if (f > 0 && _dev.AudioEndpointVolume.Mute) _dev.AudioEndpointVolume.Mute = false;
                }
            }
            catch { }
            SetBar(VolFill, VolTrack, f, false, false);
        }
        else
        {
            SetBar(BriFill, BriTrack, f, false, false);
            QueueBrightness((int)Math.Round(f * 100));
        }
    }

    // Kecerahan lewat WMI agak lambat: hanya nilai terbaru yang dikirim
    private async void QueueBrightness(int level)
    {
        _briPending = level;
        if (_briBusy) return;
        _briBusy = true;
        try
        {
            while (_briPending >= 0)
            {
                int l = _briPending;
                _briPending = -1;
                await Task.Run(() => Brightness.Set(l));
            }
        }
        finally { _briBusy = false; }
    }

    private async Task InitRadiosAsync()
    {
        try
        {
            if (await Radio.RequestAccessAsync() != RadioAccessStatus.Allowed) return;
            foreach (var r in await Radio.GetRadiosAsync())
            {
                if (r.Kind == RadioKind.WiFi && _wifi == null) { _wifi = r; r.StateChanged += (_, _) => Dispatcher.BeginInvoke(UpdateRadioTiles); }
                else if (r.Kind == RadioKind.Bluetooth && _bt == null) { _bt = r; r.StateChanged += (_, _) => Dispatcher.BeginInvoke(UpdateRadioTiles); }
            }
            UpdateRadioTiles();
        }
        catch { }
    }

    private void UpdateRadioTiles()
    {
        SetTile(TileWifi, _wifi != null, _wifi?.State == RadioState.On);
        SetTile(TileBt, _bt != null, _bt?.State == RadioState.On);
    }

    private static async Task ToggleRadio(Radio? r)
    {
        if (r == null) return;
        try { await r.SetStateAsync(r.State == RadioState.On ? RadioState.Off : RadioState.On); } catch { }
    }

    private async void Tile_Click(object sender, RoutedEventArgs e)
    {
        switch ((string)((Button)sender).Tag)
        {
            case "wifi": await ToggleRadio(_wifi); break;
            case "bt": await ToggleRadio(_bt); break;
            case "mute":
                try { if (_dev != null) _dev.AudioEndpointVolume.Mute = !_dev.AudioEndpointVolume.Mute; } catch { }
                break;
            case "shot":
                SetState(IslandState.Collapsed);
                Native.PressKeys(0x5B, 0x10, 0x53);    
                break;
            case "lock":
                Native.Lock();
                break;
            case "settings":
                SetState(IslandState.Collapsed);
                try { Process.Start(new ProcessStartInfo("ms-settings:") { UseShellExecute = true }); } catch { }
                break;
        }
    }

    private readonly ObservableCollection<NotifItem> _notifs = new();
    private UserNotificationListener? _listener;
    private bool _notifAllowed;

    private void ShowNotifHint(string text)
    {
        NotifHint.Text = text;
        NotifHint.Visibility = Visibility.Visible;
        NotifList.Visibility = Visibility.Collapsed;
    }

    private async Task RefreshNotifAsync()
    {
        const string denied = "Windows tidak mengizinkan program ini membaca notifikasi.\nGunakan tombol di bawah untuk membuka Pusat Notifikasi.";
        try
        {
            _listener ??= UserNotificationListener.Current;
            if (!_notifAllowed)
                _notifAllowed = await _listener.RequestAccessAsync() == UserNotificationListenerAccessStatus.Allowed;
            if (!_notifAllowed) { ShowNotifHint(denied); return; }

            var list = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            var items = new List<NotifItem>();
            foreach (var n in list.OrderByDescending(x => x.CreationTime).Take(8))
            {
                var binding = n.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                var texts = binding?.GetTextElements().Select(t => t.Text).ToList() ?? new List<string>();
                items.Add(new NotifItem(n.AppInfo.DisplayInfo.DisplayName,
                                        texts.ElementAtOrDefault(0) ?? "", string.Join(" ", texts.Skip(1))));
            }

            _notifs.Clear();
            foreach (var i in items) _notifs.Add(i);
            if (items.Count == 0) ShowNotifHint("Tidak ada notifikasi");
            else { NotifHint.Visibility = Visibility.Collapsed; NotifList.Visibility = Visibility.Visible; }
        }
        catch { ShowNotifHint(denied); }
    }

    private void OpenNotifCenter_Click(object sender, RoutedEventArgs e)
    {
        SetState(IslandState.Collapsed);
        Native.PressKeys(0x5B, Environment.OSVersion.Version.Build >= 22000 ? (byte)0x4E : (byte)0x41);
    }
}
