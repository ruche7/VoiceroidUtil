using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ruche.voiceroid
{
    /// <summary>
    /// Win32 API によって操作されるウィンドウクラス。
    /// </summary>
    internal class Win32Window
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
        /// 親ウィンドウまたはオーナーウィンドウを取得する。
        /// </summary>
        /// <returns>親ウィンドウまたはオーナーウィンドウ。</returns>
        public Win32Window GetParent()
        {
            return new Win32Window(GetParent(this.Handle));
        }

        /// <summary>
        /// ウィンドウテキストを取得する。
        /// </summary>
        /// <returns>ウィンドウテキスト。</returns>
        public string GetText()
        {
            var size =
                SendMessage(this.Handle, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);

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
        /// 子孫ウィンドウリストを取得する。
        /// </summary>
        /// <returns>子孫ウィンドウリスト。取得できなければ null 。</returns>
        public List<Win32Window> GetDescendants()
        {
            var descends = new List<Win32Window>();
            bool result =
                EnumChildWindows(
                    this.Handle,
                    (handle, lparam) =>
                    {
                        descends.Add(new Win32Window(handle));
                        return true;
                    },
                    IntPtr.Zero);

            return result ? descends : null;
        }

        /// <summary>
        /// 指定したウィンドウタイトルを持つ子ウィンドウを検索する。
        /// </summary>
        /// <param name="title">
        /// ウィンドウタイトル。 null を指定するとウィンドウタイトルを限定しない。
        /// </param>
        /// <param name="className">
        /// ウィンドウクラス名。 null を指定するとウィンドウクラス名を限定しない。
        /// </param>
        /// <returns>子ウィンドウ列挙。</returns>
        public IEnumerable<Win32Window> FindChildren(
            string title = null,
            string className = null)
        {
            for (IntPtr child = IntPtr.Zero; ; )
            {
                child = FindWindowEx(this.Handle, child, className, title);
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

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetParent(IntPtr windowHandle);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern int GetClassName(
            IntPtr windowHandle,
            [Out] StringBuilder name,
            int nameSize);

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
