using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using Livet;
using Livet.Messaging;
using Livet.Messaging.IO;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ruche.util;
using ruche.voiceroid;

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
        }

        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        public AppConfig Config
        {
            get { return this.ConfigKeeper.Value; }
        }

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
        }

        /// <summary>
        /// 保存先ディレクトリ選択時に呼び出される。
        /// </summary>
        /// <param name="m">フォルダ選択メッセージ。</param>
        public void OnSaveDirectorySelected(FolderSelectionMessage m)
        {
            if (m.Response == null || !CheckValidPath(m.Response))
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
        /// アプリ設定キーパーを取得する。
        /// </summary>
        private ConfigKeeper<AppConfig> ConfigKeeper { get; } =
            new ConfigKeeper<AppConfig>(nameof(VoiceroidUtil));

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

            this.Messenger.Raise(
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
        private async void ExecuteSaveCommand()
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

            // WAVEファイルを非同期で保存
            filePath = await process.Save(filePath);
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
                    File.WriteAllText(txtPath, text, encoding);
                }
                catch
                {
                    return;
                }
            }

            // TODO: ファイル保存後処理
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
