using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace RucheHome.Windows.WinApi
{
    /// <summary>
    /// Win32 API によって操作されるウィンドウクラス。
    /// </summary>
    public class Win32Window
    {
        /// <summary>
        /// デスクトップウィンドウからインスタンスを生成する。
        /// </summary>
        public static Win32Window FromDesktop() => new Win32Window(GetDesktopWindow());

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="handle">ウィンドウハンドル。</param>
        public Win32Window(IntPtr handle) => this.Handle = handle;

        /// <summary>
        /// ウィンドウハンドルを取得する。
        /// </summary>
        public IntPtr Handle { get; }

        /// <summary>
        /// ウィンドウが存在するか否かを取得する。
        /// </summary>
        public bool IsExists => IsWindow(this.Handle);

        /// <summary>
        /// ウィンドウが表示されているか否かを取得する。
        /// </summary>
        public bool IsVisible => IsWindowVisible(this.Handle);

        /// <summary>
        /// ウィンドウが有効な状態であるか否かを取得または設定する。
        /// </summary>
        public bool IsEnabled
        {
            get => IsWindowEnabled(this.Handle);
            set => EnableWindow(this.Handle, value);
        }

        /// <summary>
        /// ウィンドウクラス名を取得する。
        /// </summary>
        public string ClassName
        {
            get
            {
                if (this.className == null)
                {
                    var name = new StringBuilder(256);
                    var len = GetClassName(this.Handle, name, name.Capacity);
                    if (len <= 0)
                    {
                        ThrowLastErrorException();
                    }
                    this.className = name.ToString(0, len);
                }

                return this.className;
            }
        }
        private string className = null;

        /// <summary>
        /// ウィンドウの属するプロセスのIDを取得する。
        /// </summary>
        public int ProcessId
        {
            get
            {
                if (!this.processId.HasValue)
                {
                    GetWindowThreadProcessId(this.Handle, out int id);
                    this.processId = id;
                }

                return this.processId.Value;
            }
        }
        private int? processId = null;

        /// <summary>
        /// ウィンドウの表示状態を取得または設定する。
        /// </summary>
        public WindowState State
        {
            get =>
                IsIconic(this.Handle) ?
                    WindowState.Minimized :
                    IsZoomed(this.Handle) ?
                        WindowState.Maximized : WindowState.Normal;
            set
            {
                if (value != this.State)
                {
                    int command;
                    switch (value)
                    {
                    case WindowState.Normal:
                        command = SW_SHOWNOACTIVATE;
                        break;
                    case WindowState.Maximized:
                        command = SW_MAXIMIZED;
                        break;
                    case WindowState.Minimized:
                        command = SW_SHOWMINNOACTIVE;
                        break;
                    default:
                        throw new InvalidEnumArgumentException(
                            nameof(value),
                            (int)value,
                            value.GetType());
                    }
                    ShowWindow(this.Handle, command);
                }
            }
        }

        /// <summary>
        /// ウィンドウが最前面表示されているか否かを取得する。
        /// </summary>
        public bool IsTopmost =>
            ((GetWindowLong(this.Handle, GWL_EXSTYLE).ToInt64() & WS_EX_TOPMOST) != 0);

        /// <summary>
        /// ウィンドウが最小化や最大化されている場合は元のサイズに戻す。
        /// </summary>
        /// <remarks>
        /// 最大化状態から最小化したウィンドウの場合は最大化状態に戻る。
        /// </remarks>
        public void Restore() => ShowWindow(this.Handle, SW_RESTORE);

        /// <summary>
        /// ウィンドウをアクティブにする。
        /// </summary>
        public void Activate()
        {
            bool result =
                SetWindowPos(
                    this.Handle,
                    HWND_TOP,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOSIZE | SWP_NOMOVE);
            if (!result)
            {
                ThrowLastErrorException();
            }
        }

        /// <summary>
        /// Zオーダーを指定したウィンドウの次にする。
        /// </summary>
        /// <param name="windowInsertAfter">基準ウィンドウ。</param>
        /// <remarks>
        /// 基準ウィンドウに合わせて最前面表示状態も変化する。
        /// </remarks>
        public void MoveZOrderAfter(Win32Window windowInsertAfter)
        {
            if (windowInsertAfter == null)
            {
                throw new ArgumentNullException(nameof(windowInsertAfter));
            }

            bool result =
                SetWindowPos(
                    this.Handle,
                    windowInsertAfter.Handle,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
            if (!result)
            {
                ThrowLastErrorException();
            }
        }

        /// <summary>
        /// ウィンドウが非アクティブの場合にタスクバーボタンを点滅させる。
        /// もしくは点滅を止める。
        /// </summary>
        /// <param name="on">点滅させるならば true 。消灯させるならば false 。</param>
        public void FlashTray(bool on = true)
        {
            var info = new FLASHWINFO();
            info.StructSize = (uint)Marshal.SizeOf(info);
            info.WindowHandle = this.Handle;
            info.Flags = on ? (FLASHW_TRAY | FLASHW_TIMERNOFG) : FLASHW_STOP;
            info.Count = on ? uint.MaxValue : 0;
            info.Timeout = 0;

            FlashWindowEx(ref info);
        }

        /// <summary>
        /// 指定した階層だけ上の親ウィンドウを取得する。
        /// </summary>
        /// <param name="count">階層数。既定値は 1 。</param>
        /// <returns>親ウィンドウ。存在しない場合は null 。</returns>
        public Win32Window GetAncestor(int count = 1)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var handle = this.Handle;
            for (int i = 0; i < count; ++i)
            {
                handle = GetAncestor(handle, GA_PARENT);
                if (handle == IntPtr.Zero)
                {
                    return null;
                }
            }

            return new Win32Window(handle);
        }

        /// <summary>
        /// 指定した階層だけ上のオーナーウィンドウを取得する。
        /// </summary>
        /// <param name="count">階層数。既定値は 1 。</param>
        /// <returns>オーナーウィンドウ。存在しない場合は null 。</returns>
        public Win32Window GetOwner(int count = 1)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var handle = this.Handle;
            for (int i = 0; i < count; ++i)
            {
                handle = GetWindow(handle, GW_OWNER);
                if (handle == IntPtr.Zero)
                {
                    return null;
                }
            }

            return new Win32Window(handle);
        }

        /// <summary>
        /// 子孫ウィンドウを検索する。
        /// </summary>
        /// <param name="className">
        /// ウィンドウクラス名。 null を指定するとウィンドウクラス名を限定しない。
        /// </param>
        /// <returns>子孫ウィンドウリスト。取得できなければ null 。</returns>
        public List<Win32Window> FindDescendants(string className = null)
        {
            var descends = new List<Win32Window>();
            bool result =
                EnumChildWindows(
                    this.Handle,
                    (handle, lparam) =>
                    {
                        var window = new Win32Window(handle);
                        if (className == null || window.ClassName == className)
                        {
                            descends.Add(window);
                        }
                        return true;
                    },
                    IntPtr.Zero);

            return result ? descends : null;
        }

        /// <summary>
        /// 子ウィンドウを検索して列挙する。
        /// </summary>
        /// <param name="className">
        /// ウィンドウクラス名。 null を指定するとウィンドウクラス名を限定しない。
        /// </param>
        /// <param name="text">
        /// ウィンドウテキスト。 null を指定するとウィンドウテキストを限定しない。
        /// </param>
        /// <returns>子ウィンドウ列挙。</returns>
        public IEnumerable<Win32Window> FindChildren(
            string className = null,
            string text = null)
        {
            for (IntPtr child = IntPtr.Zero; ;)
            {
                child = FindWindowEx(this.Handle, child, className, text);
                if (child == IntPtr.Zero)
                {
                    break;
                }

                yield return new Win32Window(child);
            }
        }

        /// <summary>
        /// ウィンドウテキストを取得する。
        /// </summary>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>ウィンドウテキスト。取得できなかった場合は null 。</returns>
        public string GetText(int timeoutMilliseconds = -1)
        {
            var sw = Stopwatch.StartNew();
            var timeout = timeoutMilliseconds;

            var size =
                this.SendMessage(
                    WM_GETTEXTLENGTH,
                    timeoutMilliseconds: timeout);
            if (!size.HasValue)
            {
                return null;
            }

            if (timeout >= 0)
            {
                timeout = Math.Max(timeoutMilliseconds - (int)sw.ElapsedMilliseconds, 0);
            }

            var text = new StringBuilder(size.Value.ToInt32() + 1);
            var r =
                this.SendMessage(
                    WM_GETTEXT,
                    new IntPtr(text.Capacity),
                    text,
                    timeout);

            return r.HasValue ? text.ToString() : null;
        }

        /// <summary>
        /// ウィンドウテキストを設定する。
        /// </summary>
        /// <param name="text">ウィンドウテキスト。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>成功した場合は true 。そうでなければ false 。</returns>
        public bool SetText(string text, int timeoutMilliseconds = -1)
        {
            var result =
                this.SendMessage(
                    WM_SETTEXT,
                    IntPtr.Zero,
                    text ?? "",
                    timeoutMilliseconds);

            return (result?.ToInt32() == 1);
        }

        /// <summary>
        /// ウィンドウメッセージを送信する。
        /// </summary>
        /// <param name="message">ウィンドウメッセージ。</param>
        /// <param name="wparam">パラメータ1。</param>
        /// <param name="lparam">パラメータ2。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>結果値。タイムアウトした場合は null 。</returns>
        public IntPtr? SendMessage(
            uint message,
            IntPtr wparam = default,
            IntPtr lparam = default,
            int timeoutMilliseconds = -1)
            =>
            this.SendMessageCore(message, wparam, lparam, timeoutMilliseconds);

        /// <summary>
        /// ウィンドウメッセージをポストする。
        /// </summary>
        /// <param name="message">ウィンドウメッセージ。</param>
        /// <param name="wparam">パラメータ1。</param>
        /// <param name="lparam">パラメータ2。</param>
        public void PostMessage(
            uint message,
            IntPtr wparam = default,
            IntPtr lparam = default)
        {
            if (!PostMessage(this.Handle, message, wparam, lparam))
            {
                ThrowLastErrorException();
            }
        }

        /// <summary>
        /// 直近の Win32 エラー値を基に例外を送出する。
        /// </summary>
        private static void ThrowLastErrorException() =>
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

        /// <summary>
        /// ウィンドウメッセージを送信する。
        /// </summary>
        /// <param name="message">ウィンドウメッセージ。</param>
        /// <param name="wparam">パラメータ1。</param>
        /// <param name="lparam">パラメータ2。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>
        /// 非同期処理タスク。
        /// 処理が完了すると結果値を返す。ただしタイムアウトした場合は null を返す。
        /// </returns>
        private IntPtr? SendMessage(
            uint message,
            IntPtr wparam,
            string lparam,
            int timeoutMilliseconds = -1)
            =>
            this.SendMessageCore(message, wparam, lparam, timeoutMilliseconds);

        /// <summary>
        /// ウィンドウメッセージを送信する。
        /// </summary>
        /// <param name="message">ウィンドウメッセージ。</param>
        /// <param name="wparam">パラメータ1。</param>
        /// <param name="lparam">パラメータ2。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>結果値。タイムアウトした場合は null 。</returns>
        private IntPtr? SendMessage(
            uint message,
            IntPtr wparam,
            StringBuilder lparam,
            int timeoutMilliseconds = -1)
            =>
            this.SendMessageCore(message, wparam, lparam, timeoutMilliseconds);

        /// <summary>
        /// ウィンドウメッセージ送信の実処理を行う。
        /// </summary>
        /// <param name="message">ウィンドウメッセージ。</param>
        /// <param name="wparam">パラメータ1。</param>
        /// <param name="lparam">パラメータ2。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>結果値。タイムアウトした場合は null 。</returns>
        private IntPtr? SendMessageCore(
            uint message,
            dynamic wparam,
            dynamic lparam,
            int timeoutMilliseconds)
        {
            var result = IntPtr.Zero;

            if (timeoutMilliseconds < 0)
            {
                result = SendMessage(this.Handle, message, wparam, lparam);
            }
            else
            {
                var r =
                    SendMessageTimeout(
                        this.Handle,
                        message,
                        wparam,
                        lparam,
                        SMTO_NORMAL,
                        (uint)timeoutMilliseconds,
                        out result);
                if (r == IntPtr.Zero)
                {
                    if (Marshal.GetLastWin32Error() != 0)
                    {
                        ThrowLastErrorException();
                    }
                    return null;
                }
            }

            return result;
        }

        #region Win32 API インポート

        private const uint WM_SETTEXT = 0x000C;
        private const uint WM_GETTEXT = 0x000D;
        private const uint WM_GETTEXTLENGTH = 0x000E;

        private const uint GW_OWNER = 4;
        private const uint GA_PARENT = 1;
        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_TOPMOST = 8;
        private const int SW_MAXIMIZED = 3;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_SHOWMINNOACTIVE = 7;
        private const int SW_RESTORE = 9;
        private const uint SWP_NOSIZE = 0x01;
        private const uint SWP_NOMOVE = 0x02;
        private const uint SWP_NOACTIVATE = 0x10;
        private const uint FLASHW_STOP = 0;
        private const uint FLASHW_TRAY = 0x02;
        private const uint FLASHW_TIMERNOFG = 0x0C;
        private const uint SMTO_NORMAL = 0;

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint StructSize;
            public IntPtr WindowHandle;
            public uint Flags;
            public uint Count;
            public uint Timeout;
        };

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr windowHandle);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr windowHandle);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowEnabled(IntPtr windowHandle);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnableWindow(
            IntPtr windowHandle,
            [MarshalAs(UnmanagedType.Bool)] bool enable);

        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool EnumWindowProc(IntPtr windowHandle, IntPtr lparam);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(
            IntPtr parentWindowHandle,
            EnumWindowProc enumWindowProc,
            IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindWindowEx(
            IntPtr parentWindowHandle,
            IntPtr childAfterWindowHandle,
            string className,
            string windowName);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetAncestor(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(
            IntPtr windowHandle,
            out int processId);

        [DllImport(
            "user32.dll",
            EntryPoint = "GetWindowLong",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern IntPtr GetWindowLong32(IntPtr windowHandle, int index);

        [DllImport(
            "user32.dll",
            EntryPoint = "GetWindowLongPtr",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr windowHandle, int index);

        private static IntPtr GetWindowLong(IntPtr windowHandle, int index) =>
            (IntPtr.Size == 4) ?
                GetWindowLong32(windowHandle, index) :
                GetWindowLongPtr64(windowHandle, index);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetClassName(
            IntPtr windowHandle,
            [Out] StringBuilder name,
            int nameSize);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr windowHandle);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr windowHandle);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr windowHandle, int command);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
            IntPtr windowHandle,
            IntPtr windowHandleInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint flags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO info);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            string lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            [Out] StringBuilder lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam,
            uint flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            string lparam,
            uint flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            [Out] StringBuilder lparam,
            uint flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam);

        #endregion
    }
}
