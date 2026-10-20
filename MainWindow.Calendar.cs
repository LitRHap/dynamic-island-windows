using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DynamicIsland;

public sealed class CalEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Date { get; set; } = "";      
    public string? Time { get; set; }           
    public string Title { get; set; } = "";
}

public sealed class EventRow
{
    public CalEvent? Ev { get; init; }         
    public string TimeText { get; init; } = "";
    public string Title { get; init; } = "";
    public Brush TitleBrush { get; init; } = Brushes.White;
    public Visibility DelVisibility => Ev == null ? Visibility.Collapsed : Visibility.Visible;
}

public partial class MainWindow
{
  
    private static readonly CultureInfo Cult = new("id-ID");

  
    private static readonly (int M, int D, string Name)[] FixedHolidays =
    {
        (1, 1, "Tahun Baru Masehi"), (5, 1, "Hari Buruh"), (6, 1, "Hari Lahir Pancasila"),
        (8, 17, "HUT Kemerdekaan RI"), (12, 25, "Hari Natal"),
    };

    private static readonly string EvFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DynamicIsland", "events.json");

    private static SolidColorBrush Br(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        b.Freeze();
        return b;
    }

    private static readonly SolidColorBrush BrWhite = Br("#FFFFFFFF"), BrDim = Br("#66FFFFFF"), BrSoft = Br("#99FFFFFF"),
        BrRed = Br("#FFFF9A90"), BrSel = Br("#44FFFFFF"), BrTodayBg = Br("#F2FFFFFF"),
        BrDark = Br("#FF222633"), BrDot = Br("#FF8CCBFF");

    private readonly List<CalEvent> _events = new();
    private DateTime _today = DateTime.Today;
    private DateTime _selDate = DateTime.Today;
    private DateTime _calMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _typing;                  
    private IntPtr _prevForeground;

    private static string DayKey(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? HolidayName(DateTime d)
    {
        foreach (var h in FixedHolidays)
            if (h.M == d.Month && h.D == d.Day) return h.Name;
        return null;
    }

    private void InitCalendar()
    {
        LoadEvents();
        string[] dow = { "Sn", "Sl", "Rb", "Km", "Jm", "Sb", "Mg" };
        for (int i = 0; i < dow.Length; i++)
            CalDow.Children.Add(new TextBlock
            {
                Text = dow[i], FontSize = 9.5,
                Foreground = i == 6 ? BrRed : BrSoft,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            });
        GoToday();
    }

    
    private void GoToday()
    {
        _today = DateTime.Today;
        _selDate = _today;
        _calMonth = new DateTime(_today.Year, _today.Month, 1);
        BuildCalendar();
        RefreshEvents();
    }

    
    private void CheckNewDay()
    {
        if (DateTime.Today == _today) return;
        bool followToday = _selDate == _today;
        _today = DateTime.Today;
        if (followToday) { _selDate = _today; _calMonth = new DateTime(_today.Year, _today.Month, 1); }
        BuildCalendar();
        RefreshEvents();
    }

    private void BuildCalendar()
    {
        CalTitle.Text = _calMonth.ToString("MMMM yyyy", Cult);
        CalGrid.Children.Clear();

        int offset = ((int)_calMonth.DayOfWeek + 6) % 7;       
        var start = _calMonth.AddDays(-offset);
        var withEvents = new HashSet<string>(_events.Select(e => e.Date));

        for (int i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            bool inMonth = date.Month == _calMonth.Month;
            bool isToday = date == _today, isSel = date == _selDate;
            bool red = date.DayOfWeek == DayOfWeek.Sunday || HolidayName(date) != null;

            var tb = new TextBlock
            {
                Text = date.Day.ToString(CultureInfo.InvariantCulture), FontSize = 11,
                FontWeight = isToday ? FontWeights.SemiBold : FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -1, 0, 0),
                Foreground = isToday ? BrDark : !inMonth ? BrDim : red ? BrRed : BrWhite
            };
            var cell = new Grid();
            cell.Children.Add(tb);
            if (withEvents.Contains(DayKey(date)))
                cell.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = 3, Height = 3, Fill = isToday ? BrDark : BrDot,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 1.5), IsHitTestVisible = false
                });

            var bd = new Border
            {
                CornerRadius = new CornerRadius(9), Margin = new Thickness(1), Child = cell, Tag = date,
                Cursor = Cursors.Hand,
                Background = isToday ? BrTodayBg : isSel ? BrSel : Brushes.Transparent
            };
            bd.MouseLeftButtonDown += Cell_Down;
            CalGrid.Children.Add(bd);
        }
    }

    private void RefreshEvents()
    {
        var rows = new List<EventRow>();
        var hol = HolidayName(_selDate);
        if (hol != null) rows.Add(new EventRow { TimeText = "Libur", Title = hol, TitleBrush = BrRed });

        foreach (var ev in _events.Where(x => x.Date == DayKey(_selDate)).OrderBy(x => x.Time ?? "").ThenBy(x => x.Title))
            rows.Add(new EventRow { Ev = ev, TimeText = ev.Time ?? "Seharian", Title = ev.Title });

        EvList.ItemsSource = rows;
        EvEmpty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EvDateText.Text = _selDate == _today
            ? "Hari ini · " + _selDate.ToString("d MMM", Cult)
            : _selDate.ToString("ddd, d MMM", Cult);
    }

    
    private void Cell_Down(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (((Border)sender).Tag is not DateTime d) return;
        CancelAdd();
        _selDate = d;
        if (d.Month != _calMonth.Month || d.Year != _calMonth.Year) _calMonth = new DateTime(d.Year, d.Month, 1);
        BuildCalendar();
        RefreshEvents();
    }

    private void CalPrev_Click(object sender, RoutedEventArgs e) { _calMonth = _calMonth.AddMonths(-1); BuildCalendar(); }
    private void CalNext_Click(object sender, RoutedEventArgs e) { _calMonth = _calMonth.AddMonths(1); BuildCalendar(); }
    private void CalTitle_Click(object sender, MouseButtonEventArgs e) { CancelAdd(); GoToday(); }

   
    private void AddEv_Click(object sender, RoutedEventArgs e)
    {
        if (_typing) { CancelAdd(); return; }
        _typing = true;
        _leaveTimer.Stop();
        _prevForeground = Native.Foreground();
        EvInput.Text = "";
        EvHint.Visibility = Visibility.Visible;
        EvAddBox.Visibility = Visibility.Visible;
        Native.SetNoActivate(_hwnd, false);
        Activate();
        EvInput.Focus();
        Keyboard.Focus(EvInput);
    }

    private void EvInput_TextChanged(object sender, TextChangedEventArgs e)
        => EvHint.Visibility = EvInput.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void EvInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; CommitAdd(); }
        else if (e.Key == Key.Escape) { e.Handled = true; CancelAdd(); }
    }

    private void EvInput_LostFocus(object sender, KeyboardFocusChangedEventArgs e) => CancelAdd();

    private static readonly Regex TimeRx = new(@"^\s*(\d{1,2})[:.](\d{2})\s+(.+)$", RegexOptions.Compiled);

    private void CommitAdd()
    {
        string text = EvInput.Text.Trim();
        if (text.Length > 0)
        {
            string? time = null;
            string title = text;
            var m = TimeRx.Match(text);
            if (m.Success)
            {
                int hh = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int mm = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (hh <= 23 && mm <= 59)
                {
                    time = $"{hh:00}:{mm:00}";
                    title = m.Groups[3].Value.Trim();
                }
            }
            _events.Add(new CalEvent { Date = DayKey(_selDate), Time = time, Title = title });
            SaveEvents();
        }
        CancelAdd();
        BuildCalendar();
        RefreshEvents();
    }

    private void CancelAdd()
    {
        if (!_typing) return;
        _typing = false;
        EvAddBox.Visibility = Visibility.Collapsed;
        Native.SetNoActivate(_hwnd, true);

        // kembalikan fokus ke aplikasi sebelumnya (kecuali pengguna sudah klik ke jendela lain)
        if (_prevForeground != IntPtr.Zero && Native.IsForeground(_hwnd)) Native.SetForeground(_prevForeground);
        _prevForeground = IntPtr.Zero;

        if (!Island.IsMouseOver) { _leaveTimer.Stop(); _leaveTimer.Start(); }
    }

    private void EvDelete_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is EventRow { Ev: { } ev })
        {
            _events.Remove(ev);
            SaveEvents();
            BuildCalendar();
            RefreshEvents();
        }
    }

   
    private void LoadEvents()
    {
        try
        {
            if (File.Exists(EvFile))
                _events.AddRange(JsonSerializer.Deserialize<List<CalEvent>>(File.ReadAllText(EvFile)) ?? new());
        }
        catch { /* file rusak -> mulai kosong */ }
    }

    private void SaveEvents()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(EvFile)!);
            File.WriteAllText(EvFile, JsonSerializer.Serialize(_events));
        }
        catch { }
    }

   
    private static ScrollViewer? FindScroll(DependencyObject root)
    {
        if (root is ScrollViewer sv) return sv;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            if (FindScroll(VisualTreeHelper.GetChild(root, i)) is { } f) return f;
        return null;
    }

    private bool WheelBelongsToEventList()
        => _page == "calendar" && EvList.IsMouseOver && FindScroll(EvList) is { ScrollableHeight: > 0 };
}
