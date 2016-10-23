using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using RucheHome.Voiceroid;

namespace VoiceroidUtil
{
    /// <summary>
    /// VoiceroidVisibility インスタンスセットクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class VoiceroidVisibilitySet : VoiceroidItemSetBase<VoiceroidVisibility>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidVisibilitySet() : base()
        {
        }

        /// <summary>
        /// アイテムセットから表示するアイテムのみ選別したコレクションを作成する。
        /// </summary>
        /// <typeparam name="TItem">アイテムの型。</typeparam>
        /// <param name="src">VOICEROID識別IDに紐付くアイテムセット。</param>
        /// <param name="selectAllIfEmpty">
        /// 1アイテムも選択されない場合に全選択と見なすならば true 。
        /// </param>
        /// <returns>表示するアイテムのみ選別したコレクション。</returns>
        public ReadOnlyCollection<TItem> SelectVisibleOf<TItem>(
            VoiceroidItemSetBase<TItem> src,
            bool selectAllIfEmpty = true)
            where TItem : IVoiceroidItem
            =>
            this.SelectVisibleOfCore(src, selectAllIfEmpty, i => i.VoiceroidId);

        /// <summary>
        /// VOICEROIDプロセス列挙から表示するプロセスのみ選別したコレクションを作成する。
        /// </summary>
        /// <param name="src">プロセス列挙。</param>
        /// <param name="selectAllIfEmpty">
        /// 1プロセスも選択されない場合に全選択と見なすならば true 。
        /// </param>
        /// <returns>表示するプロセスのみ選別したコレクション。</returns>
        public ReadOnlyCollection<IProcess> SelectVisibleOf(
            IEnumerable<IProcess> src,
            bool selectAllIfEmpty = true)
            =>
            this.SelectVisibleOfCore(src, selectAllIfEmpty, p => p.Id);

        /// <summary>
        /// 列挙から表示する要素のみ選別したコレクションを作成する。
        /// </summary>
        /// <typeparam name="T">列挙要素型。</typeparam>
        /// <param name="src">列挙。</param>
        /// <param name="selectAllIfEmpty">
        /// 1要素も選択されない場合に全選択するならば true 。
        /// </param>
        /// <param name="idSelector">
        /// 列挙要素から VoiceroidId 値を取得するデリゲート。
        /// </param>
        /// <returns>表示する要素のみ選別したコレクション。</returns>
        private ReadOnlyCollection<T> SelectVisibleOfCore<T>(
            IEnumerable<T> src,
            bool selectAllIfEmpty,
            Func<T, VoiceroidId> idSelector)
        {
            Debug.Assert(idSelector != null);

            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            var c = src.Where(i => this[idSelector(i)].IsVisible);

            return ((selectAllIfEmpty && !c.Any()) ? src : c).ToList().AsReadOnly();
        }
    }
}
