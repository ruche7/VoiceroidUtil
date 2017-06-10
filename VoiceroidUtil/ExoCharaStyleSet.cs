using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// ExoCharaStyle インスタンスセットクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class ExoCharaStyleSet : VoiceroidItemSetBase<ExoCharaStyle>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ExoCharaStyleSet() : base()
        {
        }

        /// <summary>
        /// アイテムセットとして保持するVOICEROID識別ID列挙を取得する。
        /// </summary>
        /// <remarks>
        /// VoiceroidId.Voiceroid2 を除外する。
        /// </remarks>
        protected override IEnumerable<VoiceroidId> VoiceroidIds =>
            AllVoiceroidIds.Where(id => id != VoiceroidId.Voiceroid2);
    }
}
