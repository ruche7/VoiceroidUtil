using System;
using System.Windows.Controls;

namespace VoiceroidUtil
{
    /// <summary>
    /// 保存設定ユーザコントロールクラス。
    /// </summary>
    public partial class SaveSettingsControl : UserControl
    {
        private bool IsAssemblyLoaded { get; }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public SaveSettingsControl()
        {
            this.IsAssemblyLoaded = (
                typeof(System.Windows.Interactivity.Interaction) != null &&
                typeof(Microsoft.Expression.Interactivity.VisualStateUtilities) != null);

            InitializeComponent();
        }
    }
}
