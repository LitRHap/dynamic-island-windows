using System.Runtime.InteropServices;

namespace DynamicIsland;

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy { public int AccentState; public int AccentFlags; public uint GradientColor; public int AnimationId; }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinCompAttrData { public int Attribute; public IntPtr Data; public int SizeOfData; }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus; public byte BatteryFlag; public byte BatteryLifePercent; public byte SystemStatusFlag;
        public int BatteryLifeTime; public int BatteryFullLifeTime;
    }

    [DllImport("user32.dll")] private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WinCompAttrData data);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr hwnd, IntPtr rgn, bool redraw);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);
    [DllImport("kernel32.dll")] private static extern bool GetSystemPowerStatus(out SystemPowerStatus s);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] private static extern bool LockWorkStation();
    [DllImport("user32.dll")] private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder sb, int max);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    public static void EnableGlass(IntPtr hwnd, uint abgr, bool acrylic)
    {
        int state = acrylic && Environment.OSVersion.Version.Build >= 17134 ? 4 : 3;
        var accent = new AccentPolicy { AccentState = state, AccentFlags = 0, GradientColor = abgr };
        int size = Marshal.SizeOf(accent);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new WinCompAttrData { Attribute = 19, Data = ptr, SizeOfData = size };
            SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }

    public static void MakeToolNoActivate(IntPtr hwnd)
        => SetWindowLong(hwnd, -20, GetWindowLong(hwnd, -20) | 0x08000000 | 0x00000080);

    public static void SetRegion(IntPtr hwnd, int x, int y, int w, int h, int ellipseW, int ellipseH)
    {
        var rgn = CreateRoundRectRgn(x, y, x + w + 1, y + h + 1, ellipseW, ellipseH);
        SetWindowRgn(hwnd, rgn, false); 
    }

    public static int ScreenWidthPx() => GetSystemMetrics(0);

    public static void PressKeys(params byte[] vks)
    {
        foreach (var k in vks) keybd_event(k, 0, 0, UIntPtr.Zero);
        for (int i = vks.Length - 1; i >= 0; i--) keybd_event(vks[i], 0, 2, UIntPtr.Zero);
    }

    public static void Lock() => LockWorkStation();

    public static void ExcludeFromCapture(IntPtr hwnd) => SetWindowDisplayAffinity(hwnd, 0x11);

    public static void MoveResize(IntPtr hwnd, int x, int y, int w, int h)
        => SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h, 0x0004 | 0x0010); 

    public static bool ForegroundCovers(IntPtr self, int l, int t, int r, int b)
    {
        var fg = GetForegroundWindow();
        if (fg == IntPtr.Zero || fg == self || !IsWindowVisible(fg) || IsIconic(fg)) return false;

        var sb = new System.Text.StringBuilder(64);
        GetClassName(fg, sb, sb.Capacity);
        string cls = sb.ToString();
        if (cls is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return false;

        if (!GetWindowRect(fg, out var rc)) return false;
        return rc.Left < r && rc.Right > l && rc.Top < b && rc.Bottom > t;
    }

    public static void SetNoActivate(IntPtr hwnd, bool on)
    {
        int ex = GetWindowLong(hwnd, -20);
        ex = on ? ex | 0x08000000 : ex & ~0x08000000;
        SetWindowLong(hwnd, -20, ex);
    }

    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    public static IntPtr Foreground() => GetForegroundWindow();
    public static bool IsForeground(IntPtr hwnd) => GetForegroundWindow() == hwnd;
    public static void SetForeground(IntPtr hwnd) => SetForegroundWindow(hwnd);

    public static void KeepTopmost(IntPtr hwnd)
        => SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);

    public static bool TryGetPower(out bool ac, out int percent, out bool hasBattery)
    {
        ac = false; percent = 0; hasBattery = false;
        if (!GetSystemPowerStatus(out var s)) return false;
        ac = s.ACLineStatus == 1;
        hasBattery = (s.BatteryFlag & 128) == 0 && s.BatteryLifePercent <= 100;
        percent = s.BatteryLifePercent <= 100 ? s.BatteryLifePercent : 0;
        return true;
    }
}
