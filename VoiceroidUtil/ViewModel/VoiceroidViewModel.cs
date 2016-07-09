using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Hnx8.ReadJEnc;
using Livet.Messaging.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Voiceroid;
using VoiceroidUtil.Messaging;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// VOICEROIDの選択とその操作を提供する ViewModel クラス。
    /// </summary>
    public class VoiceroidViewModel : Livet.ViewModel
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidViewModel()
        {
            // アプリ設定
            this.Config =
                new ReactiveProperty<AppConfig>(new AppConfig())
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

            // アプリ設定変更時に選択プロセス反映
            this.Config
                .SelectMany(
                    config =>
                        (config == null) ?
                            Observable.Empty<VoiceroidId>() :
                            config.ObserveProperty(c => c.VoiceroidId))
                .Subscribe(id => this.SelectedProcess.Value = this.ProcessFactory.Get(id))
                .AddTo(this.CompositeDisposable);

            // 選択プロセス変更時処理
            this.SelectedProcess
                .Subscribe(
                    p =>
                    {
                        // アプリ設定へ反映
                        if (p != null && this.Config.Value != null)
                        {
                            this.Config.Value.VoiceroidId = p.Id;
                        }

                        // アプリ状態リセット
                        this.ResetLastStatus();
                    })
                .AddTo(this.CompositeDisposable);

            // 選択プロセス状態
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

            // トークテキスト
            this.TalkText =
                new ReactiveProperty<string>("")
                    .AddTo(this.CompositeDisposable);
            this.TalkTextLengthLimit =
                new ReactiveProperty<int>(100000)
                    .AddTo(this.CompositeDisposable);

            // コマンド実行用
            this.PlayStopCommandExecuter =
                new AsyncCommandExecuter(this.ExecutePlayStopCommand)
                    .AddTo(this.CompositeDisposable);
            this.SaveCommandExecuter =
                new SaveCommandExecuter(
                    this.ProcessFactory,
                    () => this.Config.Value,
                    () => this.TalkText.Value,
                    async r => await this.OnSaveCommandExecuted(r))
                    .AddTo(this.CompositeDisposable);
            this.DropTalkTextFileCommandExecuter =
                new AsyncCommandExecuter<DragEventArgs>(
                    this.ExecuteDropTalkTextFileCommand)
                    .AddTo(this.CompositeDisposable);

            // どの非同期コマンドも実行可能ならばアイドル状態とみなす
            this.IsIdle =
                new[]
                {
                    this.PlayStopCommandExecuter.ObserveExecutable(),
                    this.SaveCommandExecuter.ObserveExecutable(),
                    this.DropTalkTextFileCommandExecuter.ObserveExecutable(),
                }
                .CombineLatestValuesAreAllTrue()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.CompositeDisposable);

            // 再生/停止コマンド
            this.PlayStopCommand =
                new[]
                {
                    this.IsIdle,
                    this.IsProcessRunning,
                    this.IsProcessSaving.Select(f => !f),
                    this.IsProcessDialogShowing.Select(f => !f),
                    new[]
                    {
                        this.IsProcessPlaying,
                        this.TalkText.Select(t => !string.IsNullOrWhiteSpace(t)),
                    }
                    .CombineLatest(flags => flags.Any(f => f)),
                }
                .CombineLatestValuesAreAllTrue()
                .ToReactiveCommand(false)
                .AddTo(this.CompositeDisposable);
            this.PlayStopCommand
                .Subscribe(this.PlayStopCommandExecuter.Execute)
                .AddTo(this.CompositeDisposable);

            // 保存コマンド
            this.SaveCommand =
                new[]
                {
                    this.IsIdle,
                    this.IsProcessRunning,
                    this.IsProcessSaving.Select(f => !f),
                    this.IsProcessDialogShowing.Select(f => !f),
                    this.TalkText.Select(t => !string.IsNullOrWhiteSpace(t)),
                }
                .CombineLatestValuesAreAllTrue()
                .ToReactiveCommand(false)
                .AddTo(this.CompositeDisposable);
            this.SaveCommand
                .Subscribe(this.SaveCommandExecuter.Execute)
                .AddTo(this.CompositeDisposable);

            // トークテキスト用ファイルドラッグオーバーコマンド
            this.DragOverTalkTextFileCommand =
                this.IsIdle
                    .ToReactiveCommand<DragEventArgs>(false)
                    .AddTo(this.CompositeDisposable);
            this.DragOverTalkTextFileCommand
                .Subscribe(this.ExecuteDragOverTalkTextFileCommand)
                .AddTo(this.CompositeDisposable);

            // トークテキスト用ファイルドロップコマンド
            this.DropTalkTextFileCommand =
                this.IsIdle
                    .ToReactiveCommand<DragEventArgs>(false)
                    .AddTo(this.CompositeDisposable);
            this.DropTalkTextFileCommand
                .Subscribe(this.DropTalkTextFileCommandExecuter.Execute)
                .AddTo(this.CompositeDisposable);

            // プロセス更新タイマ設定＆開始
            this.ProcessUpdateTimer =
                new ReactiveTimer(TimeSpan.FromMilliseconds(100))
                    .AddTo(this.CompositeDisposable);
            this.ProcessUpdateTimer
                .Subscribe(_ => this.ProcessFactory.Update())
                .AddTo(this.CompositeDisposable);
            this.ProcessUpdateTimer.Start();
        }

        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        /// <remarks>
        /// 外部からの設定以外で更新されることはない。
        /// </remarks>
        public ReactiveProperty<AppConfig> Config { get; }

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
        /// テキストファイルを非同期で読み取り、 StringBuilder に追加する。
        /// </summary>
        /// <param name="reader">リーダー。</param>
        /// <param name="fileInfo">ファイル情報。</param>
        /// <param name="dest">追加先の StringBuilder 。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        private static Task<bool> AppendTextFromFile(
            FileReader reader,
            FileInfo fileInfo,
            StringBuilder dest)
        {
            return
                Task.Run(
                    () =>
                    {
                        reader.Read(fileInfo);
                        if (reader.Text == null)
                        {
                            return false;
                        }
                        dest.Append(reader.Text);
                        return true;
                    });
        }

        /// <summary>
        /// VOICEROIDプロセスファクトリを取得する。
        /// </summary>
        private ProcessFactory ProcessFactory { get; } = new ProcessFactory();

        /// <summary>
        /// VOICEROIDプロセス更新タイマを取得する。
        /// </summary>
        private ReactiveTimer ProcessUpdateTimer { get; }

        /// <summary>
        /// 再生/停止コマンドの非同期実行用オブジェクトを取得する。
        /// </summary>
        private AsyncCommandExecuter PlayStopCommandExecuter { get; }

        /// <summary>
        /// 保存コマンドの非同期実行用オブジェクトを取得する。
        /// </summary>
        private SaveCommandExecuter SaveCommandExecuter { get; }

        /// <summary>
        /// トークテキスト用ファイルドロップコマンドの非同期実行用オブジェクトを取得する。
        /// </summary>
        private AsyncCommandExecuter<DragEventArgs> DropTalkTextFileCommandExecuter
        {
            get;
        }

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
                if (!(await process.SetTalkText(this.TalkText.Value)))
                {
                    this.SetLastStatus(AppStatusType.Fail, @"文章の設定に失敗しました。");
                    return;
                }

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
                    this.Config.Value?.IsTextClearing == true &&
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
                        var totalSize = infos.Sum(i => i.Length);
                        return new { infos, maxInfo, totalSize };
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

            // 最大文字数
            int lenLimit = this.TalkTextLengthLimit.Value;
            if (lenLimit == 0)
            {
                lenLimit = int.MaxValue;
            }

            // 全ファイル読み取り
            var text = new StringBuilder();
            using (var reader = new FileReader((int)f.maxInfo.Length))
            {
                foreach (var info in f.infos)
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

                    // 読み取り
                    if (!(await AppendTextFromFile(reader, info, text)))
                    {
                        this.SetLastStatus(
                            AppStatusType.Warning,
                            info.Name + @" の読み取りに失敗しました。",
                            subStatusText: @"テキストファイルではない可能性があります。");
                        return;
                    }
                }
            }

            // 許容文字数以上は切り捨てる
            string warnText = null;
            if (text.Length > lenLimit)
            {
                text.Remove(lenLimit, text.Length - lenLimit);
                warnText = lenLimit + @" 文字以上は切り捨てました。";
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
        private void SetLastStatus(
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "")
        {
            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = statusType,
                    StatusText = statusText ?? "",
                    SubStatusType = subStatusType,
                    SubStatusText = subStatusText ?? "",
                };
        }
    }
}
