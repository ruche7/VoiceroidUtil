// 設定ロード完了まで待つならば定義
// ウィンドウ表示後のコントロール表示変化を嫌うなら。
#define WAIT_ON_CONFIG_LOADED

using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Net;
using RucheHome.Voiceroid;
using RucheHome.Windows.Mvvm.Commands;
using VoiceroidUtil.Extensions;
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
            try
            {
                this.Initialize();
            }
            catch (Exception ex)
            {
                // 必要なファイル(DLL等)が不足しているとここに来る
                this.OnInitializeException(ex);
            }
        }

        /// <summary>
        /// IDisposable.Dispose をまとめて呼び出すためのコンテナを取得する。
        /// </summary>
        private CompositeDisposable CompositeDisposable { get; set; }

        /// <summary>
        /// 設定マネージャを取得する。
        /// </summary>
        private ConfigManager ConfigManager { get; set; }

        /// <summary>
        /// 直近のアプリ状態値の設定先を取得する。
        /// </summary>
        private IReactiveProperty<IAppStatus> LastStatus { get; set; }

        /// <summary>
        /// アプリ更新情報チェッカを取得する。
        /// </summary>
        private AppUpdateChecker UpdateChecker { get; set; }

        /// <summary>
        /// VOICEROIDプロセスファクトリを取得する。
        /// </summary>
        private ProcessFactory ProcessFactory { get; set; }

        /// <summary>
        /// プロパティの初期化を行う。
        /// </summary>
        /// <remarks>
        /// 実行に必要なファイル(DLL等)が不足している場合に例外を捕捉できるよう、
        /// コンストラクタの実処理をこのメソッドで行い、コンストラクタから呼び出す。
        /// </remarks>
        private void Initialize()
        {
            this.CompositeDisposable = new CompositeDisposable();
            this.ConfigManager =
                new ConfigManager(SynchronizationContext.Current)
                    .AddTo(this.CompositeDisposable);
            this.LastStatus =
                new ReactiveProperty<IAppStatus>(new AppStatus())
                    .AddTo(this.CompositeDisposable);
            this.UpdateChecker =
#if DEBUG
                new AppUpdateChecker(AppUpdateChecker.DefaultBaseUri + @".debug/")
#else // DEBUG
                new AppUpdateChecker()
#endif // DEBUG
                {
                    SynchronizationContext = SynchronizationContext.Current,
                };
            this.ProcessFactory = new ProcessFactory().AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// Initialize メソッドで例外が発生した時の処理を行う。
        /// </summary>
        /// <param name="ex">例外。</param>
        private void OnInitializeException(Exception ex)
        {
            try
            {
                // ダイアログ表示
                MessageBox.Show(
                    nameof(VoiceroidUtil) + @" の初期化に失敗しました。" +
                    Environment.NewLine +
                    @"フォルダ内の構成を変更してしまっていませんか？" +
                    Environment.NewLine + Environment.NewLine +
                    @"---------- 報告用エラー情報 ----------" + Environment.NewLine + ex,
                    nameof(VoiceroidUtil),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // ログファイル書き出し
                var logFilePath =
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData),
                        Assembly
                            .GetEntryAssembly()
                            .GetCustomAttribute<AssemblyCompanyAttribute>()
                            .Company,
                        nameof(VoiceroidUtil),
                        @"InitializeError.txt");
                File.WriteAllText(
                    logFilePath,
                    $@"[{DateTime.Now}]" + Environment.NewLine + ex,
                    new UTF8Encoding(false));
            }
            finally
            {
                // アプリ終了
                this.Shutdown(1);
            }
        }

        /// <summary>
        /// トレースリスナのセットアップを行う。
        /// </summary>
        private void SetupTraceListener()
        {
            if (Trace.Listeners[@"ErrorLogFile"] is ErrorLogFileTraceListener listener)
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
                .FirstAsync(f => f)
                .Where(_ => this.ConfigManager.AppConfig.Value.IsUpdateCheckingOnStartup)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(async _ => await this.UpdateChecker.Run())
                .AddTo(this.CompositeDisposable);

            // アプリ更新があるなら通知
            this.UpdateChecker
                .ObserveProperty(c => c.CanUpdate)
                .FirstAsync(f => f)
                .Subscribe(
                    _ =>
                        this.LastStatus.Value =
                            new AppStatus
                            {
                                StatusType = AppStatusType.Information,
                                StatusText =
                                    this.UpdateChecker.DisplayName +
                                    @" が公開されています。",
                                SubStatusText = @"ダウンロードページを開く",
                                SubStatusCommand =
                                    new ProcessStartCommand(
                                        this.UpdateChecker.PageUri.AbsoluteUri),
                                SubStatusCommandTip =
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

            // UI Automation 利用可能設定が更新されたら ProcessFactory に設定
            this.ConfigManager.AppConfig
                .ObserveInnerProperty(c => c.IsUIAutomationEnabledOnSave)
                .Subscribe(enabled => this.ProcessFactory.SetUIAutomationEnabled(enabled))
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// アプリケーションの開始時に呼び出される。
        /// </summary>
        private
#if WAIT_ON_CONFIG_LOADED
        async
#endif // WAIT_ON_CONFIG_LOADED
        void OnStartup(object sender, StartupEventArgs e)
        {
            // ObserveOnUIDispatcher でUIスレッドを使うために初期化
            UIDispatcherScheduler.Initialize();

            this.SetupTraceListener();
            this.SetupUpdateChecker();
            this.SetupVoiceroidProcess();

            // 設定ロード開始
            var loadTask = this.ConfigManager.Load().AddTo(this.CompositeDisposable);

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

#if WAIT_ON_CONFIG_LOADED
            // ロード完了まで待つ
            await loadTask;
#endif // WAIT_ON_CONFIG_LOADED

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
