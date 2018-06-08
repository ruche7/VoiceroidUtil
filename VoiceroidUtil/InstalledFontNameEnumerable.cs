using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;

namespace VoiceroidUtil
{
    /// <summary>
    /// AviUtl用のフォント名列挙を提供するクラス。
    /// </summary>
    class InstalledFontNameEnumerable : IEnumerable<string>
    {
        /// <summary>
        /// Singletonインスタンス
        /// </summary>
        public static readonly InstalledFontNameEnumerable Instance =
            new InstalledFontNameEnumerable();

        /// <summary>
        /// 隠蔽されたコンストラクタ
        /// </summary>
        private InstalledFontNameEnumerable()
        {

        }

        /// <summary>
        /// フォン名の列挙子を取得する
        /// </summary>
        /// <returns>フォント名の列挙子</returns>
        public IEnumerator<string> GetEnumerator()
        {
            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            return installedFontCollection.Families.Select(f => f.Name).Where(n => !String.IsNullOrWhiteSpace(n)).GetEnumerator();
        }

        #region IEnumerable の明示的実装

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

    }
}
