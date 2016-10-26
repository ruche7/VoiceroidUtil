using System;
using System.Runtime.Serialization;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROID識別IDに紐付くアイテムのベースクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class VoiceroidItemBase : BindableConfigBase, IVoiceroidItem
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        public VoiceroidItemBase(VoiceroidId voiceroidId)
        {
            this.VoiceroidId = voiceroidId;
        }

        /// <summary>
        /// VOICEROID識別IDを取得する。
        /// </summary>
        public VoiceroidId VoiceroidId
        {
            get { return this.voiceroidId; }
            private set
            {
                this.SetProperty(
                    ref this.voiceroidId,
                    Enum.IsDefined(value.GetType(), value) ?
                        value : VoiceroidId.YukariEx);

                // 名前も更新
                this.VoiceroidName = this.VoiceroidId.GetInfo().Name;
            }
        }
        private VoiceroidId voiceroidId = VoiceroidId.YukariEx;

        /// <summary>
        /// VoiceroidId プロパティのシリアライズ用ラッパプロパティ。
        /// </summary>
        [DataMember(Name = nameof(VoiceroidId))]
        private string VoiceroidIdString
        {
            get { return this.VoiceroidId.ToString(); }
            set
            {
                VoiceroidId id;
                this.VoiceroidId =
                    Enum.TryParse(value, out id) ? id : VoiceroidId.YukariEx;
            }
        }

        /// <summary>
        /// VOICEROIDの名前を取得する。
        /// </summary>
        public string VoiceroidName
        {
            get { return this.voiceroidName; }
            private set { this.SetProperty(ref this.voiceroidName, value ?? ""); }
        }
        private string voiceroidName = "";
    }
}
