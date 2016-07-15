using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Data;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROID識別IDに紐付くアイテムセットを保持するジェネリッククラス。
    /// </summary>
    /// <typeparam name="TItem">
    /// VOICEROID識別IDに紐付くアイテムの型。
    /// INotifyPropertyChanged インタフェースを実装し、
    /// VOICEROID識別IDを受け取るコンストラクタと
    /// VOICEROID識別IDを返すパブリックな VoiceroidId プロパティを持つ必要がある。
    /// </typeparam>
    /// <remarks>
    /// 内容が変更されると、インデクサを対象として PropertyChanged イベントが発生する。
    /// </remarks>
    [DataContract(Namespace = "")]
    public abstract class VoiceroidItemSetBase<TItem> : BindableBase, IEnumerable<TItem>
        where TItem : INotifyPropertyChanged
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidItemSetBase()
        {
            // イベントハンドラ追加のためにプロパティ経由で設定
            this.Table = new InnerList();
        }

        /// <summary>
        /// VOICEROID識別IDに対応するアイテムを取得するインデクサ。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        public TItem this[VoiceroidId id]
        {
            get { return this.GetItem(id); }
        }

        /// <summary>
        /// 列挙子を取得する。
        /// </summary>
        /// <returns>列挙子。</returns>
        public IEnumerator<TItem> GetEnumerator()
        {
            foreach (VoiceroidId id in Enum.GetValues(typeof(VoiceroidId)))
            {
                yield return this.GetItem(id);
            }
        }

        /// <summary>
        /// VOICEROID識別IDに対応する TItem インスタンスを取得する。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <returns>TItem インスタンス。</returns>
        protected TItem GetItem(VoiceroidId id)
        {
            TItem item;

            int index = this.Table.IndexOf(id);

            if (index < 0)
            {
                // 有効なIDか？
                var name = id.GetInfo()?.Name;
                if (name == null)
                {
                    throw new InvalidEnumArgumentException(
                        nameof(id),
                        (int)id,
                        id.GetType());
                }

                // アイテムを作成して追加
                item = (TItem)Activator.CreateInstance(typeof(TItem), id);
                this.Table.Add(item);
            }
            else
            {
                // 取得
                item = this.Table[index];
            }

            return item;
        }

        /// <summary>
        /// VOICEROID識別IDに対応する TItem インスタンスを設定する。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <param name="item">TItem インスタンス。</param>
        protected void SetItem(VoiceroidId id, TItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (GetId(item) != id)
            {
                throw new ArgumentException(
                    nameof(id) + @" != " + nameof(item) + @"." + nameof(VoiceroidId));
            }

            int index = this.Table.IndexOf(id);

            if (index < 0)
            {
                // 有効なIDか？
                var name = id.GetInfo()?.Name;
                if (name == null)
                {
                    throw new InvalidEnumArgumentException(
                        nameof(item) + "." + nameof(VoiceroidId),
                        (int)id,
                        id.GetType());
                }

                // 新規追加
                this.Table.Add(item);
            }
            else
            {
                // 更新
                this.Table[index] = item;
            }
        }

        /// <summary>
        /// 内部リストクラス。
        /// </summary>
        private class InnerList : ObservableCollection<TItem>
        {
            /// <summary>
            /// 指定したVOICEROID識別IDを持つ要素が存在するか否かを取得する。
            /// </summary>
            /// <param name="id">VOICEROID識別ID。</param>
            /// <returns></returns>
            public bool Contains(VoiceroidId id)
            {
                return this.Any(item => GetId(item) == id);
            }

            /// <summary>
            /// 指定したVOICEROID識別IDを持つ要素のインデックスを取得する。
            /// </summary>
            /// <param name="id">VOICEROID識別ID。</param>
            /// <returns>インデックス。見つからなければ -1 。</returns>
            public int IndexOf(VoiceroidId id)
            {
                for (int i = 0; i < this.Count; ++i)
                {
                    if (GetId(this[i]) == id)
                    {
                        return i;
                    }
                }
                return -1;
            }

            #region Collection<TItem> のオーバライド

            protected override void InsertItem(int index, TItem item)
            {
                if (item == null)
                {
                    return;
                }

                // ID重複なら無視
                if (this.Contains(GetId(item)))
                {
                    return;
                }

                base.InsertItem(index, item);
            }

            protected override void SetItem(int index, TItem item)
            {
                if (item == null)
                {
                    return;
                }

                // ID重複なら無視
                var id = GetId(item);
                if (GetId(this[index]) != id && this.Contains(id))
                {
                    return;
                }

                base.SetItem(index, item);
            }

            #endregion
        }

        /// <summary>
        /// アイテムの VoiceroidId プロパティからVOICEROID識別ID値を取得する。
        /// </summary>
        /// <param name="item">アイテム。</param>
        /// <returns>VOICEROID識別ID値。</returns>
        private static VoiceroidId GetId(TItem item)
        {
            return ((dynamic)item).VoiceroidId;
        }

        /// <summary>
        /// 内部リストを取得または設定する。
        /// </summary>
        [DataMember]
        private InnerList Table
        {
            get { return this.table; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value != this.table)
                {
                    this.RemoveEventHandlersToTable();
                    this.table = value;
                    this.AddEventHandlersToTable();

                    // インデクサ変更通知
                    this.RaiseIndexerPropertyChanged();
                }
            }
        }
        private InnerList table = null;

        /// <summary>
        /// インデクサを対象として PropertyChanged イベントを発生させる。
        /// </summary>
        private void RaiseIndexerPropertyChanged()
        {
            this.RaisePropertyChanged(Binding.IndexerName);
        }

        /// <summary>
        /// 現在の内部リストにイベントハンドラを追加する。
        /// </summary>
        private void AddEventHandlersToTable()
        {
            if (this.Table != null)
            {
                this.Table.CollectionChanged += this.OnTableCollectionChanged;
                foreach (var item in this.Table)
                {
                    item.PropertyChanged += this.OnTableItemPropertyChanged;
                }
            }
        }

        /// <summary>
        /// 現在の内部リストからイベントハンドラを削除する。
        /// </summary>
        private void RemoveEventHandlersToTable()
        {
            if (this.Table != null)
            {
                this.Table.CollectionChanged -= this.OnTableCollectionChanged;
                foreach (var item in this.Table)
                {
                    item.PropertyChanged -= this.OnTableItemPropertyChanged;
                }
            }
        }

        /// <summary>
        /// 内部リストのコレクション内容変更時に呼び出される。
        /// </summary>
        private void OnTableCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            bool existsOld = false, existsNew = false;

            // 処理対象決定
            switch (e.Action)
            {
            case NotifyCollectionChangedAction.Add:
                existsNew = true;
                break;

            case NotifyCollectionChangedAction.Remove:
                existsOld = true;
                break;

            case NotifyCollectionChangedAction.Replace:
                existsOld = true;
                existsNew = true;
                break;

            case NotifyCollectionChangedAction.Move:
                // アイテムの追加削除はないので処理不要
                return;

            case NotifyCollectionChangedAction.Reset:
                // ここに来ないような実装にする
                Debug.Assert(false);
                return;
            }

            // イベントハンドラの削除と追加
            if (existsOld && e.OldItems != null)
            {
                foreach (TItem item in e.OldItems)
                {
                    item.PropertyChanged -= this.OnTableItemPropertyChanged;
                }
            }
            if (existsNew && e.NewItems != null)
            {
                foreach (TItem item in e.NewItems)
                {
                    item.PropertyChanged += this.OnTableItemPropertyChanged;
                }
            }

            // インデクサ変更通知
            this.RaiseIndexerPropertyChanged();
        }

        /// <summary>
        /// 内部リストのアイテム内容変更時に呼び出される。
        /// </summary>
        private void OnTableItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // インデクサ変更通知
            this.RaiseIndexerPropertyChanged();
        }

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            // null 回避
            this.Table = new InnerList();
        }

        #region IEnumerable の明示的実装

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion
    }
}
