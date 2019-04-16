namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// GynoidTalk用の IProcess インタフェース実装クラス。
        /// </summary>
        private sealed class GynoidTalkImpl : Voiceroid2Impl
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            public GynoidTalkImpl() : base(VoiceroidId.GynoidTalk, true)
            {
            }

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
                return (title?.Contains(@"ガイノイドTalk") == true);
            }
        }
    }
}