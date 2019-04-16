using System;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// VOICEROID2用の IProcess インタフェース実装クラス。
        /// </summary>
        private class Voiceroid2Impl : Voiceroid2ImplBase
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            public Voiceroid2Impl() : base(VoiceroidId.Voiceroid2)
            {
            }

            #region ImplBase のオーバライド

            /// <summary>
            /// メインウィンドウタイトルであるか否かを取得する。
            /// </summary>
            /// <param name="title">タイトル。</param>
            /// <returns>
            /// メインウィンドウタイトルならば true 。そうでなければ false 。
            /// </returns>
            /// <remarks>
            /// スプラッシュウィンドウ等の判別用に用いる。
            /// </remarks>
            protected override bool IsMainWindowTitle(string title)
            {
                return (title?.Contains(@"VOICEROID2") == true);
            }

            #endregion
        }
    }
}
