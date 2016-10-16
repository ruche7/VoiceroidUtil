using System;
using System.Threading.Tasks;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// ウィンドウをアクティブにする処理を提供するインタフェース。
    /// </summary>
    public interface IWindowActivateService
    {
        /// <summary>
        /// ウィンドウをアクティブにする。
        /// </summary>
        Task Run();
    }
}
