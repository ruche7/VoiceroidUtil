using System;
using System.Windows.Input;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリ状態を提供する構造体。
    /// </summary>
    public class AppStatus : IAppStatus
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="statusType">状態種別。</param>
        /// <param name="statusText">状態テキスト。</param>
        /// <param name="command">付随コマンド。</param>
        /// <param name="commandText">付随コマンドテキスト。</param>
        /// <param name="subStatusType">オプショナルなサブ状態種別。</param>
        /// <param name="subStatusText">オプショナルなサブ状態テキスト。</param>
        public AppStatus(
            AppStatusType statusType = AppStatusType.None,
            string statusText = null,
            ICommand command = null,
            string commandText = null,
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = null)
        {
            this.StatusType = statusType;
            this.StatusText = statusText ?? "";
            this.Command = command;
            this.CommandText = commandText ?? "";
            this.SubStatusType = subStatusType;
            this.SubStatusText = subStatusText ?? "";
        }

        /// <summary>
        /// 状態種別を取得する。
        /// </summary>
        public AppStatusType StatusType { get; private set; }

        /// <summary>
        /// 状態テキストを取得する。
        /// </summary>
        public string StatusText { get; private set; }

        /// <summary>
        /// 付随コマンドを取得する。
        /// </summary>
        public ICommand Command { get; private set; }

        /// <summary>
        /// 付随コマンドテキストを取得する。
        /// </summary>
        public string CommandText { get; private set; }

        /// <summary>
        /// オプショナルなサブ状態種別を取得する。
        /// </summary>
        public AppStatusType SubStatusType { get; private set; }

        /// <summary>
        /// オプショナルなサブ状態テキストを取得する。
        /// </summary>
        public string SubStatusText { get; private set; }
    }
}
