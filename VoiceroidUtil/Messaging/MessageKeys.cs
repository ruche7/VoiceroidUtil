using System;
using Livet.Messaging.Windows;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// アプリで用いられるメッセージキー文字列をまとめている静的クラス。
    /// </summary>
    public static class MessageKeys
    {
        /// <summary>
        /// AppSaveDirectorySelectionMessage 用のメッセージキー文字列を取得する。
        /// </summary>
        public static string AppSaveDirectorySelectionMessageKey { get; } =
            AppSaveDirectorySelectionMessage.DefaultMessageKey;

        /// <summary>
        /// DirectoryOpenMessage 用のメッセージキー文字列を取得する。
        /// </summary>
        public static string DirectoryOpenMessageKey { get; } =
            DirectoryOpenMessage.DefaultMessageKey;

        /// <summary>
        /// VoiceroidActivateMessage 用のメッセージキー文字列を取得する。
        /// </summary>
        public static string VoiceroidActivateMessageKey { get; } =
            VoiceroidActivateMessage.DefaultMessageKey;

        /// <summary>
        /// WindowActionMessage 用のメッセージキー文字列を取得する。
        /// </summary>
        public static string WindowActionMessageKey { get; } =
            typeof(WindowActionMessage).FullName;
    }
}
