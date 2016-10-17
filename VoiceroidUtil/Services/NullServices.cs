using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using RucheHome.Voiceroid;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// 何も行わないサービス群を提供する静的クラス。
    /// </summary>
    public static class NullServices
    {
        /// <summary>
        /// IOpenFileDialogService インタフェースの何も行わない実装を取得する。
        /// </summary>
        public static IOpenFileDialogService OpenFileDialog => Impl;

        /// <summary>
        /// IVoiceroidActionService インタフェースの何も行わない実装を取得する。
        /// </summary>
        public static IVoiceroidActionService VoiceroidAction => Impl;

        /// <summary>
        /// IWindowActivateService インタフェースの何も行わない実装を取得する。
        /// </summary>
        public static IWindowActivateService WindowActivate => Impl;

        /// <summary>
        /// 各サービスインタフェースの何も行わない実装を提供するクラス。
        /// </summary>
        private class ServiceImpl
            :
            IOpenFileDialogService,
            IVoiceroidActionService,
            IWindowActivateService
        {
            Task<string> IOpenFileDialogService.Run(
                string title,
                string initialDirectory,
                IEnumerable<CommonFileDialogFilter> filters,
                bool folderPicker)
                =>
                Task.FromResult<string>(null);

            Task IVoiceroidActionService.Run(IProcess process, VoiceroidAction action) =>
                Task.FromResult(0);

            Task IWindowActivateService.Run() =>
                Task.FromResult(0);
        }

        /// <summary>
        /// 各サービスインタフェースの何も行わない実装を取得する。
        /// </summary>
        private static ServiceImpl Impl { get; } = new ServiceImpl();
    }
}
