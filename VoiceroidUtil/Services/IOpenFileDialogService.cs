using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// ファイルまたはディレクトリの選択ダイアログ処理を提供するインタフェース。
    /// </summary>
    public interface IOpenFileDialogService
    {
        /// <summary>
        /// ファイルまたはディレクトリの選択ダイアログ処理を行う。
        /// </summary>
        /// <param name="title">ダイアログタイトル。</param>
        /// <param name="initialDirectory">初期ディレクトリパス。</param>
        /// <param name="filters">拡張子フィルターリスト。</param>
        /// <param name="folderPicker">
        /// ディレクトリ選択ダイアログとするならば true 。
        /// </param>
        /// <returns>選択したファイルのフルパス。選択されなかった場合は null 。</returns>
        Task<string> Run(
            string title = null,
            string initialDirectory = null,
            IEnumerable<CommonFileDialogFilter> filters = null,
            bool folderPicker = false);
    }
}
