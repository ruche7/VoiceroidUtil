using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Livet;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;
using RucheHome.Voiceroid;
using VoiceroidUtil.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// MainWindow の ViewModel クラス。
    /// </summary>
    public class MainWindowViewModel : ViewModel
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public MainWindowViewModel()
        {
            // ひとまず適当なVOICEROIDプロセスを選択
            // InitializeConfig で前回終了時の選択が復元される
            this.SelectedProcess =
                new ReactiveProperty<IProcess>(
                    this.ProcessFactory.Get(VoiceroidId.YukariEx))
                    .AddTo(this.CompositeDisposable);

            // 選択プロセス変更時処理
            this.SelectedProcess.Subscribe(
                p =>
                {
                    // Config へ反映
                    if (p != null && this.Config != null)
                    {
                        this.Config.VoiceroidId = p.Id;
                    }

                    // アプリ状態リセット
                    this.ResetLastStatus();
                });

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

            // トークテキスト
            this.TalkText =
                new ReactiveProperty<string>("")
                    .AddTo(this.CompositeDisposable);

            // コマンド実行用
            this.PlayStopCommandExecuter =
                new AsyncCommandExecuter(this.ExecutePlayStopCommand);
            this.SaveCommandExecuter = new AsyncCommandExecuter(this.ExecuteSaveCommand);
            this.SaveDirectoryCommandExecuter =
                new AsyncCommandExecuter(this.ExecuteSaveDirectoryCommand);

            // どのコマンドも実行可能ならばアイドル状態とみなす
            this.IsIdle =
                new[]
                {
                    this.PlayStopCommandExecuter.IsExecutable,
                    this.SaveCommandExecuter.IsExecutable,
                    this.SaveDirectoryCommandExecuter.IsExecutable,
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
            this.PlayStopCommand.Subscribe(this.PlayStopCommandExecuter.Execute);

            // 保存コマンド
            this.SaveCommand =
                new[]
                {
                    this.IsIdle,
                    this.IsProcessRunning,
                    this.IsProcessSaving.Select(f => !f),
                    this.TalkText.Select(t => !string.IsNullOrWhiteSpace(t)),
                }
                .CombineLatestValuesAreAllTrue()
                .ToReactiveCommand(false)
                .AddTo(this.CompositeDisposable);
            this.SaveCommand.Subscribe(this.SaveCommandExecuter.Execute);

            // 保存先ディレクトリ選択コマンド
            this.SaveDirectoryCommand =
                this.IsIdle
                    .ToReactiveCommand(false)
                    .AddTo(this.CompositeDisposable);
            this.SaveDirectoryCommand.Subscribe(this.SaveDirectoryCommandExecuter.Execute);

            // 保存先ディレクトリオープンコマンド
            this.DirectoryOpenCommand =
                this.IsIdle
                    .ToReactiveCommand(false)
                    .AddTo(this.CompositeDisposable);
            this.DirectoryOpenCommand.Subscribe(_ => this.ExecuteDirectoryOpenCommand());

            // プロセス更新タイマ設定＆開始
            this.ProcessUpdateTimer =
                new ReactiveTimer(TimeSpan.FromMilliseconds(100))
                    .AddTo(this.CompositeDisposable);
            this.ProcessUpdateTimer.Subscribe(_ => this.ProcessFactory.Update());
            this.ProcessUpdateTimer.Start();
        }

        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        public AppConfig Config
        {
            get { return this.ConfigKeeper.Value; }
        }

        /// <summary>
        /// 直近のアプリ状態を取得する。
        /// </summary>
        public IAppStatus LastStatus
        {
            get { return this.lastStatus; }
            private set
            {
                if (value != this.lastStatus)
                {
                    this.lastStatus = value ?? (new AppStatus());
                    this.RaisePropertyChanged();
                }
            }
        }
        private IAppStatus lastStatus = new AppStatus();

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
        /// 入力文を取得する。
        /// </summary>
        public ReactiveProperty<string> TalkText { get; }

        /// <summary>
        /// アイドル状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// いずれのコマンドも実行中でなければ true となる。
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
        /// 保存先ディレクトリ選択コマンドを取得する。
        /// </summary>
        public ReactiveCommand SaveDirectoryCommand { get; }

        /// <summary>
        /// 保存先ディレクトリオープンコマンドを取得する。
        /// </summary>
        public ReactiveCommand DirectoryOpenCommand { get; }

        /// <summary>
        /// アプリ設定を初期化する。
        /// </summary>
        public void InitializeConfig()
        {
            // ロード
            if (!this.ConfigKeeper.Load())
            {
                this.ConfigKeeper.Value = new AppConfig();
            }
            var config = this.ConfigKeeper.Value;

            // Config プロパティ変更通知
            this.RaisePropertyChanged(nameof(this.Config));

            // 設定変更時のイベント設定
            config.PropertyChanged += this.OnConfigChanged;
            foreach (var r in config.YmmCharaRelations)
            {
                r.PropertyChanged += this.OnConfigChanged;
            }

            // 選択プロセス反映
            var id = config.VoiceroidId;
            if (Enum.IsDefined(id.GetType(), id))
            {
                this.SelectedProcess.Value = this.ProcessFactory.Get(id);
            }

            // 保存先ディレクトリ変更時にアプリ状態リセット
            config
                .ObserveProperty(c => c.SaveDirectoryPath)
                .Subscribe(_ => this.ResetLastStatus());
        }

        /// <summary>
        /// 保存先ディレクトリ選択時に呼び出される。
        /// </summary>
        /// <param name="m">フォルダー選択メッセージ。</param>
        public void OnSaveDirectorySelected(FolderSelectionMessage m)
        {
            if (m.Response == null || !this.CheckValidPath(m.Response))
            {
                return;
            }

            this.Config.SaveDirectoryPath = m.Response;
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
        /// 『ゆっくりMovieMaker3』プロセス操作オブジェクトを取得する。
        /// </summary>
        private YmmProcess YmmProcess { get; } = new YmmProcess();

        /// <summary>
        /// アプリ設定キーパーを取得する。
        /// </summary>
        private ConfigKeeper<AppConfig> ConfigKeeper { get; } =
            new ConfigKeeper<AppConfig>(nameof(VoiceroidUtil));

        /// <summary>
        /// 再生/停止コマンドの非同期実行用オブジェクトを取得する。
        /// </summary>
        private AsyncCommandExecuter PlayStopCommandExecuter { get; }

        /// <summary>
        /// 保存コマンドの非同期実行用オブジェクトを取得する。
        /// </summary>
        private AsyncCommandExecuter SaveCommandExecuter { get; }

        /// <summary>
        /// 保存先ディレクトリ選択コマンドの非同期実行用オブジェクトを取得する。
        /// </summary>
        private AsyncCommandExecuter SaveDirectoryCommandExecuter { get; }

        /// <summary>
        /// 直近のアプリ状態をリセットする。
        /// </summary>
        private void ResetLastStatus()
        {
            this.LastStatus = null;
        }

        /// <summary>
        /// 直近のアプリ状態を設定する。
        /// </summary>
        /// <param name="statusType">状態種別。</param>
        /// <param name="statusText">状態テキスト。</param>
        /// <param name="command">付随コマンド。</param>
        /// <param name="commandText">付随コマンドテキスト。</param>
        /// <param name="subStatusType">オプショナルなサブ状態種別。</param>
        /// <param name="subStatusText">オプショナルなサブ状態テキスト。</param>
        private void SetLastStatus(
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            ICommand command = null,
            string commandText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "")
        {
            this.LastStatus =
                new AppStatus
                {
                    StatusType = statusType,
                    StatusText = statusText ?? "",
                    Command = command,
                    CommandText = commandText ?? "",
                    SubStatusType = subStatusType,
                    SubStatusText = subStatusText ?? "",
                };
        }

        /// <summary>
        /// パスが VOICEROID+ の保存パスとして正常か否かをチェックし、
        /// 不正ならばアプリ状態を更新する。
        /// </summary>
        /// <param name="path">パス。</param>
        /// <returns>正常ならば true 。そうでなければ false 。</returns>
        private bool CheckValidPath(string path)
        {
            string text = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                text = @"保存先フォルダーが未設定です。";
            }
            else
            {
                string invalidLetter = null;
                if (!FileSaveUtil.IsValidPath(path, out invalidLetter))
                {
                    text =
                        (invalidLetter == null) ?
                            @"VOICEROID+ が対応していない保存先フォルダーです。" :
                            @"保存先フォルダーパスに文字 """ +
                            invalidLetter +
                            @""" を含めないでください。";
                }
            }

            if (text == null)
            {
                return true;
            }

            this.SetLastStatus(
                AppStatusType.Warning,
                text,
                this.SaveDirectoryCommand,
                @"保存先選択...");

            return false;
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
        /// 現在の状態を基にWAVEファイルパスを作成する。
        /// </summary>
        /// <returns>WAVEファイルパス。作成できないならば null 。</returns>
        private string MakeWaveFilePath()
        {
            var process = this.SelectedProcess.Value;
            if (process == null)
            {
                return null;
            }

            var text = this.TalkText.Value;
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var name =
                FileSaveUtil.MakeFileName(this.Config.FileNameFormat, process.Id, text);

            return Path.Combine(this.Config.SaveDirectoryPath, name + @".wav");
        }

        /// <summary>
        /// 現在の設定を基にテキストをファイルへ書き出す。
        /// </summary>
        /// <param name="filePath">テキストファイルパス。</param>
        /// <param name="text">書き出すテキスト。</param>
        /// <returns>
        /// 書き出しタスク。
        /// 成功した場合は true を返す。そうでなければ false を返す。
        /// </returns>
        private async Task<bool> WriteTextFile(string filePath, string text)
        {
            if (string.IsNullOrWhiteSpace(filePath) || text == null)
            {
                return false;
            }

            var encoding =
                this.Config.IsTextFileUtf8 ?
                    Encoding.UTF8 : Encoding.GetEncoding(932);
            try
            {
                using (var writer = new StreamWriter(filePath, false, encoding))
                {
                    await writer.WriteAsync(text);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 再生/停止コマンド処理を行う。
        /// </summary>
        private async Task ExecutePlayStopCommand()
        {
            this.ResetLastStatus();

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
                if (!(await process.Play()))
                {
                    this.SetLastStatus(AppStatusType.Fail, @"再生処理に失敗しました。");
                    return;
                }
                this.SetLastStatus(AppStatusType.Success, @"再生処理に成功しました。");
            }
        }

        /// <summary>
        /// 保存コマンド処理を行う。
        /// </summary>
        private async Task ExecuteSaveCommand()
        {
            this.ResetLastStatus();

            var filePath = this.MakeWaveFilePath();
            if (filePath == null)
            {
                this.SetLastStatus(AppStatusType.Fail, @"処理を開始できませんでした。");
                return;
            }
            if (!this.CheckValidPath(filePath))
            {
                return;
            }

            var process = this.SelectedProcess.Value;
            var text = this.TalkText.Value;

            await process.Stop();
            if (!(await process.SetTalkText(text)))
            {
                this.SetLastStatus(AppStatusType.Fail, @"文章の設定に失敗しました。");
                return;
            }

            try
            {
                // WAVEファイル保存
                var result = await process.Save(filePath);
                if (!result.IsSucceeded)
                {
                    this.SetLastStatus(AppStatusType.Fail, result.Error);
                    return;
                }

                filePath = result.FilePath;
                var fileName = Path.GetFileName(filePath);

                // テキストファイル保存
                if (this.Config.IsTextFileForceMaking)
                {
                    var txtPath = Path.ChangeExtension(filePath, @".txt");
                    if (!(await this.WriteTextFile(txtPath, text)))
                    {
                        this.SetLastStatus(
                            AppStatusType.Success,
                            fileName + @" を保存しました。",
                            subStatusType: AppStatusType.Fail,
                            subStatusText: @"テキストファイルを保存できませんでした。");
                        return;
                    }
                }

                string warnText = null;

                // ゆっくりMovieMaker処理
                if (this.Config.IsSavedFileToYmm)
                {
                    this.YmmProcess.Update();
                    if (this.YmmProcess.IsRunning)
                    {
                        if (!(await this.YmmProcess.SetTimelineSpeechEditValue(filePath)))
                        {
                            warnText =
                                @"ゆっくりMovieMaker3へのパス設定に失敗しました。";
                        }
                        else if (
                            this.Config.IsYmmAddButtonClicking &&
                            !(await this.YmmProcess.ClickTimelineSpeechAddButton()))
                        {
                            warnText =
                                @"ゆっくりMovieMaker3のボタン押下に失敗しました。";
                        }
                    }
                }

                this.SetLastStatus(
                    AppStatusType.Success,
                    fileName + @" を保存しました。",
                    (warnText == null) ? this.DirectoryOpenCommand : null,
                    (warnText == null) ? @"保存先フォルダーを開く..." : "",
                    (warnText == null) ? AppStatusType.None : AppStatusType.Warning,
                    warnText);
            }
            finally
            {
                // メインウィンドウを前面へ
                await this.Messenger.RaiseAsync(
                    new WindowActionMessage(WindowAction.Active, @"Window"));
            }
        }

        /// <summary>
        /// 保存先ディレクトリ選択コマンド処理を行う。
        /// </summary>
        private Task ExecuteSaveDirectoryCommand()
        {
            return this.Messenger.RaiseAsync(new InteractionMessage(@"SaveDirectory"));
        }

        /// <summary>
        /// 保存先ディレクトリオープンコマンド処理を行う。
        /// </summary>
        private void ExecuteDirectoryOpenCommand()
        {
            var dirPath = this.Config.SaveDirectoryPath;
            if (Directory.Exists(dirPath))
            {
                try
                {
                    Process.Start(dirPath);
                }
                catch
                {
                    this.SetLastStatus(
                        AppStatusType.Fail,
                        @"保存先フォルダーを開けませんでした。");
                }
            }
            else
            {
                this.SetLastStatus(
                    AppStatusType.Warning,
                    @"保存先フォルダーが見つかりませんでした。");
            }
        }

        /// <summary>
        /// アプリ設定の変更時に呼び出される。
        /// </summary>
        private void OnConfigChanged(object sender, PropertyChangedEventArgs e)
        {
            this.ConfigKeeper.Save();
        }
    }
}
