using System;
using System.Collections.Generic;
using System.Windows;
using Livet.Messaging;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// ファイルまたはディレクトリの選択ダイアログ処理を行うメッセージクラス。
    /// </summary>
    /// <remarks>
    /// Response には選択されたパスが設定される。
    /// 選択されなかった場合は null が設定される。
    /// </remarks>
    public class OpenFileDialogMessage : ResponsiveInteractionMessage<string>
    {
        /// <summary>
        /// 既定のメッセージキー文字列を取得する。
        /// </summary>
        public static string DefaultMessageKey { get; } =
            typeof(OpenFileDialogMessage).FullName;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public OpenFileDialogMessage() : this(DefaultMessageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public OpenFileDialogMessage(string messageKey) : base(messageKey)
        {
        }

        /// <summary>
        /// IsFolderPicker 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty IsFolderPickerProperty =
            DependencyProperty.Register(
                nameof(IsFolderPicker),
                typeof(bool),
                typeof(OpenFileDialogMessage),
                new PropertyMetadata(false));

        /// <summary>
        /// ディレクトリ選択ダイアログとするか否かを取得または設定する。
        /// </summary>
        public bool IsFolderPicker
        {
            get { return (bool)this.GetValue(IsFolderPickerProperty); }
            set { this.SetValue(IsFolderPickerProperty, value); }
        }

        /// <summary>
        /// Title 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(OpenFileDialogMessage),
                new PropertyMetadata(null));

        /// <summary>
        /// ダイアログタイトルを取得または設定する。
        /// </summary>
        public string Title
        {
            get { return (string)this.GetValue(TitleProperty); }
            set { this.SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// InitialDirectory 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty InitialDirectoryProperty =
            DependencyProperty.Register(
                nameof(InitialDirectory),
                typeof(string),
                typeof(OpenFileDialogMessage),
                new PropertyMetadata(null));

        /// <summary>
        /// 初期ディレクトリパスを取得または設定する。
        /// </summary>
        public string InitialDirectory
        {
            get { return (string)this.GetValue(InitialDirectoryProperty); }
            set { this.SetValue(InitialDirectoryProperty, value); }
        }

        /// <summary>
        /// Filters 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty FiltersProperty =
            DependencyProperty.Register(
                nameof(Filters),
                typeof(List<CommonFileDialogFilter>),
                typeof(OpenFileDialogMessage),
                new PropertyMetadata(new List<CommonFileDialogFilter>()));

        /// <summary>
        /// 拡張子フィルターリストを取得または設定する。
        /// </summary>
        public List<CommonFileDialogFilter> Filters
        {
            get { return (List<CommonFileDialogFilter>)this.GetValue(FiltersProperty); }
            set { this.SetValue(FiltersProperty, value); }
        }

        #region ResponsiveInteractionMessage<string> のオーバライド

        protected override Freezable CreateInstanceCore()
        {
            return new OpenFileDialogMessage();
        }

        #endregion
    }
}
