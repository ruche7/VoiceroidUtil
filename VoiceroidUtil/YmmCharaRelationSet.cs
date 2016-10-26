using System;
using System.Runtime.Serialization;

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
    }
}
