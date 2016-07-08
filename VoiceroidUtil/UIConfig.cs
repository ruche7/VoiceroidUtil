using System;
using System.Runtime.Serialization;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// UI設定クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class UIConfig : BindableConfigBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public UIConfig()
        {
        }

        /// <summary>
        /// アプリ設定ビューの「表示」カテゴリを
        /// 開いた状態にするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsViewConfigExpanded
        {
            get { return this.viewConfigExpanded; }
            set { this.SetProperty(ref this.viewConfigExpanded, value); }
        }
        private bool viewConfigExpanded = true;

        /// <summary>
        /// アプリ設定ビューの「音声保存」カテゴリを
        /// 開いた状態にするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsSaveConfigExpanded
        {
            get { return this.saveConfigExpanded; }
            set { this.SetProperty(ref this.saveConfigExpanded, value); }
        }
        private bool saveConfigExpanded = true;

        /// <summary>
        /// アプリ設定ビューの「ゆっくりMovieMaker連携」カテゴリを
        /// 開いた状態にするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsYmmConfigExpanded
        {
            get { return this.ymmConfigExpanded; }
            set { this.SetProperty(ref this.ymmConfigExpanded, value); }
        }
        private bool ymmConfigExpanded = true;

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
