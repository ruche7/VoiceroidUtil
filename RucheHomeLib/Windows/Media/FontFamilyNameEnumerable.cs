using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace RucheHome.Windows.Media
{
    /// <summary>
    /// フォントファミリ名列挙を提供するクラス。
    /// </summary>
    public class FontFamilyNameEnumerable : IEnumerable<string>
    {
        /// <summary>
        /// CultureInfo.CurrentCulture でローカライズされるフォントファミリ名列挙。
        /// </summary>
        public static readonly FontFamilyNameEnumerable Current =
            new FontFamilyNameEnumerable(CultureInfo.CurrentCulture);

        /// <summary>
        /// コンストラクタ。ローカライズは行わない。
        /// </summary>
        public FontFamilyNameEnumerable() => this.Language = null;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="culture">
        /// ローカライズ対象のカルチャ情報。ローカライズを行わないならば null 。
        /// </param>
        public FontFamilyNameEnumerable(CultureInfo culture)
            =>
            this.Language =
                (culture == null) ? null : XmlLanguage.GetLanguage(culture.IetfLanguageTag);

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="language">
        /// ローカライズに用いる言語。ローカライズを行わないならば null 。
        /// </param>
        public FontFamilyNameEnumerable(XmlLanguage language) => this.Language = language;

        /// <summary>
        /// フォントファミリ名のローカライズに用いる言語を取得する。
        /// </summary>
        /// <returns>言語。ローカライズを行わないならば null 。</returns>
        public XmlLanguage Language { get; }

        #region IEnumerable<string> の実装

        /// <summary>
        /// フォントファミリ名の列挙子を取得する。
        /// </summary>
        /// <returns>フォントファミリ名の列挙子。</returns>
        public IEnumerator<string> GetEnumerator() =>
            (this.Language == null) ?
                Fonts.SystemFontFamilies.Select(f => f.Source).GetEnumerator() :
                Fonts.SystemFontFamilies
                    .Select(
                        f =>
                            f.FamilyNames.TryGetValue(this.Language, out var name) ?
                                name : f.Source)
                    .GetEnumerator();

        #endregion

        #region IEnumerable の明示的実装

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion
    }
}
