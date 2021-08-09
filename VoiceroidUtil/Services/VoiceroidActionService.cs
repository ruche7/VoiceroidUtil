using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using RucheHome.Util;
using RucheHome.Voiceroid;
using RucheHome.Windows.WinApi;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// VOICEROIDプロセスに対するアクションを提供するクラス。
    /// </summary>
    public class VoiceroidActionService : IVoiceroidActionService
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="mainWindow">VoiceroidUtilのメインウィンドウ。</param>
        public VoiceroidActionService(Window mainWindow)
        {
            ValidateArgumentNull(mainWindow, nameof(mainWindow));

            this.MainWindow = mainWindow;
        }

        /// <summary>
        /// VoiceroidUtilのメインウィンドウを取得する。
        /// </summary>
        private Window MainWindow { get; }

        /// <summary>
        /// Forward アクション処理を行う。
        /// </summary>
        /// <param name="processWindow">VOICEROIDプロセスのメインウィンドウ。</param>
        private void DoForwardAction(Win32Window processWindow)
        {
            // VoiceroidUtilのメインウィンドウ作成
            var mainWinHandle = new WindowInteropHelper(this.MainWindow).Handle;
            if (mainWinHandle == IntPtr.Zero)
            {
                return;
            }
            var mainWin = new Win32Window(mainWinHandle);

            // 最小化されていたら元に戻す
            if (processWindow.State == WindowState.Minimized)
            {
                processWindow.Restore();
            }

            // VoiceroidUtilが最前面表示されているか？
            if (mainWin.IsTopmost)
            {
                // アクティブにして非最前面ウィンドウ内の先頭へ
                try
                {
                    processWindow.Activate();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }
            }
            else
            {
                // VoiceroidUtilのすぐ後ろにZオーダーを設定
                try
                {
                    processWindow.MoveZOrderAfter(mainWin);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }
            }
        }

        /// <summary>
        /// StopFlash アクション処理を行う。
        /// </summary>
        /// <param name="processWindow">VOICEROIDプロセスのメインウィンドウ。</param>
        private void DoStopFlashAction(Win32Window processWindow) =>
            processWindow.FlashTray(false);

        #region IVoiceroidActionService の実装

        public async Task Run(IProcess process, VoiceroidAction action)
        {
            ValidateArgumentNull(process, nameof(process));

            if (
                process.MainWindowHandle == IntPtr.Zero ||
                action == VoiceroidAction.None)
            {
                return;
            }

            // VOICEROIDのメインウィンドウ作成
            var processWin = new Win32Window(process.MainWindowHandle);

            // アクション別メソッド作成
            Action method = null;
            switch (action)
            {
            case VoiceroidAction.Forward:
                method = () => this.DoForwardAction(processWin);
                break;

            case VoiceroidAction.StopFlash:
                method = () => this.DoStopFlashAction(processWin);
                break;

            default:
                return;
            }

            // メソッド実施
            await this.MainWindow.Dispatcher.InvokeAsync(method);
        }

        #endregion
    }
}
