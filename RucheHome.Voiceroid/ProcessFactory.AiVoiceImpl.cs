using System;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// A.I.VOICE用の IProcess インタフェース実装クラス。
        /// </summary>
        private class AiVoiceImpl : Voiceroid2ImplBase
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            public AiVoiceImpl() : base(VoiceroidId.AiVoice)
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
            protected override bool IsMainWindowTitle(string title) =>
                title?.Contains(@"A.I.VOICE") == true;

            #endregion
        }
    }
}
