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
        /// アイテムが利用可能であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// IsEnable が true かつ OldValue が空文字列でなければ利用可能。
        /// </remarks>
        public bool IsAvailable
        {
            get { return (this.IsEnabled && !string.IsNullOrEmpty(this.OldValue)); }
        }

        /// <summary>
        /// アイテムが有効であるか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsEnabled
        {
            get { return this.enabled; }
            set
            {
                bool oldAvailable = this.IsAvailable;

                this.SetProperty(ref this.enabled, value);

                if (this.IsAvailable != oldAvailable)
                {
                    this.RaisePropertyChanged(nameof(IsAvailable));
                }
            }
        }
        private bool enabled = true;

        /// <summary>
        /// 置換元文字列を取得または設定する。
        /// </summary>
        [DataMember]
        public string OldValue
        {
            get { return this.oldValue; }
            set
            {
                bool oldAvailable = this.IsAvailable;

                this.SetProperty(ref this.oldValue, value ?? "");

                if (this.IsAvailable != oldAvailable)
                {
                    this.RaisePropertyChanged(nameof(IsAvailable));
                }
            }
        }
        private string oldValue = "";

        /// <summary>
        /// 置換先文字列を取得または設定する。
        /// </summary>
        [DataMember]
        public string NewValue
        {
            get { return this.newValue; }
            set { this.SetProperty(ref this.newValue, value ?? ""); }
        }
        private string newValue = "";

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
