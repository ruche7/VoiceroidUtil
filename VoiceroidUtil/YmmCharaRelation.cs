using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROIDと『ゆっくりMovieMaker3』のキャラ名との紐付けを定義するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    [KnownType(typeof(VoiceroidId))]
    public class YmmCharaRelation : INotifyPropertyChanged
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="ymmCharaName">『ゆっくりMovieMaker3』のキャラ名。</param>
        public YmmCharaRelation(VoiceroidId voiceroidId, string ymmCharaName = "")
        {
            this.VoiceroidId = voiceroidId;
            this.YmmCharaName = ymmCharaName;
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
        /// VOICEROIDの名前を取得する。
        /// </summary>
        public string VoiceroidName
        {
            get { return this.VoiceroidId.GetInfo().Name; }
        }

        /// <summary>
        /// 『ゆっくりMovieMaker3』のキャラ名を取得または設定する。
        /// </summary>
        [DataMember]
        public string YmmCharaName
        {
            get { return this.ymmCharaName; }
            set
            {
                if (value != this.ymmCharaName)
                {
                    this.ymmCharaName = value ?? "";
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(
                            this,
                            new PropertyChangedEventArgs(nameof(this.YmmCharaName)));
                    }
                }
            }
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

        #region INotifyPropertyChanged の実装

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
