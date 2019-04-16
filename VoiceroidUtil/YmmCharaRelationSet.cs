using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// YmmCharaRelation インスタンスセットクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class YmmCharaRelationSet : VoiceroidItemSetBase<YmmCharaRelation>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public YmmCharaRelationSet() : base()
        {
        }

        /// <summary>
        /// アイテムセットとして保持するVOICEROID識別ID列挙を取得する。
        /// </summary>
        /// <remarks>
        /// VOICEROID2ライクソフトウェアを示すIDを除外する。
        /// </remarks>
        protected override IEnumerable<VoiceroidId> VoiceroidIds =>
            AllVoiceroidIds.Where(id => !id.IsVoiceroid2LikeSoftware());
    }
}
