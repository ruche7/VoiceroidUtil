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

            var icon = ruche.util.MessageBox.Icon.None;
            switch (m.Image)
            {
            case MessageBoxImage.Information:
                icon = ruche.util.MessageBox.Icon.Information;
                break;
            case MessageBoxImage.Warning:
                icon = ruche.util.MessageBox.Icon.Warning;
                break;
            case MessageBoxImage.Error:
                icon = ruche.util.MessageBox.Icon.Error;
                break;
            }

            ruche.util.MessageBox.Show(
                this.AssociatedObject as Window,
                m.Text,
                m.Caption,
                ruche.util.MessageBox.Button.Ok,
                icon);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new InfoDialogInteractionMessageAction();
        }
    }
}
