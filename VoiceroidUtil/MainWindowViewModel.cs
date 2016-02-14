using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ruche.voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// MainWindow の ViewModel クラス。
    /// </summary>
    public class MainWindowViewModel
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public MainWindowViewModel() : this(new AppConfig())
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="config">アプリ設定。</param>
        public MainWindowViewModel(AppConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            this.Config = config;

            this.SelectedProcess =
                new ReactiveProperty<IProcess>(
                    this.ProcessFactory.Get(VoiceroidId.YukariEx));
            this.IsProcessRunning =
                this.ObserveSelectedProcessProperty(p => p.IsRunning).ToReactiveProperty();
            this.IsProcessPlaying =
                this.ObserveSelectedProcessProperty(p => p.IsPlaying).ToReactiveProperty();
            this.IsProcessSaving =
                this.ObserveSelectedProcessProperty(p => p.IsSaving).ToReactiveProperty();

            this.TalkText = new ReactiveProperty<string>("");

            // 再生/停止コマンド
            this.PlayStopCommand =
                this.TalkText
                    .CombineLatest(
                        this.IsProcessRunning,
                        this.IsProcessPlaying,
                        this.IsProcessSaving,
                        (text, running, playing, saving) =>
                            (playing || !string.IsNullOrWhiteSpace(text)) &&
                            running &&
                            !saving)
                    .ToReactiveCommand(false);
            this.PlayStopCommand.Subscribe(_ => this.ExecutePlayStopCommand());

            // 保存コマンド
            this.SaveCommand =
                this.TalkText
                    .CombineLatest(
                        this.IsProcessRunning,
                        this.IsProcessSaving,
                        (text, running, saving) =>
                            !string.IsNullOrWhiteSpace(text) && running && !saving)
                    .ToReactiveCommand();
            this.SaveCommand.Subscribe(_ => this.ExecuteSaveCommand());

            // 保存先選択コマンド
            this.SelectDirectoryCommand =
                this.SelectDirectoryDialogCommand
                    .Select(cmd => cmd != null)
                    .ToReactiveCommand();
            this.SelectDirectoryCommand.Subscribe(
                _ => this.ExecuteSelectDirectoryCommand());
        }

        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        public AppConfig Config { get; }

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
        public ReactiveProperty<bool> IsProcessRunning { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスが再生中であるか否かを取得する。
        /// </summary>
        public ReactiveProperty<bool> IsProcessPlaying { get; }

        /// <summary>
        /// 選択中のVOICEROIDプロセスがWAVEファイル保存中であるか否かを取得する。
        /// </summary>
        public ReactiveProperty<bool> IsProcessSaving { get; }

        /// <summary>
        /// 入力文を取得する。
        /// </summary>
        public ReactiveProperty<string> TalkText { get; }

        /// <summary>
        /// 再生/停止コマンドを取得する。
        /// </summary>
        public ReactiveCommand PlayStopCommand { get; }

        /// <summary>
        /// 保存コマンドを取得する。
        /// </summary>
        public ReactiveCommand SaveCommand { get; }

        /// <summary>
        /// 保存先選択コマンドを取得する。
        /// </summary>
        public ReactiveCommand SelectDirectoryCommand { get; }

        /// <summary>
        /// エラーダイアログ表示コマンドを取得または設定する。
        /// </summary>
        /// <remarks>
        /// ViewModel 利用側から提供すること。
        /// </remarks>
        public ReactiveProperty<ICommand> ErrorDialogCommand { get; } =
            new ReactiveProperty<ICommand>();

        /// <summary>
        /// ディレクトリ選択ダイアログ表示コマンドを取得または設定する。
        /// </summary>
        /// <remarks>
        /// ViewModel 利用側から提供すること。
        /// </remarks>
        public ReactiveProperty<ICommand> SelectDirectoryDialogCommand { get; } =
            new ReactiveProperty<ICommand>();

        /// <summary>
        /// SelectDirectoryDialogCommand のコマンドパラメータクラス。
        /// </summary>
        public class SelectDirectoryDialogCommandParam
        {
            /// <summary>
            /// タイトルを取得または設定する。
            /// </summary>
            public string Title { get; set; } = null;

            /// <summary>
            /// ディレクトリパスを取得または設定する。
            /// </summary>
            /// <remarks>
            /// ViewModel 利用側から提供するコマンドによって選択されたパスを設定すること。
            /// キャンセルされた場合は null を設定すること。
            /// </remarks>
            public string Path { get; set; } = null;
        }

        /// <summary>
        /// VOICEROIDプロセスファクトリを取得する。
        /// </summary>
        private ProcessFactory ProcessFactory { get; } = new ProcessFactory();

        /// <summary>
        /// パスが VOICEROID+ の保存パスとして正常か否かをチェックし、
        /// 不正であればダイアログ表示を行う。
        /// </summary>
        /// <param name="path">パス。</param>
        /// <returns>正常ならば true 。そうでなければ false 。</returns>
        private bool CheckValidPath(string path)
        {
            string invalidLetter = null;
            if (FileSaveUtil.IsValidPath(path, out invalidLetter))
            {
                return true;
            }

            var message = "VOICEROID+ が対応していないパス文字が含まれています。";
            if (invalidLetter != null)
            {
                message +=
                    "\n\n" +
                    "VOICEROID+ は Unicode のパス文字に対応していません。\n" +
                    "フォルダ名に \"" + invalidLetter + "\" を含めないでください。";
            }

            var cmd = this.ErrorDialogCommand.Value;
            if (cmd?.CanExecute(message) == true)
            {
                cmd.Execute(message);
            }

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

            return Path.Combine(this.Config.SaveDirectoryPath, name + ".wav");
        }

        /// <summary>
        /// 再生/停止コマンド処理を行う。
        /// </summary>
        private void ExecutePlayStopCommand()
        {
            var process = this.SelectedProcess.Value;
            if (process == null)
            {
                return;
            }

            if (process.IsPlaying)
            {
                process.Stop();
            }
            else
            {
                if (process.SetTalkText(this.TalkText.Value))
                {
                    process.Play();
                }
            }
        }

        /// <summary>
        /// 保存コマンド処理を行う。
        /// </summary>
        private void ExecuteSaveCommand()
        {
            var filePath = this.MakeWaveFilePath();
            if (filePath == null || !CheckValidPath(filePath))
            {
                return;
            }

            var process = this.SelectedProcess.Value;
            var text = this.TalkText.Value;

            process.Stop();
            if (!process.SetTalkText(text))
            {
                return;
            }

            process
                .Save(filePath)
                .ContinueWith(
                    p => this.DoTaskAfterSaveCommand(p.Result, text),
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// 保存コマンドの後処理を行う。
        /// </summary>
        /// <param name="filePath">
        /// 保存されたWAVEファイルパス。保存失敗時は null 。
        /// </param>
        /// <param name="talkText">トークテキスト。</param>
        private void DoTaskAfterSaveCommand(string filePath, string talkText)
        {
            if (filePath == null)
            {
                return;
            }

            // テキストファイル保存
            if (this.Config.IsTextFileForceMaking)
            {
                var txtPath = Path.ChangeExtension(filePath, ".txt");
                var encoding =
                    this.Config.IsTextFileUtf8 ?
                        Encoding.UTF8 : Encoding.GetEncoding(932);
                try
                {
                    File.WriteAllText(txtPath, talkText, encoding);
                }
                catch
                {
                    return;
                }
            }

            // TODO: ファイル保存後処理
        }

        /// <summary>
        /// 保存先選択コマンド処理を行う。
        /// </summary>
        private void ExecuteSelectDirectoryCommand()
        {
            var param =
                new SelectDirectoryDialogCommandParam
                {
                    Title = @"音声保存先の選択",
                    Path = this.Config.SaveDirectoryPath,
                };

            var cmd = this.SelectDirectoryDialogCommand.Value;
            if (cmd?.CanExecute(param) != true)
            {
                return;
            }
            cmd.Execute(param);

            if (param.Path == null || !CheckValidPath(param.Path))
            {
                return;
            }

            this.Config.SaveDirectoryPath = param.Path;
        }
    }
}
