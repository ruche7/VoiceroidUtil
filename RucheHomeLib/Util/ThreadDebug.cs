using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RucheHome.Util
{
    /// <summary>
    /// 時刻、スレッドID、呼び出し元情報付きのデバッグ出力を行う静的クラス。
    /// </summary>
    public static class ThreadDebug
    {
        /// <summary>
        /// 時刻、スレッドID、呼び出し元情報を付けてテキストと改行をデバッグ出力する。
        /// </summary>
        /// <param name="text">テキスト。不要ならば null 。</param>
        /// <param name="member">
        /// 呼び出し元メンバ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        /// <param name="file">
        /// 呼び出し元ファイル名。 CallerFilePathAttribute により自動設定される。
        /// </param>
        /// <param name="line">
        /// 呼び出し元行番号。 CallerLineNumberAttribute により自動設定される。
        /// </param>
        [Conditional("DEBUG")]
        public static void WriteLine(
            string text = null,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            =>
            Debug.WriteLine(MakeMessage(text, member, file, line));

        /// <summary>
        /// 時刻、スレッドID、呼び出し元情報を付けて値と改行をデバッグ出力する。
        /// </summary>
        /// <param name="value">値。</param>
        /// <param name="member">
        /// 呼び出し元メンバ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        /// <param name="file">
        /// 呼び出し元ファイル名。 CallerFilePathAttribute により自動設定される。
        /// </param>
        /// <param name="line">
        /// 呼び出し元行番号。 CallerLineNumberAttribute により自動設定される。
        /// </param>
        [Conditional("DEBUG")]
        public static void WriteLine(
            object value,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            =>
            WriteLine(value?.ToString(), member, file, line);

        /// <summary>
        /// 時刻、スレッドID、呼び出し元情報を付けて例外情報と改行をデバッグ出力する。
        /// </summary>
        /// <param name="exception">例外情報。</param>
        /// <param name="withStackTrace">
        /// スタックトレースを出力するならば true 。そうでなければ false 。
        /// </param>
        /// <param name="member">
        /// 呼び出し元メンバ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        /// <param name="file">
        /// 呼び出し元ファイル名。 CallerFilePathAttribute により自動設定される。
        /// </param>
        /// <param name="line">
        /// 呼び出し元行番号。 CallerLineNumberAttribute により自動設定される。
        /// </param>
        [Conditional("DEBUG")]
        public static void WriteException(
            Exception exception,
            bool withStackTrace = false,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
            =>
            WriteLine(
                MakeExceptionText(exception, withStackTrace),
                member,
                file,
                line);

        /// <summary>
        /// デバッグ出力用メッセージを作成する。
        /// </summary>
        /// <param name="text">テキスト。不要ならば null 。</param>
        /// <param name="member">呼び出し元メンバ名。</param>
        /// <param name="file">呼び出し元ファイル名。</param>
        /// <param name="line">呼び出し元行番号。</param>
        /// <returns>メッセージ。</returns>
        internal static string MakeMessage(
            string text,
            string member,
            string file,
            int line)
        {
            var msg = new StringBuilder();

            msg.Append('[');
            msg.Append(DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss"));
            msg.Append(@"][TID:");
            msg.Append(Thread.CurrentThread.ManagedThreadId);
            msg.Append(@"][");
            msg.Append(member);
            msg.Append(';');
            msg.Append(Path.GetFileName(file));
            msg.Append(':');
            msg.Append(line);
            msg.Append(']');

            if (text != null)
            {
                msg.Append('>');
                msg.Append(text);
            }

            return msg.ToString();
        }

        /// <summary>
        /// 例外情報テキストを作成する。
        /// </summary>
        /// <param name="exception">例外情報。</param>
        /// <param name="withStackTrace">
        /// スタックトレースを含めるならば true 。そうでなければ false 。
        /// </param>
        /// <returns>例外情報テキスト。</returns>
        internal static string MakeExceptionText(Exception exception, bool withStackTrace)
        {
            if (exception == null)
            {
                return null;
            }

            var text = exception.GetType().FullName + @": " + exception.Message;
            if (withStackTrace)
            {
                text += Environment.NewLine + exception.StackTrace;
            }

            return text;
        }
    }
}
