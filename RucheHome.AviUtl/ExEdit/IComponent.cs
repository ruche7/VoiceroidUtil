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
        /// このコンポーネントを
        /// 拡張編集オブジェクトファイルのアイテムコレクションに変換する。
        /// </summary>
        /// <returns>アイテムコレクション。</returns>
        IniFileItemCollection ToExoFileItems();
    }
}
