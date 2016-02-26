using System;
using System.Linq;
using System.Reactive.Linq;
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
            this.Config = (new AppConfigViewModel()).AddTo(this.CompositeDisposable);
            this.LastStatus = (new AppStatusViewModel()).AddTo(this.CompositeDisposable);

            // Messenger を MainWindow のもので上書き
            this.Voiceroid.Messenger = this.Messenger;
            this.Config.Messenger = this.Messenger;
            this.LastStatus.Messenger = this.Messenger;

            // 子 ViewModel 間の関連付け
            this.Voiceroid.IsIdle.Subscribe(idle => this.Config.CanModify.Value = idle);
            this.Config
                .ObserveProperty(c => c.Value)
                .Subscribe(c => this.Voiceroid.Config.Value = c);
            Observable
                .Merge(this.Voiceroid.LastStatus, this.Config.LastStatus)
                .Where(s => s != null)
                .Subscribe(s => this.LastStatus.Value = s);
        }

        /// <summary>
        /// VOICEROID操作 ViewModel を取得する。
        /// </summary>
        public VoiceroidViewModel Voiceroid { get; }

        /// <summary>
        /// アプリ設定 ViewModel を取得する。
        /// </summary>
        public AppConfigViewModel Config { get; }

        /// <summary>
        /// 直近のアプリ状態 ViewModel を取得する。
        /// </summary>
        public AppStatusViewModel LastStatus { get; }
    }
}
