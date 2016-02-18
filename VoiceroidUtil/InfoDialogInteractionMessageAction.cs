using System;
using System.Windows;
using Livet.Behaviors.Messaging;
using Livet.Messaging;

namespace VoiceroidUtil
{
    /// <summary>
    /// 情報メッセージを表示する InteractionMessageAction クラス。
    /// </summary>
    public class InfoDialogInteractionMessageAction
        : InteractionMessageAction<FrameworkElement>
    {
        protected override void InvokeAction(InteractionMessage message)
        {
            var m = message as InformationMessage;
            if (m == null)
            {
                return;
            }

            var icon = Util.MessageBox.Icon.None;
            switch (m.Image)
            {
            case MessageBoxImage.Information:
                icon = Util.MessageBox.Icon.Information;
                break;
            case MessageBoxImage.Warning:
                icon = Util.MessageBox.Icon.Warning;
                break;
            case MessageBoxImage.Error:
                icon = Util.MessageBox.Icon.Error;
                break;
            }

            Util.MessageBox.Show(
                this.AssociatedObject as Window,
                m.Text,
                m.Caption,
                Util.MessageBox.Button.Ok,
                icon);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new InfoDialogInteractionMessageAction();
        }
    }
}
