using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Net;
using RucheHome.Voiceroid;
using VoiceroidUtil.Services;
using VoiceroidUtil.View;
using VoiceroidUtil.ViewModel;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリケーションクラス。
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public App()
        {
            this.ConfigManager = new ConfigManager().AddTo(this.CompositeDisposable);
            this.LastStatus =
                new ReactiveProperty<IAppStatus>(new AppStatus())
                    .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// IDisposable.Dispose をまとめて呼び出すためのコンテナを取得する。
        /// </summary>
        private CompositeDisposable CompositeDisposable { get; } =
            new CompositeDisposable();

        /// <summary>
        /// 設定マネージャを取得する。
        /// </summary>
        private ConfigManager ConfigManager { get; }

        /// <summary>
        /// 直近のアプリ状態値の設定先を取得する。
        /// </summary>
        private IReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// アプリ更新情報チェッカを取得する。
        /// </summary>
        private AppUpdateChecker UpdateChecker { get; } = new AppUpdateChecker();

        /// <summary>
        /// VOICEROIDプロセスファクトリを取得する。
        /// </summary>
        private ProcessFactory ProcessFactory { get; } = new ProcessFactory();

        /// <summary>
        /// トレースリスナのセットアップを行う。
        /// </summary>
        private void SetupTraceListener()
        {
            var listener = Trace.Listeners[@"ErrorLogFile"] as ErrorLogFileTraceListener;
            if (listener != null)
            {
                // 音声ファイル保存先をエラーログファイル保存先として使う
                listener.DirectoryPathGetter =
                    () => this.ConfigManager.AppConfig.Value.SaveDirectoryPath;
            }
        }

        /// <summary>
        /// アプリ更新情報チェッカ関連のセットアップを行う。
        /// </summary>
        private void SetupUpdateChecker()
        {
            // 設定ロード完了時にアプリ更新チェック設定が有効ならチェック開始
            this.ConfigManager.IsLoaded
                .Where(f => f)
                .Take(1)
                .Where(_ => this.ConfigManager.AppConfig.Value.IsUpdateCheckingOnStartup)
                .Subscribe(async _ => await this.UpdateChecker.Run())
                .AddTo(this.CompositeDisposable);

            // アプリ更新があるなら通知
            this.UpdateChecker
                .ObserveProperty(c => c.CanUpdate)
                .Where(f => f)
                .Take(1)
                .Subscribe(
                    _ =>
                        this.LastStatus.Value =
                            new AppStatus
                            {
                                StatusType = AppStatusType.Information,
                                StatusText =
                                    @"version " +
                                    this.UpdateChecker.NewestVersion +
                                    @" が公開されています。",
                                SubStatusText = @"ダウンロードページを開く",
                                SubStatusCommand =
                                    this.UpdateChecker.PageUri.AbsoluteUri,
                            })
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// VOICEROIDプロセス関連のセットアップを行う。
        /// </summary>
        private void SetupVoiceroidProcess()
        {
            // プロセス更新タイマ設定＆開始
            var processUpdateTimer =
                new ReactiveTimer(TimeSpan.FromMilliseconds(100))
                    .AddTo(this.CompositeDisposable);
            processUpdateTimer
                .Subscribe(_ => this.ProcessFactory.Update())
                .AddTo(this.CompositeDisposable);
            processUpdateTimer.Start();
        }

        /// <summary>
        /// アプリケーションの開始時に呼び出される。
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            this.SetupTraceListener();
            this.SetupUpdateChecker();
            this.SetupVoiceroidProcess();

            // 設定ロード開始
            this.ConfigManager.Load().AddTo(this.CompositeDisposable);

            // ロード済みかつ非ロード中なら設定使用可能
            var canUseConfig =
                new[]
                {
                    this.ConfigManager.IsLoaded,
                    this.ConfigManager.IsLoading.Inverse(),
                }
                .CombineLatestValuesAreAllTrue()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.CompositeDisposable);

            // メインウィンドウ作成
            var mainWindow = new MainWindow();

            // ViewModel 設定
            mainWindow.DataContext =
                new MainWindowViewModel(
                    this.ProcessFactory.Processes,
                    canUseConfig,
                    this.ConfigManager.TalkTextReplaceConfig,
                    this.ConfigManager.ExoConfig,
                    this.ConfigManager.AppConfig,
                    this.ConfigManager.UIConfig,
                    this.LastStatus,
                    new OpenFileDialogService(mainWindow),
                    new WindowActivateService(mainWindow),
                    new VoiceroidActionService(mainWindow))
                    .AddTo(this.CompositeDisposable);

            // 表示
            mainWindow.Show();
        }

        /// <summary>
        /// アプリケーションの終了時に呼び出される。
        /// </summary>
        private void OnExit(object sender, ExitEventArgs e)
        {
            this.CompositeDisposable.Dispose();
        }
    }
}
