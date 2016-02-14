using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
        /// ウィンドウテキストを取得する。
        /// </summary>
        /// <returns>ウィンドウテキスト。</returns>
        public string GetText()
        {
            var size = this.SendMessage(WM_GETTEXTLENGTH);

            var text = new StringBuilder(size.ToInt32());
            SendMessage(this.Handle, WM_GETTEXT, new IntPtr(text.Capacity), text);

            return text.ToString();
        }

        /// <summary>
        /// ウィンドウテキストを設定する。
        /// </summary>
        /// <param name="text">ウィンドウテキスト。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        public bool SetText(string text)
        {
            var result = SendMessage(this.Handle, WM_SETTEXT, IntPtr.Zero, text ?? "");
            return (result.ToInt32() == 1);
        }

        /// <summary>
        /// 子孫ウィンドウを検索する。
        /// </summary>
        /// <param name="className">
        /// ウィンドウクラス名。 null を指定するとウィンドウクラス名を限定しない。
        /// </param>
        /// <param name="text">
        /// ウィンドウテキスト。 null を指定するとウィンドウテキストを限定しない。
        /// </param>
        /// <returns>子孫ウィンドウリスト。取得できなければ null 。</returns>
        public List<Win32Window> FindDescendants(
            string className = null,
            string text = null)
        {
            var descends = new List<Win32Window>();
            bool result =
                EnumChildWindows(
                    this.Handle,
                    (handle, lparam) =>
                    {
                        var window = new Win32Window(handle);
                        if (
                            (className == null || window.ClassName == className) &&
                            (text == null || window.GetText() == text))
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
        /// ウィンドウメッセージを送信する。
        /// </summary>
        /// <param name="message">ウィンドウメッセージ。</param>
        /// <param name="wparam">パラメータ1。</param>
        /// <param name="lparam">パラメータ2。</param>
        /// <returns>結果値。</returns>
        public IntPtr SendMessage(
            uint message,
            IntPtr wparam = default(IntPtr),
            IntPtr lparam = default(IntPtr))
        {
            return SendMessage(this.Handle, message, wparam, lparam);
        }

        /// <summary>
        /// ウィンドウメッセージを非同期で送信する。
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

        #region Win32 API インポート

        private const uint WM_SETTEXT = 0x000C;
        private const uint WM_GETTEXT = 0x000D;
        private const uint WM_GETTEXTLENGTH = 0x000E;
        private const uint EM_SETSEL = 0x00B1;
        private const uint BM_CLICK = 0x00F5;

        private const uint GW_OWNER = 4;
        private const uint GA_PARENT = 1;

        [return: MarshalAs(UnmanagedType.Bool)]
        private delegate bool EnumWindowProc(IntPtr windowHandle, IntPtr lparam);

        [DllImport("user32.dll")]
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern int GetClassName(
            IntPtr windowHandle,
            [Out] StringBuilder name,
            int nameSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowEnabled(IntPtr windowHandle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnableWindow(
            IntPtr windowHandle,
            [MarshalAs(UnmanagedType.Bool)] bool enable);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            string lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            [Out] StringBuilder lparam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(
            IntPtr windowHandle,
            uint message,
            IntPtr wparam,
            IntPtr lparam);

        #endregion
    }
}
