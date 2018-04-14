using System;
using System.Threading.Tasks;
using System.Windows;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// ウィンドウをアクティブにする処理を提供するクラス。
    /// </summary>
    public class WindowActivateService : IWindowActivateService
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="window">対象ウィンドウ。</param>
        public WindowActivateService(Window window)
        {
            ValidateArgumentNull(window, nameof(window));

            this.Window = window;
        }

        /// <summary>
        /// 対象ウィンドウを取得する。
        /// </summary>
        private Window Window { get; }

        #region IWindowActivateService の実装

        public async Task Run() =>
            await this.Window.Dispatcher.InvokeAsync(this.Window.Activate);

        #endregion
    }
}
