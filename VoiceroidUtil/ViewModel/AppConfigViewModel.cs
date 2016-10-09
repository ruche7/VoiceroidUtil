using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Net;
using VoiceroidUtil.Messaging;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// アプリ設定とそれに対する処理を提供する ViewModel クラス。
    /// </summary>
    public class AppConfigViewModel : ConfigViewModelBase<AppConfig>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppConfigViewModel() : base(new AppConfig())
        {
            // UI設定値
            this.UIConfig =
                new ReactiveProperty<UIConfig>(new UIConfig())
                    .AddTo(this.CompositeDisposable);

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

            // 保存先ディレクトリドラッグオーバーコマンド
            this.DragOverSaveDirectoryCommand =
                this.CanModify
                    .ToReactiveCommand<DragEventArgs>()
                    .AddTo(this.CompositeDisposable);
            this.DragOverSaveDirectoryCommand
                .Subscribe(e => this.ExecuteDragOverSaveDirectoryCommand(e))
                .AddTo(this.CompositeDisposable);

            // 保存先ディレクトリドロップコマンド
            this.DropSaveDirectoryCommand =
                this.CanModify
                    .ToReactiveCommand<DragEventArgs>()
                    .AddTo(this.CompositeDisposable);
            this.DropSaveDirectoryCommand
                .Subscribe(e => this.ExecuteDropSaveDirectoryCommand(e))
                .AddTo(this.CompositeDisposable);

            // アプリ更新情報チェックコマンド
            this.UpdateCheckCommand =
                this
                    .ObserveProperty(self => self.Value)
                    .Select(
                        config =>
                            (config == null) ?
                                Observable.Return(false) :
                                config.ObserveProperty(c => c.IsUpdateCheckingOnStartup))
                    .Switch()
                    .ToReactiveCommand(false)
                    .AddTo(this.CompositeDisposable);
            this.UpdateCheckCommand
                .Subscribe(async _ => await this.UpdateChecker.Run())
                .AddTo(this.CompositeDisposable);

            // アプリ更新があるなら通知
            this.UpdateChecker
                .ObserveProperty(c => c.CanUpdate)
                .Where(f => f)
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
                                SubStatusCommand = this.UpdateChecker.PageUri.AbsoluteUri,
                            });
        }

        /// <summary>
        /// UI設定値を取得する。
        /// </summary>
        public ReactiveProperty<UIConfig> UIConfig { get; }

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
        /// 保存先ディレクトリドラッグオーバーコマンドを取得する。
        /// </summary>
        public ReactiveCommand<DragEventArgs> DragOverSaveDirectoryCommand { get; }

        /// <summary>
        /// 保存先ディレクトリドロップコマンドを取得する。
        /// </summary>
        public ReactiveCommand<DragEventArgs> DropSaveDirectoryCommand { get; }

        /// <summary>
        /// アプリ更新情報チェックコマンドを取得する。
        /// </summary>
        public ReactiveCommand UpdateCheckCommand { get; }

        /// <summary>
        /// IDataObject オブジェクトから有効なディレクトリパスを検索する。
        /// </summary>
        /// <param name="data">IDataObject オブジェクト。</param>
        /// <returns>ディレクトリパス。見つからなければ null 。</returns>
        private static string FindDirectoryPath(IDataObject data)
        {
            if (data == null)
            {
                return null;
            }

            string path = null;

            if (data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // 複数ディレクトリドロップは不可とする
                var pathes = data.GetData(DataFormats.FileDrop, true) as string[];
                path = (pathes?.Length == 1) ? pathes[0] : null;
            }
            else if (data.GetDataPresent(DataFormats.Text, true))
            {
                path = (data.GetData(DataFormats.Text, true) as string)?.Trim();
                if (path != null)
                {
                    // 相対パスは不可とする
                    if (
                        path.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
                        !Path.IsPathRooted(path))
                    {
                        path = null;
                    }
                }
            }

            return (path != null && Directory.Exists(path)) ? path : null;
        }

        /// <summary>
        /// アプリ更新情報チェッカを取得する。
        /// </summary>
        private AppUpdateChecker UpdateChecker { get; } = new AppUpdateChecker();

        /// <summary>
        /// SelectSaveDirectoryCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteSelectSaveDirectoryCommand()
        {
#if true
            // メッセージ送信
            var msg =
                await this.Messenger.GetResponseAsync(
                    new OpenFileDialogMessage
                    {
                        IsFolderPicker = true,
                        Title = @"音声保存先の選択",
                        InitialDirectory = this.Value.SaveDirectoryPath,
                    });

            // 選択された？
            if (msg.Response != null)
            {
                // パスが正常かチェック
                var status = FilePathUtil.CheckPathStatus(msg.Response);
                if (status.StatusType == AppStatusType.None)
                {
                    // 正常ならアプリ設定を上書き
                    this.Value.SaveDirectoryPath = msg.Response;
                }

                // ステータス更新
                this.LastStatus.Value = status;
            }
#else
            // メッセージ送信
            var msg =
                await this.Messenger.GetResponseAsync(
                    new AppSaveDirectorySelectionMessage { Config = this.Value });

            // 結果の状態値を設定
            if (msg.Response != null)
            {
                this.LastStatus.Value = msg.Response;
            }
#endif
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
        /// DragOverSaveDirectoryCommand の実処理を行う。
        /// </summary>
        /// <param name="e">ドラッグイベントデータ。</param>
        private void ExecuteDragOverSaveDirectoryCommand(DragEventArgs e)
        {
            // 有効なパスがあれば受け入れエフェクト設定
            if (FindDirectoryPath(e?.Data) != null)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        /// <summary>
        /// DropSaveDirectoryCommand の実処理を行う。
        /// </summary>
        /// <param name="e">ドラッグイベントデータ。</param>
        private void ExecuteDropSaveDirectoryCommand(DragEventArgs e)
        {
            var path = FindDirectoryPath(e?.Data);
            if (path == null)
            {
                return;
            }

            e.Handled = true;

            // 末尾がディレクトリ区切り文字なら削除し、フルパスにする
            if (Path.GetFileName(path) == "")
            {
                path = Path.GetDirectoryName(path);
            }
            path = Path.GetFullPath(path);

            // パスが正常かチェック
            var status = FilePathUtil.CheckPathStatus(path);
            if (status != null)
            {
                // 正常ならパス上書き
                if (status.StatusType == AppStatusType.None)
                {
                    this.Value.SaveDirectoryPath = path;
                }

                this.LastStatus.Value = status;
            }
        }
    }
}
