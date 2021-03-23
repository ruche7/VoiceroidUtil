using System;

namespace RucheHome.Voiceroid
{
    /// <summary>
    /// ファイル保存処理結果を保持する構造体。
    /// </summary>
    public struct FileSaveResult : IEquatable<FileSaveResult>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="succeeded">ファイル保存成否。</param>
        /// <param name="filePath">
        /// 保存ファイルパス。本体側の自動命名による保存時は空文字列。
        /// </param>
        /// <param name="error">保存失敗時のエラーテキスト。</param>
        /// <param name="extraMessage">保存失敗時の追加情報テキスト。</param>
        public FileSaveResult(
            bool succeeded,
            string filePath = null,
            string error = null,
            string extraMessage = null)
        {
            this.IsSucceeded = succeeded;
            this.FilePath = filePath;
            this.Error = error;
            this.ExtraMessage = extraMessage;
        }

        /// <summary>
        /// ファイル保存に成功したか否かを取得する。
        /// </summary>
        public bool IsSucceeded { get; }

        /// <summary>
        /// 保存ファイルパスを取得する。
        /// </summary>
        /// <remarks>
        /// 本体側の自動命名による保存時は空文字列を返す。
        /// </remarks>
        public string FilePath { get; }

        /// <summary>
        /// 保存失敗時のエラーテキストを取得する。
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// 保存失敗時の追加情報テキストを取得する。
        /// </summary>
        public string ExtraMessage { get; }

        /// <summary>
        /// 等価比較演算子のオーバーロード。
        /// </summary>
        /// <param name="left">左辺値。</param>
        /// <param name="right">右辺値。</param>
        /// <returns>等しいならば true 。そうでなければ false 。</returns>
        public static bool operator ==(FileSaveResult left, FileSaveResult right) =>
            left.Equals(right);

        /// <summary>
        /// 非等価比較演算子のオーバーロード。
        /// </summary>
        /// <param name="left">左辺値。</param>
        /// <param name="right">右辺値。</param>
        /// <returns>等しくないならば true 。そうでなければ false 。</returns>
        public static bool operator !=(FileSaveResult left, FileSaveResult right) =>
            !(left == right);

        #region Object のオーバライド

        /// <summary>
        /// 他のオブジェクトと等価であるか否かを取得する。
        /// </summary>
        /// <param name="obj">比較対象。</param>
        /// <returns>等しいならば true 。そうでなければ false 。</returns>
        public override bool Equals(object obj) =>
            (obj is FileSaveResult r) && this.Equals(r);

        /// <summary>
        /// ハッシュコード値を取得する。
        /// </summary>
        /// <returns>ハッシュコード値。</returns>
        public override int GetHashCode() =>
            this.IsSucceeded.GetHashCode() ^
            this.FilePath.GetHashCode() ^
            this.Error.GetHashCode();

        #endregion

        #region IEquatable<FileSaveResult> の実装

        /// <summary>
        /// 他のファイル保存処理結果と等価であるか否かを取得する。
        /// </summary>
        /// <param name="other">比較対象。</param>
        /// <returns>等しいならば true 。そうでなければ false 。</returns>
        public bool Equals(FileSaveResult other) =>
            this.IsSucceeded == other.IsSucceeded &&
            this.FilePath == other.FilePath &&
            this.Error == other.Error &&
            this.ExtraMessage == other.ExtraMessage;

        #endregion
    }
}
