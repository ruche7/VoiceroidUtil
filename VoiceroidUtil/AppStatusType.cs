using System;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリ状態種別列挙。
    /// </summary>
    public enum AppStatusType
    {
        /// <summary>
        /// 特に状態なし。
        /// </summary>
        None,

        /// <summary>
        /// 情報通知状態。
        /// </summary>
        Information,

        /// <summary>
        /// 警告のある状態。
        /// </summary>
        Warning,

        /// <summary>
        /// 処理に失敗した状態。
        /// </summary>
        Fail,

        /// <summary>
        /// 処理に成功した状態。
        /// </summary>
        Success,
    }
}
