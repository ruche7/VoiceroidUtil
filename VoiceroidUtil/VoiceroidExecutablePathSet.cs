using System;
using System.Runtime.Serialization;

namespace VoiceroidUtil
{
    /// <summary>
    /// VoiceroidExecutablePath インスタンスセットクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class VoiceroidExecutablePathSet
        : VoiceroidItemSetBase<VoiceroidExecutablePath>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidExecutablePathSet() : base()
        {
        }
    }
}
