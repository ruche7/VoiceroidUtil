using System;
using System.Windows.Input;

namespace VoiceroidUtil.Design
{
    /// <summary>
    /// デザイン確認用の IAppStatus 実装クラス。
    /// </summary>
    internal class AppStatusForDesign : IAppStatus
    {
        /// <summary>
        /// デザイン確認用のダミーコマンドを取得する。
        /// </summary>
        public static ICommand DummyCommand { get; } = new RoutedCommand();

        /// <summary>
        /// 状態種別を取得または設定する。
        /// </summary>
        public AppStatusType StatusType { get; set; } = AppStatusType.None;

        /// <summary>
        /// 状態テキストを取得または設定する。
        /// </summary>
        public string StatusText { get; set; } = "";

        /// <summary>
        /// 付随コマンドを取得または設定する。
        /// </summary>
        public ICommand Command { get; set; } = null;

        /// <summary>
        /// 付随コマンドテキストを取得または設定する。
        /// </summary>
        public string CommandText { get; set; } = "";

        /// <summary>
        /// オプショナルなサブ状態種別を取得または設定する。
        /// </summary>
        public AppStatusType SubStatusType { get; set; } = AppStatusType.None;

        /// <summary>
        /// オプショナルなサブ状態テキストを取得または設定する。
        /// </summary>
        public string SubStatusText { get; set; } = "";
    }
}
