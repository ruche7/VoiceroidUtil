using System;
using System.Runtime.Serialization;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// AviUtl拡張編集ファイル用設定クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class ExoConfig : BindableConfigBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ExoConfig()
        {
            // イベントハンドラ追加のためにプロパティ経由で設定
            this.Common = new ExoCommonConfig();
            this.CharaStyles = new ExoCharaStyleSet();
        }

        /// <summary>
        /// 共通設定を取得または設定する。
        /// </summary>
        [DataMember]
        public ExoCommonConfig Common
        {
            get { return this.common; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.common,
                    value ?? new ExoCommonConfig());
            }
        }
        private ExoCommonConfig common = null;

        /// <summary>
        /// キャラクター別スタイルを取得または設定する。
        /// </summary>
        [DataMember]
        public ExoCharaStyleSet CharaStyles
        {
            get { return this.charaStyles; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.charaStyles,
                    value ?? new ExoCharaStyleSet());
            }
        }
        private ExoCharaStyleSet charaStyles = null;

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.ResetDataMembers();
        }
    }
}
