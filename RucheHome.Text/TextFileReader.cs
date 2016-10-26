using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RucheHome.Text
{
    /// <summary>
    /// 文字コードを自動判別してテキストファイルを読み取る処理を提供する静的クラス。
    /// </summary>
    public static class TextFileReader
    {
        /// <summary>
        /// 文字コードを自動判別してテキストファイルを読み取る。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <returns>読み取った文字列。読み取れなかった場合は null 。</returns>
        public static string Read(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return ReadAll(new[] { filePath })[0];
        }

        /// <summary>
        /// 文字コードを自動判別してテキストファイルを読み取る。
        /// </summary>
        /// <param name="fileInfo">ファイル情報。</param>
        /// <returns>読み取った文字列。読み取れなかった場合は null 。</returns>
        public static string Read(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            return ReadAll(new[] { fileInfo })[0];
        }

        /// <summary>
        /// 文字コードを自動判別して複数のテキストファイルを読み取る。
        /// </summary>
        /// <param name="filePathes">ファイルパス列挙。</param>
        /// <returns>
        /// 読み取った文字列のリスト。
        /// 読み取れなかったファイルについては null が格納される。
        /// </returns>
        public static List<string> ReadAll(IEnumerable<string> filePathes)
        {
            if (filePathes == null)
            {
                throw new ArgumentNullException(nameof(filePathes));
            }
            if (!filePathes.Any())
            {
                throw new ArgumentException(
                    @"`" + nameof(filePathes) + @"` is empty.",
                    nameof(filePathes));
            }
            if (filePathes.Any(p => p == null))
            {
                throw new ArgumentException(
                    @"Some file pathes in `" + nameof(filePathes) + @"` are null.",
                    nameof(filePathes));
            }

            return ReadAll(filePathes.Select(p => new FileInfo(p)));
        }

        /// <summary>
        /// 文字コードを自動判別して複数のテキストファイルを読み取る。
        /// </summary>
        /// <param name="fileInfos">ファイル情報列挙。</param>
        /// <returns>
        /// 読み取った文字列のリスト。
        /// 読み取れなかったファイルについては null が格納される。
        /// </returns>
        public static List<string> ReadAll(IEnumerable<FileInfo> fileInfos)
        {
            if (fileInfos == null)
            {
                throw new ArgumentNullException(nameof(fileInfos));
            }
            if (!fileInfos.Any())
            {
                throw new ArgumentException(
                    @"`" + nameof(fileInfos) + @"` is empty.",
                    nameof(fileInfos));
            }
            if (fileInfos.Any(info => info == null))
            {
                throw new ArgumentException(
                    @"Some FileInfo in `" + nameof(fileInfos) + @"` are null.",
                    nameof(fileInfos));
            }

            var maxLength = fileInfos.Max(info => info.Length);
            if (maxLength > int.MaxValue)
            {
                throw new ArgumentException(@"Too large file.");
            }

            using (var reader = new Hnx8.ReadJEnc.FileReader((int)maxLength))
            {
                return
                    fileInfos
                        .Select(
                            info =>
                            {
                                reader.Read(info);
                                return reader.Text;
                            })
                        .ToList();
            }
        }
    }
}
