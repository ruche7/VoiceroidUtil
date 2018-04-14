using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using static RucheHome.Util.ArgumentValidater;

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
        /// <param name="window">親ウィンドウ。</param>
        public OpenFileDialogService(Window window)
        {
            ValidateArgumentNull(window, nameof(window));

            this.Window = window;
            this.UIDispatcher = window.Dispatcher;
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="uiDispatcher">UIスレッド実行のための Dispatcher 。</param>
        public OpenFileDialogService(Dispatcher uiDispatcher)
        {
            ValidateArgumentNull(uiDispatcher, nameof(uiDispatcher));

            this.Window = null;
            this.UIDispatcher = uiDispatcher;
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

        /// <summary>
        /// UIスレッド実行のための Dispatcher を取得する。
        /// </summary>
        private Dispatcher UIDispatcher { get; }

        /// <summary>
        /// IOpenFileDialogService.Run メソッドの実処理を行う。
        /// </summary>
        private string RunImpl(
            string title = null,
            string initialDirectory = null,
            List<CommonFileDialogFilter> filters = null,
            bool folderPicker = false)
        {
            string filePath = null;

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = folderPicker;
                dialog.Title = title;
                dialog.InitialDirectory = MakeInitialDirectoryPath(initialDirectory);
                filters?.ForEach(f => dialog.Filters.Add(f));
                dialog.EnsureValidNames = true;
                dialog.EnsurePathExists = false;
                dialog.EnsureFileExists = false;
                dialog.Multiselect = false;

                var handle =
                    (this.Window == null) ?
                        null : (HwndSource.FromVisual(this.Window) as HwndSource)?.Handle;
                var result =
                    handle.HasValue ? dialog.ShowDialog(handle.Value) : dialog.ShowDialog();
                if (result != CommonFileDialogResult.Ok)
                {
                    return null;
                }

                try
                {
                    filePath = Path.GetFullPath(dialog.FileName);
                }
                catch
                {
                    filePath = dialog.FileName;
                }
            }

            return filePath;
        }

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

            // コピーしておく
            var filtersClone = filters?.ToList();

            return
                await this.UIDispatcher.InvokeAsync(
                    () => this.RunImpl(title, initialDirectory, filtersClone, folderPicker));
        }

        #endregion
    }
}
