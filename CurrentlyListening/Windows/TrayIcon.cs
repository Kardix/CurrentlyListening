using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CurrentlyListening.Windows;

/// <summary>
/// Notification-area icon built directly on Shell_NotifyIcon, so the app doesn't need WinForms.
/// Create, use and dispose it on the UI thread.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private const int WM_APP = 0x8000;
    private const int CallbackMessage = WM_APP + 1;
    private const int WM_CONTEXTMENU = 0x007B;
    private const int WM_LBUTTONDBLCLK = 0x0203;

    private const uint NIM_ADD = 0x0;
    private const uint NIM_MODIFY = 0x1;
    private const uint NIM_DELETE = 0x2;
    private const uint NIM_SETVERSION = 0x4;
    private const uint NIF_MESSAGE = 0x01;
    private const uint NIF_ICON = 0x02;
    private const uint NIF_TIP = 0x04;
    private const uint NIF_INFO = 0x10;
    private const uint NIF_SHOWTIP = 0x80;
    private const uint NIIF_INFO = 0x1;
    private const uint NOTIFYICON_VERSION_4 = 4;
    private const int MaxTipLength = 127;

    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;
    private const int WS_EX_TOOLWINDOW = 0x80;

    private const uint IconId = 1;

    // Explorer broadcasts this to top-level windows when the taskbar is (re)created.
    // A message-only window would not receive it, which is why the sink is a hidden top-level window.
    private static readonly int TaskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");

    private readonly HwndSource _sink;
    private IntPtr _icon;
    private string _text = "";
    private bool _added;
    private bool _disposed;

    /// <summary>Raised when the icon is double-clicked with the left mouse button.</summary>
    public event EventHandler DoubleClick;

    public TrayIcon(Uri iconResource)
    {
        _sink = new HwndSource(new HwndSourceParameters("CurrentlyListeningTray")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0, // not WS_VISIBLE: never shown
            ExtendedWindowStyle = WS_EX_TOOLWINDOW // never gets a taskbar button
        });
        _sink.AddHook(WndProc);

        _icon = CreateIconHandle(iconResource,
            GetSystemMetrics(SM_CXSMICON), GetSystemMetrics(SM_CYSMICON));

        AddIcon();
    }

    /// <summary>The menu shown on right-click or the keyboard menu key.</summary>
    public ContextMenu ContextMenu { get; set; }

    /// <summary>Tooltip text; longer text is cut to what Windows allows.</summary>
    public string Text
    {
        get => _text;
        set
        {
            _text = value ?? "";
            if (_added)
            {
                var data = NewData(NIF_TIP | NIF_SHOWTIP);
                data.szTip = Tip;
                Shell_NotifyIcon(NIM_MODIFY, ref data);
            }
        }
    }

    private string Tip => _text.Length > MaxTipLength ? _text[..MaxTipLength] : _text;

    /// <summary>Shows a notification from the icon (a toast on Windows 10/11).</summary>
    public void ShowBalloon(string title, string text)
    {
        if (!_added) return;
        var data = NewData(NIF_INFO);
        data.szInfoTitle = Truncate(title, 63);
        data.szInfo = Truncate(text, 255);
        data.dwInfoFlags = NIIF_INFO;
        Shell_NotifyIcon(NIM_MODIFY, ref data);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_added)
        {
            var data = NewData(0);
            Shell_NotifyIcon(NIM_DELETE, ref data);
            _added = false;
        }

        if (_icon != IntPtr.Zero)
        {
            DestroyIcon(_icon);
            _icon = IntPtr.Zero;
        }

        _sink.RemoveHook(WndProc);
        _sink.Dispose();
    }

    private void AddIcon()
    {
        var data = NewData(NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP);
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = _icon;
        data.szTip = Tip;

        _added = Shell_NotifyIcon(NIM_ADD, ref data);
        if (!_added)
        {
            // Usually the taskbar isn't up yet (app started with Windows); TaskbarCreated retries.
            App.LogException("tray", new Win32Exception(Marshal.GetLastWin32Error(),
                "Could not add the tray icon; will retry when the taskbar is created."));
            return;
        }

        data.uTimeoutOrVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIcon(NIM_SETVERSION, ref data);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_disposed)
            return IntPtr.Zero;

        if (msg == CallbackMessage)
        {
            // NOTIFYICON_VERSION_4: the event is in the low word of lParam,
            // the anchor point (screen pixels) in wParam.
            switch ((int)(lParam.ToInt64() & 0xFFFF))
            {
                case WM_LBUTTONDBLCLK:
                    DoubleClick?.Invoke(this, EventArgs.Empty);
                    break;
                case WM_CONTEXTMENU:
                    var w = wParam.ToInt64();
                    ShowContextMenu((short)(w & 0xFFFF), (short)((w >> 16) & 0xFFFF));
                    break;
            }

            handled = true;
        }
        else if (msg == TaskbarCreatedMessage && TaskbarCreatedMessage != 0)
        {
            // Explorer restarted: its new taskbar doesn't know about the icon.
            _added = false;
            AddIcon();
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu(int x, int y)
    {
        if (ContextMenu == null) return;

        // Without this the menu doesn't close when the user clicks elsewhere.
        SetForegroundWindow(_sink.Handle);

        // Screen pixels to WPF units (the app is system-DPI aware, so the sink window's DPI
        // is the system DPI).
        var dpi = GetDpiForWindow(_sink.Handle);
        var scale = dpi > 0 ? 96.0 / dpi : 1.0;

        ContextMenu.Placement = PlacementMode.AbsolutePoint;
        ContextMenu.HorizontalOffset = x * scale;
        ContextMenu.VerticalOffset = y * scale;
        ContextMenu.StaysOpen = false;
        ContextMenu.IsOpen = true;
        ContextMenu.Focus();
    }

    private NOTIFYICONDATAW NewData(uint flags) => new()
    {
        cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
        hWnd = _sink.Handle,
        uID = IconId,
        uFlags = flags
    };

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length > max ? s[..max] : s;

    // Draws the .ico at exactly width × height with high-quality scaling and turns it into an
    // HICON, so the tray icon stays sharp at any system DPI even if the .ico lacks that size.
    private static IntPtr CreateIconHandle(Uri resource, int width, int height)
    {
        try
        {
            BitmapSource source;
            using (var stream = Application.GetResourceStream(resource).Stream)
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);
                // Smallest frame at least as big as needed, else the biggest one.
                source = decoder.Frames
                    .OrderBy(f => f.PixelWidth >= width ? 0 : 1)
                    .ThenBy(f => f.PixelWidth >= width ? f.PixelWidth : -f.PixelWidth)
                    .First();
            }

            var visual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
            using (var dc = visual.RenderOpen())
                dc.DrawImage(source, new Rect(0, 0, width, height));

            var rendered = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rendered.Render(visual);
            // Icons want straight (not premultiplied) alpha.
            var bgra = new FormatConvertedBitmap(rendered, PixelFormats.Bgra32, null, 0);

            var stride = width * 4;
            var pixels = new byte[stride * height];
            bgra.CopyPixels(pixels, stride, 0);

            var color = CreateBitmap(width, height, 1, 32, pixels);
            var mask = CreateBitmap(width, height, 1, 1, new byte[((width + 15) / 16) * 2 * height]);
            try
            {
                var info = new ICONINFO { fIcon = true, hbmColor = color, hbmMask = mask };
                var icon = CreateIconIndirect(ref info);
                if (icon == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return icon;
            }
            finally
            {
                DeleteObject(color);
                DeleteObject(mask);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            App.LogException("tray", ex);
            return IntPtr.Zero; // the icon is added without an image rather than not at all
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        [MarshalAs(UnmanagedType.Bool)] public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint message, ref NOTIFYICONDATAW data);

    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)]
    private static extern int RegisterWindowMessage(string name);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateIconIndirect(ref ICONINFO info);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, byte[] bits);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr obj);
}
