using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 拡張編集オブジェクトファイルの読み取りと書き出しの処理を提供する静的クラス。
    /// </summary>
    public static class ExoFileReaderWriter
    {
        /// <summary>
        /// 拡張編集オブジェクトファイルを読み取る。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="strict">厳格な形式チェックを行うならば true 。</param>
        /// <returns>拡張編集オブジェクト。</returns>
        public static ExEditObject Read(string filePath, bool strict = false)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var sections = IniFileParser.FromFile(filePath, strict);

            return ExEditObject.FromExoFileSections(sections);
        }

        /// <summary>
        /// 拡張編集オブジェクトファイルを読み取る。
        /// </summary>
        /// <param name="fileInfo">ファイル情報。</param>
        /// <param name="strict">厳格な形式チェックを行うならば true 。</param>
        /// <returns>拡張編集オブジェクト。</returns>
        public static ExEditObject Read(FileInfo fileInfo, bool strict = false)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            var sections = IniFileParser.FromFile(fileInfo, strict);

            return ExEditObject.FromExoFileSections(sections);
        }

        /// <summary>
        /// 拡張編集オブジェクトファイルを非同期で読み取る。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="strict">厳格な形式チェックを行うならば true 。</param>
        /// <returns>拡張編集オブジェクト。</returns>
        public static Task<ExEditObject> ReadAsync(
            string filePath,
            bool strict = false)
            =>
            Task.Run(() => Read(filePath, strict));

        /// <summary>
        /// 拡張編集オブジェクトファイルを非同期で読み取る。
        /// </summary>
        /// <param name="fileInfo">ファイル情報。</param>
        /// <param name="strict">厳格な形式チェックを行うならば true 。</param>
        /// <returns>拡張編集オブジェクト。</returns>
        public static Task<ExEditObject> ReadAsync(
            FileInfo fileInfo,
            bool strict = false)
            =>
            Task.Run(() => Read(fileInfo, strict));

        /// <summary>
        /// 拡張編集オブジェクトをファイルへ書き出す。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="source">拡張編集オブジェクト。</param>
        public static void Write(string filePath, ExEditObject source)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Write(new FileInfo(filePath), source);
        }

        /// <summary>
        /// 拡張編集オブジェクトをファイルへ書き出す。
        /// </summary>
        /// <param name="fileInfo">ファイル情報。</param>
        /// <param name="source">拡張編集オブジェクト。</param>
        public static void Write(FileInfo fileInfo, ExEditObject source)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var text = source.ToExoFileSections().ToString();

            using (var stream = fileInfo.OpenWrite())
            using (var writer = new StreamWriter(stream, Encoding.GetEncoding(932)))
            {
                writer.WriteLine(text);
            }
        }

        /// <summary>
        /// 拡張編集オブジェクトを非同期でファイルへ書き出す。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="source">拡張編集オブジェクト。</param>
        public static Task WriteAsync(string filePath, ExEditObject source) =>
            Task.Run(() => Write(filePath, source));

        /// <summary>
        /// 拡張編集オブジェクトを非同期でファイルへ書き出す。
        /// </summary>
        /// <param name="fileInfo">ファイル情報。</param>
        /// <param name="source">拡張編集オブジェクト。</param>
        public static Task WriteAsync(FileInfo fileInfo, ExEditObject source) =>
            Task.Run(() => Write(fileInfo, source));
    }
}
