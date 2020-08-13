using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RucheHome.Util
{
    /// <summary>
    /// プロパティ変更通知をサポートするオブジェクトのコレクションクラス。
    /// </summary>
    /// <typeparam name="TItem">
    /// コレクション要素型。 INotifyPropertyChanged インタフェースを実装する必要がある。
    /// </typeparam>
    public class BindableCollection<TItem> : ObservableCollection<TItem>
        where TItem : INotifyPropertyChanged
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public BindableCollection() : base()
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="items">コレクションの初期値となるアイテム列挙。</param>
        public BindableCollection(IEnumerable<TItem> items) : base(items)
        {
            foreach (var item in this)
            {
                if (item != null)
                {
                    item.PropertyChanged += this.OnItemPropertyChanged;
                }
            }
        }

        /// <summary>
        /// コレクション要素のプロパティ値が変更された時に呼び出されるイベント。
        /// </summary>
        public event PropertyChangedEventHandler ItemPropertyChanged = null;

        /// <summary>
        /// 要素の挿入時に呼び出される。
        /// </summary>
        /// <param name="index">挿入先インデックス。</param>
        /// <param name="item">挿入する要素。</param>
        protected override void InsertItem(int index, TItem item)
        {
            if (item != null)
            {
                item.PropertyChanged += this.OnItemPropertyChanged;
            }

            base.InsertItem(index, item);
        }

        /// <summary>
        /// 要素の上書き時に呼び出される。
        /// </summary>
        /// <param name="index">上書き先インデックス。</param>
        /// <param name="item">上書きする要素。</param>
        protected override void SetItem(int index, TItem item)
        {
            var oldItem = this[index];
            if (oldItem != null)
            {
                oldItem.PropertyChanged -= this.OnItemPropertyChanged;
            }
            if (item != null)
            {
                item.PropertyChanged += this.OnItemPropertyChanged;
            }

            base.SetItem(index, item);
        }

        /// <summary>
        /// 要素の削除時に呼び出される。
        /// </summary>
        /// <param name="index">削除先インデックス。</param>
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];
            if (oldItem != null)
            {
                oldItem.PropertyChanged -= this.OnItemPropertyChanged;
            }

            base.RemoveItem(index);
        }

        /// <summary>
        /// 要素のクリア時に呼び出される。
        /// </summary>
        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                if (item != null)
                {
                    item.PropertyChanged -= this.OnItemPropertyChanged;
                }
            }

            base.ClearItems();
        }

        /// <summary>
        /// コレクション要素のプロパティ値が変更された時に呼び出される。
        /// </summary>
        /// <param name="sender">コレクション要素。</param>
        /// <param name="e">プロパティ値変更イベントパラメータ。</param>
        protected virtual void OnItemPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
            =>
            this.ItemPropertyChanged?.Invoke(sender, e);
    }
}
