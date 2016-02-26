using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.Messaging.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;
using RucheHome.Voiceroid;
using VoiceroidUtil.Messaging;
using VoiceroidUtil.Util;

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

            // トークテキスト
            this.TalkText =
                new ReactiveProperty<string>("")
                    .AddTo(this.CompositeDisposable);

            // コマンド実行用
            this.PlayStopCommandExecuter =
                new AsyncCommandExecuter(this.ExecutePlayStopCommand)
                    .AddTo(this.CompositeDisposable);
            this.SaveCommandExecuter =
                new AsyncCommandExecuter(this.ExecuteSaveCommand)
                    .AddTo(this.CompositeDisposable);

            // どのコマンドも実行可能ならばアイドル状態とみなす
            this.IsIdle =
                new[]
                {
                    this.PlayStopCommandExecuter.IsExecutable,
                    this.SaveCommandExecuter.IsExecutable,
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
                    this.TalkText.Select(t => !string.IsNullOrWhiteSpace(t)),
                }
                .CombineLatestValuesAreAllTrue()
                .ToReactiveCommand(false)
                .AddTo(this.CompositeDisposable);
            this.SaveCommand
                .Subscribe(this.SaveCommandExecuter.Execute)
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
        /// トークテキストを取得する。
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
        /// テキストをファイルへ書き出す。
        /// </summary>
        /// <param name="filePath">テキストファイルパス。</param>
        /// <param name="text">書き出すテキスト。</param>
        /// <param name="utf8">
        /// UTF-8で書き出すならば true 。CP932で書き出すならば false 。
        /// </param>
        /// <returns>
        /// 書き出しタスク。
        /// 成功した場合は true を返す。そうでなければ false を返す。
        /// </returns>
        private static async Task<bool> WriteTextFile(
            string filePath,
            string text,
            bool utf8)
        {
            if (string.IsNullOrWhiteSpace(filePath) || text == null)
            {
                return false;
            }

            var encoding = utf8 ? Encoding.UTF8 : Encoding.GetEncoding(932);
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
        /// 再生/停止コマンドの非同期実行用オブジェクトを取得する。
        /// </summary>
        private AsyncCommandExecuter PlayStopCommandExecuter { get; }

        /// <summary>
        /// 保存コマンドの非同期実行用オブジェクトを取得する。
        /// </summary>
        private AsyncCommandExecuter SaveCommandExecuter { get; }

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

            // WAVEファイルパス作成
            var filePath = this.MakeWaveFilePath();
            if (filePath == null)
            {
                this.SetLastStatus(AppStatusType.Fail, @"処理を開始できませんでした。");
                return;
            }

            // パスが正常かチェック
            var pathStatus = FileSaveUtil.CheckPathStatus(filePath);
            if (pathStatus.StatusType != AppStatusType.None)
            {
                this.LastStatus.Value = pathStatus;
                return;
            }

            var config = this.Config.Value;
            var process = this.SelectedProcess.Value;
            var text = this.TalkText.Value;

            // トークテキスト設定
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
                if (config.IsTextFileForceMaking)
                {
                    var txtPath = Path.ChangeExtension(filePath, @".txt");
                    if (!(await WriteTextFile(txtPath, text, config.IsTextFileUtf8)))
                    {
                        this.SetLastStatus(
                            AppStatusType.Success,
                            fileName + @" を保存しました。",
                            subStatusType: AppStatusType.Fail,
                            subStatusText: @"テキストファイルを保存できませんでした。");
                        return;
                    }
                }

                // ゆっくりMovieMaker処理
                var warnText = await this.DoOperateYmm(filePath, config);

                this.SetLastStatus(
                    AppStatusType.Success,
                    fileName + @" を保存しました。",
                    (warnText == null) ? AppStatusType.None : AppStatusType.Warning,
                    warnText);
            }
            finally
            {
                // メインウィンドウを前面へ
                await this.Messenger.RaiseAsync(
                    new WindowActionMessage(
                        WindowAction.Active,
                        MessageKeys.WindowActionMessageKey));
            }
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

        /// <summary>
        /// 現在の状態を基にWAVEファイルパスを作成する。
        /// </summary>
        /// <returns>WAVEファイルパス。作成できないならば null 。</returns>
        private string MakeWaveFilePath()
        {
            var config = this.Config.Value;
            var process = this.SelectedProcess.Value;
            var text = this.TalkText.Value;

            if (
                config == null ||
                string.IsNullOrWhiteSpace(config.SaveDirectoryPath) ||
                process == null ||
                string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var name = FileSaveUtil.MakeFileName(config.FileNameFormat, process.Id, text);

            return Path.Combine(config.SaveDirectoryPath, name + @".wav");
        }

        /// <summary>
        /// 設定を基に『ゆっくりMovieMaker3』の操作を行う。
        /// </summary>
        /// <param name="filePath">WAVEファイルパス。</param>
        /// <param name="config">アプリ設定。</param>
        /// <returns>警告文字列。問題ないならば null 。</returns>
        private async Task<string> DoOperateYmm(string filePath, AppConfig config)
        {
            if (!config.IsSavedFileToYmm)
            {
                return null;
            }

            this.YmmProcess.Update();
            if (!this.YmmProcess.IsRunning)
            {
                return null;
            }

            // ファイルパス設定
            if (!(await this.YmmProcess.SetTimelineSpeechEditValue(filePath)))
            {
                return @"ゆっくりMovieMaker3へのパス設定に失敗しました。";
            }

            string warnText = null;

            // キャラ選択
            // そもそもキャラ名が存在しない場合は失敗しても警告にしない
            if (config.IsYmmCharaSelecting)
            {
                var name = config.YmmCharaRelations[config.VoiceroidId].YmmCharaName;
                if (
                    !string.IsNullOrEmpty(name) &&
                    (await this.YmmProcess.SelectTimelineCharaComboBoxItem(name)) == false)
                {
                    warnText = @"ゆっくりMovieMaker3のキャラ選択に失敗しました。";
                }
            }

            // ボタン押下
            // キャラ選択に失敗していても行う
            if (
                config.IsYmmAddButtonClicking &&
                !(await this.YmmProcess.ClickTimelineSpeechAddButton()))
            {
                warnText = @"ゆっくりMovieMaker3のボタン押下に失敗しました。";
            }

            return warnText;
        }
    }
}
