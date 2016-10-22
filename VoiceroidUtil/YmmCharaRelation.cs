using System;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROIDと『ゆっくりMovieMaker』のキャラ名との紐付けを定義するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class YmmCharaRelation : VoiceroidItemBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        public YmmCharaRelation(VoiceroidId voiceroidId) : this(voiceroidId, null)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="ymmCharaName">
        /// 『ゆっくりMovieMaker』のキャラ名。 null ならばVOICEROID識別IDから決定する。
        /// </param>
        public YmmCharaRelation(VoiceroidId voiceroidId, string ymmCharaName)
            : base(voiceroidId)
        {
            this.YmmCharaName = ymmCharaName ?? voiceroidId.GetInfo()?.Name ?? "";
        }

        /// <summary>
        /// 『ゆっくりMovieMaker』のキャラ名を取得または設定する。
        /// </summary>
        [DataMember]
        public string YmmCharaName
        {
            get { return this.ymmCharaName; }
            set { this.SetProperty(ref this.ymmCharaName, value ?? ""); }
        }
        private string ymmCharaName = "";

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            // null 回避
            this.YmmCharaName = "";
        }
    }
}
