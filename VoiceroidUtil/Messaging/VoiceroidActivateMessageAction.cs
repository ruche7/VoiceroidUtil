using System;
using System.Windows;
using System.Windows.Interop;
using Livet.Behaviors.Messaging;
using Livet.Messaging;
using RucheHome.Windows.WinApi;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// VoiceroidActivateMessage を受け取って処理するアクションクラス。
    /// </summary>
    public class VoiceroidActivateMessageAction
        : InteractionMessageAction<FrameworkElement>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidActivateMessageAction()
        {
        }

        #region InteractionMessageAction<FrameworkElement> のオーバライド

        protected override void InvokeAction(InteractionMessage message)
        {
            var m = message as VoiceroidActivateMessage;
            if (m?.Process == null || m.Process.MainWindowHandle == IntPtr.Zero)
            {
                return;
            }

            // VoiceroidUtilのメインウィンドウ作成
            var mainWinHandle =
                (HwndSource.FromVisual(this.AssociatedObject) as HwndSource)?.Handle;
            if (!mainWinHandle.HasValue)
            {
                return;
            }
            var mainWin = new Win32Window(mainWinHandle.Value);

            // VOICEROIDのメインウィンドウ作成
            var processWin = new Win32Window(m.Process.MainWindowHandle);

            // 最小化されていたら元に戻す
            if (processWin.State == WindowState.Minimized)
            {
                processWin.Restore();
            }

            // VoiceroidUtilのすぐ後ろにZオーダーを設定
            processWin.MoveZOrderAfter(mainWin);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new VoiceroidActivateMessageAction();
        }

        #endregion
    }
}
