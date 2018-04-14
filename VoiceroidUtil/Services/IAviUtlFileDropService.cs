using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RucheHome.AviUtl.ExEdit.GcmzDrops;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// AviUtl拡張編集タイムラインへのファイルドロップ処理を提供するインタフェース。
    /// </summary>
    public interface IAviUtlFileDropService
    {
        /// <summary>
        /// AviUtl拡張編集タイムラインへのファイルドロップ処理を行う。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="stepFrameCount">ドロップ後に進めるフレーム数。</param>
        /// <param name="layer">レイヤー位置指定。既定位置にするならば 0 。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>処理結果。</returns>
        Task<FileDrop.Result> Run(
            string filePath,
            int stepFrameCount = 0,
            int layer = 0,
            int timeoutMilliseconds = -1);

        /// <summary>
        /// AviUtl拡張編集タイムラインへのファイルドロップ処理を行う。
        /// </summary>
        /// <param name="filePathes">ファイルパス列挙。</param>
        /// <param name="stepFrameCount">ドロップ後に進めるフレーム数。</param>
        /// <param name="layer">レイヤー位置指定。既定位置にするならば 0 。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>処理結果。</returns>
        Task<FileDrop.Result> Run(
            IEnumerable<string> filePathes,
            int stepFrameCount = 0,
            int layer = 0,
            int timeoutMilliseconds = -1);
    }
}
