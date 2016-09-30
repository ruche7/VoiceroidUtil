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
        /// このコンポーネントを
        /// 拡張編集オブジェクトファイルのアイテムコレクションに変換する。
        /// </summary>
        /// <returns>アイテムコレクション。</returns>
        public IniFileItemCollection ToExoFileItems() => this.Items.Clone();
    }
}
