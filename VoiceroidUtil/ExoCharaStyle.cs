using System;
using System.Runtime.Serialization;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// AviUtl拡張編集ファイル用のキャラ別スタイルを保持するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class ExoCharaStyle : BindableConfigBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        public ExoCharaStyle(VoiceroidId voiceroidId)
        {
            this.VoiceroidId = voiceroidId;
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
        /// 標準描画コンポーネントを取得または設定する。
        /// </summary>
        [DataMember]
        public RenderComponent Render
        {
            get { return this.render; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.render,
                    value ?? new RenderComponent());
            }
        }
        private RenderComponent render = new RenderComponent();

        /// <summary>
        /// テキストコンポーネントを取得または設定する。
        /// </summary>
        [DataMember]
        public TextComponent Text
        {
            get { return this.text; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.text,
                    value ?? new TextComponent());
            }
        }
        private TextComponent text = new TextComponent();

        /// <summary>
        /// テキストを1つ上のオブジェクトでクリッピングするか否かを取得または設定する。
        /// </summary>
        public bool IsTextClipping
        {
            get { return this.textClipping; }
            set { this.SetProperty(ref this.textClipping, value); }
        }
        private bool textClipping = false;

        /// <summary>
        /// 標準再生コンポーネントを取得または設定する。
        /// </summary>
        [DataMember]
        public PlayComponent Play
        {
            get { return this.play; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.play,
                    value ?? new PlayComponent());
            }
        }
        private PlayComponent play = new PlayComponent();

        /// <summary>
        /// 再生速度を取得または設定する。
        /// </summary>
        [DataMember]
        public decimal PlaySpeed
        {
            get { return this.playSpeed; }
            set
            {
                this.SetProperty(
                    ref this.playSpeed,
                    decimal.Round(
                        Math.Min(
                            Math.Max(PlaySpeedConst.MinValue, value),
                            PlaySpeedConst.MaxValue),
                        PlaySpeedConst.Digits));
            }
        }
        private decimal playSpeed = 100;

        /// <summary>
        /// 再生速度の定数情報。
        /// </summary>
        private static readonly AudioFileComponent.PlaySpeedConst PlaySpeedConst =
            new AudioFileComponent.PlaySpeedConst();

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
