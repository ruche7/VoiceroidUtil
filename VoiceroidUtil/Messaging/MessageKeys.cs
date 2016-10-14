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
        /// OpenFileDialogMessage 用のメッセージキー文字列を取得する。
        /// </summary>
        public static string OpenFileDialogMessageKey { get; } =
            OpenFileDialogMessage.DefaultMessageKey;

        /// <summary>
        /// DirectoryOpenMessage 用のメッセージキー文字列を取得する。
        /// </summary>
        public static string DirectoryOpenMessageKey { get; } =
            DirectoryOpenMessage.DefaultMessageKey;

        /// <summary>
        /// VoiceroidActionMessage 用のメッセージキー文字列を取得する。
        /// </summary>
        public static string VoiceroidActionMessageKey { get; } =
            VoiceroidActionMessage.DefaultMessageKey;

        /// <summary>
        /// WindowActionMessage 用のメッセージキー文字列を取得する。
        /// </summary>
        public static string WindowActionMessageKey { get; } =
            typeof(WindowActionMessage).FullName;
    }
}
