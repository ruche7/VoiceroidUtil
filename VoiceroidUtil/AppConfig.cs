﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using ruche.voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリケーション設定クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    [KnownType(typeof(VoiceroidId))]
    [KnownType(typeof(FileNameFormat))]
    public class AppConfig : INotifyPropertyChanged, IExtensibleDataObject
    {
        /// <summary>
        /// 既定の保存先ディレクトリパス。
        /// </summary>
        public static readonly string DefaultSaveDirectoryPath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "VoiceroidWaveFiles");

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppConfig()
        {
        }

        /// <summary>
        /// 選択中VOICEROID識別IDを取得または設定する。
        /// </summary>
        [DataMember]
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
        [DataMember]
        public FileNameFormat FileNameFormat
        {
            get { return this.fileNameFormat; }
            set
            {
                this.SetProperty(
                    ref this.fileNameFormat,
                    Enum.IsDefined(value.GetType(), value) ?
                        value : FileNameFormat.NameText);
            }
        }
        private FileNameFormat fileNameFormat = FileNameFormat.DateTimeNameText;

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
        /// テキストファイルをUTF-8(BOM付き)で作成するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsTextFileUtf8
        {
            get { return this.textFileUtf8; }
            set { this.SetProperty(ref this.textFileUtf8, value); }
        }
        private bool textFileUtf8 = true;

        /// <summary>
        /// 保存したファイルのパスを『ゆっくりMovieMaker3』に設定するか否かを
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
        /// 『ゆっくりMovieMaker3』の追加ボタンを自動押下するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool IsYmmAddButtonClicking
        {
            get { return this.ymmAddButtonClicking; }
            set { this.SetProperty(ref this.ymmAddButtonClicking, value); }
        }
        private bool ymmAddButtonClicking = true;

        /// <summary>
        /// プロパティ値を設定する。
        /// </summary>
        /// <typeparam name="T">プロパティ値の型。</typeparam>
        /// <param name="field">設定先フィールド。</param>
        /// <param name="value">設定値。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        private void SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                if (propertyName != null && this.PropertyChanged != null)
                {
                    this.PropertyChanged(
                        this,
                        new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        #region INotifyPropertyChanged の実装

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region IExtensibleDataObject の明示的実装

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        #endregion
    }
}
