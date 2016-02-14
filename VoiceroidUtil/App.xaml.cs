﻿using System;
using System.ComponentModel;
using System.Windows;
using ruche.util;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリケーションクラス。
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        private ConfigKeeper<AppConfig> AppConfig { get; } =
            new ConfigKeeper<AppConfig>(typeof(App).Namespace);

        /// <summary>
        /// メインウィンドウ設定を取得する。
        /// </summary>
        private ConfigKeeper<MainWindowConfig> MainWindowConfig { get; } =
            new ConfigKeeper<MainWindowConfig>(typeof(App).Namespace);

        /// <summary>
        /// アプリの開始時に呼び出される。
        /// </summary>
        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            // 設定をロード
            if (!this.AppConfig.Load())
            {
                this.AppConfig.Value = new AppConfig();
            }
            if (!this.MainWindowConfig.Load())
            {
                this.MainWindowConfig.Value = new MainWindowConfig();
            }

            // アプリ設定変更時のイベント設定
            this.AppConfig.Value.PropertyChanged += this.OnAppConfigPropertyChanged;

            // メインウィンドウ作成
            var window = new MainWindow();

            // ViewModel 作成
            var viewModel = new MainWindowViewModel(this.AppConfig.Value);

            // ダイアログ表示コマンド設定
            viewModel.ErrorDialogCommand.Value = window.ErrorDialogCommand;
            viewModel.SelectDirectoryDialogCommand.Value =
                window.SelectDirectoryDialogCommand;

            // メインウィンドウのパラメータ設定
            window.DataContext = viewModel;
            window.Closing += this.OnMainWindowClosing;

            // メインウィンドウ表示
            this.MainWindowConfig.Value.ApplyLocationTo(window);
            window.Show();
            this.MainWindowConfig.Value.ApplyMaximizedTo(window);
        }

        /// <summary>
        /// アプリ設定の変更時に呼び出される。
        /// </summary>
        private void OnAppConfigPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.AppConfig.Save();
        }

        /// <summary>
        /// メインウィンドウが閉じようとしている時に呼び出される。
        /// </summary>
        private void OnMainWindowClosing(object sender, CancelEventArgs e)
        {
            var window = sender as Window;
            if (window != null)
            {
                this.MainWindowConfig.Value.CopyFrom(window);
                this.MainWindowConfig.Save();
            }
        }
    }
}
