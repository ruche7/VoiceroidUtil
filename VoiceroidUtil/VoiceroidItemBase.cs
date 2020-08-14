using System;
using System.Diagnostics.CodeAnalysis;
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
        public VoiceroidItemBase(VoiceroidId voiceroidId) => this.VoiceroidId = voiceroidId;

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

                // 関連プロパティを更新
                var info = this.VoiceroidId.GetInfo();
                this.VoiceroidName = info.Name;
                this.VoiceroidShortName = info.ShortName;
                this.HasMultiVoiceroidCharacters = info.HasMultiCharacters;
            }
        }
        private VoiceroidId voiceroidId = VoiceroidId.YukariEx;

        /// <summary>
        /// VoiceroidId プロパティのシリアライズ用ラッパプロパティ。
        /// </summary>
        [DataMember(Name = nameof(VoiceroidId))]
        [SuppressMessage("CodeQuality", "IDE0051")]
        private string VoiceroidIdString
        {
            get => this.VoiceroidId.ToString();
            set =>
                this.VoiceroidId =
                    Enum.TryParse(value, out VoiceroidId id) ? id : VoiceroidId.YukariEx;
        }

        /// <summary>
        /// VOICEROID名を取得する。
        /// </summary>
        public string VoiceroidName
        {
            get => this.voiceroidName;
            private set => this.SetProperty(ref this.voiceroidName, value ?? "");
        }
        private string voiceroidName = "";

        /// <summary>
        /// VOICEROID短縮名を取得する。
        /// </summary>
        public string VoiceroidShortName
        {
            get => this.voiceroidShortName;
            private set => this.SetProperty(ref this.voiceroidShortName, value ?? "");
        }
        private string voiceroidShortName = "";

        /// <summary>
        /// 複数キャラクターを保持しているか否かを取得する。
        /// </summary>
        public bool HasMultiVoiceroidCharacters
        {
            get => this.multiVoiceroidCharacter;
            private set => this.SetProperty(ref this.multiVoiceroidCharacter, value);
        }
        private bool multiVoiceroidCharacter = false;
    }
}
