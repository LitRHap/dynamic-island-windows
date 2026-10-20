using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DynamicIsland;


internal sealed class CapsLockWatcher : IDisposable
{
    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc fn, IntPtr hMod, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll")] private static extern short GetKeyState(int vk);

    private readonly HookProc _proc;      
    private readonly IntPtr _hook;
    private bool _down;

    public bool IsOn { get; private set; }
    public event Action<bool>? Changed;

    public CapsLockWatcher()
    {
        IsOn = (GetKeyState(0x14) & 1) != 0;
        _proc = Callback;
        using var p = Process.GetCurrentProcess();
        _hook = SetWindowsHookEx(13, _proc, GetModuleHandle(p.MainModule?.ModuleName), 0);
    }

    private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && Marshal.ReadInt32(lParam) == 0x14)   
        {
            int msg = wParam.ToInt32();
            if (msg == 0x100 || msg == 0x104)                  
            {
                if (!_down) { _down = true; IsOn = !IsOn; Changed?.Invoke(IsOn); }
            }
            else if (msg == 0x101 || msg == 0x105) _down = false;
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero) UnhookWindowsHookEx(_hook);
    }
}
