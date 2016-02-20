using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
                    this.ProcessFactory.Get(VoiceroidId.YukariEx));

            // 選択プロセス変更時に Config へ反映
            this.SelectedProcess.Subscribe(
                p =>
                {
                    if (p != null && this.Config != null)
                    {
                        this.Config.VoiceroidId = p.Id;
                    }
                });

            // 選択プロセス状態
            this.IsProcessRunning =
                this
                    .ObserveSelectedProcessProperty(p => p.IsRunning)
                    .ToReadOnlyReactiveProperty();
            this.IsProcessPlaying =
                this
                    .ObserveSelectedProcessProperty(p => p.IsPlaying)
                    .ToReadOnlyReactiveProperty();
            this.IsProcessSaving =
                this
                    .ObserveSelectedProcessProperty(p => p.IsSaving)
                    .ToReadOnlyReactiveProperty();

            // トークテキスト
            this.TalkText = new ReactiveProperty<string>("");

            // コマンド実行用
            this.PlayStopCommandExecuter =
                new AsyncCommandExecuter(this.ExecutePlayStopCommand);
            this.SaveCommandExecuter = new AsyncCommandExecuter(this.ExecuteSaveCommand);

            // どのコマンドも実行可能ならばアイドル状態とみなす
            this.IsIdle =
                new[]
                {
                    this.PlayStopCommandExecuter.IsExecutable,
                    this.SaveCommandExecuter.IsExecutable,
                }
                .CombineLatestValuesAreAllTrue()
                .ToReadOnlyReactiveProperty();

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
                .ToReactiveCommand(false);
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
                .ToReactiveCommand(false);
            this.SaveCommand.Subscribe(this.SaveCommandExecuter.Execute);

            // 保存先ディレクトリ選択コマンド
            this.SaveDirectoryCommand = this.IsIdle.ToReactiveCommand(false);
            this.SaveDirectoryCommand.Subscribe(
                _ => this.Messenger.RaiseAsync(new InteractionMessage(@"SaveDirectory")));

            // プロセス更新タイマ設定＆開始
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
        public AppStatus LastStatus
        {
            get { return this.lastStatus; }
            private set
            {
                this.lastStatus = value;
                this.RaisePropertyChanged();
            }
        }
        private AppStatus lastStatus = new AppStatus();

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
        /// アプリ設定を初期化する。
        /// </summary>
        public void InitializeConfig()
        {
            // ロード
            if (!this.ConfigKeeper.Load())
            {
                this.ConfigKeeper.Value = new AppConfig();
            }

            // 設定変更時のイベント設定
            this.ConfigKeeper.Value.PropertyChanged += OnConfigChanged;

            // Config プロパティ変更通知
            this.RaisePropertyChanged(nameof(this.Config));

            // 選択プロセス反映
            var id = this.Config.VoiceroidId;
            if (Enum.IsDefined(id.GetType(), id))
            {
                this.SelectedProcess.Value = this.ProcessFactory.Get(id);
            }
        }

        /// <summary>
        /// 保存先ディレクトリ選択時に呼び出される。
        /// </summary>
        /// <param name="m">フォルダ選択メッセージ。</param>
        public async void OnSaveDirectorySelected(FolderSelectionMessage m)
        {
            if (m.Response == null || !(await CheckValidPath(m.Response)))
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
        private ReactiveTimer ProcessUpdateTimer { get; } =
            new ReactiveTimer(TimeSpan.FromMilliseconds(100));

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
        /// パスが VOICEROID+ の保存パスとして正常か否かをチェックし、
        /// 不正であればダイアログ表示を行う。
        /// </summary>
        /// <param name="path">パス。</param>
        /// <returns>
        /// チェックタスク。
        /// 正常ならば true を返す。そうでなければ false を返す。
        /// </returns>
        private async Task<bool> CheckValidPath(string path)
        {
            string invalidLetter = null;
            if (FileSaveUtil.IsValidPath(path, out invalidLetter))
            {
                return true;
            }

            var message = new StringBuilder();
            message.Append(@"VOICEROID+ が対応していないパス文字が含まれています。");
            if (invalidLetter != null)
            {
                message.AppendLine();
                message.AppendLine();
                message.Append(@"VOICEROID+ は Unicode のパス文字に対応していません。");
                message.AppendLine();
                message.Append(@"フォルダ名に """);
                message.Append(invalidLetter);
                message.Append(@""" を含めないでください。");
            }

            await this.Messenger.RaiseAsync(
                new InformationMessage(
                    message.ToString(),
                    @"エラー",
                    MessageBoxImage.Error,
                    @"Info"));

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
            var process = this.SelectedProcess.Value;
            if (process == null)
            {
                return;
            }

            if (process.IsPlaying)
            {
                await process.Stop();
            }
            else
            {
                if (await process.SetTalkText(this.TalkText.Value))
                {
                    await process.Play();
                }
            }
        }

        /// <summary>
        /// 保存コマンド処理を行う。
        /// </summary>
        private async Task ExecuteSaveCommand()
        {
            var filePath = this.MakeWaveFilePath();
            if (filePath == null || !(await CheckValidPath(filePath)))
            {
                return;
            }

            var process = this.SelectedProcess.Value;
            var text = this.TalkText.Value;

            await process.Stop();
            if (!(await process.SetTalkText(text)))
            {
                return;
            }

            try
            {
                // WAVEファイル保存
                var result = await process.Save(filePath);
                if (!result.IsSucceeded)
                {
                    return;
                }
                filePath = result.FilePath;

                // テキストファイル保存
                if (this.Config.IsTextFileForceMaking)
                {
                    var txtPath = Path.ChangeExtension(filePath, ".txt");
                    if (!(await this.WriteTextFile(txtPath, text)))
                    {
                        return;
                    }
                }

                // ゆっくりMovieMaker処理
                if (this.Config.IsSavedFileToYmm)
                {
                    this.YmmProcess.Update();
                    if (
                        this.YmmProcess.IsRunning &&
                        (await this.YmmProcess.SetTimelineSpeechEditValue(filePath)) &&
                        this.Config.IsYmmAddButtonClicking)
                    {
                        await this.YmmProcess.ClickTimelineSpeechAddButton();
                    }
                }
            }
            finally
            {
                // メインウィンドウを前面へ
                await this.Messenger.RaiseAsync(
                    new WindowActionMessage(WindowAction.Active, @"Window"));
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
