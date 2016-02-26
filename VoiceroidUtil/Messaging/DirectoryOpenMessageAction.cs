using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Livet.Behaviors.Messaging;
using Livet.Messaging;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// DirectoryOpenMessage を受け取って処理するアクションクラス。
    /// </summary>
    public class DirectoryOpenMessageAction : InteractionMessageAction<FrameworkElement>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public DirectoryOpenMessageAction()
        {
        }

        #region InteractionMessageAction<FrameworkElement> のオーバライド

        protected override void InvokeAction(InteractionMessage message)
        {
            var m = message as DirectoryOpenMessage;
            if (m == null)
            {
                return;
            }

            m.Response = null;

            if (string.IsNullOrWhiteSpace(m.Path))
            {
                return;
            }
            if (!Directory.Exists(m.Path))
            {
                m.Response =
                    new AppStatus
                    {
                        StatusType = AppStatusType.Warning,
                        StatusText = @"保存先フォルダーが見つかりませんでした。",
                    };
                return;
            }

            try
            {
                Process.Start(m.Path);
            }
            catch
            {
                m.Response =
                    new AppStatus
                    {
                        StatusType = AppStatusType.Fail,
                        StatusText = @"保存先フォルダーを開けませんでした。",
                    };
                return;
            }

            m.Response = new AppStatus();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new DirectoryOpenMessageAction();
        }

        #endregion
    }
}
