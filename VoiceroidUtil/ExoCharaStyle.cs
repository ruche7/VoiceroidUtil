using System;
using System.Runtime.Serialization;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// AviUtl拡張編集ファイル用のキャラ別スタイルを保持するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class ExoCharaStyle : VoiceroidItemBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        public ExoCharaStyle(VoiceroidId voiceroidId) : base(voiceroidId)
        {
            // イベントハンドラ追加のためにプロパティ経由で設定
            this.Render = new RenderComponent();
            this.Text = new TextComponent();
            this.Play = new PlayComponent();
            this.PlaySpeed = new MovableValue<AudioFileComponent.PlaySpeedConst>();
        }

        /// <summary>
        /// 標準描画コンポーネントを取得または設定する。
        /// </summary>
        [DataMember]
        public RenderComponent Render
        {
            get => this.render;
            set =>
                this.SetPropertyWithPropertyChangedChain(
                    ref this.render,
                    value ?? new RenderComponent());
        }
        private RenderComponent render = null;

        /// <summary>
        /// テキストコンポーネントを取得または設定する。
        /// </summary>
        [DataMember]
        public TextComponent Text
        {
            get => this.text;
            set =>
                this.SetPropertyWithPropertyChangedChain(
                    ref this.text,
                    value ?? new TextComponent());
        }
        private TextComponent text = null;

        /// <summary>
        /// テキストを1つ上のオブジェクトでクリッピングするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsTextClipping
        {
            get => this.textClipping;
            set => this.SetProperty(ref this.textClipping, value);
        }
        private bool textClipping = false;

        /// <summary>
        /// 標準再生コンポーネントを取得または設定する。
        /// </summary>
        [DataMember]
        public PlayComponent Play
        {
            get => this.play;
            set =>
                this.SetPropertyWithPropertyChangedChain(
                    ref this.play,
                    value ?? new PlayComponent());
        }
        private PlayComponent play = null;

        /// <summary>
        /// 再生速度を取得または設定する。
        /// </summary>
        [DataMember]
        public MovableValue<AudioFileComponent.PlaySpeedConst> PlaySpeed
        {
            get => this.playSpeed;
            set =>
                this.SetPropertyWithPropertyChangedChain(
                    ref this.playSpeed,
                    value ?? new MovableValue<AudioFileComponent.PlaySpeedConst>());
        }
        private MovableValue<AudioFileComponent.PlaySpeedConst> playSpeed = null;

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) =>
            this.ResetDataMembers(VoiceroidId.YukariEx);
    }
}
