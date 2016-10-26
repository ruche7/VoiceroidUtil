using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// ファイルまたはディレクトリの選択ダイアログ処理を提供するクラス。
    /// </summary>
    public class OpenFileDialogService : IOpenFileDialogService
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="window">親となるウィンドウ。</param>
        public OpenFileDialogService(Window window)
        {
            this.Window = window;
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

        /// <summary>
        /// 親ウィンドウを取得する。
        /// </summary>
        private Window Window { get; }

        #region IOpenFileDialogService の実装

        public async Task<string> Run(
            string title = null,
            string initialDirectory = null,
            IEnumerable<CommonFileDialogFilter> filters = null,
            bool folderPicker = false)
        {
            if (!CommonOpenFileDialog.IsPlatformSupported)
            {
                return null;
            }

            string filePath = null;

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = folderPicker;
                dialog.Title = title;
                dialog.InitialDirectory = MakeInitialDirectoryPath(initialDirectory);
                filters?.ToList().ForEach(f => dialog.Filters.Add(f));
                dialog.EnsureValidNames = true;
                dialog.EnsurePathExists = false;
                dialog.EnsureFileExists = false;
                dialog.Multiselect = false;

                var handle =
                    (HwndSource.FromVisual(this.Window) as HwndSource)?.Handle;
                var result =
                    await this.Window.Dispatcher.InvokeAsync(
                        () =>
                            handle.HasValue ?
                                dialog.ShowDialog(handle.Value) : dialog.ShowDialog());
                if (result != CommonFileDialogResult.Ok)
                {
                    return null;
                }

                filePath = Path.GetFullPath(dialog.FileName);
            }

            return filePath;
        }

        #endregion
    }
}
