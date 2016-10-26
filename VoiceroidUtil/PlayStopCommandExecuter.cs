using System;
using System.Threading.Tasks;
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
            public Parameter(
                IProcess process,
                TalkTextReplaceItemCollection voiceReplaceItems,
                string talkText)
            {
                this.Process = process;
                this.VoiceReplaceItems = voiceReplaceItems;
                this.TalkText = talkText;
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
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="voiceReplaceItemsGetter">
        /// 音声文字列置換アイテムコレクション取得デリゲート。
        /// </param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="resultNotifier">処理結果のアプリ状態通知デリゲート。</param>
        public PlayStopCommandExecuter(
            Func<IProcess> processGetter,
            Func<TalkTextReplaceItemCollection> voiceReplaceItemsGetter,
            Func<string> talkTextGetter,
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
            if (resultNotifier == null)
            {
                throw new ArgumentNullException(nameof(resultNotifier));
            }

            this.AsyncFunc = this.ExecuteAsync;
            this.ParameterConverter =
                _ =>
                    new Parameter(
                        processGetter(),
                        voiceReplaceItemsGetter(),
                        talkTextGetter());

            this.ResultNotifier = resultNotifier;
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="processGetter">VOICEROIDプロセス取得デリゲート。</param>
        /// <param name="voiceReplaceItemsGetter">
        /// 音声文字列置換アイテムコレクション取得デリゲート。
        /// </param>
        /// <param name="talkTextGetter">トークテキスト取得デリゲート。</param>
        /// <param name="resultNotifier">処理結果のアプリ状態通知デリゲート。</param>
        public PlayStopCommandExecuter(
            Func<IProcess> processGetter,
            Func<TalkTextReplaceItemCollection> voiceReplaceItemsGetter,
            Func<string> talkTextGetter,
            Action<IAppStatus, Parameter> resultNotifier)
            :
            this(
                processGetter,
                voiceReplaceItemsGetter,
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
            if (process == null)
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"処理を開始できませんでした。");
                return;
            }

            await (
                process.IsPlaying ?
                    this.ExecuteStop(parameter) : this.ExecutePlay(parameter));
        }

        /// <summary>
        /// 再生処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        private async Task ExecutePlay(Parameter parameter)
        {
            var process = parameter.Process;

            var text = parameter.TalkText;
            if (text == null)
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"処理を開始できませんでした。");
                return;
            }

            // テキスト置換
            text = parameter.VoiceReplaceItems?.Replace(text) ?? text;
            if (string.IsNullOrWhiteSpace(text))
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"文章の音声用置換結果が空文字列になります。",
                    subStatusText: @"空文字列を再生することはできません。");
                return;
            }

            // テキスト設定
            if (!(await process.SetTalkText(text)))
            {
                await this.NotifyResult(
                    parameter,
                    AppStatusType.Fail,
                    @"文章の設定に失敗しました。");
                return;
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
