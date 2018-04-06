using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

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

        /// <summary>
        /// アイテムセットとして保持するVOICEROID識別ID列挙を取得する。
        /// </summary>
        /// <remarks>
        /// 操作不可能なものを除外する。
        /// </remarks>
        protected override IEnumerable<VoiceroidId> VoiceroidIds =>
            AllVoiceroidIds.Where(id => id.GetInfo().IsControllable);
    }
}
