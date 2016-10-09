﻿using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// UI設定クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class UIConfig : BindableConfigBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public UIConfig()
        {
            // イベントハンドラ追加のためにプロパティ経由で設定
            this.VoiceroidExecutablePathes = new VoiceroidExecutablePathSet();
        }

        /// <summary>
        /// 選択中VOICEROID識別IDを取得または設定する。
        /// </summary>
        public VoiceroidId VoiceroidId
        {
            get { return this.voiceroidId; }
            set
            {
                this.SetProperty(
                    ref this.voiceroidId,
                    Enum.IsDefined(value.GetType(), value) ?
                        value : VoiceroidId.YukariEx);
            }
        }
        private VoiceroidId voiceroidId = VoiceroidId.YukariEx;

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
        /// VOICEROIDの実行ファイルパスセットを取得または設定する。
        /// </summary>
        [DataMember]
        public VoiceroidExecutablePathSet VoiceroidExecutablePathes
        {
            get { return this.voiceroidExecutablePathes; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.voiceroidExecutablePathes,
                    value ?? (new VoiceroidExecutablePathSet()));
            }
        }
        private VoiceroidExecutablePathSet voiceroidExecutablePathes = null;

        /// <summary>
        /// トークテキスト置換設定ビューの選択中タブインデックスを取得または設定する。
        /// </summary>
        [DataMember]
        public int TalkTextReplaceConfigTabIndex
        {
            get { return this.talkTextReplaceConfigTabIndex; }
            set { this.SetProperty(ref this.talkTextReplaceConfigTabIndex, value); }
        }
        private int talkTextReplaceConfigTabIndex = 0;

        /// <summary>
        /// アプリ設定ビューの「一般」カテゴリを
        /// 開いた状態にするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsGeneralConfigExpanded
        {
            get { return this.generalConfigExpanded; }
            set { this.SetProperty(ref this.generalConfigExpanded, value); }
        }
        private bool generalConfigExpanded = true;

        /// <summary>
        /// アプリ設定ビューの「音声保存」カテゴリを
        /// 開いた状態にするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsSaveConfigExpanded
        {
            get { return this.saveConfigExpanded; }
            set { this.SetProperty(ref this.saveConfigExpanded, value); }
        }
        private bool saveConfigExpanded = true;

        /// <summary>
        /// アプリ設定ビューの「ゆっくりMovieMaker連携」カテゴリを
        /// 開いた状態にするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsYmmConfigExpanded
        {
            get { return this.ymmConfigExpanded; }
            set { this.SetProperty(ref this.ymmConfigExpanded, value); }
        }
        private bool ymmConfigExpanded = true;

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
