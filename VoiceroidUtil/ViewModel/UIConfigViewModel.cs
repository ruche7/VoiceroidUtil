using System;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// UI設定とそれに対する処理を提供する ViewModel クラス。
    /// </summary>
    public class UIConfigViewModel : ConfigViewModelBase<UIConfig>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public UIConfigViewModel() : base(new UIConfig())
        {
        }
    }
}
