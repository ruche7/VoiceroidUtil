using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using VoiceroidUtil.Extensions;
using VoiceroidUtil.Services;

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
        public AppConfigViewModel(
            IReadOnlyReactiveProperty<bool> canModify,
            IReadOnlyReactiveProperty<AppConfig> config,
            IReadOnlyReactiveProperty<UIConfig> uiConfig,
            IReactiveProperty<IAppStatus> lastStatus,
            IOpenFileDialogService openFileDialogService)
            : base(canModify, config)
        {
            this.ValidateArgNull(uiConfig, nameof(uiConfig));
            this.ValidateArgNull(lastStatus, nameof(lastStatus));
            this.ValidateArgNull(openFileDialogService, nameof(openFileDialogService));

            this.LastStatus = lastStatus;
            this.OpenFileDialogService = openFileDialogService;

            // UI開閉設定
            this.IsGeneralUIExpanded =
                uiConfig
                    .MakeInnerReactivePropery(c => c.IsGeneralConfigExpanded)
                    .AddTo(this.CompositeDisposable);
            this.IsSaveUIExpanded =
                uiConfig
                    .MakeInnerReactivePropery(c => c.IsSaveConfigExpanded)
                    .AddTo(this.CompositeDisposable);
            this.IsYmmUIExpanded =
                uiConfig
                    .MakeInnerReactivePropery(c => c.IsYmmConfigExpanded)
                    .AddTo(this.CompositeDisposable);

            // 保存先ディレクトリ選択コマンド
            this.SelectSaveDirectoryCommand =
                this.MakeAsyncCommand(
                    this.ExecuteSelectSaveDirectoryCommand,
                    this.CanModify);

            // 保存先ディレクトリオープンコマンド
            this.OpenSaveDirectoryCommand =
                this.MakeAsyncCommand(this.ExecuteOpenSaveDirectoryCommand);

            // 保存先ディレクトリドラッグオーバーコマンド
            this.DragOverSaveDirectoryCommand =
                this.MakeCommand<DragEventArgs>(
                    this.ExecuteDragOverSaveDirectoryCommand,
                    this.CanModify);

            // 保存先ディレクトリドロップコマンド
            this.DropSaveDirectoryCommand =
                this.MakeCommand<DragEventArgs>(
                    this.ExecuteDropSaveDirectoryCommand,
                    this.CanModify);
        }

        /// <summary>
        /// アプリ設定値を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<AppConfig> Config => this.BaseConfig;

        /// <summary>
        /// 一般設定UIを開いた状態にするか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsGeneralUIExpanded { get; }

        /// <summary>
        /// 音声保存設定UIを開いた状態にするか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsSaveUIExpanded { get; }

        /// <summary>
        /// ゆっくりMovieMaker連携設定UIを開いた状態にするか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsYmmUIExpanded { get; }

        /// <summary>
        /// 保存先ディレクトリ選択コマンドを取得する。
        /// </summary>
        public ICommand SelectSaveDirectoryCommand { get; }

        /// <summary>
        /// 保存先ディレクトリオープンコマンドを取得する。
        /// </summary>
        public ICommand OpenSaveDirectoryCommand { get; }

        /// <summary>
        /// 保存先ディレクトリドラッグオーバーコマンドを取得する。
        /// </summary>
        public ICommand DragOverSaveDirectoryCommand { get; }

        /// <summary>
        /// 保存先ディレクトリドロップコマンドを取得する。
        /// </summary>
        public ICommand DropSaveDirectoryCommand { get; }

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
        /// 直近のアプリ状態値の設定先を取得する。
        /// </summary>
        private IReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// ファイル選択ダイアログサービスを取得する。
        /// </summary>
        private IOpenFileDialogService OpenFileDialogService { get; }

        /// <summary>
        /// SelectSaveDirectoryCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteSelectSaveDirectoryCommand()
        {
            // ダイアログ処理
            var filePath =
                await this.OpenFileDialogService.Run(
                    title: @"音声保存先の選択",
                    initialDirectory: this.Config.Value.SaveDirectoryPath,
                    folderPicker: true);

            // 選択された？
            if (filePath != null)
            {
                // パスが正常かチェック
                var status = FilePathUtil.CheckPathStatus(filePath);
                if (status.StatusType == AppStatusType.None)
                {
                    // 正常ならアプリ設定を上書き
                    this.Config.Value.SaveDirectoryPath = filePath;
                }

                // ステータス更新
                this.LastStatus.Value = status;
            }
        }

        /// <summary>
        /// OpenSaveDirectoryCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteOpenSaveDirectoryCommand()
        {
            var path = this.Config.Value.SaveDirectoryPath;

            if (string.IsNullOrEmpty(path))
            {
                this.SetLastStatus(
                    AppStatusType.Warning,
                    @"保存先フォルダー未設定です。");
                return;
            }
            if (!Directory.Exists(path))
            {
                this.SetLastStatus(
                    AppStatusType.Warning,
                    @"保存先フォルダーが見つかりませんでした。");
                return;
            }

            try
            {
                await Task.Run(() => Process.Start(path));
            }
            catch
            {
                this.SetLastStatus(
                    AppStatusType.Fail,
                    @"保存先フォルダーを開けませんでした。");
                return;
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
                    this.Config.Value.SaveDirectoryPath = path;
                }

                this.LastStatus.Value = status;
            }
        }

        /// <summary>
        /// 直近のアプリ状態を設定する。
        /// </summary>
        /// <param name="statusType">状態種別。</param>
        /// <param name="statusText">状態テキスト。</param>
        /// <param name="subStatusType">オプショナルなサブ状態種別。</param>
        /// <param name="subStatusText">オプショナルなサブ状態テキスト。</param>
        /// <param name="subStatusCommand">オプショナルなサブ状態コマンド。</param>
        private void SetLastStatus(
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "",
            string subStatusCommand = "")
        {
            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = statusType,
                    StatusText = statusText ?? "",
                    SubStatusType = subStatusType,
                    SubStatusText = subStatusText ?? "",
                    SubStatusCommand = subStatusCommand ?? "",
                };
        }

        #region デザイン時用定義

        /// <summary>
        /// デザイン時用コンストラクタ。
        /// </summary>
        [Obsolete(@"Design time only.")]
        public AppConfigViewModel()
            :
            this(
                new ReactiveProperty<bool>(true),
                new ReactiveProperty<AppConfig>(new AppConfig()),
                new ReactiveProperty<UIConfig>(new UIConfig()),
                new ReactiveProperty<IAppStatus>(new AppStatus()),
                NullServices.OpenFileDialog)
        {
        }

        #endregion
    }
}
