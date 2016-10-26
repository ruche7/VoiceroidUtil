﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// ReactiveCommand でWAVEファイル保存処理の非同期実行を行うためのクラス。
    /// </summary>
    public class SaveCommandExecuter : AsyncCommandExecuter<SaveCommandExecuter.Parameter>
    {
        /// <summary>
        /// コマンドパラメータクラス。
        /// </summary>
        public class Parameter
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="process">VOICEROIDプロセス。</param>
            /// <param name="talkTextReplaceConfig">トークテキスト置換設定。</param>
            /// <param name="exoConfig">AviUtl拡張編集ファイル用設定。</param>
            /// <param name="appConfig">アプリ設定。</param>
            /// <param name="talkText">トークテキスト。</param>
            public Parameter(
                IProcess process,
                TalkTextReplaceConfig talkTextReplaceConfig,
                ExoConfig exoConfig,
                AppConfig appConfig,
                string talkText)
            {
                this.Process = process;
                this.TalkTextReplaceConfig = talkTextReplaceConfig;
                this.ExoConfig = exoConfig;
                this.AppConfig = appConfig;
                this.TalkText = talkText;
            }

            /// <summary>
            /// VOICEROIDプロセスを取得する。
            /// </summary>
            public IProcess Process { get; }

            /// <summary>
            /// トークテキスト置換設定を取得する。
            /// </summary>
            public TalkTextReplaceConfig TalkTextReplaceConfig { get; }

            /// <summary>
            /// AviUtl拡張編集ファイル用設定を取得する。
            /// </summary>
            public ExoConfig ExoConfig { get; }

            /// <summary>
            /// アプリ設定を取得する。
            /// </summary>
            public AppConfig AppConfig { get; }

            /// <summary>
            /// トークテキストを取得する。
            /// </summary>
            public string TalkText { get; }
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="talkTextReplaceConfigGetter">
        /// トークテキスト置換設定取得デリゲート。
        /// </param>
        /// <param name="exoConfigGetter">
        /// AviUtl拡張編集ファイル用設定取得デリゲート。
        /// </param>
        /// <param name="appConfigGetter">アプリ設定取得デリゲート。</param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="resultNotifier">処理結果のアプリ状態通知デリゲート。</param>
        public SaveCommandExecuter(
            Func<IProcess> processGetter,
            Func<TalkTextReplaceConfig> talkTextReplaceConfigGetter,
            Func<ExoConfig> exoConfigGetter,
            Func<AppConfig> appConfigGetter,
            Func<string> talkTextGetter,
            Func<IAppStatus, Parameter, Task> resultNotifier)
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
            if (exoConfigGetter == null)
            {
                throw new ArgumentNullException(nameof(exoConfigGetter));
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

            this.AsyncFunc = this.ExecuteAsync;
            this.ParameterConverter =
                _ =>
                    new Parameter(
                        processGetter(),
                        talkTextReplaceConfigGetter(),
                        exoConfigGetter(),
                        appConfigGetter(),
                        talkTextGetter());

            this.ResultNotifier = resultNotifier;
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="talkTextReplaceConfigGetter">
        /// トークテキスト置換設定取得デリゲート。
        /// </param>
        /// <param name="exoConfigGetter">
        /// AviUtl拡張編集ファイル用設定取得デリゲート。
        /// </param>
        /// <param name="appConfigGetter">アプリ設定取得デリゲート。</param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="resultNotifier">処理結果のアプリ状態通知デリゲート。</param>
        public SaveCommandExecuter(
            Func<IProcess> processGetter,
            Func<TalkTextReplaceConfig> talkTextReplaceConfigGetter,
            Func<ExoConfig> exoConfigGetter,
            Func<AppConfig> appConfigGetter,
            Func<string> talkTextGetter,
            Action<IAppStatus, Parameter> resultNotifier)
            :
            this(
                processGetter,
                talkTextReplaceConfigGetter,
                exoConfigGetter,
                appConfigGetter,
                talkTextGetter,
                (resultNotifier == null) ?
                    (Func<IAppStatus, Parameter, Task>)null :
                    async (r, p) =>
                    {
                        resultNotifier(r, p);
                        await Task.FromResult(0);
                    })
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
        private static async Task<string> MakeWaveFilePath(
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
            await Task.Run(
                () =>
                {
                    for (
                        int i = 1;
                        File.Exists(path + @".wav") ||
                        File.Exists(path + @".txt") ||
                        File.Exists(path + @".exo");
                        ++i)
                    {
                        path = basePath + @"[" + i + @"]";
                    }
                });

            return (path + @".wav");
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
        /// 処理結果のアプリ状態通知デリゲートを取得する。
        /// </summary>
        private Func<IAppStatus, Parameter, Task> ResultNotifier { get; }

        /// <summary>
        /// 非同期の実処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        private async Task ExecuteAsync(Parameter parameter)
        {
            var process = parameter?.Process;
            var talkTextReplaceConfig = parameter?.TalkTextReplaceConfig;
            var exoConfig = parameter?.ExoConfig;
            var appConfig = parameter?.AppConfig;
            var text = parameter?.TalkText;

            if (process == null || exoConfig == null || appConfig == null || text == null)
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"ファイル保存を開始できませんでした。");
                return;
            }

            if (!process.IsRunning || process.IsSaving || process.IsDialogShowing)
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"ファイル保存を開始できませんでした。");
                return;
            }

            // テキスト作成
            var voiceText =
                talkTextReplaceConfig?.VoiceReplaceItems.Replace(text) ?? text;
            if (string.IsNullOrWhiteSpace(voiceText))
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"文章の音声用置換結果が空文字列になります。",
                    subStatusText: @"空文字列を再生することはできません。");
                return;
            }
            var fileText =
                talkTextReplaceConfig?.TextFileReplaceItems.Replace(text) ?? text;

            // WAVEファイルパス決定
            var filePath = await MakeWaveFilePath(appConfig, process, text);
            if (filePath == null)
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"ファイル保存を開始できませんでした。");
                return;
            }

            // パスが正常かチェック
            var pathStatus = FilePathUtil.CheckPathStatus(filePath);
            if (pathStatus.StatusType != AppStatusType.None)
            {
                await this.ResultNotifier(pathStatus, parameter);
                return;
            }

            // トークテキスト設定
            if (!(await process.SetTalkText(voiceText)))
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"文章の設定に失敗しました。");
                return;
            }

            // WAVEファイル保存
            var result = await process.Save(filePath);
            if (!result.IsSucceeded)
            {
                await this.NotifyResult(parameter, AppStatusType.Fail, result.Error);
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
                        parameter,
                        AppStatusType.Success,
                        statusText,
                        AppStatusType.Fail,
                        @"テキストファイルを保存できませんでした。");
                    return;
                }
            }

            // .exo ファイル保存
            if (appConfig.IsExoFileMaking)
            {
                var exoPath = Path.ChangeExtension(filePath, @".exo");
                var ok =
                    await this.DoWriteExoFile(
                        exoPath,
                        exoConfig,
                        process.Id,
                        filePath,
                        fileText);
                if (!ok)
                {
                    await this.NotifyResult(
                        parameter,
                        AppStatusType.Success,
                        statusText,
                        AppStatusType.Fail,
                        @".exo ファイルを保存できませんでした。");
                    return;
                }
            }

            // ゆっくりMovieMaker処理
            var warnText = await DoOperateYmm(filePath, process.Id, appConfig);

            await this.NotifyResult(
                parameter,
                AppStatusType.Success,
                statusText,
                (warnText == null) ? AppStatusType.None : AppStatusType.Warning,
                warnText ?? @"保存先フォルダーを開く",
                (warnText == null) ? appConfig.SaveDirectoryPath : null);
        }

        /// <summary>
        /// 設定を基にAviUtl拡張編集ファイル書き出しを行う。
        /// </summary>
        /// <param name="exoFilePath">AviUtl拡張編集ファイルパス。</param>
        /// <param name="exoConfig">AviUtl拡張編集ファイル用設定。</param>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="waveFilePath">WAVEファイルパス。</param>
        /// <param name="text">テキスト。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        private async Task<bool> DoWriteExoFile(
            string exoFilePath,
            ExoConfig exoConfig,
            VoiceroidId voiceroidId,
            string waveFilePath,
            string text)
        {
            var common = exoConfig.Common;
            var charaStyle = exoConfig.CharaStyles[voiceroidId];

            // フレーム数算出
            int frameCount = 0;
            try
            {
                var waveTime =
                    await Task.Run(() => (new WaveFileInfo(waveFilePath)).TotalTime);
                var f =
                    (waveTime.Ticks * common.Fps) /
                    (charaStyle.PlaySpeed.Begin * (TimeSpan.TicksPerSecond / 100));
                frameCount = (int)decimal.Ceiling(f);
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
                return false;
            }

            var exo = new ExEditObject();

            exo.Width = common.Width;
            exo.Height = common.Height;
            exo.Length = frameCount + common.ExtraFrames;

            var scale = (decimal.GetBits(common.Fps)[3] & 0xFF0000) >> 16;
            exo.FpsScale = (int)Math.Pow(10, scale);
            exo.FpsBase = decimal.Floor(common.Fps * exo.FpsScale);

            // テキストレイヤー追加
            {
                var item = new LayerItem();

                item.BeginFrame = 1;
                item.EndFrame = exo.Length;
                item.LayerId = 1;
                item.GroupId = common.IsGrouping ? 1 : 0;
                item.IsClipping = charaStyle.IsTextClipping;
                {
                    var c = charaStyle.Text.Clone();
                    ExoTextStyleTemplate.ClearUnused(c);
                    item.Components.Add(c);
                }
                item.Components.Add(charaStyle.Render.Clone());

                exo.LayerItems.Add(item);
            }

            // 音声レイヤー追加
            {
                var item = new LayerItem();

                item.BeginFrame = 1;
                item.EndFrame = frameCount;
                item.LayerId = 2;
                item.GroupId = common.IsGrouping ? 1 : 0;
                item.IsAudio = true;
                {
                    var c = new AudioFileComponent();
                    c.PlaySpeed = charaStyle.PlaySpeed.Clone();
                    c.FilePath = waveFilePath;
                    item.Components.Add(c);
                }
                item.Components.Add(charaStyle.Play.Clone());

                exo.LayerItems.Add(item);
            }

            // ファイル書き出し
            try
            {
                await ExoFileReaderWriter.WriteAsync(exoFilePath, exo);
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
                return false;
            }

            return true;
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
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <param name="statusType">状態種別。</param>
        /// <param name="statusText">状態テキスト。</param>
        /// <param name="subStatusType">オプショナルなサブ状態種別。</param>
        /// <param name="subStatusText">オプショナルなサブ状態テキスト。</param>
        /// <param name="subStatusCommand">オプショナルなサブ状態コマンド。</param>
        private Task NotifyResult(
            Parameter parameter,
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "",
            string subStatusCommand = "")
            =>
            this.ResultNotifier(
                new AppStatus
                {
                    StatusType = statusType,
                    StatusText = statusText ?? "",
                    SubStatusType = subStatusType,
                    SubStatusText = subStatusText ?? "",
                    SubStatusCommand = subStatusCommand ?? "",
                },
                parameter);
    }
}
