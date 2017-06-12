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
            get => this.voiceroidId;
            private set
            {
                this.SetProperty(
                    ref this.voiceroidId,
                    Enum.IsDefined(value.GetType(), value) ?
                        value : VoiceroidId.YukariEx);

                // 名前も更新
                this.VoiceroidName = MakeVoiceroidName(this.VoiceroidId);
            }
        }
        private VoiceroidId voiceroidId = VoiceroidId.YukariEx;

        /// <summary>
        /// VoiceroidId プロパティのシリアライズ用ラッパプロパティ。
        /// </summary>
        [DataMember(Name = nameof(VoiceroidId))]
        private string VoiceroidIdString
        {
            get => this.VoiceroidId.ToString();
            set =>
                this.VoiceroidId =
                    Enum.TryParse(value, out VoiceroidId id) ? id : VoiceroidId.YukariEx;
        }

        /// <summary>
        /// VOICEROIDの名前を取得する。
        /// </summary>
        public string VoiceroidName
        {
            get => this.voiceroidName;
            private set => this.SetProperty(ref this.voiceroidName, value ?? "");
        }
        private string voiceroidName = "";

        /// <summary>
        /// VOICEROID識別IDに対応するVOICEROIDの名前を作成する。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <returns>VOICEROIDの名前。</returns>
        /// <remarks>
        /// コンストラクタおよびデシリアライズによって
        /// VoiceroidId プロパティ値が設定される時に呼び出される。
        /// 既定では voiceroidId.GetInfo().Name を返す。
        /// </remarks>
        protected virtual string MakeVoiceroidName(VoiceroidId voiceroidId)
        {
            return voiceroidId.GetInfo().Name;
        }
    }
}
