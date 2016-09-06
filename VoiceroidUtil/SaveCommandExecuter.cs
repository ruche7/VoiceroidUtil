using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// ReactiveCommand でWAVEファイル保存周りの非同期実行を行うためのクラス。
    /// </summary>
    public class SaveCommandExecuter : AsyncCommandExecuter
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="talkTextReplaceConfigGetter">
        /// トークテキスト置換設定取得デリゲート。
        /// </param>
        /// <param name="appConfigGetter">アプリ設定取得デリゲート。</param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="resultNotifier">処理結果のアプリ状態通知デリゲート。</param>
        public SaveCommandExecuter(
            Func<IProcess> processGetter,
            Func<TalkTextReplaceConfig> talkTextReplaceConfigGetter,
            Func<AppConfig> appConfigGetter,
            Func<string> talkTextGetter,
            Func<IAppStatus, Task> resultNotifier)
            : base()
        {
            if (processGetter == null)
            {
                throw new ArgumentNullException(nameof(processGetter));
            }
            if (talkTextReplaceConfigGetter == null)
            {
                throw new ArgumentNullException(nameof(talkTextReplaceConfigGetter));
            }
            if (appConfigGetter == null)
            {
                throw new ArgumentNullException(nameof(appConfigGetter));
            }
            if (talkTextGetter == null)
            {
                throw new ArgumentNullException(nameof(talkTextGetter));
            }
            if (resultNotifier == null)
            {
                throw new ArgumentNullException(nameof(resultNotifier));
            }

            this.AsyncFunc = _ => this.ExecuteAsync();

            this.ProcessGetter = processGetter;
            this.TalkTextReplaceConfigGetter = talkTextReplaceConfigGetter;
            this.AppConfigGetter = appConfigGetter;
            this.TalkTextGetter = talkTextGetter;
            this.ResultNotifier = resultNotifier;
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="talkTextReplaceConfigGetter">
        /// トークテキスト置換設定取得デリゲート。
        /// </param>
        /// <param name="appConfigGetter">アプリ設定取得デリゲート。</param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="resultNotifier">処理結果のアプリ状態通知デリゲート。</param>
        public SaveCommandExecuter(
            Func<IProcess> processGetter,
            Func<TalkTextReplaceConfig> talkTextReplaceConfigGetter,
            Func<AppConfig> appConfigGetter,
            Func<string> talkTextGetter,
            Action<IAppStatus> resultNotifier)
            :
            this(
                processGetter,
                talkTextReplaceConfigGetter,
                appConfigGetter,
                talkTextGetter,
                (resultNotifier == null) ?
                    null :
                    new Func<IAppStatus, Task>(
                        async r => await Task.Run(() => resultNotifier(r))))
        {
        }

        /// <summary>
        /// 『ゆっくりMovieMaker』プロセス操作インスタンスを取得する。
        /// </summary>
        private static YmmProcess YmmProcess { get; } = new YmmProcess();

        /// <summary>
        /// WAVEファイルパスを作成する。
        /// </summary>
        /// <param name="config">アプリ設定。</param>
        /// <param name="process">VOICEROIDプロセス。</param>
        /// <param name="talkText">トークテキスト。</param>
        /// <returns>WAVEファイルパス。作成できないならば null 。</returns>
        private static string MakeWaveFilePath(
            AppConfig config,
            IProcess process,
            string talkText)
        {
            if (
                config == null ||
                string.IsNullOrWhiteSpace(config.SaveDirectoryPath) ||
                process == null ||
                string.IsNullOrWhiteSpace(talkText))
            {
                return null;
            }

            var name =
                FilePathUtil.MakeFileName(config.FileNameFormat, process.Id, talkText);
            var basePath = Path.Combine(config.SaveDirectoryPath, name);

            // 同名ファイルがあるならば名前の末尾に "[数字]" を付ける
            var path = basePath;
            for (
                int i = 1;
                File.Exists(path + @".wav") || File.Exists(path + @".txt");
                ++i)
            {
                path = basePath + @"[" + i + @"]";
            }

            return path;
        }

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

            var encoding = utf8 ? (new UTF8Encoding(false)) : Encoding.GetEncoding(932);

            // VOICEROID側がテキストファイル書き出し中だと失敗するので複数回試行
            bool saved = false;
            for (int i = 0; !saved && i < 10; ++i)
            {
                try
                {
                    using (var writer = new StreamWriter(filePath, false, encoding))
                    {
                        await writer.WriteAsync(text);
                    }

                    saved = true;
                }
                catch (IOException)
                {
                    await Task.Delay(50);
                    continue;
                }
                catch
                {
                    break;
                }
            }

            return saved;
        }

        /// <summary>
        /// VOICEROIDプロセス取得デリゲートを取得する。
        /// </summary>
        private Func<IProcess> ProcessGetter { get; }

        /// <summary>
        /// トークテキスト置換設定取得デリゲートを取得する。
        /// </summary>
        private Func<TalkTextReplaceConfig> TalkTextReplaceConfigGetter { get; }

        /// <summary>
        /// アプリ設定取得デリゲートを取得する。
        /// </summary>
        private Func<AppConfig> AppConfigGetter { get; }

        /// <summary>
        /// トークテキスト取得デリゲートを取得する。
        /// </summary>
        private Func<string> TalkTextGetter { get; }

        /// <summary>
        /// 処理結果のアプリ状態通知デリゲートを取得する。
        /// </summary>
        private Func<IAppStatus, Task> ResultNotifier { get; }

        /// <summary>
        /// 非同期の実処理を行う。
        /// </summary>
        private async Task ExecuteAsync()
        {
            var appConfig = this.AppConfigGetter();
            if (appConfig == null)
            {
                await this.NotifyResult(
                    AppStatusType.Fail,
                    @"ファイル保存を開始できませんでした。");
                return;
            }

            var process = this.ProcessGetter();
            if (
                process == null ||
                !process.IsRunning ||
                process.IsSaving ||
                process.IsDialogShowing)
            {
                await this.NotifyResult(
                    AppStatusType.Fail,
                    @"ファイル保存を開始できませんでした。");
                return;
            }

            // テキスト作成
            var text = this.TalkTextGetter();
            var talkTextReplaceConfig = this.TalkTextReplaceConfigGetter();
            var voiceText = talkTextReplaceConfig?.VoiceReplaceItems.Replace(text) ?? text;
            var fileText =
                talkTextReplaceConfig?.TextFileReplaceItems.Replace(text) ?? text;

            // WAVEファイルパス決定
            var filePath = MakeWaveFilePath(appConfig, process, text);
            if (filePath == null)
            {
                await this.NotifyResult(
                    AppStatusType.Fail,
                    @"ファイル保存を開始できませんでした。");
                return;
            }

            // パスが正常かチェック
            var pathStatus = FilePathUtil.CheckPathStatus(filePath);
            if (pathStatus.StatusType != AppStatusType.None)
            {
                await this.ResultNotifier(pathStatus);
                return;
            }

            // トークテキスト設定
            if (!(await process.SetTalkText(voiceText)))
            {
                await this.NotifyResult(AppStatusType.Fail, @"文章の設定に失敗しました。");
                return;
            }

            // WAVEファイル保存
            var result = await process.Save(filePath);
            if (!result.IsSucceeded)
            {
                await this.NotifyResult(AppStatusType.Fail, result.Error);
                return;
            }

            filePath = result.FilePath;

            var statusText = Path.GetFileName(filePath) + @" を保存しました。";

            // テキストファイル保存
            if (appConfig.IsTextFileForceMaking)
            {
                var txtPath = Path.ChangeExtension(filePath, @".txt");
                if (!(await WriteTextFile(txtPath, fileText, appConfig.IsTextFileUtf8)))
                {
                    await this.NotifyResult(
                        AppStatusType.Success,
                        statusText,
                        AppStatusType.Fail,
                        @"テキストファイルを保存できませんでした。");
                    return;
                }
            }

            // ゆっくりMovieMaker処理
            var warnText = await DoOperateYmm(filePath, process.Id, appConfig);

            await this.NotifyResult(
                AppStatusType.Success,
                statusText,
                (warnText == null) ? AppStatusType.None : AppStatusType.Warning,
                warnText ?? @"保存先フォルダーを開く",
                (warnText == null) ? appConfig.SaveDirectoryPath : null);
        }

        /// <summary>
        /// 設定を基に『ゆっくりMovieMaker』の操作を行う。
        /// </summary>
        /// <param name="filePath">WAVEファイルパス。</param>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="config">アプリ設定。</param>
        /// <returns>警告文字列。問題ないならば null 。</returns>
        private async Task<string> DoOperateYmm(
            string filePath,
            VoiceroidId voiceroidId,
            AppConfig config)
        {
            if (!config.IsSavedFileToYmm)
            {
                return null;
            }

            // 状態更新
            try
            {
                await YmmProcess.Update();
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
                return @"ゆっくりMovieMakerの起動状態確認に失敗しました。";
            }

            // そもそも起動していないなら何もしない
            if (!YmmProcess.IsRunning)
            {
                return null;
            }

            // タイムラインウィンドウが開いていない？
            if (!YmmProcess.IsTimelineOpened)
            {
                return @"ゆっくりMovieMakerのタイムラインが見つかりません。";
            }

            // ファイルパス設定
            if (!(await YmmProcess.SetTimelineSpeechEditValue(filePath)))
            {
                return @"ゆっくりMovieMakerへのパス設定に失敗しました。";
            }

            string warnText = null;

            // キャラ選択
            // そもそもキャラ名が存在しない場合は失敗しても警告にしない
            if (config.IsYmmCharaSelecting)
            {
                var name = config.YmmCharaRelations[voiceroidId].YmmCharaName;
                if (
                    !string.IsNullOrEmpty(name) &&
                    (await YmmProcess.SelectTimelineCharaComboBoxItem(name)) == false)
                {
                    warnText = @"ゆっくりMovieMakerのキャラ選択に失敗しました。";
                }
            }

            // ボタン押下
            // キャラ選択に失敗していても行う
            if (
                config.IsYmmAddButtonClicking &&
                !(await YmmProcess.ClickTimelineSpeechAddButton()))
            {
                warnText = @"ゆっくりMovieMakerの追加ボタン押下に失敗しました。";
            }

            return warnText;
        }

        /// <summary>
        /// 処理結果のアプリ状態を通知する。
        /// </summary>
        /// <param name="statusType">状態種別。</param>
        /// <param name="statusText">状態テキスト。</param>
        /// <param name="subStatusType">オプショナルなサブ状態種別。</param>
        /// <param name="subStatusText">オプショナルなサブ状態テキスト。</param>
        /// <param name="subStatusCommand">オプショナルなサブ状態コマンド。</param>
        private Task NotifyResult(
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "",
            string subStatusCommand = "")
        {
            return
                this.ResultNotifier(
                    new AppStatus
                    {
                        StatusType = statusType,
                        StatusText = statusText ?? "",
                        SubStatusType = subStatusType,
                        SubStatusText = subStatusText ?? "",
                        SubStatusCommand = subStatusCommand ?? "",
                    });
        }
    }
}
