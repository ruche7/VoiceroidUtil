using System;
using System.Runtime.Serialization;
using RucheHome.AviUtl.ExEdit.GcmzDrops;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROIDのAviUtl拡張編集ファイルドロップ先レイヤー番号を保持するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class AviUtlDropLayer : VoiceroidItemBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        public AviUtlDropLayer(VoiceroidId voiceroidId) : this(voiceroidId, MinLayer - 1)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="layer">
        /// レイヤー番号。範囲外ならばVOICEROID識別IDから決定する。
        /// </param>
        public AviUtlDropLayer(VoiceroidId voiceroidId, int layer) : base(voiceroidId)
        {
            this.Layer =
                (layer < MinLayer || layer > MaxLayer) ? ((int)voiceroidId * 2 + 10) : layer;
        }

        /// <summary>
        /// レイヤー番号の最小許容値。
        /// </summary>
        public const int MinLayer = FileDrop.MinLayer;

        /// <summary>
        /// レイヤー番号の最大許容値。
        /// </summary>
        public const int MaxLayer = FileDrop.MaxLayer - 1;

        /// <summary>
        /// レイヤー番号を取得または設定する。
        /// </summary>
        [DataMember]
        public int Layer
        {
            get => this.layer;
            set =>
                this.SetProperty(
                    ref this.layer,
                    Math.Min(Math.Max(MinLayer, value), MaxLayer));
        }
        private int layer = 1;
    }
}
