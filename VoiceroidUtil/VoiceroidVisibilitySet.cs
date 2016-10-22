using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

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
        /// アイテムセットから表示するもののみ選別したコレクションを作成する。
        /// </summary>
        /// <typeparam name="TItem">アイテムの型。</typeparam>
        /// <param name="src">VOICEROID識別IDに紐付くアイテムセット。</param>
        /// <param name="selectAllIfEmpty">
        /// 1アイテムも表示されない場合に全表示と見なすならば true 。
        /// </param>
        /// <returns>表示するもののみ選別したコレクション。</returns>
        public ReadOnlyCollection<TItem> SelectVisibleItems<TItem>(
            VoiceroidItemSetBase<TItem> src,
            bool selectAllIfEmpty = false)
            where TItem : IVoiceroidItem
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            var c = src.Where(i => this[i.VoiceroidId].IsVisible);

            return ((selectAllIfEmpty && !c.Any()) ? src : c).ToList().AsReadOnly();
        }
    }
}
