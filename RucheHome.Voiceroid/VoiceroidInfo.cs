using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        /// <param name="keywords">
        /// VOICEROIDを識別するためのキーワード列挙。キーワード不要ならば null 。
        /// </param>
        /// <param name="appProcessName">アプリプロセス名。</param>
        /// <param name="product">プロダクト名。</param>
        /// <param name="displayProduct">
        /// 表示プロダクト名。プロダクト名と同一ならば null を指定してよい。
        /// </param>
        internal VoiceroidInfo(
            VoiceroidId id,
            string name,
            IEnumerable<string> keywords,
            string appProcessName,
            string product,
            string displayProduct = null)
        {
            this.Id = id;
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Keywords =
                new ReadOnlyCollection<string>((keywords ?? new string[0]).ToList());
            this.AppProcessName =
                appProcessName ?? throw new ArgumentNullException(nameof(appProcessName));
            this.Product = product ?? throw new ArgumentNullException(nameof(product));
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
        /// VOICEROIDを識別するためのキーワードコレクションを取得する。
        /// </summary>
        public ReadOnlyCollection<string> Keywords { get; }

        /// <summary>
        /// アプリプロセス名を取得する。
        /// </summary>
        public string AppProcessName { get; }

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
