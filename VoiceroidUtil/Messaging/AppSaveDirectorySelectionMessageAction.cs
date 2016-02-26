using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// AppSaveDirectorySelectionMessage を受け取って処理するアクションクラス。
    /// </summary>
    public class AppSaveDirectorySelectionMessageAction
        : InteractionMessageAction<FrameworkElement>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppSaveDirectorySelectionMessageAction()
        {
        }

        /// <summary>
        /// 初期表示ディレクトリパスを作成する。
        /// </summary>
        /// <param name="path">ヒントとなるパス。</param>
        /// <returns>作成したパス。</returns>
        private static string MakeInitialDirectoryPath(string path)
        {
            try
            {
                while (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }
            }
            catch
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(path) ? null : path;
        }

        #region InteractionMessageAction<FrameworkElement> のオーバライド

        protected override void InvokeAction(InteractionMessage message)
        {
            var m = message as AppSaveDirectorySelectionMessage;
            if (m == null)
            {
                return;
            }

            m.Response = null;

            if (m.Config == null || !CommonOpenFileDialog.IsPlatformSupported)
            {
                return;
            }

            // フォルダ選択ダイアログでパスを取得
            string path;
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = @"音声保存先の選択";
                dialog.InitialDirectory =
                    MakeInitialDirectoryPath(m.Config.SaveDirectoryPath);
                dialog.EnsureValidNames = true;
                dialog.EnsurePathExists = false;
                dialog.EnsureFileExists = false;
                dialog.Multiselect = false;

                var handle =
                    (HwndSource.FromVisual(this.AssociatedObject) as HwndSource)?.Handle;
                var result =
                    handle.HasValue ?
                        dialog.ShowDialog(handle.Value) : dialog.ShowDialog();
                if (result != CommonFileDialogResult.Ok)
                {
                    return;
                }

                path = Path.GetFullPath(dialog.FileName);
            }

            // パスが正常かチェック
            m.Response = FileSaveUtil.CheckPathStatus(path);
            if (m.Response.StatusType == AppStatusType.None)
            {
                // 正常ならアプリ設定を上書き
                m.Config.SaveDirectoryPath = path;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new AppSaveDirectorySelectionMessageAction();
        }

        #endregion
    }
}
