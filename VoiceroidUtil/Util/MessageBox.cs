using System;
using System.Windows;
using System.Windows.Interop;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace VoiceroidUtil.Util
{
    /// <summary>
    /// メッセージボックスの表示処理を提供する静的クラス。
    /// </summary>
    public static class MessageBox
    {
        /// <summary>
        /// ボタン種別列挙。
        /// </summary>
        public enum Button
        {
            Ok,
            OkCancel,
        }

        /// <summary>
        /// Button 値を TaskDialogStandardButtons 値に変換する。
        /// </summary>
        /// <param name="self">Button 値。</param>
        /// <returns>TaskDialogStandardButtons 値。</returns>
        private static TaskDialogStandardButtons ToTaskDialogStandardButtons(
            this Button self)
        {
            switch (self)
            {
            case Button.OkCancel:
                return (
                    TaskDialogStandardButtons.Ok |
                    TaskDialogStandardButtons.Cancel);
            }
            return TaskDialogStandardButtons.Ok;
        }

        /// <summary>
        /// Button 値を MessageBoxButton 値に変換する。
        /// </summary>
        /// <param name="self">Button 値。</param>
        /// <returns>MessageBoxButton 値。</returns>
        private static MessageBoxButton ToMessageBoxButton(this Button self)
        {
            switch (self)
            {
            case Button.OkCancel: return MessageBoxButton.OKCancel;
            }
            return MessageBoxButton.OK;
        }

        /// <summary>
        /// アイコン種別列挙。
        /// </summary>
        public enum Icon
        {
            None,
            Information,
            Warning,
            Error,
        }

        /// <summary>
        /// Icon 値を TaskDialogStandardIcon 値に変換する。
        /// </summary>
        /// <param name="self">Icon 値。</param>
        /// <returns>TaskDialogStandardIcon 値。</returns>
        private static TaskDialogStandardIcon ToTaskDialogStandardIcon(this Icon self)
        {
            switch (self)
            {
            case Icon.Information: return TaskDialogStandardIcon.Information;
            case Icon.Warning: return TaskDialogStandardIcon.Warning;
            case Icon.Error: return TaskDialogStandardIcon.Error;
            }
            return TaskDialogStandardIcon.None;
        }

        /// <summary>
        /// Icon 値を MessageBoxImage 値に変換する。
        /// </summary>
        /// <param name="self">Icon 値。</param>
        /// <returns>MessageBoxImage 値。</returns>
        private static MessageBoxImage ToMessageBoxImage(this Icon self)
        {
            switch (self)
            {
            case Icon.Information: return MessageBoxImage.Information;
            case Icon.Warning: return MessageBoxImage.Warning;
            case Icon.Error: return MessageBoxImage.Error;
            }
            return MessageBoxImage.None;
        }

        /// <summary>
        /// 結果種別列挙。
        /// </summary>
        public enum Result
        {
            None,
            Ok,
            Cancel,
        }

        /// <summary>
        /// TaskDialogResult 値を Result 値に変換する。
        /// </summary>
        /// <param name="src">TaskDialogResult 値。</param>
        /// <returns>Result 値。</returns>
        private static Result Convert(TaskDialogResult src)
        {
            switch (src)
            {
            case TaskDialogResult.Ok: return Result.Ok;
            case TaskDialogResult.Cancel: return Result.Cancel;
            }
            return Result.None;
        }

        /// <summary>
        /// MessageBoxResult 値を Result 値に変換する。
        /// </summary>
        /// <param name="src">MessageBoxResult 値。</param>
        /// <returns>Result 値。</returns>
        private static Result Convert(MessageBoxResult src)
        {
            switch (src)
            {
            case MessageBoxResult.OK: return Result.Ok;
            case MessageBoxResult.Cancel: return Result.Cancel;
            }
            return Result.None;
        }

        /// <summary>
        /// メッセージボックスを表示する。
        /// </summary>
        /// <param name="text">表示テキスト。</param>
        /// <param name="caption">キャプション。</param>
        /// <param name="button">表示ボタン。</param>
        /// <param name="icon">表示アイコン。</param>
        /// <returns>表示結果値。</returns>
        public static Result Show(
            string text,
            string caption = "",
            Button button = Button.Ok,
            Icon icon = Icon.None)
        {
            return Show(null, text, caption, button, icon);
        }

        /// <summary>
        /// メッセージボックスを表示する。
        /// </summary>
        /// <param name="owner">オーナーウィンドウ。</param>
        /// <param name="text">表示テキスト。</param>
        /// <param name="caption">キャプション。</param>
        /// <param name="button">表示ボタン。</param>
        /// <param name="icon">表示アイコン。</param>
        /// <returns>表示結果値。</returns>
        public static Result Show(
            Window owner,
            string text,
            string caption = "",
            Button button = Button.Ok,
            Icon icon = Icon.None)
        {
            Result result = Result.None;

            if (TaskDialog.IsPlatformSupported)
            {
                using (var dialog = new TaskDialog())
                {
                    if (owner != null)
                    {
                        dialog.OwnerWindowHandle =
                            (new WindowInteropHelper(owner)).Handle;
                    }

                    dialog.Text = text;
                    dialog.Caption = caption;
                    dialog.Icon = icon.ToTaskDialogStandardIcon();
                    dialog.StandardButtons = button.ToTaskDialogStandardButtons();
                    dialog.StartupLocation = TaskDialogStartupLocation.CenterOwner;
                    dialog.Opened += OnTaskDialogOpened;

                    result = Convert(dialog.Show());
                }
            }
            else
            {
                result =
                    Convert(
                        System.Windows.MessageBox.Show(
                            owner,
                            text,
                            caption,
                            button.ToMessageBoxButton(),
                            icon.ToMessageBoxImage()));
            }

            return result;
        }

        /// <summary>
        /// TaskDialog が開いた時の処理を行う。
        /// </summary>
        private static void OnTaskDialogOpened(object sender, EventArgs e)
        {
            // アイコンが表示されないバグに対処
            var dialog = sender as TaskDialog;
            if (dialog != null)
            {
                dialog.Icon = dialog.Icon;
                dialog.InstructionText = dialog.InstructionText;
            }
        }
    }
}
