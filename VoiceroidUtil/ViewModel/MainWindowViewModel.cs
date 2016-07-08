using System;
using System.Linq;
using System.Reactive.Linq;
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

            // Messenger を MainWindow のもので上書き
            this.Voiceroid.Messenger = this.Messenger;
            this.AppConfig.Messenger = this.Messenger;
            this.LastStatus.Messenger = this.Messenger;

            // ViewModel 間の関連付け
            this
                .ObserveProperty(self => self.UIConfig)
                .Subscribe(c => this.AppConfig.UIConfig.Value = c)
                .AddTo(this.CompositeDisposable);
            this.Voiceroid.IsIdle
                .Subscribe(idle => this.AppConfig.CanModify.Value = idle)
                .AddTo(this.CompositeDisposable);
            this.AppConfig
                .ObserveProperty(c => c.Value)
                .Subscribe(c => this.Voiceroid.Config.Value = c)
                .AddTo(this.CompositeDisposable);
            Observable
                .Merge(this.Voiceroid.LastStatus, this.AppConfig.LastStatus)
                .Where(s => s != null)
                .Subscribe(s => this.LastStatus.Value = s)
                .AddTo(this.CompositeDisposable);

            // UI設定ロードコマンド作成
            this.UIConfigLoadCommand =
                (new ReactiveCommand()).AddTo(this.CompositeDisposable);
            this.UIConfigLoadCommand
                .Subscribe(async _ => await this.ExecuteUIConfigLoadCommand())
                .AddTo(this.CompositeDisposable);

            // UI設定セーブコマンド作成
            this.UIConfigSaveCommand =
                (new ReactiveCommand()).AddTo(this.CompositeDisposable);
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
        /// UIConfigLoadCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteUIConfigLoadCommand()
        {
            if (await Task.Run(() => this.UIConfigKeeper.Load()))
            {
                this.RaisePropertyChanged(nameof(this.UIConfig));
            }
        }

        /// <summary>
        /// UIConfigSaveCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteUIConfigSaveCommand()
        {
            await Task.Run(() => this.UIConfigKeeper.Save());
        }
    }
}
