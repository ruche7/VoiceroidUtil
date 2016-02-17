using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ruche.windows
{
    /// <summary>
    /// Win32 API によって操作されるウィンドウクラス。
    /// </summary>
    public class Win32Window
    {
        /// <summary>
        /// デスクトップウィンドウ。
        /// </summary>
        public static readonly Win32Window Desktop = new Win32Window(IntPtr.Zero);

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="handle">ウィンドウハンドル。</param>
        public Win32Window(IntPtr handle)
        {
            this.Handle = handle;
        }

        /// <summary>
        /// ウィンドウハンドルを取得する。
        /// </summary>
        public IntPtr Handle { get; }

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
                    if (GetClassName(this.Handle, name, name.Capacity) > 0)
                    {
                        this.className = name.ToString();
                    }
                }

                return this.className;
            }
        }
        private string className = null;

        /// <summary>
        /// ウィンドウが有効な状態であるか否かを取得または設定する。
        /// </summary>
        public bool IsEnabled
        {
            get { return IsWindowEnabled(this.Handle); }
            set { EnableWindow(this.Handle, value); }
        }

        /// <summary>
        /// ウィンドウの表示状態を取得または設定する。
        /// </summary>
        public Win32WindowState State
        {
            get
            {
                return
                    IsIconic(this.Handle) ?
                        Win32WindowState.Minimized :
                        IsZoomed(this.Handle) ?
                            Win32WindowState.Maximized : Win32WindowState.Normal;
            }
            set
            {
                if (value != this.State)
                {
                    int command = 0;
                    switch (value)
                    {
                    case Win32WindowState.Normal:
                        command = SW_SHOWNOACTIVATE;
                        break;
                    case Win32WindowState.Maximized:
                        command = SW_MAXIMIZED;
                        break;
                    case Win32WindowState.Minimized:
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
        /// 指定した階層だけ上の親ウィンドウを取得する。
        /// </summary>
        /// <param name="count">階層数。既定値は 1 。</param>
        /// <returns>親ウィンドウ。存在しない場合は null 。</returns>
        public Win32Window GetAncestor(int count = 1)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException("count");
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
        /// <returns>
        /// オーナーウィンドウ。このウィンドウが子ウィンドウの場合は null 。
        /// </returns>
        public Win32Window GetOwner(int count = 1)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            var handle = this.Handle;
            for (int i = 0; i < count; ++i)
            {
                // デスクトップにオーナーはいない
                if (handle == Desktop.Handle)
                {
                    return null;
                }

                var h = GetWindow(handle, GW_OWNER);
                if (h == IntPtr.Zero)
                {
                    // 親ウィンドウがいるなら子ウィンドウ
                    if (GetAncestor(handle, GA_PARENT) != IntPtr.Zero)
                    {
                        return null;
                    }
                }

                handle = h;
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
            for (IntPtr child = IntPtr.Zero; ; )
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
        /// <returns>
        /// ウィンドウテキスト取得タスク。
        /// 成功するとウィンドウテキストを返す。そうでなければ null を返す。
        /// </returns>
        public async Task<string> GetText(int timeoutMilliseconds = -1)
        {
            var size =
                await this.SendMessage(
                    WM_GETTEXTLENGTH,
                    timeoutMilliseconds: timeoutMilliseconds);
            if (!size.HasValue)
            {
                return null;
            }

            var text = new StringBuilder(size.Value.ToInt32() + 1);
            var r =
                await this.SendMessage(
                    WM_GETTEXT,
                    new IntPtr(text.Capacity),
                    text,
                    timeoutMilliseconds);
            if (!r.HasValue)
            {
                return null;
            }

            return text.ToString();
        }

        /// <summary>
        /// ウィンドウテキストを設定する。
        /// </summary>
        /// <param name="text">ウィンドウテキスト。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>
        /// ウィンドウテキスト設定タスク。
        /// 成功すると true を返す。そうでなければ false を返す。
        /// </returns>
        public async Task<bool> SetText(string text, int timeoutMilliseconds = -1)
        {
            var result =
                await this.SendMessage(
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
        /// <returns>
        /// 非同期処理タスク。
        /// 処理が完了すると結果値を返す。ただしタイムアウトした場合は null を返す。
        /// </returns>
        public Task<IntPtr?> SendMessage(
            uint message,
            IntPtr wparam = default(IntPtr),
            IntPtr lparam = default(IntPtr),
            int timeoutMilliseconds = -1)
        {
            return this.SendMessageCore(message, wparam, lparam, timeoutMilliseconds);
        }

        /// <summary>
        /// ウィンドウメッセージをポストする。
        /// </summary>
        /// <param name="message">ウィンドウメッセージ。</param>
        /// <param name="wparam">パラメータ1。</param>
        /// <param name="lparam">パラメータ2。</param>
        /// <returns>送信できたならば true 。そうでなければ false 。</returns>
        public bool PostMessage(
            uint message,
            IntPtr wparam = default(IntPtr),
            IntPtr lparam = default(IntPtr))
        {
            return PostMessage(this.Handle, message, wparam, lparam);
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
        /// <returns>
        /// 非同期処理タスク。
        /// 処理が完了すると結果値を返す。ただしタイムアウトした場合は null を返す。
        /// </returns>
        private Task<IntPtr?> SendMessage(
            uint message,
            IntPtr wparam,
            string lparam,
            int timeoutMilliseconds = -1)
        {
            return this.SendMessageCore(message, wparam, lparam, timeoutMilliseconds);
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
        /// <returns>
        /// 非同期処理タスク。
        /// 処理が完了すると結果値を返す。ただしタイムアウトした場合は null を返す。
        /// </returns>
        private Task<IntPtr?> SendMessage(
            uint message,
            IntPtr wparam,
            StringBuilder lparam,
            int timeoutMilliseconds = -1)
        {
            return this.SendMessageCore(message, wparam, lparam, timeoutMilliseconds);
        }

        /// <summary>
        /// ウィンドウメッセージ送信の実処理を行う。
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
        private Task<IntPtr?> SendMessageCore(
            uint message,
            dynamic wparam,
            dynamic lparam,
            int timeoutMilliseconds)
        {
            var handle = this.Handle;

            return
                Task.Run(() =>
                {
                    IntPtr result = IntPtr.Zero;

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
                            return null;
                        }
                    }

                    return (IntPtr?)result;
                });
        }

        #region Win32 API インポート

        private const uint WM_SETTEXT = 0x000C;
        private const uint WM_GETTEXT = 0x000D;
        private const uint WM_GETTEXTLENGTH = 0x000E;

        private const uint GW_OWNER = 4;
        private const uint GA_PARENT = 1;
        private const int SW_MAXIMIZED = 3;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_SHOWMINNOACTIVE = 7;
        private const uint SMTO_NORMAL = 0;

        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool EnumWindowProc(IntPtr windowHandle, IntPtr lparam);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(
            IntPtr parentWindowHandle,
            EnumWindowProc enumWindowProc,
            IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindWindowEx(
            IntPtr parentWindowHandle,
            IntPtr childAfterWindowHandle,
            string className,
            string windowName);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(
            IntPtr windowHandle,
            [Out] StringBuilder name,
            int nameSize);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowEnabled(IntPtr windowHandle);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnableWindow(
            IntPtr windowHandle,
            [MarshalAs(UnmanagedType.Bool)] bool enable);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr windowHandle);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr windowHandle);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr windowHandle, int command);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            string lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            [Out] StringBuilder lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam,
            uint flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            string lparam,
            uint flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            [Out] StringBuilder lparam,
            uint flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam);

        #endregion
    }
}
