using System;

namespace RucheHome.Voiceroid
{
    /// <summary>
    /// ファイル保存処理結果を保持する構造体。
    /// </summary>
    public struct FileSaveResult
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="succeeded">ファイル保存成否。</param>
        /// <param name="filePath">保存ファイルパス。</param>
        /// <param name="error">保存失敗時のエラーテキスト。</param>
        public FileSaveResult(
            bool succeeded,
            string filePath = null,
            string error = null)
        {
            this.IsSucceeded = succeeded;
            this.FilePath = filePath;
            this.Error = error;
        }

        /// <summary>
        /// ファイル保存に成功したか否かを取得する。
        /// </summary>
        public bool IsSucceeded { get; }

        /// <summary>
        /// 保存ファイルパスを取得する。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 保存失敗時のエラーテキストを取得する。
        /// </summary>
        public string Error { get; }
    }
}
