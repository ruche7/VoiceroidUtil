using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// YmmCharaRelation インスタンスセットクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    [KnownType(typeof(YmmCharaRelation))]
    public class YmmCharaRelationSet : IEnumerable<YmmCharaRelation>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public YmmCharaRelationSet()
        {
        }

        /// <summary>
        /// VOICEROID識別IDから YmmCharaRelation インスタンスを取得するインデクサ。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <returns>YmmCharaRelation インスタンス。</returns>
        public YmmCharaRelation this[VoiceroidId id]
        {
            get
            {
                var value = this.Table.FirstOrDefault(r => r?.VoiceroidId == id);

                if (value == null)
                {
                    // 有効なIDなのにアイテムが無いならば追加する
                    var name = id.GetInfo()?.Name;
                    if (name == null)
                    {
                        throw new InvalidEnumArgumentException(
                            nameof(id),
                            (int)id,
                            id.GetType());
                    }
                    value = new YmmCharaRelation(id, name);
                    this.Table.Add(value);
                }

                return value;
            }
        }

        /// <summary>
        /// 列挙子を取得する。
        /// </summary>
        /// <returns>列挙子。</returns>
        public IEnumerator<YmmCharaRelation> GetEnumerator()
        {
            foreach (VoiceroidId id in Enum.GetValues(typeof(VoiceroidId)))
            {
                yield return this[id];
            }
        }

        /// <summary>
        /// 内部リストクラス。
        /// </summary>
        private class InnerList : Collection<YmmCharaRelation>
        {
            /// <summary>
            /// 指定したVOICEROID識別IDを持つ要素が存在するか否かを取得する。
            /// </summary>
            /// <param name="id">VOICEROID識別ID。</param>
            /// <returns></returns>
            public bool Contains(VoiceroidId id)
            {
                return this.Any(r => r?.VoiceroidId == id);
            }

            #region Collection<YmmCharaRelation> のオーバライド

            protected override void InsertItem(int index, YmmCharaRelation item)
            {
                // null もしくはID重複なら無視
                if (item == null || this.Contains(item.VoiceroidId))
                {
                    return;
                }

                base.InsertItem(index, item);
            }

            protected override void SetItem(int index, YmmCharaRelation item)
            {
                // null もしくはID重複なら無視
                if (
                    item == null ||
                    (this[index].VoiceroidId != item.VoiceroidId &&
                     this.Contains(item.VoiceroidId)))
                {
                    return;
                }

                base.SetItem(index, item);
            }

            #endregion
        }

        /// <summary>
        /// 内部リストを取得または設定する。
        /// </summary>
        [DataMember]
        private InnerList Table { get; set; } = new InnerList();

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
