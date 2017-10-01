using System;
using System.Threading.Tasks;
using System.Windows.Input;
using RucheHome.Util;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// ReactiveCommand で再生/停止処理の非同期実行を行うためのクラス。
    /// </summary>
    public class PlayStopCommandExecuter
        : AsyncCommandExecuter<PlayStopCommandExecuter.Parameter>
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
            /// <param name="voiceReplaceItems">
            /// 音声文字列置換アイテムコレクション。
            /// </param>
            /// <param name="talkText">トークテキスト。</param>
            /// <param name="useTargetText">本体側のテキストを使うならば true 。</param>
            public Parameter(
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
        /// コンストラクタ。
        /// </summary>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="voiceReplaceItemsGetter">
        /// 音声文字列置換アイテムコレクション取得デリゲート。
        /// </param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="useTargetTextGetter">
        /// 本体側のテキストを使うか否かの取得デリゲート。
        /// </param>
        /// <param name="resultNotifier">処理結果のアプリ状態通知デリゲート。</param>
        public PlayStopCommandExecuter(
            Func<IProcess> processGetter,
            Func<TalkTextReplaceItemCollection> voiceReplaceItemsGetter,
            Func<string> talkTextGetter,
            Func<bool> useTargetTextGetter,
            Func<IAppStatus, Parameter, Task> resultNotifier)
            : base()
        {
            if (processGetter == null)
            {
                throw new ArgumentNullException(nameof(processGetter));
            }
            if (voiceReplaceItemsGetter == null)
            {
                throw new ArgumentNullException(nameof(voiceReplaceItemsGetter));
            }
            if (talkTextGetter == null)
            {
                throw new ArgumentNullException(nameof(talkTextGetter));
            }
            if (useTargetTextGetter == null)
            {
                throw new ArgumentNullException(nameof(useTargetTextGetter));
            }

            this.ResultNotifier =
                resultNotifier ?? throw new ArgumentNullException(nameof(resultNotifier));

            this.AsyncFunc = this.ExecuteAsync;
            this.ParameterConverter =
                _ =>
                    new Parameter(
                        processGetter(),
                        voiceReplaceItemsGetter(),
                        talkTextGetter(),
                        useTargetTextGetter());
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="voiceReplaceItemsGetter">
        /// 音声文字列置換アイテムコレクション取得デリゲート。
        /// </param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="useTargetTextGetter">
        /// 本体側のテキストを使うか否かの取得デリゲート。
        /// </param>
        /// <param name="resultNotifier">処理結果のアプリ状態通知デリゲート。</param>
        public PlayStopCommandExecuter(
            Func<IProcess> processGetter,
            Func<TalkTextReplaceItemCollection> voiceReplaceItemsGetter,
            Func<string> talkTextGetter,
            Func<bool> useTargetTextGetter,
            Action<IAppStatus, Parameter> resultNotifier)
            :
            this(
                processGetter,
                voiceReplaceItemsGetter,
                talkTextGetter,
                useTargetTextGetter,
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
        /// 処理結果のアプリ状態通知デリゲートを取得する。
        /// </summary>
        private Func<IAppStatus, Parameter, Task> ResultNotifier { get; }

        /// <summary>
        /// 非同期の実処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        private async Task ExecuteAsync(Parameter parameter)
        {
            if (parameter?.IsPlayAction == true)
            {
                await this.ExecutePlay(parameter);
            }
            else if (parameter?.IsStopAction == true)
            {
                await this.ExecuteStop(parameter);
            }
            else
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"処理を開始できませんでした。");
            }
        }

        /// <summary>
        /// 再生処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        private async Task ExecutePlay(Parameter parameter)
        {
            var process = parameter.Process;

            // 本体側のテキストを使わない場合のみテキスト設定を行う
            if (!parameter.UseTargetText)
            {
                // テキスト取得
                var text = parameter.TalkText;
                if (text == null)
                {
                    await this.NotifyResult(
                        parameter,
                        AppStatusType.Fail,
                        @"再生処理を開始できませんでした。");
                    return;
                }

                // テキスト置換
                text = parameter.VoiceReplaceItems?.Replace(text) ?? text;

                // テキスト設定
                bool setOk = await process.SetTalkText(text);
                if (!setOk && process.Id == VoiceroidId.Voiceroid2)
                {
                    // VOICEROID2の場合、本体の入力欄が読み取り専用になることがある。
                    // 一旦 再生→停止 の操作を行うことで解除を試みる

                    if (!(await process.Play()))
                    {
                        ThreadTrace.WriteLine(@"VOICEROID2文章入力欄の復旧(再生)に失敗");

                        await this.NotifyResult(
                            parameter,
                            AppStatusType.Fail,
                            @"再生処理に失敗しました。");
                        return;
                    }

                    setOk = (await process.Stop()) && (await process.SetTalkText(text));
                    if (!setOk)
                    {
                        ThreadTrace.WriteLine(@"VOICEROID2文章入力欄の復旧に失敗");
                    }
                }
                if (!setOk)
                {
                    await this.NotifyResult(
                        parameter,
                        AppStatusType.Fail,
                        @"文章の設定に失敗しました。");
                    return;
                }
            }

            // 再生
            var success = await process.Play();

            await this.NotifyResult(
                parameter,
                success ? AppStatusType.Success : AppStatusType.Fail,
                success ? @"再生処理に成功しました。" : @"再生処理に失敗しました。");
        }

        /// <summary>
        /// 停止処理を行う。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        private async Task ExecuteStop(Parameter parameter)
        {
            // 停止
            var success = await parameter.Process.Stop();

            await this.NotifyResult(
                parameter,
                success ? AppStatusType.Success : AppStatusType.Fail,
                success ? @"停止処理に成功しました。" : @"停止処理に失敗しました。");
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
        private Task NotifyResult(
            Parameter parameter,
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "",
            ICommand subStatusCommand = null,
            string subStatusCommandTip = "")
            =>
            this.ResultNotifier(
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
    }
}
