using System;
using System.Runtime.Serialization;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// トークテキスト置換アイテムクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class TalkTextReplaceItem : BindableConfigBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TalkTextReplaceItem()
        {
        }

        /// <summary>
        /// アイテムが有効であるか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsEnabled
        {
            get => this.enabled;
            set => this.SetProperty(ref this.enabled, value);
        }
        private bool enabled = true;

        /// <summary>
        /// 置換元文字列を取得または設定する。
        /// </summary>
        [DataMember]
        public string OldValue
        {
            get => this.oldValue;
            set => this.SetProperty(ref this.oldValue, value ?? "");
        }
        private string oldValue = "";

        /// <summary>
        /// 置換先文字列を取得または設定する。
        /// </summary>
        [DataMember]
        public string NewValue
        {
            get => this.newValue;
            set => this.SetProperty(ref this.newValue, value ?? "");
        }
        private string newValue = "";

        /// <summary>
        /// アイテムが利用可能であるか否かを取得する。
        /// </summary>
        /// <returns>利用可能ならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// IsEnable が true かつ OldValue が空文字列でなければ利用可能。
        /// </remarks>
        public bool IsAvailable() =>
            this.IsEnabled && !string.IsNullOrEmpty(this.OldValue);

        /// <summary>
        /// アイテムのクローンを作成する。
        /// </summary>
        /// <returns>アイテムのクローン。</returns>
        public TalkTextReplaceItem Clone()
            =>
            new TalkTextReplaceItem
            {
                IsEnabled = this.IsEnabled,
                OldValue = this.OldValue,
                NewValue = this.NewValue,
            };

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.ResetDataMembers();
        }
    }
}
