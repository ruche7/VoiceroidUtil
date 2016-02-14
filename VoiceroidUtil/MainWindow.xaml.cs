using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using Reactive.Bindings;

namespace VoiceroidUtil
{
    /// <summary>
    /// メインウィンドウクラス。
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // エラーダイアログ表示コマンド設定
            {
                var cmd = new ReactiveCommand();
                cmd.Subscribe(
                    param =>
                        ruche.util.MessageBox.Show(
                            this,
                            param.ToString(),
                            @"エラー",
                            ruche.util.MessageBox.Button.Ok,
                            ruche.util.MessageBox.Icon.Error));
                this.ErrorDialogCommand = cmd;
            }

            // ディレクトリ選択ダイアログ表示コマンド設定
            {
                var cmd = new ReactiveCommand();
                cmd.Subscribe(this.ExecuteSelectDirectoryDialogCommand);
                this.SelectDirectoryDialogCommand = cmd;
            }
        }

        /// <summary>
        /// エラーダイアログ表示コマンドを取得する。
        /// </summary>
        public ICommand ErrorDialogCommand { get; }

        /// <summary>
        /// ディレクトリ選択ダイアログ表示コマンドを取得する。
        /// </summary>
        public ICommand SelectDirectoryDialogCommand { get; }

        /// <summary>
        /// SelectDirectoryDialogCommand の実処理を行う。
        /// </summary>
        /// <param name="param">コマンドパラメータ。</param>
        private void ExecuteSelectDirectoryDialogCommand(object param)
        {
            // パラメータ取得
            var p = param as MainWindowViewModel.SelectDirectoryDialogCommandParam;
            if (p == null)
            {
                return;
            }

            // ダイアログ表示可能か？
            if (!CommonOpenFileDialog.IsPlatformSupported)
            {
                p.Path = null;
                return;
            }

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = p.Title;
                dialog.InitialDirectory = p.Path;
                dialog.EnsureValidNames = true;
                dialog.EnsureFileExists = false;
                dialog.EnsurePathExists = false;

                if (dialog.ShowDialog(this) != CommonFileDialogResult.Ok)
                {
                    p.Path = null;
                    return;
                }

                p.Path = dialog.FileName;
            }
        }
    }
}
