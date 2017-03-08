using System;
using System.Runtime.Serialization;
using System.Windows.Data;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// トークテキスト置換設定クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class TalkTextReplaceConfig : BindableConfigBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TalkTextReplaceConfig()
        {
            // イベントハンドラ追加のためにプロパティ経由で設定
            this.VoiceReplaceItems =
                new TalkTextReplaceItemCollection { new TalkTextReplaceItem() };
            this.TextFileReplaceItems =
                new TalkTextReplaceItemCollection { new TalkTextReplaceItem() };
        }

        /// <summary>
        /// 音声文字列置換アイテムコレクションを取得または設定する。
        /// </summary>
        [DataMember]
        public TalkTextReplaceItemCollection VoiceReplaceItems
        {
            get => this.voiceReplaceItems;
            set
            {
                var v = value ?? new TalkTextReplaceItemCollection();
                if (v != this.voiceReplaceItems)
                {
                    this.RemoveBindableCollectionEventChain(this.voiceReplaceItems);
                    this.AddBindableCollectionEventChain(v);
                    this.SetProperty(ref this.voiceReplaceItems, v);

                    // 複数スレッドからのアクセスを許可
                    BindingOperations.EnableCollectionSynchronization(
                        this.VoiceReplaceItems,
                        new object());
                }
            }
        }
        private TalkTextReplaceItemCollection voiceReplaceItems = null;

        /// <summary>
        /// テキストファイル文字列置換アイテムコレクションを取得または設定する。
        /// </summary>
        [DataMember]
        public TalkTextReplaceItemCollection TextFileReplaceItems
        {
            get => this.textFileReplaceItems;
            set
            {
                var v = value ?? new TalkTextReplaceItemCollection();
                if (v != this.textFileReplaceItems)
                {
                    this.RemoveBindableCollectionEventChain(this.textFileReplaceItems);
                    this.AddBindableCollectionEventChain(v);
                    this.SetProperty(ref this.textFileReplaceItems, v);

                    // 複数スレッドからのアクセスを許可
                    BindingOperations.EnableCollectionSynchronization(
                        this.TextFileReplaceItems,
                        new object());
                }
            }
        }
        private TalkTextReplaceItemCollection textFileReplaceItems = null;

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.ResetDataMembers();

            // コレクションは空にしておく
            this.VoiceReplaceItems.Clear();
            this.TextFileReplaceItems.Clear();
        }

        /// <summary>
        /// デシリアライズの完了時に呼び出される。
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // コレクションが空ならアイテムを1つ追加しておく
            if (this.VoiceReplaceItems.Count <= 0)
            {
                this.VoiceReplaceItems.Add(new TalkTextReplaceItem());
            }
            if (this.TextFileReplaceItems.Count <= 0)
            {
                this.TextFileReplaceItems.Add(new TalkTextReplaceItem());
            }
        }
    }
}
