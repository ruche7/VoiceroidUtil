using System;
using System.ComponentModel;
using System.Windows;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリケーションクラス。
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// メインウィンドウ設定を取得する。
        /// </summary>
        private ConfigKeeper<MainWindowConfig> MainWindowConfig { get; } =
            new ConfigKeeper<MainWindowConfig>(nameof(VoiceroidUtil));

        /// <summary>
        /// アプリの開始時に呼び出される。
        /// </summary>
        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            // メインウィンドウ設定をロード
            if (!this.MainWindowConfig.Load())
            {
                this.MainWindowConfig.Value = new MainWindowConfig();
            }

            // メインウィンドウ作成
            var window = new MainWindow();
            window.Closing += this.OnMainWindowClosing;

            // メインウィンドウ表示
            this.MainWindowConfig.Value.ApplyLocationTo(window);
            window.Show();
            this.MainWindowConfig.Value.ApplyMaximizedTo(window);
        }

        /// <summary>
        /// メインウィンドウが閉じようとしている時に呼び出される。
        /// </summary>
        private void OnMainWindowClosing(object sender, CancelEventArgs e)
        {
            // メインウィンドウ設定をセーブ
            var window = sender as Window;
            if (window != null)
            {
                this.MainWindowConfig.Value.CopyFrom(window);
                this.MainWindowConfig.Save();
            }
        }
    }
}
