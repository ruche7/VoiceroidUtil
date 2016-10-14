using System;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// アプリ状態を提供する ViewModel クラス。
    /// </summary>
    public class AppStatusViewModel : ViewModelBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppStatusViewModel()
        {
        }

        /// <summary>
        /// アプリ状態値を取得または設定する。
        /// </summary>
        public IAppStatus Value
        {
            get { return this.value; }
            set { this.SetProperty(ref this.value, value ?? new AppStatus()); }
        }
        private IAppStatus value = new AppStatus();
    }
}
