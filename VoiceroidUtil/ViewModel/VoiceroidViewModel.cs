using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Livet.Messaging.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Text;
using RucheHome.Util.Extensions.String;
using RucheHome.Voiceroid;
using VoiceroidUtil.Messaging;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// VOICEROIDの選択とその操作を提供する ViewModel クラス。
    /// </summary>
    public class VoiceroidViewModel : ViewModelBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidViewModel()
        {
            // 設定
            this.TalkTextReplaceConfig =
                new ReactiveProperty<TalkTextReplaceConfig>(new TalkTextReplaceConfig())
                    .AddTo(this.CompositeDisposable);
            this.AppConfig =
                new ReactiveProperty<AppConfig>(new AppConfig())
                    .AddTo(this.CompositeDisposable);
            this.UIConfig =
                new ReactiveProperty<UIConfig>(new UIConfig())
                    .AddTo(this.CompositeDisposable);

            // 直近のアプリ状態値
            this.LastStatus =
                new ReactiveProperty<IAppStatus>(new AppStatus())
                    .AddTo(this.CompositeDisposable);

            // 選択中VOICEROIDプロセス
            this.SelectedProcess =
                new ReactiveProperty<IProcess>(
                    this.ProcessFactory.Get(VoiceroidId.YukariEx))
                    .AddTo(this.CompositeDisposable);

            // UI設定周りのセットアップ
            this.SetupUIConfig();

            // 選択プロセス状態
            this.IsProcessStartup =
                this
                    .ObserveSelectedProcessProperty(p => p.IsStartup)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.IsProcessRunning =
                this
                    .ObserveSelectedProcessProperty(p => p.IsRunning)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.IsProcessPlaying =
                this
                    .ObserveSelectedProcessProperty(p => p.IsPlaying)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.IsProcessSaving =
                this
                    .ObserveSelectedProcessProperty(p => p.IsSaving)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.IsProcessDialogShowing =
                this
                    .ObserveSelectedProcessProperty(p => p.IsDialogShowing)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.IsProcessExecutable =
                Observable.CombineLatest(
                    this.SelectedProcess,
                    this.UIConfig
                        .Select(
                            config =>
                                (config == null) ?
                                    Observable.Empty<VoiceroidExecutablePathSet>() :
                                    config.ObserveProperty(
                                        c => c.VoiceroidExecutablePathes))
                        .Switch(),
                    (p, pathes) =>
                    {
                        var path = pathes[p.Id]?.Path;
                        return (!string.IsNullOrEmpty(path) && File.Exists(path));
                    })
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // トークテキスト
            this.TalkText =
                new ReactiveProperty<string>("")
                    .AddTo(this.CompositeDisposable);
            this.TalkTextLengthLimit =
                new ReactiveProperty<int>(TextComponent.TextLengthLimit)
                    .AddTo(this.CompositeDisposable);

            // 非同期実行コマンドヘルパー
            var runExitCommandExecuter =
                new AsyncCommandExecuter(this.ExecuteRunExitCommand)
                    .AddTo(this.CompositeDisposable);
            var playStopCommandExecuter =
                new AsyncCommandExecuter(this.ExecutePlayStopCommand)
                    .AddTo(this.CompositeDisposable);
            var saveCommandExecuter =
                new SaveCommandExecuter(
                    () => this.SelectedProcess.Value,
                    () => this.TalkTextReplaceConfig.Value,
                    () => this.AppConfig.Value,
                    () => this.TalkText.Value,
                    async r => await this.OnSaveCommandExecuted(r))
                    .AddTo(this.CompositeDisposable);
            var dropTalkTextFileCommandExecuter =
                new AsyncCommandExecuter<DragEventArgs>(
                    this.ExecuteDropTalkTextFileCommand)
                    .AddTo(this.CompositeDisposable);

            // どの非同期コマンドも実行可能ならばアイドル状態とみなす
            this.IsIdle =
                new[]
                {
                    runExitCommandExecuter.ObserveExecutable(),
                    playStopCommandExecuter.ObserveExecutable(),
                    saveCommandExecuter.ObserveExecutable(),
                    dropTalkTextFileCommandExecuter.ObserveExecutable(),
                }
                .CombineLatestValuesAreAllTrue()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.CompositeDisposable);

            // 実行/終了コマンド
            this.RunExitCommand =
                this.MakeAsyncCommand(
                    runExitCommandExecuter,
                    this.IsIdle,
                    this.IsProcessStartup.Select(f => !f),
                    this.IsProcessSaving.Select(f => !f),
                    this.IsProcessDialogShowing.Select(f => !f));

            // 再生/停止コマンド
            this.PlayStopCommand =
                this.MakeAsyncCommand(
                    playStopCommandExecuter,
                    this.IsIdle,
                    this.IsProcessRunning,
                    this.IsProcessSaving.Select(f => !f),
                    this.IsProcessDialogShowing.Select(f => !f),
                    new[]
                    {
                        this.IsProcessPlaying,
                        this.TalkText.Select(t => !string.IsNullOrWhiteSpace(t)),
                    }
                    .CombineLatest(flags => flags.Any(f => f)));

            // 保存コマンド
            this.SaveCommand =
                this.MakeAsyncCommand(
                    saveCommandExecuter,
                    this.IsIdle,
                    this.IsProcessRunning,
                    this.IsProcessSaving.Select(f => !f),
                    this.IsProcessDialogShowing.Select(f => !f),
                    this.TalkText.Select(t => !string.IsNullOrWhiteSpace(t)));

            // トークテキスト用ファイルドラッグオーバーコマンド
            this.DragOverTalkTextFileCommand =
                this.MakeCommand<DragEventArgs>(
                    this.ExecuteDragOverTalkTextFileCommand,
                    this.IsIdle);

            // トークテキスト用ファイルドロップコマンド
            this.DropTalkTextFileCommand =
                this.MakeAsyncCommand(dropTalkTextFileCommandExecuter, this.IsIdle);

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
        /// トークテキスト置換設定を取得する。
        /// </summary>
        /// <remarks>
        /// 外部からの設定以外で更新されることはない。
        /// </remarks>
        public ReactiveProperty<TalkTextReplaceConfig> TalkTextReplaceConfig { get; }

        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        /// <remarks>
        /// 外部からの設定以外で更新されることはない。
        /// </remarks>
        public ReactiveProperty<AppConfig> AppConfig { get; }

        /// <summary>
        /// UI設定値を取得する。
        /// </summary>
        public ReactiveProperty<UIConfig> UIConfig { get; }

        /// <summary>
        /// 直近のアプリ状態値を取得する。
        /// </summary>
        public ReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// VOICEROIDプロセスリストを取得する。
        /// </summary>
        public ReadOnlyCollection<IProcess> Processes
        {
            get { return this.ProcessFactory.Processes; }
        }

        /// <summary>
        /// 選択中のVOICEROIDプロセスを取得する。
        /// </summary>
        public ReactiveProperty<IProcess> SelectedProcess { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスが起動中であるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsProcessStartup { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスが実行中であるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsProcessRunning { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスが再生中であるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsProcessPlaying { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスがWAVEファイル保存中であるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsProcessSaving { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスがダイアログ表示中であるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsProcessDialogShowing { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスが実行ファイルパス登録済みであるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsProcessExecutable { get; }

        /// <summary>
        /// トークテキストを取得する。
        /// </summary>
        public ReactiveProperty<string> TalkText { get; }

        /// <summary>
        /// トークテキストの最大許容文字数を取得する。
        /// </summary>
        /// <remarks>
        /// 0 ならば上限を定めない。
        /// TalkText に直接文字列を設定する場合、この値は考慮されない。
        /// </remarks>
        public ReactiveProperty<int> TalkTextLengthLimit { get; }

        /// <summary>
        /// アイドル状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// いずれの非同期コマンドも実行中でなければ true となる。
        /// </remarks>
        public ReadOnlyReactiveProperty<bool> IsIdle { get; }

        /// <summary>
        /// 実行/終了コマンドを取得する。
        /// </summary>
        public ReactiveCommand RunExitCommand { get; }

        /// <summary>
        /// 再生/停止コマンドを取得する。
        /// </summary>
        public ReactiveCommand PlayStopCommand { get; }

        /// <summary>
        /// 保存コマンドを取得する。
        /// </summary>
        public ReactiveCommand SaveCommand { get; }

        /// <summary>
        /// トークテキスト用ファイルドラッグオーバーコマンドを取得する。
        /// </summary>
        public ReactiveCommand<DragEventArgs> DragOverTalkTextFileCommand { get; }

        /// <summary>
        /// トークテキスト用ファイルドロップコマンドを取得する。
        /// </summary>
        public ReactiveCommand<DragEventArgs> DropTalkTextFileCommand { get; }

        /// <summary>
        /// IDataObject オブジェクトからファイルパス配列を取得する。
        /// </summary>
        /// <param name="data">IDataObject オブジェクト。</param>
        /// <returns>ファイルパス配列。取得できなければ null 。</returns>
        private static string[] GetFilePathes(IDataObject data)
        {
            if (data == null || !data.GetDataPresent(DataFormats.FileDrop, true))
            {
                return null;
            }

            var pathes = data.GetData(DataFormats.FileDrop, true) as string[];
            if (pathes == null || pathes.Length == 0)
            {
                return null;
            }

            return pathes.All(p => File.Exists(p)) ? pathes : null;
        }

        /// <summary>
        /// VOICEROIDプロセスファクトリを取得する。
        /// </summary>
        private ProcessFactory ProcessFactory { get; } = new ProcessFactory();

        /// <summary>
        /// 選択中のVOICEROIDプロセスのプロパティ変更を監視する
        /// IObservable{T} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">プロパティの型。</typeparam>
        /// <param name="propertySelector">プロパティセレクタ。</param>
        /// <returns>IObservable{T} オブジェクト。</returns>
        /// <remarks>
        /// VOICEROIDプロセスが非選択状態になった場合は
        /// default(T) を一度だけ返す IObservable{T} オブジェクトを作成する。
        /// </remarks>
        private IObservable<T> ObserveSelectedProcessProperty<T>(
            Expression<Func<IProcess, T>> propertySelector)
        {
            return
                this.SelectedProcess
                    .Select(
                        p =>
                            (p == null) ?
                                Observable.Return(default(T)) :
                                p.ObserveProperty(propertySelector))
                    .Switch();
        }

        /// <summary>
        /// UI設定周りのセットアップを行う。
        /// </summary>
        private void SetupUIConfig()
        {
            // UI設定変更時に選択プロセス反映
            this.UIConfig
                .Select(
                    config =>
                        (config == null) ?
                            Observable.Empty<VoiceroidId>() :
                            config.ObserveProperty(c => c.VoiceroidId))
                .Switch()
                .Subscribe(id => this.SelectedProcess.Value = this.ProcessFactory.Get(id))
                .AddTo(this.CompositeDisposable);

            // 選択プロセス変更時処理
            this.SelectedProcess
                .Subscribe(
                    p =>
                    {
                        // UI設定へ反映
                        if (p != null && this.UIConfig.Value != null)
                        {
                            this.UIConfig.Value.VoiceroidId = p.Id;
                        }

                        // アプリ状態リセット
                        this.ResetLastStatus();
                    })
                .AddTo(this.CompositeDisposable);

            // 実行ファイルパス反映用デリゲート
            Action<VoiceroidId, string> pathSetter =
                (id, path) =>
                {
                    // パスが有効な場合のみ反映する
                    if (
                        this.UIConfig.Value != null &&
                        !string.IsNullOrEmpty(path) &&
                        File.Exists(path))
                    {
                        this.UIConfig.Value.VoiceroidExecutablePathes[id].Path = path;
                    }
                };

            // UI設定変更時に実行ファイルパスを反映する
            this.UIConfig
                .Subscribe(
                    c =>
                    {
                        foreach (var process in this.Processes)
                        {
                            pathSetter(process.Id, process.ExecutablePath);
                        }
                    })
                .AddTo(this.CompositeDisposable);

            // VOICEROIDプロセスの実行ファイルパスが判明したらUI設定に反映する
            foreach (var process in this.Processes)
            {
                var id = process.Id;

                // 現在値を設定
                pathSetter(id, process.ExecutablePath);

                // 変更時に反映する
                process
                    .ObserveProperty(p => p.ExecutablePath)
                    .Subscribe(path => pathSetter(id, path))
                    .AddTo(this.CompositeDisposable);
            }
        }

        /// <summary>
        /// メインウィンドウをアクティブにする。
        /// </summary>
        private Task ActivateMainWindow()
        {
            return
                this.Messenger.RaiseAsync(
                    new WindowActionMessage(
                        WindowAction.Active,
                        MessageKeys.WindowActionMessageKey));
        }

        /// <summary>
        /// VOICEROIDプロセスに対してアクションを行う。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        /// <param name="action">アクション種別。</param>
        private Task RaiseVoiceroidAction(IProcess process, VoiceroidAction action)
        {
            return
                this.Messenger.RaiseAsync(
                    new VoiceroidActionMessage(
                        process,
                        action,
                        MessageKeys.VoiceroidActionMessageKey));
        }

        /// <summary>
        /// RunExitCommand コマンドの実処理を行う。
        /// </summary>
        private async Task ExecuteRunExitCommand()
        {
            var process = this.SelectedProcess.Value;
            if (process == null)
            {
                this.SetLastStatus(AppStatusType.Fail, @"処理を開始できませんでした。");
                return;
            }

            if (process.IsRunning)
            {
                // プロセス終了
                if (!(await process.Exit()))
                {
                    this.SetLastStatus(
                        process.IsDialogShowing ?
                            AppStatusType.Warning : AppStatusType.Fail,
                        @"VOICEROIDを終了できませんでした。",
                        subStatusText:
                            process.IsDialogShowing ?
                                @"ダイアログが表示されたため中止しました。" : @"");
                    return;
                }
            }
            else
            {
                // パス情報取得
                var info = this.UIConfig.Value?.VoiceroidExecutablePathes[process.Id];
                if (info == null)
                {
                    this.SetLastStatus(
                        AppStatusType.Fail,
                        @"処理を開始できませんでした。");
                    return;
                }

                // 未登録か？
                if (string.IsNullOrEmpty(info.Path))
                {
                    this.SetLastStatus(
                        AppStatusType.Warning,
                        @"VOICEROID情報が未登録のため起動できません。",
                        subStatusText: @"一度手動で起動することで登録されます。");
                    return;
                }

                // ファイルが見つからない？
                if (!File.Exists(info.Path))
                {
                    this.SetLastStatus(
                        AppStatusType.Warning,
                        @"VOICEROIDの実行ファイルが見つかりません。",
                        subStatusText: @"一度手動で起動し直してください。");
                    return;
                }

                // プロセス起動
                try
                {
                    if (!(await process.Run(info.Path)))
                    {
                        this.SetLastStatus(
                            AppStatusType.Fail,
                            @"VOICEROIDを起動できませんでした。");
                        return;
                    }

                    // スタートアップを終えたら1回だけメインウィンドウをアクティブにする
                    this.IsProcessStartup
                        .FirstAsync(f => !f)
                        .Subscribe(_ => this.ActivateMainWindow());
                }
                catch (Exception ex)
                {
                    this.SetLastStatus(
                        AppStatusType.Fail,
                        @"VOICEROIDを起動できませんでした。",
                        subStatusText: @"内部情報: " + ex.GetType().Name);
                    return;
                }
            }

            // 成功時はアプリ状態リセット
            this.ResetLastStatus();
        }

        /// <summary>
        /// PlayStopCommand コマンドの実処理を行う。
        /// </summary>
        private async Task ExecutePlayStopCommand()
        {
            var process = this.SelectedProcess.Value;
            if (process == null)
            {
                this.SetLastStatus(AppStatusType.Fail, @"処理を開始できませんでした。");
                return;
            }

            if (process.IsPlaying)
            {
                if (!(await process.Stop()))
                {
                    this.SetLastStatus(AppStatusType.Fail, @"停止処理に失敗しました。");
                    return;
                }
                this.SetLastStatus(AppStatusType.Success, @"停止処理に成功しました。");
            }
            else
            {
                // テキスト作成
                var text = this.TalkText.Value;
                text =
                    this.TalkTextReplaceConfig.Value?.VoiceReplaceItems.Replace(text) ??
                    text;

                // テキスト設定
                if (!(await process.SetTalkText(text)))
                {
                    this.SetLastStatus(AppStatusType.Fail, @"文章の設定に失敗しました。");
                    return;
                }

                // 再生
                try
                {
                    if (!(await process.Play()))
                    {
                        this.SetLastStatus(
                            AppStatusType.Fail,
                            @"再生処理に失敗しました。");
                        return;
                    }
                    this.SetLastStatus(AppStatusType.Success, @"再生処理に成功しました。");
                }
                finally
                {
                    // 対象VOICEROIDを前面へ
                    await this.RaiseVoiceroidAction(process, VoiceroidAction.Forward);

                    // メインウィンドウを前面へ
                    await this.ActivateMainWindow();
                }
            }
        }

        /// <summary>
        /// SaveCommand コマンド完了時処理を行う。
        /// </summary>
        private async Task OnSaveCommandExecuted(IAppStatus result)
        {
            if (result != null)
            {
                // アプリ状態更新
                this.LastStatus.Value = result;

                // 保存成功時のトークテキストクリア処理
                if (
                    this.AppConfig.Value?.IsTextClearing == true &&
                    result.StatusType == AppStatusType.Success)
                {
                    this.TalkText.Value = "";
                }
            }

            // 対象VOICEROIDのタスクバーボタン点滅を止める
            await this.RaiseVoiceroidAction(
                this.SelectedProcess.Value,
                VoiceroidAction.StopFlash);

            // メインウィンドウを前面へ
            await this.ActivateMainWindow();
        }

        /// <summary>
        /// DragOverTalkTextFileCommand コマンドの実処理を行う。
        /// </summary>
        /// <param name="e">ドラッグイベントデータ。</param>
        private void ExecuteDragOverTalkTextFileCommand(DragEventArgs e)
        {
            if (GetFilePathes(e?.Data) != null)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        /// <summary>
        /// トークテキスト用ファイル単体のMB単位の最大許容サイズ。
        /// </summary>
        private const int TalkTextFileSizeLimitMB = 5;

        /// <summary>
        /// DropTalkTextFileCommand コマンドの実処理を行う。
        /// </summary>
        /// <param name="e">ドラッグイベントデータ。</param>
        private async Task ExecuteDropTalkTextFileCommand(DragEventArgs e)
        {
            var pathes = GetFilePathes(e?.Data);
            if (pathes == null)
            {
                return;
            }

            e.Handled = true;

            // ファイルに関する情報を取得
            var f =
                await Task.Run(
                    () =>
                    {
                        var infos = Array.ConvertAll(pathes, p => new FileInfo(p));
                        var maxInfo =
                            infos.Aggregate((i1, i2) => (i1.Length > i2.Length) ? i1 : i2);
                        return new { infos, maxInfo };
                    });

            // ファイルサイズチェック
            if (f.maxInfo.Length > TalkTextFileSizeLimitMB * 1024L * 1024)
            {
                this.SetLastStatus(
                    AppStatusType.Warning,
                    f.maxInfo.Name + @" のファイルサイズが大きすぎます。",
                    subStatusText:
                        @"許容サイズは " + TalkTextFileSizeLimitMB + @" MBまでです。");
                return;
            }

            // 全ファイル読み取り
            List<string> fileTexts = null;
            try
            {
                fileTexts = await Task.Run(() => TextFileReader.ReadAll(f.infos));
            }
            catch
            {
                this.SetLastStatus(
                    AppStatusType.Fail,
                    @"ファイルの読み取りに失敗しました。");
                return;
            }

            // 読み取り失敗したファイルがある？
            var failIndex = fileTexts.IndexOf(null);
            if (failIndex >= 0)
            {
                this.SetLastStatus(
                    AppStatusType.Warning,
                    f.infos[failIndex].Name + @" の読み取りに失敗しました。",
                    subStatusText: @"テキストファイルではない可能性があります。");
                return;
            }

            // 最大文字数
            int lenLimit = this.TalkTextLengthLimit.Value;
            if (lenLimit == 0)
            {
                lenLimit = int.MaxValue;
            }

            // 文字列連結
            var text = new StringBuilder();
            foreach (var t in fileTexts)
            {
                // 空文字列でなく、末尾が改行以外ならば改行追加
                if (text.Length > 0)
                {
                    var end = text[text.Length - 1];
                    if (end != '\r' && end != '\n')
                    {
                        text.AppendLine();
                    }
                }

                if (text.Length >= lenLimit)
                {
                    break;
                }
                text.Append(t);
            }

            // 許容文字数以上は切り捨てる
            string warnText = null;
            if (text.Length > lenLimit)
            {
                text.RemoveSurrogateSafe(lenLimit);
                warnText = text.Length + @" 文字以上は切り捨てました。";
            }

            // テキスト設定
            this.TalkText.Value = text.ToString();

            this.SetLastStatus(
                AppStatusType.Success,
                @"テキストファイルから文章を設定しました。",
                (warnText == null) ? AppStatusType.None : AppStatusType.Warning,
                warnText);
        }

        /// <summary>
        /// 直近のアプリ状態をリセットする。
        /// </summary>
        private void ResetLastStatus()
        {
            this.LastStatus.Value = new AppStatus();
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
    }
}
