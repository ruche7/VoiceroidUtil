using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;

namespace RucheHome.AviUtl
{
    /// <summary>
    /// AviUtlで使われるフォントファミリ名列挙を提供するクラス。
    /// </summary>
    public class FontFamilyNameEnumerable : IEnumerable<string>
    {
        /// <summary>
        /// コンストラクタ。フォントファミリ名でソートする。
        /// </summary>
        public FontFamilyNameEnumerable() : this(true)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="sorting">フォントファミリ名でソートするならば true 。</param>
        public FontFamilyNameEnumerable(bool sorting) => this.IsSorting = sorting;

        /// <summary>
        /// フォントコレクションを取得する。
        /// </summary>
        private InstalledFontCollection Fonts { get; } = new InstalledFontCollection();

        /// <summary>
        /// フォントファミリ名でソートするか否かを取得する。
        /// </summary>
        private bool IsSorting { get; }

        #region IEnumerable<string> の実装

        /// <summary>
        /// フォントファミリ名の列挙子を取得する。
        /// </summary>
        /// <returns>フォントファミリ名の列挙子。</returns>
        public IEnumerator<string> GetEnumerator()
        {
            var e =
                this.Fonts.Families
                    .Select(f => f.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n));
            return this.IsSorting ? e.OrderBy(n => n).GetEnumerator() : e.GetEnumerator();
        }

        #endregion

        #region IEnumerable の明示的実装

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion
    }
}
