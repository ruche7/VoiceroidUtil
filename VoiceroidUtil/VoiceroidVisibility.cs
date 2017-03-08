using System;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROIDの表示設定を保持するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class VoiceroidVisibility : VoiceroidItemBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        public VoiceroidVisibility(VoiceroidId voiceroidId) : this(voiceroidId, true)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="visible">表示するならば true 。</param>
        public VoiceroidVisibility(VoiceroidId voiceroidId, bool visible)
            : base(voiceroidId)
        {
            this.IsVisible = visible;
        }

        /// <summary>
        /// VOICEROIDを表示するか否かを取得する。
        /// </summary>
        [DataMember]
        public bool IsVisible
        {
            get => this.visible;
            set => this.SetProperty(ref this.visible, value);
        }
        private bool visible = true;

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.IsVisible = true;
        }
    }
}
