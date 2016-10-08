using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリケーション設定クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class AppConfig : BindableConfigBase
    {
        /// <summary>
        /// 既定の保存先ディレクトリパス。
        /// </summary>
        public static readonly string DefaultSaveDirectoryPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                @"VoiceroidWaveFiles");

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppConfig()
        {
            // イベントハンドラ追加のためにプロパティ経由で設定
            this.YmmCharaRelations = new YmmCharaRelationSet();
        }

        /// <summary>
        /// 起動時にアプリの更新をチェックするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsUpdateCheckingOnStartup
        {
            get { return this.updateCheckingOnStartup; }
            set { this.SetProperty(ref this.updateCheckingOnStartup, value); }
        }
        private bool updateCheckingOnStartup = true;

        /// <summary>
        /// ウィンドウを常に最前面に表示するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsTopmost
        {
            get { return this.topmost; }
            set { this.SetProperty(ref this.topmost, value); }
        }
        private bool topmost = false;

        /// <summary>
        /// タブ文字の入力を受け付けるか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsTabAccepted
        {
            get { return this.tabAccepted; }
            set { this.SetProperty(ref this.tabAccepted, value); }
        }
        private bool tabAccepted = true;

        /// <summary>
        /// 保存先ディレクトリパスを取得または設定する。
        /// </summary>
        [DataMember]
        public string SaveDirectoryPath
        {
            get { return this.saveDirectoryPath; }
            set
            {
                this.SetProperty(
                    ref this.saveDirectoryPath,
                    string.IsNullOrWhiteSpace(value) ? DefaultSaveDirectoryPath : value);
            }
        }
        private string saveDirectoryPath = DefaultSaveDirectoryPath;

        /// <summary>
        /// ファイル名フォーマットを取得または設定する。
        /// </summary>
        public FileNameFormat FileNameFormat
        {
            get { return this.fileNameFormat; }
            set
            {
                this.SetProperty(
                    ref this.fileNameFormat,
                    Enum.IsDefined(value.GetType(), value) ?
                        value : FileNameFormat.DateTimeNameText);
            }
        }
        private FileNameFormat fileNameFormat = FileNameFormat.DateTimeNameText;

        /// <summary>
        /// FileNameFormat プロパティのシリアライズ用ラッパプロパティ。
        /// </summary>
        [DataMember(Name = nameof(FileNameFormat))]
        private string FileNameFormatString
        {
            get { return this.FileNameFormat.ToString(); }
            set
            {
                FileNameFormat f;
                this.FileNameFormat =
                    Enum.TryParse(value, out f) ? f : FileNameFormat.DateTimeNameText;
            }
        }

        /// <summary>
        /// テキストファイルを必ず作成するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsTextFileForceMaking
        {
            get { return this.textFileForceMaking; }
            set { this.SetProperty(ref this.textFileForceMaking, value); }
        }
        private bool textFileForceMaking = true;

        /// <summary>
        /// テキストファイルをUTF-8(BOMなし)で作成するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsTextFileUtf8
        {
            get { return this.textFileUtf8; }
            set { this.SetProperty(ref this.textFileUtf8, value); }
        }
        private bool textFileUtf8 = true;

        /// <summary>
        /// 音声保存成功時にテキストをクリアするか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsTextClearing
        {
            get { return this.textClearing; }
            set { this.SetProperty(ref this.textClearing, value); }
        }
        private bool textClearing = false;

        /// <summary>
        /// 保存したファイルのパスを『ゆっくりMovieMaker』に設定するか否かを
        /// 取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsSavedFileToYmm
        {
            get { return this.savedFileToYmm; }
            set { this.SetProperty(ref this.savedFileToYmm, value); }
        }
        private bool savedFileToYmm = true;

        /// <summary>
        /// 『ゆっくりMovieMaker』のキャラを自動選択するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsYmmCharaSelecting
        {
            get { return this.ymmCharaSelecting; }
            set { this.SetProperty(ref this.ymmCharaSelecting, value); }
        }
        private bool ymmCharaSelecting = true;

        /// <summary>
        /// VOICEROIDと『ゆっくりMovieMaker』のキャラ名との紐付けを取得または設定する。
        /// </summary>
        [DataMember]
        public YmmCharaRelationSet YmmCharaRelations
        {
            get { return this.ymmCharaRelations; }
            set
            {
                if (value != this.ymmCharaRelations)
                {
                    // 古い値からイベントハンドラを削除
                    if (this.ymmCharaRelations != null)
                    {
                        this.ymmCharaRelations.PropertyChanged -=
                            this.OnYmmCharaRelationsPropertyChanged;
                    }

                    this.SetProperty(
                        ref this.ymmCharaRelations,
                        value ?? (new YmmCharaRelationSet()));

                    // 新しい値にイベントハンドラを追加
                    this.ymmCharaRelations.PropertyChanged +=
                        this.OnYmmCharaRelationsPropertyChanged;
                }
            }
        }
        private YmmCharaRelationSet ymmCharaRelations = null;

        /// <summary>
        /// 『ゆっくりMovieMaker』の追加ボタンを自動押下するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsYmmAddButtonClicking
        {
            get { return this.ymmAddButtonClicking; }
            set { this.SetProperty(ref this.ymmAddButtonClicking, value); }
        }
        private bool ymmAddButtonClicking = true;

        /// <summary>
        /// YmmCharaRelations プロパティの内容変更時に呼び出される。
        /// </summary>
        private void OnYmmCharaRelationsPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            // YmmCharaRelations プロパティ自身の変更通知を行う
            this.RaisePropertyChanged(nameof(YmmCharaRelations));
        }

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
