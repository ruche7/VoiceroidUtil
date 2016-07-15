using System;
using System.Runtime.Serialization;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROIDの実行ファイルパスを保持するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    [KnownType(typeof(VoiceroidId))]
    public class VoiceroidExecutablePath : BindableConfigBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        public VoiceroidExecutablePath(VoiceroidId voiceroidId) : this(voiceroidId, null)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="path">実行ファイルパス。</param>
        public VoiceroidExecutablePath(VoiceroidId voiceroidId, string path) : base()
        {
            this.VoiceroidId = voiceroidId;
            this.Path = path;
        }

        /// <summary>
        /// VOICEROID識別IDを取得する。
        /// </summary>
        public VoiceroidId VoiceroidId { get; private set; }

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
        /// 実行ファイルパスを取得または設定する。
        /// </summary>
        [DataMember]
        public string Path
        {
            get { return this.path; }
            set { this.SetProperty(ref this.path, value); }
        }
        private string path = null;
    }
}
