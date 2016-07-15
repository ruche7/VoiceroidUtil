using System;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VoiceroidExecutablePath インスタンスセットクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    [KnownType(typeof(VoiceroidExecutablePath))]
    public class VoiceroidExecutablePathSet : VoiceroidItemSetBase<VoiceroidExecutablePath>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidExecutablePathSet() : base()
        {
        }
    }
}
