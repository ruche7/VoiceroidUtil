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
    /// OpenFileDialogMessage を受け取って処理するアクションクラス。
    /// </summary>
    public class OpenFileDialogMessageAction : InteractionMessageAction<FrameworkElement>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public OpenFileDialogMessageAction()
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
                // 存在するパスが見つかるまで親ディレクトリを辿る
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
            var m = message as OpenFileDialogMessage;
            if (m == null)
            {
                return;
            }

            m.Response = null;

            if (!CommonOpenFileDialog.IsPlatformSupported)
            {
                return;
            }

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = m.IsFolderPicker;
                dialog.Title = m.Title;
                dialog.InitialDirectory = MakeInitialDirectoryPath(m.InitialDirectory);
                m.Filters?.ForEach(f => dialog.Filters.Add(f));
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

                m.Response = Path.GetFullPath(dialog.FileName);
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new OpenFileDialogMessageAction();
        }

        #endregion
    }
}
