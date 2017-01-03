using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Windows.Mvvm.Commands;
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

            // 選択中タブインデックス
            this.SelectedTabIndex =
                this.MakeInnerPropertyOf(uiConfig, c => c.AppConfigTabIndex);

            // 表示状態の YmmCharaRelation コレクション
            this.VisibleYmmCharaRelations =
                Observable
                    .CombineLatest(
                        this.ObserveConfigProperty(c => c.YmmCharaRelations),
                        this.ObserveConfigProperty(c => c.VoiceroidVisibilities),
                        (r, vv) => vv.SelectVisibleOf(r))
                    .ToReadOnlyReactiveProperty()
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
        /// 選択中タブインデックスを取得する。
        /// </summary>
        public IReactiveProperty<int> SelectedTabIndex { get; }

        /// <summary>
        /// アプリ設定値を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<AppConfig> Config => this.BaseConfig;

        /// <summary>
        /// 表示状態の YmmCharaRelation コレクションを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<IReadOnlyCollection<YmmCharaRelation>>
        VisibleYmmCharaRelations
        {
            get;
        }

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
        /// 保存先ディレクトリパス設定を更新する。
        /// </summary>
        /// <param name="path">ディレクトリパス。</param>
        private void UpdateSaveDirectoryPath(string path)
        {
            // パスが正常かチェック
            var status = FilePathUtil.CheckPathStatus(path, pathIsFile: false);

            // 正常でなければステータス更新のみ
            if (status.StatusType != AppStatusType.None)
            {
                this.LastStatus.Value = status;
                return;
            }

            // フルパスにして設定
            this.Config.Value.SaveDirectoryPath = Path.GetFullPath(path);

            // 成功ステータス設定
            this.SetLastStatus(
                AppStatusType.Success,
                @"保存先フォルダーを設定しました。",
                subStatusText: @"保存先フォルダーを開く",
                subStatusCommand: new ProcessStartCommand(path),
                subStatusCommandTip: path);
        }

        /// <summary>
        /// SelectSaveDirectoryCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteSelectSaveDirectoryCommand()
        {
            // ダイアログ処理
            var path =
                await this.OpenFileDialogService.Run(
                    title: @"音声保存先の選択",
                    initialDirectory: this.Config.Value.SaveDirectoryPath,
                    folderPicker: true);

            // 選択されたなら設定更新
            if (path != null)
            {
                this.UpdateSaveDirectoryPath(path);
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
                e.Effects = DragDropEffects.Copy | DragDropEffects.Move;
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

            // 末尾がディレクトリ区切り文字なら削除
            if (Path.GetFileName(path) == "")
            {
                // ドライブルートの場合は null が返ってくる
                path = Path.GetDirectoryName(path) ?? path;
            }

            // 設定更新
            this.UpdateSaveDirectoryPath(path);
        }

        /// <summary>
        /// 直近のアプリ状態を設定する。
        /// </summary>
        /// <param name="statusType">状態種別。</param>
        /// <param name="statusText">状態テキスト。</param>
        /// <param name="subStatusType">オプショナルなサブ状態種別。</param>
        /// <param name="subStatusText">オプショナルなサブ状態テキスト。</param>
        /// <param name="subStatusCommand">オプショナルなサブ状態コマンド。</param>
        /// <param name="subStatusCommandTip">
        /// オプショナルなサブ状態コマンドのチップテキスト。
        /// </param>
        private void SetLastStatus(
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "",
            ICommand subStatusCommand = null,
            string subStatusCommandTip = "")
        {
            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = statusType,
                    StatusText = statusText ?? "",
                    SubStatusType = subStatusType,
                    SubStatusText = subStatusText ?? "",
                    SubStatusCommand = subStatusCommand,
                    SubStatusCommandTip = subStatusCommandTip ?? "",
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
