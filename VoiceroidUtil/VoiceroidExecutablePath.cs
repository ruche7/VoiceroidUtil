using System;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROIDの実行ファイルパスを保持するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class VoiceroidExecutablePath : VoiceroidItemBase
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
        public VoiceroidExecutablePath(VoiceroidId voiceroidId, string path)
            : base(voiceroidId)
        {
            this.Path = path;
        }

        /// <summary>
        /// 実行ファイルパスを取得または設定する。
        /// </summary>
        [DataMember]
        public string Path
        {
            get => this.path;
            set => this.SetProperty(ref this.path, value);
        }
        private string path = null;
    }
}
