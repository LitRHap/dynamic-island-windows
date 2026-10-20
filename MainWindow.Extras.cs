using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Windows.Networking.Connectivity;

namespace DynamicIsland;

public partial class MainWindow
{
    private static string FmtBytes(double b)
        => b >= 1073741824 ? $"{b / 1073741824:0.0} GB" : b >= 1048576 ? $"{b / 1048576:0.0} MB" : $"{b / 1024:0} KB";

    
    private sealed record NetInfo(string Name, bool IsWifi, bool Online, ulong? Bytes);

    private int _wifiTick;
    private bool _wifiBusy;

    private async Task RefreshWifiAsync()
    {
        if (_wifiBusy) return;
        _wifiBusy = true;
        try
        {
            var info = await Task.Run(ReadNetInfoAsync);
            NetKindGlyph.Kind = info.Online && info.IsWifi ? "wifi" : "globe";
            WifiName.Text = info.Online ? info.Name : "Tidak terhubung";
            WifiUsage.Text = info.Bytes.HasValue ? $"{FmtBytes(info.Bytes.Value)} · 30 hari" : "";
        }
        catch { WifiName.Text = "--"; }
        finally { _wifiBusy = false; }
    }

    private static async Task<NetInfo> ReadNetInfoAsync()
    {
        var p = NetworkInformation.GetInternetConnectionProfile();
        if (p == null) return new NetInfo("", false, false, null);

        bool wifi = p.IsWlanConnectionProfile;
        string name = p.ProfileName ?? "";
        if (wifi)
        {
            try
            {
                var ssid = p.WlanConnectionProfileDetails?.GetConnectedSsid();
                if (!string.IsNullOrWhiteSpace(ssid)) name = ssid;
            }
            catch { }
        }

        ulong? bytes = null;
        try
        {
            var usage = await p.GetNetworkUsageAsync(
                DateTimeOffset.Now.AddDays(-30), DateTimeOffset.Now, DataUsageGranularity.Total,
                new NetworkUsageStates { Roaming = TriStates.DoNotCare, Shared = TriStates.DoNotCare });
            ulong sum = 0;
            foreach (var u in usage) sum += u.BytesSent + u.BytesReceived;
            bytes = sum;
        }
        catch { }
        return new NetInfo(name, wifi, true, bytes);
    }

   
    private readonly DispatcherTimer _btTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private readonly Dictionary<string, int?> _btSeen = new();
    private List<BtDev> _btDevs = new();
    private bool _btBusy, _btFirst = true;

    private void StartBluetoothWatch()
    {
        _btTimer.Tick += async (_, _) => await PollBluetoothAsync();
        _btTimer.Start();
        _ = PollBluetoothAsync();
    }

    private async Task PollBluetoothAsync()
    {
        if (_btBusy) return;
        _btBusy = true;
        try
        {
            var devs = await BtBattery.GetConnectedAsync();
            foreach (var gone in _btSeen.Keys.Where(k => devs.All(d => d.Name != k)).ToList()) _btSeen.Remove(gone);

            foreach (var d in devs)
            {
                bool isNew = !_btSeen.ContainsKey(d.Name);
                int? old = isNew ? null : _btSeen[d.Name];
                _btSeen[d.Name] = d.Percent;
                if (_btFirst) continue;                  // yang sudah terhubung saat aplikasi mulai: tanpa toast

                if (isNew && d.Percent is int p)
                    ShowToast("headphones", $"{d.Name} · {p}%", p / 100.0, 3500);
                else if (isNew)
                    ShowToast("bluetooth", $"{d.Name} terhubung", null, 2500);
                else if (old == null && d.Percent is int p2)          // baterai baru dilaporkan beberapa detik setelah terhubung
                    ShowToast("headphones", $"{d.Name} · {p2}%", p2 / 100.0, 3000);
                else if (d.Percent is int lo && lo <= 20 && old is > 20)
                    ShowToast("battery-low", $"Baterai {d.Name} lemah · {lo}%", lo / 100.0, 3500);
            }
            _btFirst = false;
            _btDevs = devs;
            UpdateLeftSlot();
        }
        catch { }
        finally { _btBusy = false; }
    }


    private DownloadWatcher? _dl;
    private bool _dlActive;
    private double _dlSpeed;
    private long _dlBytes;

    private void StartDownloadWatch()
    {
        try
        {
            _dl = new DownloadWatcher();
            _dl.Updated += () => Dispatcher.BeginInvoke(OnDownloadUpdated);
            _dl.Completed += name => Dispatcher.BeginInvoke(() => ShowToast("check", "Selesai · " + name, 1.0, 3500));
        }
        catch { }
    }

    private void OnDownloadUpdated()
    {
        if (_dl == null) return;
        bool was = _dlActive;
        _dlActive = _dl.Active;
        _dlSpeed = _dl.Speed;
        _dlBytes = _dl.Bytes;

        if (_dlActive && !was)
        {
            ShowToast("download", "Mengunduh…", null, 2200);
            SetDownloadBar(true);
        }
        else if (!_dlActive && was) SetDownloadBar(false);

        UpdateCollapsedText();
        UpdateLeftSlot();
    }

   
    private void SetDownloadBar(bool on)
    {
        DlTrack.BeginAnimation(OpacityProperty, new DoubleAnimation(on ? 1 : 0, TimeSpan.FromMilliseconds(250)));
        if (on)
        {
            var a = new DoubleAnimation(-40, 110, TimeSpan.FromMilliseconds(1100))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Timeline.SetDesiredFrameRate(a, 30);
            DlSweepX.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, a);
        }
        else
        {
            DlSweepX.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, null);
            DlSweepX.X = -40;
        }
    }

    
    private void UpdateLeftSlot()
    {
        if (_dlActive)
        {
            LeftGlyph.Kind = "download";
            LeftText.Text = Rate(_dlSpeed);
            LeftSlot.ToolTip = "Mengunduh · " + FmtBytes(_dlBytes);
            LeftSlot.Visibility = Visibility.Visible;
            return;
        }
        var d = _btDevs.FirstOrDefault(x => x.Percent.HasValue) ?? _btDevs.FirstOrDefault();
        if (d == null) { LeftSlot.Visibility = Visibility.Collapsed; return; }

        LeftGlyph.Kind = "headphones";
        LeftText.Text = d.Percent.HasValue ? $"{d.Percent}%" : "";
        LeftSlot.ToolTip = d.Percent.HasValue ? $"{d.Name} · {d.Percent}%" : d.Name;
        LeftSlot.Visibility = Visibility.Visible;
    }
}
