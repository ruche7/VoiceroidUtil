using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROIDのファイルパスに関するユーティリティ静的クラス。
    /// </summary>
    public static class FilePathUtil
    {
        /// <summary>
        /// VOICEROIDの保存パスとして正常か否かを取得する。
        /// </summary>
        /// <param name="path">調べるパス。</param>
        /// <param name="invalidLetter">
        /// 不正原因文字の設定先。正常な場合や文字不明の場合は null が設定される。
        /// </param>
        /// <returns>正常ならば true 。そうでなければ false 。</returns>
        public static bool IsValidPath(string path, out string invalidLetter)
        {
            invalidLetter = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var cp932Path = ToCodePage932String(path);
            if (cp932Path != path)
            {
                var fileElems = new TextElementEnumerable(path);
                var cp932Elems = new TextElementEnumerable(cp932Path);
                invalidLetter =
                    fileElems
                        .Zip(cp932Elems, (e1, e2) => (e1 == e2) ? null : e1)
                        .FirstOrDefault(e => e != null);
                return false;
            }

            return true;
        }

        /// <summary>
        /// VOICEROIDの保存パスとして正常か調べ、結果のアプリ状態を返す。
        /// </summary>
        /// <param name="path">調べるパス。</param>
        /// <param name="pathIsFile">
        /// path がファイルならば true 。ディレクトリならば false 。
        /// </param>
        /// <returns>
        /// 正常ならば StatusType が AppStatusType.None のアプリ状態。
        /// そうでなければ StatusType が AppStatusType.Warning のアプリ状態。
        /// </returns>
        public static IAppStatus CheckPathStatus(string path, bool pathIsFile)
        {
            var status = new AppStatus();

            if (string.IsNullOrWhiteSpace(path))
            {
                status.StatusType = AppStatusType.Warning;
                status.StatusText =
                    pathIsFile ?
                        @"ファイル名が空です。" :
                        @"保存先フォルダーが設定されていません。";
                return status;
            }

            if (RegexRecyclePath.IsMatch(path))
            {
                status.StatusType = AppStatusType.Warning;
                status.StatusText = @"ごみ箱内は保存先フォルダーに設定できません。";
                status.SubStatusText = @"別の保存先を設定してください。";
                return status;
            }

            if (!IsValidPath(path, out string invalidLetter))
            {
                status.StatusType = AppStatusType.Warning;
                status.StatusText = @"VOICEROIDが対応していない保存先フォルダーです。";
                status.SubStatusText =
                    (invalidLetter == null) ?
                        null :
                        @"保存先フォルダーパスに文字 """ +
                        invalidLetter +
                        @""" を含めないでください。";
                return status;
            }

            var filePath =
                pathIsFile ?
                    path : Path.Combine(path, new string('x', MaxFileNameLength));
            try
            {
                Path.GetFullPath(filePath);
            }
            catch (PathTooLongException)
            {
                status.StatusType = AppStatusType.Warning;
                status.StatusText = @"保存先フォルダーのパスが長すぎます。";
                status.SubStatusText = @"別の保存先を設定してください。";
                return status;
            }
            catch
            {
                status.StatusType = AppStatusType.Fail;
                status.StatusText = @"保存先フォルダーが不正です。";
                status.SubStatusText = @"別の保存先を設定してください。";
                return status;
            }

            return status;
        }

        /// <summary>
        /// ファイル名フォーマット種別に従いファイル名を作成する。
        /// </summary>
        /// <param name="format">ファイル名フォーマット種別。</param>
        /// <param name="charaName">キャラ名。</param>
        /// <param name="talkText">トークテキスト。</param>
        /// <returns>ファイル名。拡張子なし。</returns>
        public static string MakeFileName(
            FileNameFormat format,
            string charaName,
            string talkText)
        {
            if (charaName == null)
            {
                throw new ArgumentNullException(nameof(charaName));
            }

            var chara = MakeFileNamePart(charaName, MaxFileNamePartFromCharaNameLength);
            var text = MakeFileNamePart(talkText, MaxFileNamePartFromTextLength);
            var time = DateTime.Now.ToString("yyMMdd_HHmmss");

            string name;
            switch (format)
            {
            case FileNameFormat.Text:
                name = text;
                break;
            case FileNameFormat.DateTimeText:
                name = string.Join("_", time, text);
                break;
            case FileNameFormat.NameText:
                name = string.Join("_", chara, text);
                break;
            case FileNameFormat.DateTimeNameText:
                name = string.Join("_", time, chara, text);
                break;
            case FileNameFormat.TextInNameDirectory:
                name = Path.Combine(chara, text);
                break;
            case FileNameFormat.DateTimeTextInNameDirectory:
                name = Path.Combine(chara, string.Join("_", time, text));
                break;
            default:
                throw new InvalidEnumArgumentException(
                    nameof(format),
                    (int)format,
                    format.GetType());
            }

            return name;
        }

        /// <summary>
        /// キャラ名から作成されるファイル名の一部の最大文字数。
        /// </summary>
        private const int MaxFileNamePartFromCharaNameLength = 16;

        /// <summary>
        /// テキストから作成されるファイル名の一部の最大文字数。
        /// </summary>
        private const int MaxFileNamePartFromTextLength = 13;

        /// <summary>
        /// 作成されるファイル名の最大文字数。
        /// </summary>
        private static readonly int MaxFileNameLength =
            @"20010101_000000_".Length +
            MaxFileNamePartFromCharaNameLength +
            @"_".Length +
            MaxFileNamePartFromTextLength +
            @".wav".Length;

        /// <summary>
        /// 1文字以上の空白文字にマッチする正規表現。
        /// </summary>
        private static readonly Regex RegexBlank = new Regex(@"\s+");

        /// <summary>
        /// ごみ箱およびそれ以下のファイルやディレクトリのパスにマッチする正規表現。
        /// </summary>
        private static readonly Regex RegexRecyclePath =
            new Regex(@"^[a-z]+:[\\/]\$RECYCLE.BIN\b.*$", RegexOptions.IgnoreCase);

        /// <summary>
        /// CodePage932 エンコーディング。
        /// </summary>
        private static readonly Encoding CodePage932 = Encoding.GetEncoding(932);

        /// <summary>
        /// CodePage932 エンコーディングで表現可能な文字列に変換する。
        /// </summary>
        /// <param name="src">文字列。</param>
        /// <returns>CodePage932 エンコーディングで表現可能な文字列。</returns>
        private static string ToCodePage932String(string src)
        {
            return new string(CodePage932.GetChars(CodePage932.GetBytes(src)));
        }

        /// <summary>
        /// 文字列からファイル名の一部を作成する。
        /// </summary>
        /// <param name="src">文字列。</param>
        /// <param name="maxLength">最大文字数。 3 以上にすること。</param>
        /// <returns>作成した文字列。</returns>
        /// <remarks>
        /// テキストの文字数が maxLength を超える場合、末尾に "+残り文字数" が付与され、
        /// それと合わせて文字数が maxLength となるようにテキストが削られる。
        /// "+残り文字数" を入れる余裕がない場合は代わりに "-" が付与される。
        /// </remarks>
        private static string MakeFileNamePart(string src, int maxLength)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }
            if (maxLength < 3)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxLength),
                    maxLength,
                    nameof(maxLength) + @" is less than 3.");
            }

            // CP932で表せない文字はVOICEROIDで扱えないため変換
            var dest = ToCodePage932String(src);

            // 空白文字をアンダーバー1文字に短縮
            // ファイル名に使えない文字を 'x' に置換
            var invalidChars = Path.GetInvalidFileNameChars();
            dest =
                string.Join(
                    "",
                    from c in RegexBlank.Replace(dest, "_")
                    select (Array.IndexOf(invalidChars, c) < 0) ? c : 'x');

            if (dest.Length <= maxLength)
            {
                // 無いはずだが、空文字列になってしまったらアンダーバー1文字とする
                return (dest.Length > 0) ? dest : "_";
            }

            // "+残り文字数" 付与を試みる
            for (int len = maxLength - 2; len > 0; --len)
            {
                var tail = @"+" + (dest.Length - len);
                if (len + tail.Length <= maxLength)
                {
                    return dest.Substring(0, len) + tail;
                }
            }

            // "-" を付与して返す
            return dest.Substring(0, maxLength - 1) + @"-";
        }
    }
}
