﻿using System;
using System.Runtime.Serialization;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 音声ファイルコンポーネントを表すクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class AudioFileComponent : ComponentBase
    {
        /// <summary>
        /// 拡張編集オブジェクトファイルのセクションデータからコンポーネントを作成する。
        /// </summary>
        /// <param name="section">セクションデータ。</param>
        /// <returns>コンポーネント。作成できないならば null 。</returns>
        public static AudioFileComponent FromExoFileSection(IniFileSection section)
        {
            return FromExoFileSectionCore(section, () => new AudioFileComponent());
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AudioFileComponent() : base()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        public override string ComponentName => @"音声ファイル";

        /// <summary>
        /// 再生開始位置を取得または設定する。
        /// </summary>
        /// <remarks>
        /// このプロパティでは最大値を 100.0 固定としている。
        /// AviUtl拡張編集では再生対象の長さに応じて最大値が変化する。
        /// </remarks>
        [ExoFileItem(@"再生位置", Order = 1)]
        [DataMember]
        public MovableValue<PlayPositionConst> PlayPosition
        {
            get { return this.playPosition; }
            set
            {
                this.SetProperty(
                    ref this.playPosition,
                    value ?? new MovableValue<PlayPositionConst>());
            }
        }
        private MovableValue<PlayPositionConst> playPosition =
            new MovableValue<PlayPositionConst>();

        /// <summary>
        /// 再生速度を取得または設定する。
        /// </summary>
        [ExoFileItem(@"再生速度", Order = 2)]
        [DataMember]
        public MovableValue<PlaySpeedConst> PlaySpeed
        {
            get { return this.playSpeed; }
            set
            {
                this.SetProperty(
                    ref this.playSpeed,
                    value ?? new MovableValue<PlaySpeedConst>());
            }
        }
        private MovableValue<PlaySpeedConst> playSpeed =
            new MovableValue<PlaySpeedConst>();

        /// <summary>
        /// ループ再生するか否かを取得または設定する。
        /// </summary>
        [ExoFileItem(@"ループ再生", Order = 3)]
        [DataMember]
        public bool IsLooping
        {
            get { return this.looping; }
            set { this.SetProperty(ref this.looping, value); }
        }
        private bool looping = false;

        /// <summary>
        /// 動画ファイルと連携するか否かを取得または設定する。
        /// </summary>
        [ExoFileItem(@"動画ファイルと連携", Order = 4)]
        [DataMember]
        public bool IsVideoFileLinking
        {
            get { return this.videoFileLinking; }
            set { this.SetProperty(ref this.videoFileLinking, value); }
        }
        private bool videoFileLinking = false;

        /// <summary>
        /// 参照ファイルパスを取得または設定する。
        /// </summary>
        [ExoFileItem(@"file", Order = 5)]
        [DataMember]
        public string FilePath
        {
            get { return this.filePath; }
            set { this.SetProperty(ref this.filePath, value ?? ""); }
        }
        private string filePath = "";

        /// <summary>
        /// このコンポーネントの内容を別のコンポーネントへコピーする。
        /// </summary>
        /// <param name="target">コピー先。</param>
        public void CopyTo(AudioFileComponent target)
        {
            this.CopyToCore(target);
        }

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.ResetDataMembers();
        }

        #region MovableValue{TConstants} ジェネリッククラス用の定数情報構造体群

        /// <summary>
        /// 再生開始位置用の定数情報クラス。
        /// </summary>
        public struct PlayPositionConst : IMovableValueConstants
        {
            public int Digits => 2;
            public decimal DefaultValue => 0;
            public decimal MinValue => 0;
            public decimal MaxValue => 100;
            public decimal MinSliderValue => 0;
            public decimal MaxSliderValue => 100;
        }

        /// <summary>
        /// 再生速度用の定数情報クラス。
        /// </summary>
        public struct PlaySpeedConst : IMovableValueConstants
        {
            public int Digits => 1;
            public decimal DefaultValue => 100;
            public decimal MinValue => 10;
            public decimal MaxValue => 800;
            public decimal MinSliderValue => 10;
            public decimal MaxSliderValue => 800;
        }

        #endregion
    }
}
