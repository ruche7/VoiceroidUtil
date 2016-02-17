using System;

namespace ruche.windows
{
    /// <summary>
    /// Win32Window の表示状態を表す列挙。
    /// </summary>
    public enum Win32WindowState
    {
        /// <summary>
        /// 通常表示。
        /// </summary>
        Normal,

        /// <summary>
        /// 最大化表示。
        /// </summary>
        Maximized,

        /// <summary>
        /// 最小化表示。
        /// </summary>
        Minimized,
    }
}
