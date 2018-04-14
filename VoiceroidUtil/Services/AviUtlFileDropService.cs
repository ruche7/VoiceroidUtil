using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using RucheHome.AviUtl.ExEdit.GcmzDrops;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// AviUtl拡張編集タイムラインへのファイルドロップ処理を提供するクラス。
    /// </summary>
    public class AviUtlFileDropService : IAviUtlFileDropService
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="mainWindow">VoiceroidUtilのメインウィンドウ。</param>
        public AviUtlFileDropService(Window mainWindow)
        {
            ValidateArgumentNull(mainWindow, nameof(mainWindow));

            this.MainWindow = mainWindow;
        }

        /// <summary>
        /// VoiceroidUtilのメインウィンドウを取得する。
        /// </summary>
        private Window MainWindow { get; }

        #region IAviUtlFileDropService の実装

        public Task<FileDrop.Result> Run(
            string filePath,
            int stepFrameCount = 0,
            int layer = 0,
            int timeoutMilliseconds = -1)
        {
            return Run(new[] { filePath }, stepFrameCount, layer, timeoutMilliseconds);
        }

        public async Task<FileDrop.Result> Run(
            IEnumerable<string> filePathes,
            int stepFrameCount = 0,
            int layer = 0,
            int timeoutMilliseconds = -1)
        {
            // コピーしておく
            var filePathesClone = new List<string>(filePathes);

            return
                await this.MainWindow.Dispatcher.InvokeAsync(
                    () =>
                    {
                        // VoiceroidUtilのメインウィンドウハンドル取得
                        var mainWinHandle =
                            (HwndSource.FromVisual(this.MainWindow) as HwndSource)?.Handle;
                        if (!mainWinHandle.HasValue)
                        {
                            return FileDrop.Result.Fail;
                        }

                        return
                            FileDrop.Run(
                                mainWinHandle.Value,
                                filePathesClone,
                                stepFrameCount,
                                layer,
                                timeoutMilliseconds);
                    });
        }

        #endregion
    }
}
