using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Util.Extensions.String;

namespace VoiceroidUtil
{
    /// <summary>
    /// AviUtl拡張編集ファイル用のテキストスタイル雛形クラス。
    /// </summary>
    public class ExoTextStyleTemplate : IEquatable<ExoTextStyleTemplate>
    {
        /// <summary>
        /// テキストコンポーネントから
        /// AviUtl拡張編集ファイル用設定で利用されないパラメータをクリアする。
        /// </summary>
        /// <param name="target">テキストコンポーネント。</param>
        public static void ClearUnused(TextComponent target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.IsAutoAdjusting = false;
            target.IsAutoScrolling = false;
            target.Text = "";
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="render">標準描画コンポーネントのコピー元。</param>
        /// <param name="text">テキストコンポーネントのコピー元。</param>
        /// <param name="textClipping">
        /// テキストを1つ上のオブジェクトでクリッピングするならば true 。
        /// </param>
        /// <param name="withoutUnused">
        /// AviUtl拡張編集ファイル用設定で利用されないパラメータをクリアするならば true 。
        /// </param>
        public ExoTextStyleTemplate(
            RenderComponent render,
            TextComponent text,
            bool textClipping,
            bool withoutUnused = false)
        {
            if (render == null)
            {
                throw new ArgumentNullException(nameof(render));
            }
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            this.Render = render.Clone();
            this.Text = text.Clone();
            this.IsTextClipping = textClipping;

            if (withoutUnused)
            {
                this.ClearUnused();
            }
        }

        /// <summary>
        /// コピーコンストラクタ。
        /// </summary>
        /// <param name="src">コピー元。</param>
        /// <param name="withoutUnused">
        /// AviUtl拡張編集ファイル用設定で利用されないパラメータをクリアするならば true 。
        /// </param>
        public ExoTextStyleTemplate(ExoTextStyleTemplate src, bool withoutUnused = false)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            this.Render = src.Render.Clone();
            this.Text = src.Text.Clone();
            this.IsTextClipping = src.IsTextClipping;

            if (withoutUnused)
            {
                this.ClearUnused();
            }
        }

        /// <summary>
        /// 標準描画コンポーネントを取得する。
        /// </summary>
        public RenderComponent Render { get; }

        /// <summary>
        /// テキストコンポーネントを取得する。
        /// </summary>
        public TextComponent Text { get; }

        /// <summary>
        /// テキストを1つ上のオブジェクトでクリッピングするか否かを取得する。
        /// </summary>
        public bool IsTextClipping { get; }

        /// <summary>
        /// 説明文を取得する。
        /// </summary>
        public string Description
        {
            get
            {
                var text = this.Text.Text?.Trim();

                if (string.IsNullOrEmpty(text))
                {
                    var c = this.Text.FontColor;
                    text = $@"RGB({c.R},{c.G},{c.B})";
                }
                else
                {
                    text = RegexBlank.Replace(text, @" ");
                    if (text.Length > MaxDescriptionLength)
                    {
                        text =
                            text.SubstringSurrogateSafe(0, MaxDescriptionLength - 1) +
                            @"…";
                    }
                }

                return text;
            }
        }

        /// <summary>
        /// このオブジェクトの内容を ExoCharaStyle オブジェクトへ上書きする。
        /// </summary>
        /// <param name="target">上書き対象の ExoCharaStyle オブジェクト。</param>
        /// <param name="withoutUnused">
        /// AviUtl拡張編集ファイル用設定で利用されないパラメータを無視するならば true 。
        /// </param>
        public void CopyTo(ExoCharaStyle target, bool withoutUnused = false)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var src =
                (!withoutUnused || this.IsUnusedCleared) ?
                    this : new ExoTextStyleTemplate(this);

            target.Render = src.Render.Clone();
            target.Text = src.Text.Clone();
            target.IsTextClipping = src.IsTextClipping;
        }

        /// <summary>
        /// このオブジェクトと他のオブジェクトが、AviUtl拡張編集ファイル用設定で
        /// 利用されないパラメータ以外等価であるか否かを調べる。
        /// </summary>
        /// <param name="other">調べるオブジェクト。</param>
        /// <returns>等価ならば true 。そうでなければ false 。</returns>
        public bool EqualsWithoutUnused(ExoTextStyleTemplate other)
        {
            if (other == null)
            {
                return false;
            }

            var c1 =
                this.IsUnusedCleared ?
                    this : new ExoTextStyleTemplate(this, withoutUnused: true);
            var c2 =
                other.IsUnusedCleared ?
                    other : new ExoTextStyleTemplate(other, withoutUnused: true);

            return c1.Equals(c2);
        }

        /// <summary>
        /// MaxDescriptionLength プロパティの最大文字数。
        /// </summary>
        private const int MaxDescriptionLength = 20;

        /// <summary>
        /// 1文字以上の空白文字にマッチする正規表現。
        /// </summary>
        private static readonly Regex RegexBlank = new Regex(@"\s+");

        /// <summary>
        /// ComponentBase 派生クラス型オブジェクトが等価であるか否かを調べる。
        /// </summary>
        /// <typeparam name="T">ComponentBase 派生クラス型。</typeparam>
        /// <param name="c1">調べるオブジェクト1。</param>
        /// <param name="c2">調べるオブジェクト2。</param>
        /// <returns>等価ならば true 。そうでなければ false 。</returns>
        private static bool EqualsComponent<T>(T c1, T c2)
            where T : ComponentBase
            =>
            (c1 == c2) ||
            typeof(T)
                .GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Where(
                    p =>
                        p.CanRead &&
                        p.IsDefined(typeof(ExoFileItemAttribute), true))
                .All(
                    prop =>
                    {
                        var v1 = prop.GetMethod.Invoke(c1, null);
                        var v2 = prop.GetMethod.Invoke(c2, null);
                        return
                            ((v1 is IMovableValue mv1) && (v2 is IMovableValue mv2)) ?
                                EqualsMovableValue(mv1, mv2) :
                                ((v1 == v2) || v1?.Equals(v2) == true);
                    });

        /// <summary>
        /// IMovableValue オブジェクトが等価であるか否かを調べる。
        /// </summary>
        /// <param name="v1">調べるオブジェクト1。</param>
        /// <param name="v2">調べるオブジェクト2。</param>
        /// <returns>等価ならば true 。そうでなければ false 。</returns>
        private static bool EqualsMovableValue(IMovableValue v1, IMovableValue v2)
        {
            if (v1 == v2)
            {
                return true;
            }
            if (v1 == null || v2 == null)
            {
                return false;
            }

            return
                v1.Begin == v2.Begin &&
                v1.End == v2.End &&
                v1.MoveMode == v2.MoveMode &&
                v1.IsAccelerating == v2.IsAccelerating &&
                v1.IsDecelerating == v2.IsDecelerating &&
                v1.Interval == v2.Interval;
        }

        /// <summary>
        /// AviUtl拡張編集ファイル用設定で利用されないパラメータが
        /// クリアされているか否かを取得する。
        /// </summary>
        private bool IsUnusedCleared =>
            this.Text.IsAutoAdjusting == false &&
            this.Text.IsAutoScrolling == false &&
            this.Text.Text == "";

        /// <summary>
        /// AviUtl拡張編集ファイル用設定で利用されないパラメータをクリアする。
        /// </summary>
        private void ClearUnused() => ClearUnused(this.Text);

        #region Object のオーバライド

        /// <summary>
        /// このオブジェクトと他のオブジェクトが等価であるか否かを調べる。
        /// </summary>
        /// <param name="obj">調べるオブジェクト。</param>
        /// <returns>等価ならば true 。そうでなければ false 。</returns>
        public override bool Equals(object obj) =>
            this.Equals(obj as ExoTextStyleTemplate);

        /// <summary>
        /// このオブジェクトのハッシュコード値を取得する。
        /// </summary>
        /// <returns>ハッシュコード値。</returns>
        public override int GetHashCode() =>
            this.Render.X.GetHashCode() ^
            this.Text.FontColor.GetHashCode() ^
            this.IsTextClipping.GetHashCode();

        #endregion

        #region IEquatable<ExoTextStyleTemplate> の実装

        /// <summary>
        /// このオブジェクトと他のオブジェクトが等価であるか否かを調べる。
        /// </summary>
        /// <param name="other">調べるオブジェクト。</param>
        /// <returns>等価ならば true 。そうでなければ false 。</returns>
        public bool Equals(ExoTextStyleTemplate other) =>
            other != null &&
            this.IsTextClipping == other.IsTextClipping &&
            EqualsComponent(this.Render, other.Render) &&
            EqualsComponent(this.Text, other.Text);

        #endregion
    }
}
