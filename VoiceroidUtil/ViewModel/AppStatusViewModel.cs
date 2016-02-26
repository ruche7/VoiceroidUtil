using System;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// アプリ状態を提供する ViewModel クラス。
    /// </summary>
    public class AppStatusViewModel : Livet.ViewModel
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
            set
            {
                var old = this.Value;
                this.value = value ?? (new AppStatus());
                if (this.Value != old)
                {
                    this.RaisePropertyChanged();
                }
            }
        }
        private IAppStatus value = new AppStatus();
    }
}
