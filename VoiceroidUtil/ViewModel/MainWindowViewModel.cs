using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;

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
            this.UIConfigKeeper.Value = new UIConfig();

            // 子 ViewModel 作成
            this.Voiceroid = (new VoiceroidViewModel()).AddTo(this.CompositeDisposable);
            this.AppConfig = (new AppConfigViewModel()).AddTo(this.CompositeDisposable);
            this.LastStatus = (new AppStatusViewModel()).AddTo(this.CompositeDisposable);

            // Messenger を MainWindowViewModel のもので上書き
            this.Voiceroid.Messenger = this.Messenger;
            this.AppConfig.Messenger = this.Messenger;
            this.LastStatus.Messenger = this.Messenger;

            // 同期コンテキスト取得
            var syncContext = SynchronizationContext.Current;

            // 同期コンテキスト設定
            this.UIConfig.SynchronizationContext = syncContext;
            this.AppConfig.Value.SynchronizationContext = syncContext;

            // UI設定を MainWindowViewModel のもので上書き
            this.Voiceroid.UIConfig.Value = this.UIConfig;
            this.AppConfig.UIConfig.Value = this.UIConfig;
            this
                .ObserveProperty(self => self.UIConfig)
                .Subscribe(
                    c =>
                    {
                        // 同期コンテキスト設定
                        c.SynchronizationContext = syncContext;

                        // 各 ViewModel に設定
                        this.Voiceroid.UIConfig.Value = c;
                        this.AppConfig.UIConfig.Value = c;
                    })
                .AddTo(this.CompositeDisposable);

            // アプリ設定を AppConfigViewModel のもので上書き
            this.Voiceroid.AppConfig.Value = this.AppConfig.Value;
            this.AppConfig
                .ObserveProperty(c => c.Value)
                .Subscribe(
                    c =>
                    {
                        // 同期コンテキスト設定
                        c.SynchronizationContext = syncContext;

                        // VoiceroidViewModel に設定
                        this.Voiceroid.AppConfig.Value = c;
                    })
                .AddTo(this.CompositeDisposable);

            // その他 ViewModel 間の関連付け
            this.Voiceroid.IsIdle
                .Subscribe(idle => this.AppConfig.CanModify.Value = idle)
                .AddTo(this.CompositeDisposable);
            Observable
                .Merge(this.Voiceroid.LastStatus, this.AppConfig.LastStatus)
                .Where(s => s != null)
                .Subscribe(s => this.LastStatus.Value = s)
                .AddTo(this.CompositeDisposable);

            // UI設定ロード実施済みフラグ
            this.IsUIConfigLoaded =
                new ReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);

            // UI設定ロードコマンド
            this.UIConfigLoadCommand =
                (new ReactiveCommand()).AddTo(this.CompositeDisposable);
            this.UIConfigLoadCommand
                .Subscribe(async _ => await this.ExecuteUIConfigLoadCommand())
                .AddTo(this.CompositeDisposable);

            // UI設定セーブコマンド
            this.UIConfigSaveCommand =
                this.IsUIConfigLoaded.ToReactiveCommand().AddTo(this.CompositeDisposable);
            this.UIConfigSaveCommand
                .Subscribe(async _ => await this.ExecuteUIConfigSaveCommand())
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// VOICEROID操作 ViewModel を取得する。
        /// </summary>
        public VoiceroidViewModel Voiceroid { get; }

        /// <summary>
        /// アプリ設定 ViewModel を取得する。
        /// </summary>
        public AppConfigViewModel AppConfig { get; }

        /// <summary>
        /// 直近のアプリ状態 ViewModel を取得する。
        /// </summary>
        public AppStatusViewModel LastStatus { get; }

        /// <summary>
        /// UI設定値を取得または設定する。
        /// </summary>
        public UIConfig UIConfig
        {
            get { return this.UIConfigKeeper.Value; }
            set
            {
                var old = this.UIConfig;
                this.UIConfigKeeper.Value = value ?? (new UIConfig());
                if (this.UIConfig != old)
                {
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// UI設定ロードコマンドを取得する。
        /// </summary>
        public ReactiveCommand UIConfigLoadCommand { get; }

        /// <summary>
        /// UI設定セーブコマンドを取得する。
        /// </summary>
        public ReactiveCommand UIConfigSaveCommand { get; }

        /// <summary>
        /// UI設定の保持と読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<UIConfig> UIConfigKeeper { get; } =
            new ConfigKeeper<UIConfig>(nameof(VoiceroidUtil));

        /// <summary>
        /// UI設定のロードが1回以上行われたか否かを取得する。
        /// </summary>
        private ReactiveProperty<bool> IsUIConfigLoaded { get; }

        /// <summary>
        /// UIConfigLoadCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteUIConfigLoadCommand()
        {
            // 成否に関わらずロード実施済みとする
            // プロパティ変更通知によりセーブコマンドが発行される場合があるため、
            // ロード処理よりも前に立てておく
            this.IsUIConfigLoaded.Value = true;

            if (await Task.Run(() => this.UIConfigKeeper.Load()))
            {
                this.RaisePropertyChanged(nameof(this.UIConfig));
            }
            else
            {
                // ロードに失敗した場合は現在値をセーブしておく
                await Task.Run(() => this.UIConfigKeeper.Save());
            }
        }

        /// <summary>
        /// UIConfigSaveCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteUIConfigSaveCommand()
        {
            // 1回以上 UIConfigLoadCommand が実施されていなければ処理しない
            if (!this.IsUIConfigLoaded.Value)
            {
                return;
            }

            await Task.Run(() => this.UIConfigKeeper.Save());
        }
    }
}
