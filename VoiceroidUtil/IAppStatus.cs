using System;
using System.Windows.Input;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリ状態を提供するインタフェース。
    /// </summary>
    public interface IAppStatus
    {
        /// <summary>
        /// 状態種別を取得する。
        /// </summary>
        AppStatusType StatusType { get; }

        /// <summary>
        /// 状態テキストを取得する。
        /// </summary>
        string StatusText { get; }

        /// <summary>
        /// 付随コマンドを取得する。
        /// </summary>
        ICommand Command { get; }

        /// <summary>
        /// 付随コマンドテキストを取得する。
        /// </summary>
        string CommandText { get; }

        /// <summary>
        /// オプショナルなサブ状態種別を取得する。
        /// </summary>
        AppStatusType SubStatusType { get; }

        /// <summary>
        /// オプショナルなサブ状態テキストを取得する。
        /// </summary>
        string SubStatusText { get; }
    }
}