using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace RucheHome.Util
{
    /// <summary>
    /// サロゲートペアを考慮した文字列挙を提供するクラス。
    /// </summary>
    public class TextElementEnumerable : IEnumerable<string>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="text">対象文字列。</param>
        public TextElementEnumerable(string text) => this.Text = text;

        /// <summary>
        /// 対象文字列を取得する。
        /// </summary>
        public string Text { get; }

        #region IEnumerable<string> の実装

        /// <summary>
        /// 文字列挙子を取得する。
        /// </summary>
        /// <returns>文字列挙子。</returns>
        public IEnumerator<string> GetEnumerator()
        {
            for (var e = StringInfo.GetTextElementEnumerator(this.Text); e.MoveNext(); )
            {
                yield return e.GetTextElement();
            }
        }

        #endregion

        #region IEnumerable の明示的実装

        IEnumerator IEnumerable.GetEnumerator() =>
            StringInfo.GetTextElementEnumerator(this.Text);

        #endregion
    }
}
