using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class ForceWindowed
{
    public static void SetClientSize(string processNameOrPid, int clientWidth, int clientHeight)
    {
        IntPtr hWnd = GetHandle(processNameOrPid);
        if (hWnd == IntPtr.Zero) throw new InvalidOperationException("Window not found");

        // make it a standard overlapped window
        int style = GetWindowLong(hWnd, GWL_STYLE);
        style &= ~WS_POPUP;
        style |= WS_OVERLAPPEDWINDOW | WS_VISIBLE;
        SetWindowLong(hWnd, GWL_STYLE, style);

        int ex = GetWindowLong(hWnd, GWL_EXSTYLE);
        SetWindowLong(hWnd, GWL_EXSTYLE, ex); // keep exstyle as is

        // compute the outer rect needed to achieve desired client size
        RECT outer = CalcOuterRectForClient(hWnd, clientWidth, clientHeight);

        // center on its current monitor
        MONITORINFO mi = GetMonitorInfoForWindow(hWnd);
        int monW = mi.rcWork.Right - mi.rcWork.Left;
        int monH = mi.rcWork.Bottom - mi.rcWork.Top;
        int x = mi.rcWork.Left + (monW - (outer.Right - outer.Left)) / 2;
        int y = mi.rcWork.Top + (monH - (outer.Bottom - outer.Top)) / 2;

        SetWindowPos(hWnd, HWND_TOP, x, y, outer.Right - outer.Left, outer.Bottom - outer.Top,
            SWP_NOZORDER | SWP_FRAMECHANGED | SWP_NOOWNERZORDER);
        ShowWindow(hWnd, SW_SHOWNORMAL);
    }

    // overload if you already have a PID or HWND
    public static void SetClientSize(int pid, int clientWidth, int clientHeight) =>
        SetClientSize(pid.ToString(), clientWidth, clientHeight);

    static RECT CalcOuterRectForClient(IntPtr hWnd, int clientW, int clientH)
    {
        RECT r = new RECT { Left = 0, Top = 0, Right = clientW, Bottom = clientH };

        // prefer AdjustWindowRectExForDpi on Win10 1607+
        uint exStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
        int style = GetWindowLong(hWnd, GWL_STYLE);

        if (AdjustWindowRectExForDpiPtr != IntPtr.Zero)
        {
            uint dpi = GetDpiForWindowSafe(hWnd);
            if (!AdjustWindowRectExForDpi(ref r, style, false, exStyle, dpi))
                throw new System.ComponentModel.Win32Exception();
        }
        else
        {
            if (!AdjustWindowRectEx(ref r, style, false, exStyle))
                throw new System.ComponentModel.Win32Exception();
        }
        return r;
    }

    static MONITORINFO GetMonitorInfoForWindow(IntPtr hWnd)
    {
        IntPtr mon = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
        MONITORINFO mi = new MONITORINFO();
        mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        if (!GetMonitorInfo(mon, ref mi)) throw new System.ComponentModel.Win32Exception();
        return mi;
    }

    static IntPtr GetHandle(string processNameOrPid)
    {
        if (int.TryParse(processNameOrPid, out int pid))
        {
            var p = Process.GetProcessById(pid);
            return p.MainWindowHandle;
        }
        else
        {
            var procs = Process.GetProcessesByName(processNameOrPid);
            foreach (var p in procs)
                if (p.MainWindowHandle != IntPtr.Zero) return p.MainWindowHandle;
            return IntPtr.Zero;
        }
    }

    // PInvoke

    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;

    const int WS_VISIBLE = 0x10000000;
    const int WS_POPUP = unchecked((int)0x80000000);
    const int WS_OVERLAPPEDWINDOW = 0x00CF0000;

    const uint SWP_NOZORDER = 0x0004;
    const uint SWP_NOOWNERZORDER = 0x0200;
    const uint SWP_FRAMECHANGED = 0x0020;
    const int SW_SHOWNORMAL = 1;

    static readonly IntPtr HWND_TOP = IntPtr.Zero;

    const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool AdjustWindowRectEx(ref RECT lpRect, int dwStyle, bool bMenu, uint dwExStyle);

    // Win10 1607+
    [DllImport("user32.dll", EntryPoint = "AdjustWindowRectExForDpi", SetLastError = true)]
    static extern bool AdjustWindowRectExForDpi(ref RECT lpRect, int dwStyle, bool bMenu, uint dwExStyle, uint dpi);

    // runtime resolve presence of AdjustWindowRectExForDpi
    static readonly IntPtr AdjustWindowRectExForDpiPtr = GetProcAddress(GetModuleHandle("user32.dll"), "AdjustWindowRectExForDpi");

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr GetModuleHandle(string lpModuleName);

    // DPI helpers
    static uint GetDpiForWindowSafe(IntPtr hWnd)
    {
        if (GetDpiForWindowPtr != IntPtr.Zero) return GetDpiForWindow(hWnd);
        // fallback to system DPI 96 if we cannot query
        return 96;
    }

    [DllImport("user32.dll", EntryPoint = "GetDpiForWindow")]
    static extern uint GetDpiForWindow(IntPtr hWnd);

    static readonly IntPtr GetDpiForWindowPtr = GetProcAddress(GetModuleHandle("user32.dll"), "GetDpiForWindow");

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
}