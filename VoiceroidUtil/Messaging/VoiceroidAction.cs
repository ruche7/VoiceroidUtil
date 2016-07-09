using System;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// VOICEROIDプロセスに対するアクションを表す列挙。
    /// </summary>
    public enum VoiceroidAction
    {
        /// <summary>
        /// 何もしない。
        /// </summary>
        None,

        /// <summary>
        /// 前面に出す。
        /// </summary>
        /// <remarks>
        /// ZオーダーをVoiceroidUtilのメインウィンドウの次に設定する。
        /// ただし最前面表示状態にすることはない。
        /// </remarks>
        Forward,

        /// <summary>
        /// タスクバーボタンの点滅を止める。
        /// </summary>
        StopFlash,
    }
}
