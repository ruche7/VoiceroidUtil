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
        /// <param name="name">VOICEROID名。</param>
        /// <param name="product">プロダクト名。</param>
        /// <param name="displayProduct">
        /// 表示プロダクト名。プロダクト名と同一ならば null を指定してよい。
        /// </param>
        internal VoiceroidInfo(
            VoiceroidId id,
            string name,
            string product,
            string displayProduct = null)
        {
            this.Id = id;
            this.Name = name;
            this.Product = product;
            this.DisplayProduct = displayProduct ?? product;
        }

        /// <summary>
        /// VOICEROID識別IDを取得する。
        /// </summary>
        public VoiceroidId Id { get; }

        /// <summary>
        /// VOICEROID名を取得する。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// プロダクト名を取得する。
        /// </summary>
        public string Product { get; }

        /// <summary>
        /// 表示プロダクト名を取得する。
        /// </summary>
        public string DisplayProduct { get; }
    }
}
