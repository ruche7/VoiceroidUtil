using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Data;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// トークテキスト置換設定クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    [KnownType(typeof(TalkTextReplaceItem))]
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
            get { return this.voiceReplaceItems; }
            set
            {
                if (value != this.voiceReplaceItems)
                {
                    // 古い値からイベントハンドラを削除
                    if (this.voiceReplaceItems != null)
                    {
                        this.voiceReplaceItems.CollectionChanged -=
                            this.OnVoiceCollectionChanged;
                        this.voiceReplaceItems.ItemPropertyChanged -=
                            this.OnVoiceItemPropertyChanged;
                    }

                    this.SetProperty(
                        ref this.voiceReplaceItems,
                        value ?? (new TalkTextReplaceItemCollection()));

                    // 新しい値にイベントハンドラを追加
                    this.voiceReplaceItems.CollectionChanged +=
                        this.OnVoiceCollectionChanged;
                    this.voiceReplaceItems.ItemPropertyChanged +=
                        this.OnVoiceItemPropertyChanged;

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
            get { return this.textFileReplaceItems; }
            set
            {
                if (value != this.textFileReplaceItems)
                {
                    // 古い値からイベントハンドラを削除
                    if (this.textFileReplaceItems != null)
                    {
                        this.textFileReplaceItems.CollectionChanged -=
                            this.OnTextFileCollectionChanged;
                        this.textFileReplaceItems.ItemPropertyChanged -=
                            this.OnTextFileItemPropertyChanged;
                    }

                    this.SetProperty(
                        ref this.textFileReplaceItems,
                        value ?? (new TalkTextReplaceItemCollection()));

                    // 新しい値にイベントハンドラを追加
                    this.textFileReplaceItems.CollectionChanged +=
                        this.OnTextFileCollectionChanged;
                    this.textFileReplaceItems.ItemPropertyChanged +=
                        this.OnTextFileItemPropertyChanged;

                    // 複数スレッドからのアクセスを許可
                    BindingOperations.EnableCollectionSynchronization(
                        this.TextFileReplaceItems,
                        new object());
                }
            }
        }
        private TalkTextReplaceItemCollection textFileReplaceItems = null;

        /// <summary>
        /// VoiceReplaceItems プロパティのコレクション変更時に呼び出される。
        /// </summary>
        private void OnVoiceCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(VoiceReplaceItems));
        }

        /// <summary>
        /// VoiceReplaceItems プロパティのコレクション要素変更時に呼び出される。
        /// </summary>
        private void OnVoiceItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(VoiceReplaceItems));
        }

        /// <summary>
        /// TextFileReplaceItems プロパティのコレクション変更時に呼び出される。
        /// </summary>
        private void OnTextFileCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(TextFileReplaceItems));
        }

        /// <summary>
        /// TextFileReplaceItems プロパティのコレクション要素変更時に呼び出される。
        /// </summary>
        private void OnTextFileItemPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(TextFileReplaceItems));
        }

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
