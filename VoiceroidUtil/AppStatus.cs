using System;
using System.Windows.Input;

namespace VoiceroidUtil
{
    /// <summary>
    /// アプリ状態を提供するクラス。
    /// </summary>
    internal class AppStatus : IAppStatus
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppStatus()
        {
        }

        /// <summary>
        /// 状態種別を取得または設定する。
        /// </summary>
        public AppStatusType StatusType { get; set; } = AppStatusType.None;

        /// <summary>
        /// 状態テキストを取得または設定する。
        /// </summary>
        public string StatusText { get; set; } = "";

        /// <summary>
        /// オプショナルなサブ状態種別を取得または設定する。
        /// </summary>
        public AppStatusType SubStatusType { get; set; } = AppStatusType.None;

        /// <summary>
        /// オプショナルなサブ状態テキストを取得または設定する。
        /// </summary>
        public string SubStatusText { get; set; } = "";

        /// <summary>
        /// オプショナルなサブ状態コマンドを取得または設定する。
        /// </summary>
        public ICommand SubStatusCommand { get; set; } = null;

        /// <summary>
        /// オプショナルなサブ状態コマンドのチップテキストを取得または設定する。
        /// </summary>
        public string SubStatusCommandTip { get; set; } = null;
    }
}
