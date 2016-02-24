using System;

namespace RucheHome.Voiceroid
{
    /// <summary>
    /// VOICEROID識別IDに紐付く情報を保持するクラス。
    /// </summary>
    public class VoiceroidInfo
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <param name="windowTitle">メインウィンドウタイトル。</param>
        /// <param name="name">名前。</param>
        internal VoiceroidInfo(
            VoiceroidId id,
            string windowTitle,
            string name)
        {
            this.Id = id;
            this.WindowTitle = windowTitle;
            this.Name = name;
        }

        /// <summary>
        /// VOICEROID識別IDを取得する。
        /// </summary>
        public VoiceroidId Id { get; }

        /// <summary>
        /// メインウィンドウタイトルを取得する。
        /// </summary>
        public string WindowTitle { get; }

        /// <summary>
        /// 名前を取得する。
        /// </summary>
        public string Name { get; }
    }
}
