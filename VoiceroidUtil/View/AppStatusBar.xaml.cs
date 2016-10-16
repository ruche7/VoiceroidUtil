using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows.Controls;
using VoiceroidUtil.ViewModel;

namespace VoiceroidUtil.View
{
    /// <summary>
    /// AppStatus 情報表示ステータスバーを保持するユーザコントロールクラス。
    /// </summary>
    public partial class AppStatusBar : UserControl
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppStatusBar()
        {
            this.InitializeComponent();
        }
    }
}
