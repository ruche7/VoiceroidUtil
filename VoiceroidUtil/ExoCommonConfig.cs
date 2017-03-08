using System;
using System.Runtime.Serialization;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// AviUtl拡張編集ファイル用の共通設定を保持するクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class ExoCommonConfig : BindableConfigBase
    {
        #region 定数群

        /// <summary>
        /// 表示領域の幅の最小値。
        /// </summary>
        public static readonly int MinWidth = 1;

        /// <summary>
        /// 表示領域の幅の最大値。
        /// </summary>
        public static readonly int MaxWidth = 9999;

        /// <summary>
        /// 表示領域の高さの最小値。
        /// </summary>
        public static readonly int MinHeight = 1;

        /// <summary>
        /// 表示領域の高さの最大値。
        /// </summary>
        public static readonly int MaxHeight = 9999;

        /// <summary>
        /// フレームレートの最小値。
        /// </summary>
        public static readonly decimal MinFps = 1;

        /// <summary>
        /// フレームレートの最大値。
        /// </summary>
        public static readonly decimal MaxFps = 1000;

        /// <summary>
        /// フレームレートの小数点以下最大桁数。
        /// </summary>
        public static readonly int MaxFpsDigits = 3;

        /// <summary>
        /// 追加フレーム数の最小値。
        /// </summary>
        public static readonly int MinExtraFrames = 0;

        /// <summary>
        /// 追加フレーム数の最大値。
        /// </summary>
        public static readonly int MaxExtraFrames = 9999;

        #endregion

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ExoCommonConfig()
        {
        }

        /// <summary>
        /// 表示領域の幅を取得または設定する。
        /// </summary>
        [DataMember]
        public int Width
        {
            get => this.width;
            set =>
                this.SetProperty(
                    ref this.width,
                    Math.Min(Math.Max(MinWidth, value), MaxWidth));
        }
        private int width = 1280;

        /// <summary>
        /// 表示領域の高さを取得または設定する。
        /// </summary>
        [DataMember]
        public int Height
        {
            get => this.height;
            set =>
                this.SetProperty(
                    ref this.height,
                    Math.Min(Math.Max(MinHeight, value), MaxHeight));
        }
        private int height = 720;

        /// <summary>
        /// フレームレートを取得または設定する。
        /// </summary>
        [DataMember]
        public decimal Fps
        {
            get => this.fps;
            set =>
                this.SetProperty(
                    ref this.fps,
                    decimal.Round(
                        Math.Min(Math.Max(MinFps, value), MaxFps),
                        MaxFpsDigits));
        }
        private decimal fps = 60;

        /// <summary>
        /// 追加フレーム数を取得または設定する。
        /// </summary>
        [DataMember]
        public int ExtraFrames
        {
            get => this.extraFrames;
            set =>
                this.SetProperty(
                    ref this.extraFrames,
                    Math.Min(Math.Max(MinExtraFrames, value), MaxExtraFrames));
        }
        private int extraFrames = 0;

        /// <summary>
        /// テキストと音声をグループ化するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsGrouping
        {
            get => this.grouping;
            set => this.SetProperty(ref this.grouping, value);
        }
        private bool grouping = true;

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
