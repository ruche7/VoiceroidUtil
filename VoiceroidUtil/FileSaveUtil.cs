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
    /// VOICEROID+ のファイル保存に関するユーティリティ静的クラス。
    /// </summary>
    public static class FileSaveUtil
    {
        /// <summary>
        /// VOICEROID+ の保存パスとして正常か否かを取得する。
        /// </summary>
        /// <param name="path">調べるパス。</param>
        /// <param name="invalidLetter">
        /// 不正原因文字の設定先。正常な場合や文字不明の場合は null が設定される。
        /// </param>
        /// <returns>正常ならば true 。そうでなければ false 。</returns>
        public static bool IsValidPath(string path, out string invalidLetter)
        {
            invalidLetter = null;

            if (path == null)
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
        /// ファイル名フォーマット種別に従いファイル名を作成する。
        /// </summary>
        /// <param name="format">ファイル名フォーマット種別。</param>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <param name="talkText">トークテキスト。</param>
        /// <returns>ファイル名。拡張子なし。</returns>
        public static string MakeFileName(
            FileNameFormat format,
            VoiceroidId id,
            string talkText)
        {
            var info = id.GetInfo();
            if (info == null)
            {
                throw new InvalidEnumArgumentException(nameof(id), (int)id, id.GetType());
            }

            var text = MakeFileNamePartFromText(talkText);
            var time = DateTime.Now.ToString("yyMMdd_hhmmss");

            var name = text;
            switch (format)
            {
            case FileNameFormat.Text:
                name = text;
                break;
            case FileNameFormat.NameText:
                name = string.Join("_", info.Name, text);
                break;
            case FileNameFormat.ShortNameText:
                name = string.Join("_", info.ShortName, text);
                break;
            case FileNameFormat.DateTimeText:
                name = string.Join("_", time, text);
                break;
            case FileNameFormat.DateTimeNameText:
                name = string.Join("_", time, info.Name, text);
                break;
            case FileNameFormat.DateTimeShortNameText:
                name = string.Join("_", time, info.ShortName, text);
                break;
            case FileNameFormat.TextInNameDirectory:
                name = Path.Combine(info.Name, text);
                break;
            case FileNameFormat.TextInShortNameDirectory:
                name = Path.Combine(info.ShortName, text);
                break;
            case FileNameFormat.DateTimeTextInNameDirectory:
                name = Path.Combine(info.Name, string.Join("_", time, text));
                break;
            case FileNameFormat.DateTimeTextInShortNameDirectory:
                name = Path.Combine(info.ShortName, string.Join("_", time, text));
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
        /// 1文字以上の空白文字にマッチする正規表現。
        /// </summary>
        private static readonly Regex RegexBlank = new Regex(@"\s+");

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
        /// テキストからファイル名の一部を作成する。
        /// </summary>
        /// <param name="text">テキスト。</param>
        /// <param name="maxLength">最大文字数。 1 以上であること。</param>
        /// <returns>作成した文字列。</returns>
        private static string MakeFileNamePartFromText(string text, int maxLength = 12)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (maxLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            var dest = ToCodePage932String(text);

            // 空白文字を半角スペース1文字に短縮
            // ファイル名に使えない文字を置換
            var invalidChars = Path.GetInvalidFileNameChars();
            dest =
                string.Join(
                    "",
                    from c in RegexBlank.Replace(dest, " ")
                    select (Array.IndexOf(invalidChars, c) < 0) ? c : '_');

            // 文字数制限
            if (dest.Length > maxLength)
            {
                dest = dest.Substring(0, maxLength - 1) + "-";
            }

            return dest;
        }
    }
}
