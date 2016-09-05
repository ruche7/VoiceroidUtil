using System;
using System.Collections.Generic;
using System.Linq;
using RucheHome.Util;
using RucheHome.Util.Extensions.String;

namespace VoiceroidUtil
{
    /// <summary>
    /// TalkTextReplaceItem インスタンスコレクションクラス。
    /// </summary>
    public class TalkTextReplaceItemCollection : BindableCollection<TalkTextReplaceItem>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TalkTextReplaceItemCollection() : base()
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="items">コレクションの初期値となるアイテム列挙。</param>
        public TalkTextReplaceItemCollection(IEnumerable<TalkTextReplaceItem> items)
            : base(items)
        {
        }

        /// <summary>
        /// 現在のアイテム群を用いて文字列を置換する。
        /// </summary>
        /// <param name="src">置換対象文字列。</param>
        /// <returns>置換後の文字列。</returns>
        public string Replace(string src)
        {
            if (src != null)
            {
                var items = this.Where(i => i.IsAvailable());
                if (items.Any())
                {
                    return
                        src.Replace(
                            items.Select(i => i.OldValue),
                            items.Select(i => i.NewValue));
                }
            }

            return src;
        }

        #region BindableCollection<TalkTextReplaceItem> のオーバライド

        protected override void InsertItem(int index, TalkTextReplaceItem item)
        {
            // null は無視
            if (item != null)
            {
                base.InsertItem(index, item);
            }
        }

        protected override void SetItem(int index, TalkTextReplaceItem item)
        {
            // null は無視
            if (item != null)
            {
                base.SetItem(index, item);
            }
        }

        #endregion
    }
}
