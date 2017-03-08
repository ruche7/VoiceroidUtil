using System;
using System.Linq;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// VoiceroidUtilが認識しないコンポーネントを表すクラス。
    /// </summary>
    public class UnknownComponent : IComponent, ICloneable
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public UnknownComponent()
        {
        }

        /// <summary>
        /// コピーコンストラクタ。
        /// </summary>
        /// <param name="src">コピー元。</param>
        public UnknownComponent(UnknownComponent src)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            this.Items = src.Items.Clone();
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
            get => this.items;
            set => this.items = value ?? new IniFileItemCollection();
        }
        private IniFileItemCollection items = new IniFileItemCollection();

        /// <summary>
        /// このコンポーネントを
        /// 拡張編集オブジェクトファイルのアイテムコレクションに変換する。
        /// </summary>
        /// <returns>アイテムコレクション。</returns>
        public IniFileItemCollection ToExoFileItems() => this.Items.Clone();

        /// <summary>
        /// このコンポーネントのクローンを作成する。
        /// </summary>
        /// <returns>クローン。</returns>
        public UnknownComponent Clone() => new UnknownComponent(this);

        #region ICloneable の明示的実装

        /// <summary>
        /// このオブジェクトのクローンを作成する。
        /// </summary>
        /// <returns>クローン。</returns>
        object ICloneable.Clone() => this.Clone();

        #endregion
    }
}
