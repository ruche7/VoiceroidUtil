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
        /// コンストラクタ。
        /// </summary>
        public ExoTextStyleTemplate()
        {
        }

        /// <summary>
        /// 標準描画コンポーネントを取得または設定する。
        /// </summary>
        public RenderComponent Render
        {
            get { return this.render; }
            set { this.render = value ?? new RenderComponent(); }
        }
        private RenderComponent render = new RenderComponent();

        /// <summary>
        /// テキストコンポーネントを取得または設定する。
        /// </summary>
        public TextComponent Text
        {
            get { return this.text; }
            set { this.text = value ?? new TextComponent(); }
        }
        private TextComponent text = new TextComponent();

        /// <summary>
        /// テキストを1つ上のオブジェクトでクリッピングするか否かを取得または設定する。
        /// </summary>
        public bool IsTextClipping { get; set; } = false;

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
                    text = $"RGB({c.R},{c.G},{c.B})";
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
        /// このオブジェクトの内容を別のオブジェクトへ上書きする。
        /// </summary>
        /// <param name="target">上書き対象。</param>
        /// <param name="withoutText">テキストを上書きしないならば true 。</param>
        public void CopyTo(ExoTextStyleTemplate target, bool withoutText = false)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            this.Render.CopyTo(target.Render);

            var oldText = target.Text.Text;
            this.Text.CopyTo(target.Text);
            if (withoutText)
            {
                target.Text.Text = oldText;
            }

            target.IsTextClipping = this.IsTextClipping;
        }

        /// <summary>
        /// このオブジェクトの内容を ExoCharaStyle オブジェクトへ上書きする。
        /// </summary>
        /// <param name="target">上書き対象の ExoCharaStyle オブジェクト。</param>
        /// <param name="withoutText">テキストを上書きしないならば true 。</param>
        public void CopyTo(ExoCharaStyle target, bool withoutText = false)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            this.Render.CopyTo(target.Render);

            var oldText = target.Text.Text;
            this.Text.CopyTo(target.Text);
            if (withoutText)
            {
                target.Text.Text = oldText;
            }

            target.IsTextClipping = this.IsTextClipping;
        }

        /// <summary>
        /// このオブジェクトと他のオブジェクトがテキスト以外等価であるか否かを調べる。
        /// </summary>
        /// <param name="other">調べるオブジェクト。</param>
        /// <returns>テキスト以外等価ならば true 。そうでなければ false 。</returns>
        public bool EqualsWithoutText(ExoTextStyleTemplate other)
        {
            if (other == null)
            {
                return false;
            }

            var c1 = this;
            if (this.Text.Text != "")
            {
                c1 = new ExoTextStyleTemplate();
                this.CopyTo(c1, true);
            }

            var c2 = other;
            if (other.Text.Text != "")
            {
                c2 = new ExoTextStyleTemplate();
                other.CopyTo(c2, true);
            }

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
        {
            var props =
                typeof(T)
                    .GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .Where(
                        p =>
                            p.CanRead &&
                            p.IsDefined(typeof(ExoFileItemAttribute), true));

            foreach (var prop in props)
            {
                var v1 = prop.GetMethod.Invoke(c1, null);
                var v2 = prop.GetMethod.Invoke(c2, null);
                if (v1?.Equals(v2) != true && (v1 != null || v2 != null))
                {
                    return false;
                }
            }

            return true;
        }

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
