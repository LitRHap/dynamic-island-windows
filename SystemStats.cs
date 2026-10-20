using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace DynamicIsland;

internal sealed class SystemStats
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime { public uint Low, High; public ulong Value => ((ulong)High << 32) | Low; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemStatus
    {
        public uint Length, Load;
        public ulong TotalPhys, AvailPhys, TotalPageFile, AvailPageFile, TotalVirtual, AvailVirtual, AvailExtVirtual;
    }

    [DllImport("kernel32.dll")] private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
    [DllImport("kernel32.dll")] private static extern bool GlobalMemoryStatusEx(ref MemStatus m);

    public double Cpu, RamPercent, RamUsedGb, RamTotalGb, NetDown, NetUp;
    public double Gpu = -1;               // -1 = tidak tersedia

    private ulong _pIdle, _pTotal;
    private bool _hasCpu;
    private long _pRx, _pTx;
    private DateTime _pNet;
    private PerformanceCounterCategory? _gpuCat;
    private readonly Dictionary<string, CounterSample> _prevGpu = new();
    private bool _gpuFailed;

    public void Sample()
    {
        SampleCpu();
        SampleRam();
        SampleNet();
        SampleGpu();
    }

    private void SampleCpu()
    {
        if (!GetSystemTimes(out var i, out var k, out var u)) return;
        ulong idle = i.Value, total = k.Value + u.Value;  
        if (_hasCpu)
        {
            ulong dT = total - _pTotal, dI = idle - _pIdle;
            if (dT > 0) Cpu = Math.Clamp(100.0 * (dT - dI) / dT, 0, 100);
        }
        _pIdle = idle; _pTotal = total; _hasCpu = true;
    }

    private void SampleRam()
    {
        var m = new MemStatus { Length = (uint)Marshal.SizeOf<MemStatus>() };
        if (!GlobalMemoryStatusEx(ref m)) return;
        RamTotalGb = m.TotalPhys / 1073741824.0;
        RamUsedGb = (m.TotalPhys - m.AvailPhys) / 1073741824.0;
        RamPercent = m.Load;
    }

    private void SampleNet()
    {
        long rx = 0, tx = 0;
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
            try
            {
                var st = ni.GetIPv4Statistics();
                rx += st.BytesReceived; tx += st.BytesSent;
            }
            catch { }
        }
        var now = DateTime.UtcNow;
        if (_pNet != default)
        {
            double dt = (now - _pNet).TotalSeconds;
            if (dt > 0.2)
            {
                NetDown = Math.Max(0, (rx - _pRx) / dt);
                NetUp = Math.Max(0, (tx - _pTx) / dt);
            }
        }
        _pRx = rx; _pTx = tx; _pNet = now;
    }

    private void SampleGpu()
    {
        if (_gpuFailed) return;
        try
        {
            _gpuCat ??= new PerformanceCounterCategory("GPU Engine");
            var data = _gpuCat.ReadCategory();
            if (!data.Contains("Utilization Percentage")) { _gpuFailed = true; return; }

            var sums = new Dictionary<string, double>();
            var seen = new HashSet<string>();
            bool any = false;
            foreach (InstanceData inst in data["Utilization Percentage"].Values)
            {
                string name = inst.InstanceName;
                seen.Add(name);
                var cur = inst.Sample;
                if (_prevGpu.TryGetValue(name, out var prev))
                {
                    double v = CounterSample.Calculate(prev, cur);
                    any = true;
                    int ix = name.LastIndexOf("engtype_", StringComparison.Ordinal);
                    string type = ix >= 0 ? name[(ix + 8)..] : "?";
                    sums[type] = sums.GetValueOrDefault(type) + v;
                }
                _prevGpu[name] = cur;
            }
            foreach (var key in _prevGpu.Keys.Where(k => !seen.Contains(k)).ToList()) _prevGpu.Remove(key);
            if (any) Gpu = Math.Clamp(sums.Count > 0 ? sums.Values.Max() : 0, 0, 100);
        }
        catch
        {
            _gpuFailed = true;
            Gpu = -1;
        }
    }
}
