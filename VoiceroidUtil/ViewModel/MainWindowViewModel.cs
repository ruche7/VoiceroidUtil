using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Reactive.Bindings.Extensions;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// メインウィンドウの ViewModel クラス。
    /// </summary>
    public class MainWindowViewModel : Livet.ViewModel
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public MainWindowViewModel()
        {
            // 子 ViewModel 作成
            this.Voiceroid = (new VoiceroidViewModel()).AddTo(this.CompositeDisposable);
            this.UIConfig = (new UIConfigViewModel()).AddTo(this.CompositeDisposable);
            this.TalkTextReplaceConfig =
                (new TalkTextReplaceConfigViewModel()).AddTo(this.CompositeDisposable);
            this.AppConfig = (new AppConfigViewModel()).AddTo(this.CompositeDisposable);
            this.LastStatus = (new AppStatusViewModel()).AddTo(this.CompositeDisposable);

            // Messenger を MainWindowViewModel のもので上書き
            this.Voiceroid.Messenger = this.Messenger;
            this.UIConfig.Messenger = this.Messenger;
            this.TalkTextReplaceConfig.Messenger = this.Messenger;
            this.AppConfig.Messenger = this.Messenger;
            this.LastStatus.Messenger = this.Messenger;

            // 同期コンテキスト取得
            var syncContext = SynchronizationContext.Current;

            // 同期コンテキスト設定
            this.UIConfig.Value.SynchronizationContext = syncContext;
            this.TalkTextReplaceConfig.Value.SynchronizationContext = syncContext;
            this.AppConfig.Value.SynchronizationContext = syncContext;

            // UI設定を UIConfigViewModel のもので上書き
            this.Voiceroid.UIConfig.Value = this.UIConfig.Value;
            this.TalkTextReplaceConfig.UIConfig.Value = this.UIConfig.Value;
            this.AppConfig.UIConfig.Value = this.UIConfig.Value;
            this.UIConfig
                .ObserveProperty(c => c.Value)
                .Subscribe(
                    c =>
                    {
                        // 同期コンテキスト設定
                        c.SynchronizationContext = syncContext;

                        // 各 ViewModel に設定
                        this.Voiceroid.UIConfig.Value = c;
                        this.TalkTextReplaceConfig.UIConfig.Value = c;
                        this.AppConfig.UIConfig.Value = c;
                    })
                .AddTo(this.CompositeDisposable);

            // トークテキスト置換設定を TalkTextReplaceConfigViewModel のもので上書き
            this.Voiceroid.TalkTextReplaceConfig.Value = this.TalkTextReplaceConfig.Value;
            this.TalkTextReplaceConfig
                .ObserveProperty(c => c.Value)
                .Subscribe(
                    c =>
                    {
                        // 同期コンテキスト設定
                        c.SynchronizationContext = syncContext;

                        // 各 ViewModel に設定
                        this.Voiceroid.TalkTextReplaceConfig.Value = c;
                    })
                .AddTo(this.CompositeDisposable);

            // アプリ設定を AppConfigViewModel のもので上書き
            this.Voiceroid.AppConfig.Value = this.AppConfig.Value;
            this.TalkTextReplaceConfig.AppConfig.Value = this.AppConfig.Value;
            this.AppConfig
                .ObserveProperty(c => c.Value)
                .Subscribe(
                    c =>
                    {
                        // 同期コンテキスト設定
                        c.SynchronizationContext = syncContext;

                        // 各 ViewModel に設定
                        this.Voiceroid.AppConfig.Value = c;
                        this.TalkTextReplaceConfig.AppConfig.Value = c;
                    })
                .AddTo(this.CompositeDisposable);

            // その他 ViewModel 間の関連付け
            this.Voiceroid.IsIdle
                .Subscribe(
                    idle =>
                    {
                        this.TalkTextReplaceConfig.CanModify.Value = idle;
                        this.AppConfig.CanModify.Value = idle;
                    })
                .AddTo(this.CompositeDisposable);
            Observable
                .Merge(
                    this.Voiceroid.LastStatus,
                    this.TalkTextReplaceConfig.LastStatus,
                    this.AppConfig.LastStatus)
                .Where(s => s != null)
                .Subscribe(s => this.LastStatus.Value = s)
                .AddTo(this.CompositeDisposable);

            // トレースリスナ設定
            var listener = Trace.Listeners[@"ErrorLogFile"] as ErrorLogFileTraceListener;
            if (listener != null)
            {
                // 音声ファイル保存先をエラーログファイル保存先として使う
                listener.DirectoryPathGetter =
                    () => this.AppConfig.Value.SaveDirectoryPath;
            }
        }

        /// <summary>
        /// タイトルを取得する。
        /// </summary>
        public string Title
        {
            get { return nameof(VoiceroidUtil); }
        }

        /// <summary>
        /// VOICEROID操作 ViewModel を取得する。
        /// </summary>
        public VoiceroidViewModel Voiceroid { get; }

        /// <summary>
        /// UI設定 ViewModel を取得する。
        /// </summary>
        public UIConfigViewModel UIConfig { get; }

        /// <summary>
        /// トークテキスト置換設定 ViewModel を取得する。
        /// </summary>
        public TalkTextReplaceConfigViewModel TalkTextReplaceConfig { get; }

        /// <summary>
        /// アプリ設定 ViewModel を取得する。
        /// </summary>
        public AppConfigViewModel AppConfig { get; }

        /// <summary>
        /// 直近のアプリ状態 ViewModel を取得する。
        /// </summary>
        public AppStatusViewModel LastStatus { get; }
    }
}
