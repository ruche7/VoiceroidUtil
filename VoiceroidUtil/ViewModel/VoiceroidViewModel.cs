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
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Text;
using RucheHome.Util.Extensions.String;
using RucheHome.Voiceroid;
using VoiceroidUtil.Extensions;
using VoiceroidUtil.Services;

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
        /// <param name="processes">VOICEROIDプロセスコレクション。</param>
        /// <param name="canUseConfig">各設定値を利用可能な状態であるか否か。</param>
        /// <param name="talkTextReplaceConfig">トークテキスト置換設定値。</param>
        /// <param name="exoConfig">AviUtl拡張編集ファイル用設定値。</param>
        /// <param name="appConfig">アプリ設定値。</param>
        /// <param name="uiConfig">UI設定値。</param>
        /// <param name="lastStatus">直近のアプリ状態値の設定先。</param>
        /// <param name="canModifyNotifier">
        /// 設定変更可能な状態であるか否かの設定先。
        /// </param>
        /// <param name="windowActivateService">ウィンドウアクティブ化サービス。</param>
        /// <param name="voiceroidActionService">
        /// VOICEROIDプロセスアクションサービス。
        /// </param>
        public VoiceroidViewModel(
            IReadOnlyCollection<IProcess> processes,
            IReadOnlyReactiveProperty<bool> canUseConfig,
            IReadOnlyReactiveProperty<TalkTextReplaceConfig> talkTextReplaceConfig,
            IReadOnlyReactiveProperty<ExoConfig> exoConfig,
            IReadOnlyReactiveProperty<AppConfig> appConfig,
            IReadOnlyReactiveProperty<UIConfig> uiConfig,
            IReactiveProperty<IAppStatus> lastStatus,
            IReactiveProperty<bool> canModifyNotifier,
            IWindowActivateService windowActivateService,
            IVoiceroidActionService voiceroidActionService)
        {
            this.ValidateArgNull(processes, nameof(processes));
            this.ValidateArgNull(canUseConfig, nameof(canUseConfig));
            this.ValidateArgNull(talkTextReplaceConfig, nameof(talkTextReplaceConfig));
            this.ValidateArgNull(exoConfig, nameof(exoConfig));
            this.ValidateArgNull(appConfig, nameof(appConfig));
            this.ValidateArgNull(uiConfig, nameof(uiConfig));
            this.ValidateArgNull(lastStatus, nameof(lastStatus));
            this.ValidateArgNull(canModifyNotifier, nameof(canModifyNotifier));
            this.ValidateArgNull(windowActivateService, nameof(windowActivateService));
            this.ValidateArgNull(voiceroidActionService, nameof(voiceroidActionService));

            this.LastStatus = lastStatus;
            this.WindowActivateService = windowActivateService;
            this.VoiceroidActionService = voiceroidActionService;

            this.IsTextClearing =
                this.MakeInnerPropertyOf(appConfig, c => c.IsTextClearing);
            this.VoiceroidExecutablePathes =
                this.MakeInnerPropertyOf(uiConfig, c => c.VoiceroidExecutablePathes);

            // 表示状態のVOICEROIDプロセスコレクション
            this.VisibleProcesses =
                appConfig
                    .ObserveInnerProperty(c => c.VoiceroidVisibilities)
                    .Select(vv => vv.SelectVisibleOf(processes))
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // VOICEROID選択コマンド実行可能状態
            // 表示状態のVOICEROIDプロセスが2つ以上あれば選択可能
            this.IsSelectVoiceroidCommandExecutable =
                this.VisibleProcesses
                    .Select(vp => vp.Count >= 2)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // VOICEROID選択コマンドのチップテキスト
            this.SelectVoiceroidCommandTip =
                this.VisibleProcesses
                    .Select(_ => this.MakeSelectVoiceroidCommandTip())
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // 最適表示列数
            // 6プロセス単位で列数を増やす
            this.VisibleProcessesColumnCount =
                this.VisibleProcesses
                    .Select(vp => Math.Min(Math.Max(1, (vp.Count + 5) / 6), 3))
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // 選択中VOICEROIDプロセス
            this.SelectedProcess =
                new ReactiveProperty<IProcess>(this.VisibleProcesses.Value.First())
                    .AddTo(this.CompositeDisposable);

            // UI設定周りのセットアップ
            this.SetupUIConfig(uiConfig, processes);

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
            this.IsProcessExecutable =
                Observable
                    .CombineLatest(
                        this.SelectedProcess,
                        uiConfig.ObserveInnerProperty(c => c.VoiceroidExecutablePathes),
                        (p, pathes) =>
                        {
                            var path = (p == null) ? null : pathes[p.Id]?.Path;
                            return (!string.IsNullOrEmpty(path) && File.Exists(path));
                        })
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            var processSaving = this.ObserveSelectedProcessProperty(p => p.IsSaving);
            var processDialogShowing =
                this.ObserveSelectedProcessProperty(p => p.IsDialogShowing);

            // トークテキスト
            this.TalkText =
                new ReactiveProperty<string>("").AddTo(this.CompositeDisposable);
            this.TalkTextLengthLimit =
                new ReactiveProperty<int>(TextComponent.TextLengthLimit)
                    .AddTo(this.CompositeDisposable);
            this.IsTalkTextTabAccepted =
                this.MakeInnerPropertyOf(appConfig, c => c.IsTabAccepted);

            // 非同期実行コマンドヘルパー
            var playStopCommandExecuter =
                new PlayStopCommandExecuter(
                    () => this.SelectedProcess.Value,
                    () => talkTextReplaceConfig.Value.VoiceReplaceItems,
                    () => this.TalkText.Value,
                    this.OnPlayStopCommandExecuted);
            var saveCommandExecuter =
                new SaveCommandExecuter(
                    () => this.SelectedProcess.Value,
                    () => talkTextReplaceConfig.Value,
                    () => exoConfig.Value,
                    () => appConfig.Value,
                    () => this.TalkText.Value,
                    this.OnSaveCommandExecuted);
            var dropTalkTextFileCommandExecuter =
                new AsyncCommandExecuter<DragEventArgs>(
                    this.ExecuteDropTalkTextFileCommand);

            // 再生も音声保存もしていない時をアイドル状態とみなす
            this.IsIdle =
                new[]
                {
                    playStopCommandExecuter.IsExecutable,
                    saveCommandExecuter.IsExecutable,
                }
                .CombineLatestValuesAreAllTrue()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.CompositeDisposable);

            // アイドル状態なら設定変更可能とする
            this.IsIdle.Subscribe(f => canModifyNotifier.Value = f);

            // 音声保存しており、保存成功時クリア設定が有効ならトークテキスト編集不可
            this.IsTalkTextEditable =
                new[]
                {
                    saveCommandExecuter.IsExecutable.Inverse(),
                    this.IsTextClearing,
                }
                .CombineLatestValuesAreAllTrue()
                .Inverse()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.CompositeDisposable);

            // VOICEROID選択コマンドコレクション(要素数 10 固定)
            this.SelectVoiceroidCommands =
                new ReadOnlyCollection<ICommand>(
                    Enumerable.Range(0, 10)
                        .Select(
                            index =>
                                this.MakeCommand(
                                    () => this.ExecuteSelectVoiceroidCommand(index),
                                    this.IsSelectVoiceroidCommandExecutable,
                                    this.VisibleProcesses.Select(vp => index < vp.Count)))
                        .ToArray());

            // 前方/後方VOICEROID選択コマンド
            this.SelectPreviousVoiceroidCommand =
                this.MakeCommand(
                    this.ExecuteSelectPreviousVoiceroidCommand,
                    this.IsSelectVoiceroidCommandExecutable);
            this.SelectNextVoiceroidCommand =
                this.MakeCommand(
                    this.ExecuteSelectNextVoiceroidCommand,
                    this.IsSelectVoiceroidCommandExecutable);

            // 起動/終了コマンド
            this.RunExitCommand =
                this.MakeAsyncCommand(
                    this.ExecuteRunExitCommand,
                    canUseConfig,
                    this.IsIdle,
                    this.IsProcessStartup.Inverse(),
                    processSaving.Inverse(),
                    processDialogShowing.Inverse());

            // 再生/停止コマンド
            this.PlayStopCommand =
                this.MakeAsyncCommand(
                    playStopCommandExecuter,
                    canUseConfig,
                    this.IsIdle,
                    this.IsProcessRunning,
                    processSaving.Inverse(),
                    processDialogShowing.Inverse(),
                    new[]
                    {
                        this.IsProcessPlaying,
                        this.TalkText.Select(t => !string.IsNullOrWhiteSpace(t)),
                    }
                    .CombineLatest(flags => flags.Any(f => f)),
                    dropTalkTextFileCommandExecuter.IsExecutable);

            // 保存コマンド
            this.SaveCommand =
                this.MakeAsyncCommand(
                    saveCommandExecuter,
                    canUseConfig,
                    this.IsIdle,
                    this.IsProcessRunning,
                    processSaving.Inverse(),
                    processDialogShowing.Inverse(),
                    this.TalkText.Select(t => !string.IsNullOrWhiteSpace(t)),
                    dropTalkTextFileCommandExecuter.IsExecutable);

            // トークテキスト用ファイルドラッグオーバーコマンド
            this.DragOverTalkTextFileCommand =
                this.MakeCommand<DragEventArgs>(
                    this.ExecuteDragOverTalkTextFileCommand,
                    this.IsTalkTextEditable);

            // トークテキスト用ファイルドロップコマンド
            this.DropTalkTextFileCommand =
                this.MakeAsyncCommand(
                    dropTalkTextFileCommandExecuter,
                    this.IsTalkTextEditable);
        }

        /// <summary>
        /// 表示状態のVOICEROIDプロセスコレクションを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<ReadOnlyCollection<IProcess>> VisibleProcesses
        {
            get;
        }

        /// <summary>
        /// VOICEROID選択コマンドを実行可能な状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// (VisibleProcesses.Value.Count >= 2) の判定結果を返す。
        /// </remarks>
        public IReadOnlyReactiveProperty<bool> IsSelectVoiceroidCommandExecutable { get; }

        /// <summary>
        /// VOICEROID選択コマンドのチップテキストを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<string> SelectVoiceroidCommandTip { get; }

        /// <summary>
        /// 表示状態のVOICEROIDプロセスコレクションの最適表示列数を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<int> VisibleProcessesColumnCount { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスを取得する。
        /// </summary>
        public IReactiveProperty<IProcess> SelectedProcess { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスが起動中であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsProcessStartup { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスが実行中であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsProcessRunning { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスがトークテキストを再生中であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsProcessPlaying { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスが実行ファイルパス登録済みであるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsProcessExecutable { get; }

        /// <summary>
        /// トークテキストを取得する。
        /// </summary>
        public IReactiveProperty<string> TalkText { get; }

        /// <summary>
        /// トークテキストの最大許容文字数を取得する。
        /// </summary>
        /// <remarks>
        /// 0 ならば上限を定めない。
        /// TalkText に直接文字列を設定する場合、この値は考慮されない。
        /// </remarks>
        public IReactiveProperty<int> TalkTextLengthLimit { get; }

        /// <summary>
        /// トークテキストにタブ文字の入力を受け付けるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsTalkTextTabAccepted { get; }

        /// <summary>
        /// トークテキストを編集可能な状態であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsTalkTextEditable { get; }

        /// <summary>
        /// アイドル状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// 再生も音声保存も実行中でなければ true となる。
        /// </remarks>
        public IReadOnlyReactiveProperty<bool> IsIdle { get; }

        /// <summary>
        /// VOICEROID選択コマンドコレクションを取得する。
        /// </summary>
        /// <remarks>
        /// 要素数は 10 固定。表示中VOICEROIDのインデックスに対応する。
        /// </remarks>
        public ReadOnlyCollection<ICommand> SelectVoiceroidCommands { get; }

        /// <summary>
        /// 前方VOICEROID選択コマンドを取得する。
        /// </summary>
        public ICommand SelectPreviousVoiceroidCommand { get; }

        /// <summary>
        /// 後方VOICEROID選択コマンドを取得する。
        /// </summary>
        public ICommand SelectNextVoiceroidCommand { get; }

        /// <summary>
        /// 実行/終了コマンドを取得する。
        /// </summary>
        public ICommand RunExitCommand { get; }

        /// <summary>
        /// 再生/停止コマンドを取得する。
        /// </summary>
        public ICommand PlayStopCommand { get; }

        /// <summary>
        /// 保存コマンドを取得する。
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// トークテキスト用ファイルドラッグオーバーコマンドを取得する。
        /// </summary>
        public ICommand DragOverTalkTextFileCommand { get; }

        /// <summary>
        /// トークテキスト用ファイルドロップコマンドを取得する。
        /// </summary>
        public ICommand DropTalkTextFileCommand { get; }

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
        /// 音声保存成功時にテキストをクリアするか否かを取得する。
        /// </summary>
        private IReadOnlyReactiveProperty<bool> IsTextClearing { get; }

        /// <summary>
        /// VOICEROIDの実行ファイルパスセットを取得する。
        /// </summary>
        private IReadOnlyReactiveProperty<VoiceroidExecutablePathSet>
        VoiceroidExecutablePathes
        {
            get;
        }

        /// <summary>
        /// 直近のアプリ状態値の設定先を取得する。
        /// </summary>
        private IReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// ウィンドウアクティブ化サービスを取得する。
        /// </summary>
        private IWindowActivateService WindowActivateService { get; }

        /// <summary>
        /// VOICEROIDプロセスアクションサービスを取得する。
        /// </summary>
        private IVoiceroidActionService VoiceroidActionService { get; }

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
        /// <param name="uiConfig">UI設定値。</param>
        /// <param name="processes">VOICEROIDプロセスコレクション。</param>
        private void SetupUIConfig(
            IReadOnlyReactiveProperty<UIConfig> uiConfig,
            IReadOnlyCollection<IProcess> processes)
        {
            // 設定変更時に選択中プロセス反映
            Observable
                .CombineLatest(
                    this.VisibleProcesses,
                    uiConfig.ObserveInnerProperty(c => c.VoiceroidId),
                    (vp, id) => vp.FirstOrDefault(p => p.Id == id) ?? vp.First())
                .Subscribe(p => this.SelectedProcess.Value = p)
                .AddTo(this.CompositeDisposable);

            // 選択中プロセス変更時処理
            // 上書きは即座に行うとうまくいかないので少し待ちを入れる
            this.SelectedProcess
                .Where(p => p != null)
                .Subscribe(p => uiConfig.Value.VoiceroidId = p.Id)
                .AddTo(this.CompositeDisposable);
            this.SelectedProcess
                .Throttle(TimeSpan.FromMilliseconds(10))
                .Where(p => p == null)
                .Subscribe(
                    _ =>
                        this.SelectedProcess.Value =
                            processes.First(p => p.Id == uiConfig.Value.VoiceroidId))
                .AddTo(this.CompositeDisposable);

            // 実行ファイルパス反映用デリゲート
            Action<VoiceroidId, string> pathSetter =
                (id, path) =>
                {
                    // パスが有効な場合のみ反映する
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        uiConfig.Value.VoiceroidExecutablePathes[id].Path = path;
                    }
                };

            // UI設定変更時に実行ファイルパスを反映する
            uiConfig
                .Subscribe(
                    c =>
                    {
                        foreach (var process in processes)
                        {
                            pathSetter(process.Id, process.ExecutablePath);
                        }
                    })
                .AddTo(this.CompositeDisposable);

            // VOICEROIDプロセスの実行ファイルパスが判明したらUI設定に反映する
            foreach (var process in processes)
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
        /// VOICEROID選択コマンドのチップテキストを作成する。
        /// </summary>
        /// <returns>チップテキスト。表示不要ならば null 。</returns>
        private string MakeSelectVoiceroidCommandTip() =>
            !this.IsSelectVoiceroidCommandExecutable.Value ?
                null :
                @"F1/F2 : 前/次のVOICEROIDを選択" + Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    this.VisibleProcesses.Value
                        .Take(10)
                        .Select(
                            (p, i) =>
                                @"Ctrl+" + ((i < 9) ? (i + 1) : 0) + @" : " +
                                p.Name + @" を選択"));

        /// <summary>
        /// メインウィンドウをアクティブにする。
        /// </summary>
        private Task ActivateMainWindow() => this.WindowActivateService.Run();

        /// <summary>
        /// VOICEROIDプロセスに対してアクションを行う。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        /// <param name="action">アクション種別。</param>
        private Task RaiseVoiceroidAction(IProcess process, VoiceroidAction action) =>
            this.VoiceroidActionService.Run(process, action);

        /// <summary>
        /// 各 SelectVoiceroidCommands コマンドの実処理を行う。
        /// </summary>
        /// <param name="index">インデックス。</param>
        private void ExecuteSelectVoiceroidCommand(int index)
        {
            if (
                this.IsSelectVoiceroidCommandExecutable.Value &&
                index < this.VisibleProcesses.Value.Count)
            {
                this.SelectedProcess.Value = this.VisibleProcesses.Value[index];
            }
        }

        /// <summary>
        /// SelectPreviousVoiceroidCommand コマンドの実処理を行う。
        /// </summary>
        private void ExecuteSelectPreviousVoiceroidCommand()
        {
            var index =
                Array.IndexOf(
                    this.VisibleProcesses.Value.Select(p => p.Id).ToArray(),
                    this.SelectedProcess.Value.Id);
            if (index >= 0)
            {
                --index;
                this.ExecuteSelectVoiceroidCommand(
                    (index < 0) ? (this.VisibleProcesses.Value.Count - 1) : index);
            }
        }

        /// <summary>
        /// SelectNextVoiceroidCommand コマンドの実処理を行う。
        /// </summary>
        private void ExecuteSelectNextVoiceroidCommand()
        {
            var index =
                Array.IndexOf(
                    this.VisibleProcesses.Value.Select(p => p.Id).ToArray(),
                    this.SelectedProcess.Value.Id);
            if (index >= 0)
            {
                ++index;
                this.ExecuteSelectVoiceroidCommand(
                    (index < this.VisibleProcesses.Value.Count) ? index : 0);
            }
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
                var info = this.VoiceroidExecutablePathes.Value?[process.Id];
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
        }

        /// <summary>
        /// PlayStopCommand コマンド完了時処理を行う。
        /// </summary>
        /// <param name="result">アプリステータス。</param>
        /// <param name="parameter">コマンドパラメータ。</param>
        private async Task OnPlayStopCommandExecuted(
            IAppStatus result,
            PlayStopCommandExecuter.Parameter parameter)
        {
            if (result != null)
            {
                // アプリ状態更新
                this.LastStatus.Value = result;
            }

            // 成否に関わらず再生処理が行われた時の処理
            if (parameter?.IsPlayAction == true)
            {
                // 対象VOICEROIDを前面へ
                await this.RaiseVoiceroidAction(
                    parameter.Process,
                    VoiceroidAction.Forward);

                // メインウィンドウを前面へ
                await this.ActivateMainWindow();
            }
        }

        /// <summary>
        /// SaveCommand コマンド完了時処理を行う。
        /// </summary>
        /// <param name="result">アプリステータス。</param>
        /// <param name="parameter">コマンドパラメータ。</param>
        private async Task OnSaveCommandExecuted(
            IAppStatus result,
            SaveCommandExecuter.Parameter parameter)
        {
            if (result != null)
            {
                // アプリ状態更新
                this.LastStatus.Value = result;

                // 保存成功時のトークテキストクリア処理
                if (
                    this.IsTextClearing.Value &&
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
                e.Effects = DragDropEffects.Copy | DragDropEffects.Move;
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
                warnText = text.Length + @" 文字以降は切り捨てました。";
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
                    SubStatusCommandTip =
                        string.IsNullOrEmpty(subStatusCommandTip) ?
                            null : subStatusCommandTip,
                };
        }

        #region デザイン時用定義

        /// <summary>
        /// デザイン時用コンストラクタ。
        /// </summary>
        [Obsolete(@"Design time only.")]
        public VoiceroidViewModel()
            :
            this(
                new ProcessFactory().Processes,
                new ReactiveProperty<bool>(true),
                new ReactiveProperty<TalkTextReplaceConfig>(new TalkTextReplaceConfig()),
                new ReactiveProperty<ExoConfig>(new ExoConfig()),
                new ReactiveProperty<AppConfig>(new AppConfig()),
                new ReactiveProperty<UIConfig>(new UIConfig()),
                new ReactiveProperty<IAppStatus>(new AppStatus()),
                new ReactiveProperty<bool>(true),
                NullServices.WindowActivate,
                NullServices.VoiceroidAction)
        {
        }

        #endregion
    }
}
