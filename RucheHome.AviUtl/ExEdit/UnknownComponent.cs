using System;
using System.Linq;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// VoiceroidUtilが認識しないコンポーネントを表すクラス。
    /// </summary>
    public class UnknownComponent : IComponent
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public UnknownComponent()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        public string ComponentName =>
            this.Items.FirstOrDefault(item => item?.Name == @"_name").Value ?? "";

        /// <summary>
        /// アイテムコレクションを取得または設定する。
        /// </summary>
        public IniFileItemCollection Items
        {
            get { return this.items; }
            set { this.items = value ?? new IniFileItemCollection(); }
        }
        private IniFileItemCollection items = new IniFileItemCollection();

        /// <summary>
        /// このコンポーネントを拡張編集オブジェクトファイルのセクション形式に変換する。
        /// </summary>
        /// <param name="name">セクション名。</param>
        /// <returns>セクションデータ。</returns>
        public IniFileSection ToExoFileSection(string name) =>
            new IniFileSection(name, this.Items);
    }
}
