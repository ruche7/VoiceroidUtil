using System;
using System.Threading.Tasks;
using System.Windows;

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
            this.Window = window;
        }

        /// <summary>
        /// 対象ウィンドウを取得する。
        /// </summary>
        private Window Window { get; }

        #region IWindowActivateService の実装

        public Task Run() => Task.Run(() => this.Window.Activate());

        #endregion
    }
}
