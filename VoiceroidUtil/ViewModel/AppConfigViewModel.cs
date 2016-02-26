using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;
using VoiceroidUtil.Messaging;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// アプリ設定とそれに対する処理を提供する ViewModel クラス。
    /// </summary>
    public class AppConfigViewModel : Livet.ViewModel
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppConfigViewModel()
        {
            this.ConfigKeeper.Value = new AppConfig();

            // 修正可否
            this.CanModify = new ReactiveProperty<bool>(true);

            // 直近のアプリ状態値
            this.LastStatus =
                new ReactiveProperty<IAppStatus>(new AppStatus())
                    .AddTo(this.CompositeDisposable);

            // 保存先ディレクトリ選択コマンド
            this.SelectSaveDirectoryCommand =
                this.CanModify.ToReactiveCommand().AddTo(this.CompositeDisposable);
            this.SelectSaveDirectoryCommand
                .Subscribe(async _ => await this.ExecuteSelectSaveDirectoryCommand())
                .AddTo(this.CompositeDisposable);

            // 保存先ディレクトリオープンコマンド
            this.OpenSaveDirectoryCommand =
                (new ReactiveCommand()).AddTo(this.CompositeDisposable);
            this.OpenSaveDirectoryCommand
                .Subscribe(async _ => await this.ExecuteOpenSaveDirectoryCommand())
                .AddTo(this.CompositeDisposable);

            // ロードコマンド
            this.LoadCommand =
                this.CanModify.ToReactiveCommand().AddTo(this.CompositeDisposable);
            this.LoadCommand
                .Subscribe(async _ => await this.ExecuteLoadCommand())
                .AddTo(this.CompositeDisposable);

            // セーブコマンド
            this.SaveCommand = (new ReactiveCommand()).AddTo(this.CompositeDisposable);
            this.SaveCommand
                .Subscribe(async _ => await this.ExecuteSaveCommand())
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// 設定値を修正可能な状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// 既定では常に true を返す。外部からの設定以外で更新されることはない。
        /// </remarks>
        public ReactiveProperty<bool> CanModify { get; }

        /// <summary>
        /// アプリ設定値を取得または設定する。
        /// </summary>
        public AppConfig Value
        {
            get { return this.ConfigKeeper.Value; }
            set
            {
                var old = this.Value;
                this.ConfigKeeper.Value = value ?? (new AppConfig());
                if (this.Value != old)
                {
                    this.RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// 直近のアプリ状態値を取得する。
        /// </summary>
        public ReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// 保存先ディレクトリ選択コマンドを取得する。
        /// </summary>
        public ReactiveCommand SelectSaveDirectoryCommand { get; }

        /// <summary>
        /// 保存先ディレクトリオープンコマンドを取得する。
        /// </summary>
        public ReactiveCommand OpenSaveDirectoryCommand { get; }

        /// <summary>
        /// アプリ設定ロードコマンドを取得する。
        /// </summary>
        public ReactiveCommand LoadCommand { get; }

        /// <summary>
        /// アプリ設定セーブコマンドを取得する。
        /// </summary>
        public ReactiveCommand SaveCommand { get; }

        /// <summary>
        /// アプリ設定の保持と読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<AppConfig> ConfigKeeper { get; } =
            new ConfigKeeper<AppConfig>(nameof(VoiceroidUtil));

        /// <summary>
        /// SelectSaveDirectoryCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteSelectSaveDirectoryCommand()
        {
            // メッセージ送信
            var msg =
                await this.Messenger.GetResponseAsync(
                    new AppSaveDirectorySelectionMessage { Config = this.Value });

            // 結果の状態値を設定
            if (msg.Response != null)
            {
                this.LastStatus.Value = msg.Response;
            }
        }

        /// <summary>
        /// OpenSaveDirectoryCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteOpenSaveDirectoryCommand()
        {
            // メッセージ送信
            var msg =
                await this.Messenger.GetResponseAsync(
                    new DirectoryOpenMessage { Path = this.Value.SaveDirectoryPath });

            // 結果の状態値を設定
            // None なら今の状態を残す(状態リセットするほどの処理ではないため)
            if (msg.Response != null && msg.Response.StatusType != AppStatusType.None)
            {
                this.LastStatus.Value = msg.Response;
            }
        }

        /// <summary>
        /// LoadCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteLoadCommand()
        {
            if (await Task.Run(() => this.ConfigKeeper.Load()))
            {
                this.RaisePropertyChanged(nameof(this.Value));
            }
        }

        /// <summary>
        /// SaveCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteSaveCommand()
        {
            await Task.Run(() => this.ConfigKeeper.Save());
        }
    }
}
