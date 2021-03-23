using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Util;
using RucheHome.Voiceroid;
using RucheHome.Windows.Mvvm.Commands;
using VoiceroidUtil.Services;
using GcmzDrops = RucheHome.AviUtl.ExEdit.GcmzDrops;
using static RucheHome.Util.ArgumentValidater;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace VoiceroidUtil
{
    /// <summary>
    /// ReactiveCommand でWAVEファイル保存処理の非同期実行を行うためのクラス。
    /// </summary>
    public class AsyncSaveCommandHolder
        :
        AsyncCommandHolderBase<
            AsyncSaveCommandHolder.CommandParameter,
            AsyncSaveCommandHolder.CommandResult>
    {
        /// <summary>
        /// コマンドパラメータクラス。
        /// </summary>
        [SuppressMessage("Design", "CA1034")]
        public class CommandParameter
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="process">VOICEROIDプロセス。</param>
            /// <param name="talkTextReplaceConfig">トークテキスト置換設定。</param>
            /// <param name="exoConfig">AviUtl拡張編集ファイル用設定。</param>
            /// <param name="appConfig">アプリ設定。</param>
            /// <param name="talkText">トークテキスト。</param>
            public CommandParameter(
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
        /// コマンド戻り値クラス。
        /// </summary>
        [SuppressMessage("Design", "CA1034")]
        public class CommandResult
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="appStatus">アプリ状態値。</param>
            /// <param name="parameter">コマンド実施時に作成されたパラメータ。</param>
            public CommandResult(IAppStatus appStatus, CommandParameter parameter)
            {
                this.AppStatus = appStatus;
                this.Parameter = parameter;
            }

            /// <summary>
            /// アプリ状態値を取得する。
            /// </summary>
            public IAppStatus AppStatus { get; }

            /// <summary>
            /// コマンド実施時に作成されたパラメータを取得する。
            /// </summary>
            public CommandParameter Parameter { get; }
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canExecuteSource">コマンド実施可否状態のプッシュ通知。</param>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="talkTextReplaceConfigGetter">
        /// トークテキスト置換設定取得デリゲート。
        /// </param>
        /// <param name="exoConfigGetter">
        /// AviUtl拡張編集ファイル用設定取得デリゲート。
        /// </param>
        /// <param name="appConfigGetter">アプリ設定取得デリゲート。</param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="aviUtlFileDropService">
        /// AviUtl拡張編集ファイルドロップサービス。
        /// </param>
        public AsyncSaveCommandHolder(
            IObservable<bool> canExecuteSource,
            Func<IProcess> processGetter,
            Func<TalkTextReplaceConfig> talkTextReplaceConfigGetter,
            Func<ExoConfig> exoConfigGetter,
            Func<AppConfig> appConfigGetter,
            Func<string> talkTextGetter,
            IAviUtlFileDropService aviUtlFileDropService)
            : base(canExecuteSource)
        {
            ValidateArgumentNull(processGetter, nameof(processGetter));
            ValidateArgumentNull(
                talkTextReplaceConfigGetter,
                nameof(talkTextReplaceConfigGetter));
            ValidateArgumentNull(exoConfigGetter, nameof(exoConfigGetter));
            ValidateArgumentNull(appConfigGetter, nameof(appConfigGetter));
            ValidateArgumentNull(talkTextGetter, nameof(talkTextGetter));
            ValidateArgumentNull(aviUtlFileDropService, nameof(aviUtlFileDropService));

            this.ParameterMaker =
                () =>
                    new CommandParameter(
                        processGetter(),
                        talkTextReplaceConfigGetter(),
                        exoConfigGetter(),
                        appConfigGetter(),
                        talkTextGetter());
            this.AviUtlFileDropService = aviUtlFileDropService;
        }

        /// <summary>
        /// VOICEROID2ライクソフトウェアの分割ファイル名にマッチする正規表現。
        /// </summary>
        private static readonly Regex RegexSplitFileName =
            new Regex(
                @"\-\d+\.(wav|txt)$",
                RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

        /// <summary>
        /// WAVEファイルパスを作成する。
        /// </summary>
        /// <param name="config">アプリ設定。</param>
        /// <param name="charaName">キャラ名。</param>
        /// <param name="talkText">トークテキスト。</param>
        /// <param name="voiceroid2Like">VOICEROID2ライクソフトウェア用ならば true 。</param>
        /// <returns>WAVEファイルパス。作成できないならば null 。</returns>
        private static async Task<string> MakeWaveFilePath(
            AppConfig config,
            string charaName,
            string talkText,
            bool voiceroid2Like)
        {
            if (
                config == null ||
                string.IsNullOrWhiteSpace(config.SaveDirectoryPath) ||
                charaName == null ||
                talkText == null)
            {
                return null;
            }

            var dirPath = config.SaveDirectoryPath;
            var baseName =
                FilePathUtil.MakeFileName(config.FileNameFormat, charaName, talkText);

            // 同名ファイルがあるならば名前の末尾に "[数字]" を付ける
            // 作成される可能性のあるテキストファイルや.exoファイルも確認する
            // VOICEROID2ライクの場合は連番ファイルも確認する
            var name = baseName;
            await Task.Run(
                () =>
                {
                    for (int i = 1; ; ++i)
                    {
                        var path = Path.Combine(dirPath, name);
                        if (
                            !File.Exists(path + @".wav") &&
                            !File.Exists(path + @".txt") &&
                            !File.Exists(path + @".exo"))
                        {
                            // VOICEROID2ライクなら連番ファイルも存在チェック
                            var splitFileFound =
                                voiceroid2Like &&
                                Directory.EnumerateFiles(dirPath, name + @"-*.*")
                                    .Select(fp => Path.GetFileName(fp))
                                    .Any(fn => RegexSplitFileName.IsMatch(fn));
                            if (!splitFileFound)
                            {
                                break;
                            }
                        }

                        name = baseName + @"[" + i + @"]";
                    }
                });

            return Path.Combine(dirPath, name + @".wav");
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
        /// 文字列内にキーワードが含まれているVOICEROID識別IDを取得する。
        /// </summary>
        /// <param name="src">文字列。</param>
        /// <returns>VOICEROID識別ID。見つからなければ null 。</returns>
        /// <remarks>
        /// より多くのキーワードが一致するVOICEROID識別IDが優先される。
        /// 一致数が同じ場合はより前方で一致したVOICEROID識別IDが優先される。
        /// 一致位置も同じ場合はより小さいVOICEROID識別IDが優先される。
        /// </remarks>
        private static VoiceroidId? FindKeywordContainedVoiceroidId(string src)
        {
            if (src == null)
            {
                return null;
            }

            VoiceroidId? resultId = null;
            var maxCount = 1;
            var minIndex = int.MaxValue;

            foreach (var id in (VoiceroidId[])Enum.GetValues(typeof(VoiceroidId)))
            {
                var keywords = id.GetInfo().Keywords;
                if (keywords != null)
                {
                    // 一致キーワード群の位置配列作成
                    var indices =
                        keywords.Select(k => src.IndexOf(k)).Where(i => i >= 0).ToArray();

                    // 少なくともこれまでのID以上の一致数でなければダメ
                    // maxCount は初期値 1 なので一致なしでもダメ
                    if (indices.Length >= maxCount)
                    {
                        // 最も前方の一致位置検索
                        var index = indices.Min();

                        // 初回 or 一致数がより多い or 一致位置がより前方
                        if (!resultId.HasValue || indices.Length > maxCount || index < minIndex)
                        {
                            resultId = id;
                            maxCount = indices.Length;
                            minIndex = index;
                        }
                    }
                }
            }

            return resultId;
        }

        /// <summary>
        /// DoOperateExo メソッドの処理結果を表す列挙。
        /// </summary>
        private enum ExoOperationResult
        {
            /// <summary>
            /// 成功。
            /// </summary>
            Success,

            /// <summary>
            /// .exo ファイル保存失敗。
            /// </summary>
            SaveFail,

            /// <summary>
            /// .exo ファイルドロップ失敗。
            /// </summary>
            DropFail,
        }

        /// <summary>
        /// AviUtl拡張編集ファイルドロップ処理タイムアウトミリ秒数。
        /// </summary>
        private const int ExoDropTimeoutMilliseconds = 3000;

        /// <summary>
        /// 設定を基にAviUtl拡張編集ファイル関連の処理を行う。
        /// </summary>
        /// <param name="filePath">WAVEファイルパス。</param>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="text">テキスト。</param>
        /// <param name="appConfig">アプリ設定。</param>
        /// <param name="exoConfig">AviUtl拡張編集ファイル用設定。</param>
        /// <param name="aviUtlFileDropService">
        /// AviUtl拡張編集ファイルドロップサービス。
        /// </param>
        /// <returns>処理結果とエラー文字列のタプル。</returns>
        private static async Task<Tuple<ExoOperationResult, string>> DoOperateExo(
            string filePath,
            VoiceroidId voiceroidId,
            string text,
            AppConfig appConfig,
            ExoConfig exoConfig,
            IAviUtlFileDropService aviUtlFileDropService)
        {
            if (appConfig.IsExoFileMaking)
            {
                var exoFilePath = Path.ChangeExtension(filePath, @".exo");

                // 共通設定更新
                ExoCommonConfig common = null;
                var gcmzResult = GcmzDrops.FileDrop.Result.Success;
                if (
                    appConfig.IsSavedExoFileToAviUtl &&
                    appConfig.IsExoFileParamReplacedByAviUtl)
                {
                    common = exoConfig.Common.Clone();
                    gcmzResult = UpdateExoCommonConfigByAviUtl(ref common);
                }

                // ファイル保存
                var exo =
                    await DoOperateExoSave(
                        exoFilePath,
                        filePath,
                        text,
                        common ?? exoConfig.Common,
                        exoConfig.CharaStyles[voiceroidId]);
                if (exo == null)
                {
                    return
                        Tuple.Create(
                            ExoOperationResult.SaveFail,
                            @".exo ファイルを保存できませんでした。");
                }

                // ファイルドロップ
                if (appConfig.IsSavedExoFileToAviUtl)
                {
                    // UpdateExoCommonConfigByAviUtl で失敗しているなら実施しない
                    var failMessage =
                        (gcmzResult != GcmzDrops.FileDrop.Result.Success) ?
                            MakeFailMessageFromExoDropResult(gcmzResult, true) :
                            await DoOperateExoDrop(
                                exoFilePath,
                                exo.Length,
                                appConfig.AviUtlDropLayers[voiceroidId].Layer,
                                aviUtlFileDropService);
                    if (failMessage != null)
                    {
                        return Tuple.Create(ExoOperationResult.DropFail, failMessage);
                    }
                }
            }

            return Tuple.Create(ExoOperationResult.Success, (string)null);
        }

        /// <summary>
        /// 設定を基にAviUtl拡張編集ファイルの保存処理を行う。
        /// </summary>
        /// <param name="exoFilePath">AviUtl拡張編集ファイルパス。</param>
        /// <param name="waveFilePath">WAVEファイルパス。</param>
        /// <param name="text">テキスト。</param>
        /// <param name="common">共通設定。</param>
        /// <param name="charaStyle">キャラ別スタイル。</param>
        /// <returns>保存した拡張編集オブジェクト。失敗したならば null 。</returns>
        private static async Task<ExEditObject> DoOperateExoSave(
            string exoFilePath,
            string waveFilePath,
            string text,
            ExoCommonConfig common,
            ExoCharaStyle charaStyle)
        {
            // フレーム数算出
            int frameCount = 0;
            try
            {
                var waveTime =
                    await Task.Run(() => (new WaveFileInfo(waveFilePath)).TotalTime);
                var f =
                    (waveTime.Ticks * common.Fps) /
                    (charaStyle.PlaySpeed.Begin * (TimeSpan.TicksPerSecond / 100));
                frameCount = (int)decimal.Floor(f); // 拡張編集の仕様に合わせて切り捨て
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
                return null;
            }

            var exo =
                new ExEditObject
                {
                    Width = common.Width,
                    Height = common.Height,
                    Length = frameCount + common.ExtraFrames,
                    AudioSampleRate = common.AudioSampleRate,
                    AudioChannelCount = common.AudioChannelCount,
                };

            // decimal の小数部桁数を取得
            var scale = (decimal.GetBits(common.Fps)[3] & 0xFF0000) >> 16;

            exo.FpsScale = (int)Math.Pow(10, scale);
            exo.FpsBase = decimal.Floor(common.Fps * exo.FpsScale);

            // テキストレイヤー追加
            {
                var item =
                    new LayerItem
                    {
                        BeginFrame = 1,
                        EndFrame = exo.Length,
                        LayerId = 1,
                        GroupId = common.IsGrouping ? 1 : 0,
                        IsClipping = charaStyle.IsTextClipping
                    };

                var c = charaStyle.Text.Clone();
                ExoTextStyleTemplate.ClearUnused(c);
                c.Text = text;
                item.Components.Add(c);
                item.Components.Add(charaStyle.Render.Clone());

                exo.LayerItems.Add(item);
            }

            // 音声レイヤー追加
            {
                var item =
                    new LayerItem
                    {
                        BeginFrame = 1,
                        EndFrame = frameCount,
                        LayerId = 2,
                        GroupId = common.IsGrouping ? 1 : 0,
                        IsAudio = true,
                    };

                item.Components.Add(
                    new AudioFileComponent
                    {
                        PlaySpeed = charaStyle.PlaySpeed.Clone(),
                        FilePath = waveFilePath,
                    });
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
                return null;
            }

            return exo;
        }

        /// <summary>
        /// AviUtl拡張編集ファイルのドロップ処理を行う。
        /// </summary>
        /// <param name="exoFilePath">AviUtl拡張編集ファイルパス。</param>
        /// <param name="exoLength">AviUtl拡張編集オブジェクトのフレーム数。</param>
        /// <param name="layer">ドロップ先レイヤー番号。</param>
        /// <param name="aviUtlFileDropService">
        /// AviUtl拡張編集ファイルドロップサービス。
        /// </param>
        /// <returns>警告文字列。成功したならば null 。</returns>
        private static async Task<string> DoOperateExoDrop(
            string exoFilePath,
            int exoLength,
            int layer,
            IAviUtlFileDropService aviUtlFileDropService)
        {
            GcmzDrops.FileDrop.Result result;
            try
            {
                result =
                    await aviUtlFileDropService.Run(
                        exoFilePath,
                        exoLength,
                        layer,
                        ExoDropTimeoutMilliseconds);
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
                result = GcmzDrops.FileDrop.Result.Fail;
            }

            return MakeFailMessageFromExoDropResult(result, true);
        }

        /// <summary>
        /// 現在開いているAviUtl拡張編集プロジェクトの情報から、
        /// AviUtl拡張編集ファイル用の共通設定を更新する。
        /// </summary>
        /// <param name="common">更新対象の共通設定。処理成功時のみ変更される。</param>
        /// <returns>処理結果値。</returns>
        private static GcmzDrops.FileDrop.Result UpdateExoCommonConfigByAviUtl(
            ref ExoCommonConfig common)
        {
            switch (GcmzDrops.GcmzInfoReader.Read(out var info))
            {
            case GcmzDrops.GcmzInfoReader.Result.Success:
                {
                    if (!info.IsWindowOpened)
                    {
                        return GcmzDrops.FileDrop.Result.GcmzWindowNotFound;
                    }
                    if (!info.IsProjectOpened)
                    {
                        return GcmzDrops.FileDrop.Result.ProjectNotFound;
                    }

                    // 更新
                    common.Width = info.Width;
                    common.Height = info.Height;
                    common.ExtraFrames =
                        (int)(common.ExtraFrames * info.FrameRate / common.Fps + 0.5m);
                    common.Fps = info.FrameRate;
                    common.AudioSampleRate = info.AudioSampleRate;
                    common.AudioChannelCount = info.AudioChannelCount;
                }
                break;

            case GcmzDrops.GcmzInfoReader.Result.FileMappingFail:
                return GcmzDrops.FileDrop.Result.FileMappingFail;

            case GcmzDrops.GcmzInfoReader.Result.MapViewFail:
                return GcmzDrops.FileDrop.Result.MapViewFail;

            default:
                // 来ないはず…
                return GcmzDrops.FileDrop.Result.Fail;
            }

            return GcmzDrops.FileDrop.Result.Success;
        }

        /// <summary>
        /// AviUtl拡張編集ファイルドロップ処理結果値から失敗文字列を作成する。
        /// </summary>
        /// <param name="result">AviUtl拡張編集ファイルドロップ処理結果値。</param>
        /// <param name="bootCheckOnFileMappingFail">
        /// result が <see cref="GcmzDrops.FileDrop.Result.FileMappingFail"/>
        /// の時にAviUtlプロセスの起動確認を行うならば true 。
        /// </param>
        /// <returns>失敗文字列。成功値または成功扱いならば null 。</returns>
        private static string MakeFailMessageFromExoDropResult(
            GcmzDrops.FileDrop.Result result,
            bool bootCheckOnFileMappingFail)
        {
            switch (result)
            {
            case GcmzDrops.FileDrop.Result.Success:
                break;

            case GcmzDrops.FileDrop.Result.FileMappingFail:
                // AviUtlが起動していない or ごちゃまぜドロップス未導入
                // bootCheckOnFileMappingFail == true かつ起動していないなら成功扱い
                if (
                    !bootCheckOnFileMappingFail ||
                    Process.GetProcessesByName(@"aviutl").Length > 0)
                {
                    return @"ごちゃまぜドロップス情報を取得できません。";
                }
                break;

            case GcmzDrops.FileDrop.Result.GcmzWindowNotFound:
                return @"ごちゃまぜドロップス情報が未初期化です。";

            case GcmzDrops.FileDrop.Result.ProjectNotFound:
                return @"AviUtl拡張編集プロジェクトが未作成です。";

            case GcmzDrops.FileDrop.Result.ExEditWindowNotFound:
                return @"AviUtl拡張編集ウィンドウが見つかりません。";

            case GcmzDrops.FileDrop.Result.ExEditWindowInvisible:
                return @"AviUtl拡張編集ウィンドウが表示されていません。";

            case GcmzDrops.FileDrop.Result.MessageTimeout:
                return @"AviUtl拡張編集との連携がタイムアウトしました。";

            case GcmzDrops.FileDrop.Result.Fail:
            case GcmzDrops.FileDrop.Result.MapViewFail:
            case GcmzDrops.FileDrop.Result.MessageFail:
            case GcmzDrops.FileDrop.Result.MutexOpenFail:
            case GcmzDrops.FileDrop.Result.MutexLockFail:
            case GcmzDrops.FileDrop.Result.MutexLockTimeout:
                ThreadTrace.WriteLine(@"AviUtl拡張編集 連携失敗 : " + result);
                return @"AviUtl拡張編集との連携に失敗しました。";
            }

            return null;
        }

        /// <summary>
        /// 『ゆっくりMovieMaker3』プロセス操作インスタンスを取得する。
        /// </summary>
        private static YmmProcess YmmProcess { get; } = new YmmProcess();

        /// <summary>
        /// 『ゆっくりMovieMaker3』プロセス操作失敗時のリトライ回数。
        /// </summary>
        private const int YmmRetryCount = 8;

        /// <summary>
        /// 『ゆっくりMovieMaker3』プロセス操作失敗時のリトライインターバル。
        /// </summary>
        private static readonly TimeSpan YmmRetryInterval = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// 設定を基に『ゆっくりMovieMaker3』の操作を行う。
        /// </summary>
        /// <param name="filePath">WAVEファイルパス。</param>
        /// <param name="voiceroidId">VOICEROID識別ID。</param>
        /// <param name="charaNameForMulti">
        /// voiceoidId が複数キャラクターを保持するプロセスを表す場合に用いるキャラ名。
        /// </param>
        /// <param name="config">アプリ設定。</param>
        /// <returns>警告文字列。問題ないならば null 。</returns>
        private static async Task<string> DoOperateYmm(
            string filePath,
            VoiceroidId voiceroidId,
            string charaNameForMulti,
            AppConfig config)
        {
            if (!config.IsSavedFileToYmm)
            {
                return null;
            }

            // YMM3キャラ名決定
            string charaName =
                voiceroidId.GetInfo().HasMultiCharacters ?
                    charaNameForMulti :
                    config.YmmCharaRelations[voiceroidId].YmmCharaName;

            string warnText = null;

            for (int ri = 0; ri <= YmmRetryCount; ++ri)
            {
                // リトライ時処理
                if (ri > 0)
                {
                    YmmProcess.Reset();
                    await Task.Delay(YmmRetryInterval);
                }

                // 状態更新
                try
                {
                    await YmmProcess.Update();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return @"ゆっくりMovieMaker3の起動状態確認に失敗しました。";
                }

                // そもそも起動していないなら何もしない
                if (!YmmProcess.IsRunning)
                {
                    return null;
                }

                // タイムラインウィンドウが見つからなければ即失敗
                if (!YmmProcess.IsTimelineWindowFound)
                {
                    return @"ゆっくりMovieMaker3のタイムラインが見つかりません。";
                }

                // コントロール群が見つからなければリトライ
                if (!YmmProcess.IsTimelineElementFound)
                {
                    warnText = @"ゆっくりMovieMaker3のタイムラインを操作できませんでした。";
                    continue;
                }

                // ファイルパス設定
                if (!(await YmmProcess.SetTimelineSpeechEditValue(filePath)))
                {
                    warnText = @"ゆっくりMovieMaker3へのファイルパス設定に失敗しました。";
                    continue;
                }

                // キャラ選択
                // そもそもキャラ名が存在しない場合は何もしない
                if (
                    config.IsYmmCharaSelecting &&
                    !string.IsNullOrEmpty(charaName) &&
                    (await YmmProcess.SelectTimelineCharaComboBoxItem(charaName)) == false)
                {
                    warnText = @"ゆっくりMovieMaker3のキャラ選択に失敗しました。";
                    continue;
                }

                // ボタン押下
                if (
                    config.IsYmmAddButtonClicking &&
                    !(await YmmProcess.ClickTimelineSpeechAddButton()))
                {
                    warnText = @"ゆっくりMovieMaker3の追加ボタンクリックに失敗しました。";
                    continue;
                }

#if DEBUG
                // デバッグ時にはリトライ回数を報告
                if (ri > 0)
                {
                    warnText = @"YMM3リトライ回数 : " + ri;
                    ThreadDebug.WriteLine(warnText);
                    return warnText;
                }
#endif // DEBUG

                return null;
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
        /// <param name="subStatusCommandTip">
        /// オプショナルなサブ状態コマンドのチップテキスト。
        /// </param>
        private static CommandResult MakeResult(
            CommandParameter parameter,
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "",
            ICommand subStatusCommand = null,
            string subStatusCommandTip = "")
            =>
            new CommandResult(
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
                },
                parameter);

        /// <summary>
        /// コマンドパラメータ作成デリゲートを取得する。
        /// </summary>
        private Func<CommandParameter> ParameterMaker { get; }

        /// <summary>
        /// AviUtl拡張編集ファイルドロップサービスを取得する。
        /// </summary>
        private IAviUtlFileDropService AviUtlFileDropService { get; }

        #region AsyncCommandHolderBase<TParameter, TResult> のオーバライド

        /// <summary>
        /// コマンドパラメータを変換する。
        /// </summary>
        /// <param name="parameter">元のコマンドパラメータ。無視される。</param>
        /// <returns>変換されたコマンドパラメータ。</returns>
        protected override sealed CommandParameter ConvertParameter(
            CommandParameter parameter)
            =>
            this.ParameterMaker();

        /// <summary>
        /// コマンド処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <returns>コマンドの戻り値。</returns>
        protected override sealed async Task<CommandResult> Execute(
            CommandParameter parameter)
        {
            var process = parameter?.Process;
            var talkTextReplaceConfig = parameter?.TalkTextReplaceConfig;
            var exoConfig = parameter?.ExoConfig;
            var appConfig = parameter?.AppConfig;

            if (process == null || exoConfig == null || appConfig == null)
            {
                return
                    MakeResult(
                        parameter,
                        AppStatusType.Fail,
                        @"ファイル保存を開始できませんでした。");
            }

            if (!process.IsRunning || process.IsSaving || process.IsDialogShowing)
            {
                return
                    MakeResult(
                        parameter,
                        AppStatusType.Fail,
                        @"ファイル保存を開始できませんでした。");
            }

            // 基テキスト、音声用テキスト作成
            string text, voiceText;
            if (appConfig.UseTargetText)
            {
                // 本体側のテキストを使う設定ならそちらから取得
                voiceText = text = await process.GetTalkText();
                if (text == null)
                {
                    return
                        MakeResult(
                            parameter,
                            AppStatusType.Fail,
                            @"本体側の文章を取得できませんでした。");
                }
                if (!process.CanSaveBlankText && string.IsNullOrWhiteSpace(voiceText))
                {
                    return
                        MakeResult(
                            parameter,
                            AppStatusType.Fail,
                            @"本体側の文章が空白文です。",
                            subStatusText: @"空白文を音声保存することはできません。");
                }
            }
            else
            {
                // 基テキスト取得
                text = parameter?.TalkText;
                if (text == null)
                {
                    return
                        MakeResult(
                            parameter,
                            AppStatusType.Fail,
                            @"ファイル保存を開始できませんでした。");
                }

                // 音声用テキスト作成
                voiceText = talkTextReplaceConfig?.VoiceReplaceItems.Replace(text) ?? text;
                if (!process.CanSaveBlankText && string.IsNullOrWhiteSpace(voiceText))
                {
                    return
                        MakeResult(
                            parameter,
                            AppStatusType.Fail,
                            @"文章の音声用置換結果が空白になります。",
                            subStatusText: @"空白文を音声保存することはできません。");
                }
            }

            // 字幕用テキスト作成
            var fileText =
                talkTextReplaceConfig?.TextFileReplaceItems.Replace(text) ?? text;

            // キャラクター名取得
            var charaName = await process.GetCharacterName();

            // VOICEROID2ライクか？
            bool voiceroid2Like = process.Id.IsVoiceroid2LikeSoftware();

            // WAVEファイルパス決定
            string filePath;
            try
            {
                filePath =
                    await MakeWaveFilePath(
                        appConfig,
                        charaName,
                        text,
                        voiceroid2Like);
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
                return
                    MakeResult(
                        parameter,
                        AppStatusType.Fail,
                        @"ファイル名の決定に失敗しました。");
            }
            if (filePath == null)
            {
                return
                    MakeResult(
                        parameter,
                        AppStatusType.Fail,
                        @"ファイル保存を開始できませんでした。");
            }

            // パスが正常かチェック
            var pathStatus = FilePathUtil.CheckPathStatus(filePath, pathIsFile: true);
            if (pathStatus.StatusType != AppStatusType.None)
            {
                return new CommandResult(pathStatus, parameter);
            }

            // トークテキスト設定
            if (!appConfig.UseTargetText && !(await process.SetTalkText(voiceText)))
            {
                // VOICEROID2ライクの場合、本体の入力欄が読み取り専用になることがある。
                // 再生時と違い、メッセージを返すのみでリカバリはしない。

                return
                    MakeResult(
                        parameter,
                        AppStatusType.Fail,
                        @"文章の設定に失敗しました。",
                        AppStatusType.Information,
                        voiceroid2Like ? @"一度再生を行ってみてください。" : null);
            }

            // WAVEファイル保存
            var result = await process.Save(filePath);
            if (!result.IsSucceeded)
            {
                return
                    MakeResult(
                        parameter,
                        AppStatusType.Fail,
                        result.Error,
                        subStatusText: result.ExtraMessage);
            }

            var requiredFilePath = filePath;
            filePath = result.FilePath;

            // 本体側の自動命名時はファイルパスが空文字列            
            if (string.IsNullOrEmpty(filePath))
            {
                return
                    MakeResult(
                        parameter,
                        AppStatusType.Success,
                        @"音声ファイルを保存しました。",
                        AppStatusType.Warning,
                        @"本体側での自動命名時は音声ファイル保存のみ行います。");
            }

            var statusText = Path.GetFileName(filePath) + @" を保存しました。";

            // VOICEROID2ライクかつファイル名が異なる
            // → ファイル分割されているので以降の処理は行わない
            if (
                voiceroid2Like &&
                !string.Equals(
                    Path.GetFileNameWithoutExtension(requiredFilePath),
                    Path.GetFileNameWithoutExtension(filePath),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return
                    MakeResult(
                        parameter,
                        AppStatusType.Success,
                        statusText,
                        AppStatusType.Warning,
                        @"ファイル分割時は音声ファイル保存のみ行います。");
            }

            // テキストファイル保存
            if (appConfig.IsTextFileForceMaking)
            {
                var txtPath = Path.ChangeExtension(filePath, @".txt");
                if (!(await WriteTextFile(txtPath, fileText, appConfig.IsTextFileUtf8)))
                {
                    return
                        MakeResult(
                            parameter,
                            AppStatusType.Success,
                            statusText,
                            AppStatusType.Fail,
                            @"テキストファイルを保存できませんでした。");
                }
            }

            // 以降の処理の対象となるVOICEROID識別ID
            // 複数キャラクターを保持するならキャラクター名からキャラ選別
            var voiceroidId =
                (process.HasMultiCharacters ? FindKeywordContainedVoiceroidId(charaName) : null) ??
                process.Id;

            // .exo ファイル関連処理
            var exoResult =
                await DoOperateExo(
                    filePath,
                    voiceroidId,
                    fileText,
                    appConfig,
                    exoConfig,
                    this.AviUtlFileDropService);
            if (exoResult.Item1 == ExoOperationResult.SaveFail)
            {
                return
                    MakeResult(
                        parameter,
                        AppStatusType.Success,
                        statusText,
                        AppStatusType.Fail,
                        exoResult.Item2);
            }
            var exoWarnText =
                (exoResult.Item1 == ExoOperationResult.Success) ? null : exoResult.Item2;

            // ゆっくりMovieMaker3処理
            var ymmWarnText =
                await DoOperateYmm(filePath, voiceroidId, charaName, appConfig);

            var warnText = exoWarnText ?? ymmWarnText;
            return
                MakeResult(
                    parameter,
                    AppStatusType.Success,
                    statusText,
                    (warnText == null) ? AppStatusType.None : AppStatusType.Warning,
                    warnText ?? @"保存先フォルダーを開く",
                    (warnText == null) ?
                        new ProcessStartCommand(@"explorer.exe", $@"/select,""{filePath}""") :
                        null,
                    (warnText == null) ? Path.GetDirectoryName(filePath) : null);
        }

        #endregion
    }
}
