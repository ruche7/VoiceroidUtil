using System;
using System.Threading.Tasks;
using System.Windows.Input;
using RucheHome.Util;
using RucheHome.Voiceroid;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil
{
    /// <summary>
    /// 再生/停止処理を行う非同期コマンドを保持するクラス。
    /// </summary>
    public class AsyncPlayStopCommandHolder
        :
        AsyncCommandHolderBase<
            AsyncPlayStopCommandHolder.CommandParameter,
            AsyncPlayStopCommandHolder.CommandResult>
    {
        /// <summary>
        /// コマンドパラメータクラス。
        /// </summary>
        public class CommandParameter
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="process">VOICEROIDプロセス。</param>
            /// <param name="voiceReplaceItems">
            /// 音声文字列置換アイテムコレクション。
            /// </param>
            /// <param name="talkText">トークテキスト。</param>
            /// <param name="useTargetText">本体側のテキストを使うならば true 。</param>
            public CommandParameter(
                IProcess process,
                TalkTextReplaceItemCollection voiceReplaceItems,
                string talkText,
                bool useTargetText)
            {
                this.Process = process;
                this.VoiceReplaceItems = voiceReplaceItems;
                this.TalkText = talkText;
                this.UseTargetText = useTargetText;

                var playing = process?.IsPlaying;
                this.IsPlayAction = (playing == false);
                this.IsStopAction = (playing == true);
            }

            /// <summary>
            /// VOICEROIDプロセスを取得する。
            /// </summary>
            public IProcess Process { get; }

            /// <summary>
            /// 音声文字列置換アイテムコレクションを取得する。
            /// </summary>
            public TalkTextReplaceItemCollection VoiceReplaceItems { get; }

            /// <summary>
            /// トークテキストを取得する。
            /// </summary>
            public string TalkText { get; }

            /// <summary>
            /// 本体側のテキストを使うか否かを取得する。
            /// </summary>
            public bool UseTargetText { get; }

            /// <summary>
            /// 再生処理を行うか否かを取得する。
            /// </summary>
            public bool IsPlayAction { get; }

            /// <summary>
            /// 停止処理を行うか否かを取得する。
            /// </summary>
            public bool IsStopAction { get; }
        }

        /// <summary>
        /// コマンド戻り値クラス。
        /// </summary>
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
        /// <param name="voiceReplaceItemsGetter">
        /// 音声文字列置換アイテムコレクション取得デリゲート。
        /// </param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="useTargetTextGetter">
        /// 本体側のテキストを使うか否かの取得デリゲート。
        /// </param>
        public AsyncPlayStopCommandHolder(
            IObservable<bool> canExecuteSource,
            Func<IProcess> processGetter,
            Func<TalkTextReplaceItemCollection> voiceReplaceItemsGetter,
            Func<string> talkTextGetter,
            Func<bool> useTargetTextGetter)
            : base(canExecuteSource)
        {
            ValidateArgumentNull(processGetter, nameof(processGetter));
            ValidateArgumentNull(voiceReplaceItemsGetter, nameof(voiceReplaceItemsGetter));
            ValidateArgumentNull(talkTextGetter, nameof(talkTextGetter));
            ValidateArgumentNull(useTargetTextGetter, nameof(useTargetTextGetter));

            this.ParameterMaker =
                () =>
                    new CommandParameter(
                        processGetter(),
                        voiceReplaceItemsGetter(),
                        talkTextGetter(),
                        useTargetTextGetter());
        }

        /// <summary>
        /// コマンド戻り値を作成する。
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
        /// 再生処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <returns>コマンドの戻り値。</returns>
        private async Task<CommandResult> ExecutePlay(CommandParameter parameter)
        {
            var process = parameter.Process;

            // 本体側のテキストを使わない場合のみテキスト設定を行う
            if (!parameter.UseTargetText)
            {
                // テキスト取得
                var text = parameter.TalkText;
                if (text == null)
                {
                    return
                        MakeResult(
                            parameter,
                            AppStatusType.Fail,
                            @"再生処理を開始できませんでした。");
                }

                // テキスト置換
                text = parameter.VoiceReplaceItems?.Replace(text) ?? text;

                // テキスト設定
                bool setOk = await process.SetTalkText(text);
                if (!setOk && process.Id.IsVoiceroid2LikeSoftware())
                {
                    // VOICEROID2ライクの場合、本体の入力欄が読み取り専用になることがある。
                    // 一旦 再生→停止 の操作を行うことで解除を試みる

                    if (!(await process.Play()))
                    {
                        ThreadTrace.WriteLine(@"VOICEROID2文章入力欄の復旧(再生)に失敗");

                        return
                            MakeResult(
                                parameter,
                                AppStatusType.Fail,
                                @"再生処理に失敗しました。");
                    }

                    setOk = (await process.Stop()) && (await process.SetTalkText(text));
                    if (!setOk)
                    {
                        ThreadTrace.WriteLine(@"VOICEROID2文章入力欄の復旧に失敗");
                    }
                }
                if (!setOk)
                {
                    return
                        MakeResult(
                            parameter,
                            AppStatusType.Fail,
                            @"文章の設定に失敗しました。");
                }
            }

            // 再生
            var success = await process.Play();

            return
                MakeResult(
                    parameter,
                    success ? AppStatusType.Success : AppStatusType.Fail,
                    success ? @"再生処理に成功しました。" : @"再生処理に失敗しました。");
        }

        /// <summary>
        /// 停止処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <returns>コマンドの戻り値。</returns>
        private async Task<CommandResult> ExecuteStop(CommandParameter parameter)
        {
            // 停止
            var success = await parameter.Process.Stop();

            return
                MakeResult(
                    parameter,
                    success ? AppStatusType.Success : AppStatusType.Fail,
                    success ? @"停止処理に成功しました。" : @"停止処理に失敗しました。");
        }

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
            if (parameter?.IsPlayAction == true)
            {
                return await this.ExecutePlay(parameter);
            }
            else if (parameter?.IsStopAction == true)
            {
                return await this.ExecuteStop(parameter);
            }

            return
                MakeResult(
                    parameter,
                    AppStatusType.Fail,
                    @"処理を開始できませんでした。");
        }

        #endregion
    }
}
