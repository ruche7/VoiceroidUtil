using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace VoiceroidUtil
{
    /// <summary>
    /// サロゲートペアを考慮した文字列挙を提供するクラス。
    /// </summary>
    public class TextElementEnumerable : IEnumerable<string>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="src">対象文字列。</param>
        public TextElementEnumerable(string src)
        {
            this.Enumerator = StringInfo.GetTextElementEnumerator(src);
        }

        /// <summary>
        /// TextElementEnumerator オブジェクト。
        /// </summary>
        private TextElementEnumerator Enumerator { get; }

        #region IEnumerable<string> の実装

        public IEnumerator<string> GetEnumerator()
        {
            while (this.Enumerator.MoveNext())
            {
                yield return this.Enumerator.GetTextElement();
            }
        }

        #endregion

        #region IEnumerable の明示的実装

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Enumerator;
        }

        #endregion
    }
}
