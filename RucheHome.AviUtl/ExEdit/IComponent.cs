using System;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// レイヤーアイテムのコンポーネントインタフェース。
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        string ComponentName { get; }

        /// <summary>
        /// このコンポーネントを拡張編集オブジェクトファイルのセクション形式に変換する。
        /// </summary>
        /// <param name="name">セクション名。</param>
        /// <returns>セクションデータ。</returns>
        IniFileSection ToExoFileSection(string name);
    }
}
