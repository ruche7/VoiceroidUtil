using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VoiceroidUtil
{
    /// <summary>
    /// VoiceroidUtilのトレース情報をエラーログファイルに書き出す TraceListener クラス。
    /// </summary>
    public class ErrorLogFileTraceListener : TraceListener
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ErrorLogFileTraceListener() : base()
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">このインスタンスの名前。</param>
        public ErrorLogFileTraceListener(string name) : base(name)
        {
        }

        /// <summary>
        /// 保存先ディレクトリパスを取得するためのデリゲートを取得または設定する。
        /// </summary>
        /// <remarks>
        /// この値が null の場合はマイドキュメントが利用される。
        /// </remarks>
        public Func<string> DirectoryPathGetter { get; set; } = null;

        /// <summary>
        /// エラーログ書き出し先ファイルパスを作成する。
        /// </summary>
        /// <returns>ファイルパス。作成できなかった場合は null 。</returns>
        private string MakeFilePath()
        {
            var dirPath =
                (this.DirectoryPathGetter != null) ?
                    this.DirectoryPathGetter() :
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.Personal,
                        Environment.SpecialFolderOption.DoNotVerify);
            if (string.IsNullOrWhiteSpace(dirPath))
            {
                return null;
            }

            if (!Directory.Exists(dirPath))
            {
                try
                {
                    Directory.CreateDirectory(dirPath);
                }
                catch
                {
                    return null;
                }
            }

            return
                Path.Combine(
                    dirPath,
                    nameof(VoiceroidUtil) + @"Error-" +
                    DateTime.Now.ToString(@"yyMMdd") + @".txt");
        }

        #region TraceListener のオーバライド

        public override void Write(string message)
        {
            var path = this.MakeFilePath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                File.AppendAllText(path, message, Encoding.Unicode);
            }
            catch { }
        }

        public override void WriteLine(string message) =>
            this.Write(message + Environment.NewLine);

        #endregion
    }
}
