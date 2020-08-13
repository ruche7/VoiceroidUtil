using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RucheHome.Util
{
    /// <summary>
    /// 要素に null 値を含まないコレクションクラス。
    /// </summary>
    /// <typeparam name="T">要素型。</typeparam>
    public class NonNullCollection<T> : Collection<T>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public NonNullCollection() : base()
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="list">ラップ対象のリスト。</param>
        public NonNullCollection(IList<T> list) : base(list)
        {
            if (list.Any(v => v == null))
            {
                throw new ArgumentException(@"Some items are null.", nameof(list));
            }
        }

        /// <summary>
        /// 要素の挿入時に呼び出される。
        /// </summary>
        /// <param name="index">挿入先インデックス。</param>
        /// <param name="item">挿入する要素。</param>
        protected override void InsertItem(int index, T item)
        {
            if (item == null)
            {
                throw new ArgumentException(@"The item is null.", nameof(item));
            }

            base.InsertItem(index, item);
        }

        /// <summary>
        /// 要素の上書き時に呼び出される。
        /// </summary>
        /// <param name="index">上書き先インデックス。</param>
        /// <param name="item">上書きする要素。</param>
        protected override void SetItem(int index, T item)
        {
            if (item == null)
            {
                throw new ArgumentException(@"The item is null.", nameof(item));
            }

            base.SetItem(index, item);
        }
    }
}
