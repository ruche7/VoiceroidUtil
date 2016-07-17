using System;
using System.Windows;
using System.Windows.Interop;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using RucheHome.Util;
using RucheHome.Windows.WinApi;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// VoiceroidActionMessage を受け取って処理するアクションクラス。
    /// </summary>
    public class VoiceroidActionMessageAction : InteractionMessageAction<FrameworkElement>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidActionMessageAction()
        {
        }

        /// <summary>
        /// Forward アクション処理を行う。
        /// </summary>
        /// <param name="processWindow">VOICEROIDプロセスのメインウィンドウ。</param>
        private void DoForwardAction(Win32Window processWindow)
        {
            // VoiceroidUtilのメインウィンドウ作成
            var mainWinHandle =
                (HwndSource.FromVisual(this.AssociatedObject) as HwndSource)?.Handle;
            if (!mainWinHandle.HasValue)
            {
                return;
            }
            var mainWin = new Win32Window(mainWinHandle.Value);

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
        private void DoStopFlashAction(Win32Window processWindow)
        {
            processWindow.FlashTray(false);
        }

        #region InteractionMessageAction<FrameworkElement> のオーバライド

        protected override void InvokeAction(InteractionMessage message)
        {
            var m = message as VoiceroidActionMessage;
            if (
                m?.Process == null ||
                m.Process.MainWindowHandle == IntPtr.Zero ||
                m.Action == VoiceroidAction.None)
            {
                return;
            }

            // VOICEROIDのメインウィンドウ作成
            var processWin = new Win32Window(m.Process.MainWindowHandle);

            // アクション別処理
            switch (m.Action)
            {
            case VoiceroidAction.Forward:
                this.DoForwardAction(processWin);
                break;

            case VoiceroidAction.StopFlash:
                this.DoStopFlashAction(processWin);
                break;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new VoiceroidActionMessageAction();
        }

        #endregion
    }
}
