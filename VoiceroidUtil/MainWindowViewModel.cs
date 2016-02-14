using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.WindowsAPICodePack.Dialogs;
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
                Observable
                    .Return(CommonOpenFileDialog.IsPlatformSupported)
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
        /// 1文字以上の空白文字にマッチする正規表現。
        /// </summary>
        private static readonly Regex RegexBlank = new Regex(@"\s+");

        /// <summary>
        /// CodePage932 エンコーディング。
        /// </summary>
        private static readonly Encoding CodePage932 = Encoding.GetEncoding(932);

        /// <summary>
        /// CodePage932 エンコーディングで表現可能な文字列に変換する。
        /// </summary>
        /// <param name="src">文字列。</param>
        /// <returns>CodePage932 エンコーディングで表現可能な文字列。</returns>
        private static string ToCodePage932String(string src)
        {
            return new string(CodePage932.GetChars(CodePage932.GetBytes(src)));
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

            var info = process.Id.GetInfo();
            var text = this.MakeFileNamePartFromTalkText();
            var time = DateTime.Now.ToString("yyMMdd_hhmmss");

            var name = text;
            switch (this.Config.FileNameFormat)
            {
            case FileNameFormat.Text:
                name = text;
                break;
            case FileNameFormat.NameText:
                name = string.Join("_", info.Name, text);
                break;
            case FileNameFormat.ShortNameText:
                name = string.Join("_", info.ShortName, text);
                break;
            case FileNameFormat.DateTimeText:
                name = string.Join("_", time, text);
                break;
            case FileNameFormat.DateTimeNameText:
                name = string.Join("_", time, info.Name, text);
                break;
            case FileNameFormat.DateTimeShortNameText:
                name = string.Join("_", time, info.ShortName, text);
                break;
            case FileNameFormat.TextInNameDirectory:
                name = Path.Combine(info.Name, text);
                break;
            case FileNameFormat.TextInShortNameDirectory:
                name = Path.Combine(info.ShortName, text);
                break;
            case FileNameFormat.DateTimeTextInNameDirectory:
                name = Path.Combine(info.Name, string.Join("_", time, text));
                break;
            case FileNameFormat.DateTimeTextInShortNameDirectory:
                name = Path.Combine(info.ShortName, string.Join("_", time, text));
                break;
            }

            return Path.Combine(this.Config.SaveDirectoryPath, name + ".wav");
        }

        /// <summary>
        /// トークテキストからファイル名パーツを作成する。
        /// </summary>
        /// <returns>ファイル名パーツ。</returns>
        private string MakeFileNamePartFromTalkText()
        {
            var dest = ToCodePage932String(this.TalkText.Value);

            // 空白文字を半角スペース1文字に短縮
            // ファイル名に使えない文字を置換
            var invalidChars = Path.GetInvalidFileNameChars();
            dest =
                string.Join(
                    "",
                    from c in RegexBlank.Replace(dest, " ")
                    select (Array.IndexOf(invalidChars, c) < 0) ? c : '_');

            // 文字数制限
            int maxLength = 12;
            if (dest.Length > maxLength)
            {
                dest = dest.Substring(0, maxLength - 1) + "-";
            }

            return dest;
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
            var process = this.SelectedProcess.Value;
            if (process == null)
            {
                return;
            }

            var text = this.TalkText.Value;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var filePath = this.MakeWaveFilePath();
            if (filePath == null)
            {
                return;
            }

            process.Stop();
            if (!process.SetTalkText(text))
            {
                return;
            }

            // WAVEファイル保存
            filePath = process.Save(filePath);
            if (!File.Exists(filePath))
            {
                return;
            }

            // テキストファイル保存
            if (this.Config.IsTextFileForceMaking)
            {
                var txtPath = Path.ChangeExtension(filePath, ".txt");
                var encoding =
                    this.Config.IsTextFileUtf8 ? Encoding.UTF8 : Encoding.GetEncoding(932);
                try
                {
                    File.WriteAllText(txtPath, text, encoding);
                }
                catch
                {
                    return;
                }
            }

            // TODO: 保存後処理
        }

        /// <summary>
        /// 保存先選択コマンド処理を行う。
        /// </summary>
        private void ExecuteSelectDirectoryCommand()
        {
            if (!CommonOpenFileDialog.IsPlatformSupported)
            {
                return;
            }

            string filePath = null;
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = @"音声保存先の選択";
                dialog.DefaultDirectory = this.Config.SaveDirectoryPath;
                dialog.EnsureValidNames = true;
                dialog.EnsureFileExists = false;
                dialog.EnsurePathExists = false;

                if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    return;
                }
                filePath = dialog.FileName;
            }

            // CodePage932 で表現可能なパスでなければダメ
            var cp932Path = ToCodePage932String(filePath);
            if (cp932Path != filePath)
            {
                var message =
                    "VOICEROID+ が対応していない文字が含まれています。\n" +
                    "VOICEROID+ は Unicode のファイルパスに対応していません。";

                // 問題の文字を探す
                var fileElems = new TextElementEnumerable(filePath);
                var cp932Elems = new TextElementEnumerable(cp932Path);
                var invalidElem =
                    fileElems
                        .Zip(cp932Elems, (e1, e2) => (e1 == e2) ? null : e1)
                        .FirstOrDefault(e => e != null);
                if (invalidElem != null)
                {
                    message +=
                        "\nフォルダ名に \"" + invalidElem + "\" を使用しないでください。";
                }

                MessageBox.Show(
                    message,
                    "エラー",
                    MessageBox.Button.Ok,
                    MessageBox.Icon.Error);
                return;
            }

            this.Config.SaveDirectoryPath = filePath;
        }
    }
}
