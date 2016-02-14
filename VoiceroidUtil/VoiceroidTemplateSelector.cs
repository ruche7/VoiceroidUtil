using System;
using System.Windows;
using System.Windows.Controls;
using ruche.voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VOICEROIDプロセスのIDと実行状態によるテンプレートセレクタクラス。
    /// </summary>
    public class VoiceroidTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(
            object item,
            DependencyObject container)
        {
            var process = item as IProcess;
            var elem = container as FrameworkElement;

            if (process == null || elem == null)
            {
                return null;
            }

            var resName =
                process.Id.ToString() + "_" + (process.IsRunning ? "Running" : "Dead");
            return elem.FindResource(resName) as DataTemplate;
        }
    }
}
