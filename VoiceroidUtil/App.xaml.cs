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
        /// メインウィンドウの最小許容縦幅を取得または設定する。
        /// </summary>
        private double MainWindowMinHeight { get; set; } = 0;

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

            // 最小許容縦幅を保存
            this.MainWindowMinHeight = window.MinHeight;

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
            var window = sender as Window;
            if (window != null)
            {
                // ビヘイビアによって最小許容縦幅が変動していたら元の値に補正
                var diff = window.MinHeight - this.MainWindowMinHeight;
                if (diff > 0)
                {
                    window.MinHeight -= diff;
                    window.Height -= diff;
                }
                else
                {
                    window.Height -= diff;
                    window.MinHeight -= diff;
                }

                // メインウィンドウ設定をセーブ
                this.MainWindowConfig.Value.CopyFrom(window);
                this.MainWindowConfig.Save();
            }
        }
    }
}
