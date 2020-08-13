using System;
using System.Collections.Generic;
using System.Linq;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// レイヤーアイテムを表すクラス。
    /// </summary>
    public class LayerItem
    {
        #region アイテム名定数群

        /// <summary>
        /// 開始フレームを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfBeginFrame = @"start";

        /// <summary>
        /// 終端フレームを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfEndFrame = @"end";

        /// <summary>
        /// レイヤーIDを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfLayerId = @"layer";

        /// <summary>
        /// グループIDを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfGroupId = @"group";

        /// <summary>
        /// オーバレイフラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsOverlay = @"overlay";

        /// <summary>
        /// オーディオフラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsAudio = @"audio";

        /// <summary>
        /// クリッピングフラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsClipping = @"clipping";

        /// <summary>
        /// カメラターゲットフラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsCameraTarget = @"camera";

        /// <summary>
        /// 先行アイテムインデックスを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfChainIndex = @"chain";

        #endregion

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public LayerItem()
        {
        }

        /// <summary>
        /// 開始フレーム位置を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfBeginFrame, Order = 0)]
        public int BeginFrame { get; set; } = 0;

        /// <summary>
        /// 終端フレーム位置を取得または設定する。
        /// </summary>
        /// <remarks>
        /// このフレーム自体も範囲に含む。
        /// </remarks>
        [ExoFileItem(ExoFileItemNameOfEndFrame, Order = 1)]
        public int EndFrame { get; set; } = 0;

        /// <summary>
        /// レイヤーIDを取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfLayerId, Order = 2)]
        public int LayerId { get; set; } = 0;

        /// <summary>
        /// グループIDを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 0 以下ならばグループ化しない。
        /// </remarks>
        [ExoFileItem(ExoFileItemNameOfGroupId, Order = 3)]
        public int GroupId { get; set; } = 0;

        /// <summary>
        /// オーバレイフラグを取得または設定する。
        /// </summary>
        /// <remarks>
        /// どの設定値と紐付いているのか現状不明。観測範囲では必ず true 。
        /// </remarks>
        [ExoFileItem(ExoFileItemNameOfIsOverlay, Order = 4)]
        public bool IsOverlay { get; set; } = true;

        /// <summary>
        /// オーディオフラグを取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfIsAudio, Order = 5)]
        public bool IsAudio { get; set; } = false;

        /// <summary>
        /// 上のオブジェクトでクリッピングするか否かを取得する。
        /// </summary>
        /// <remarks>
        /// オーディオフラグが有効な場合は無視される。
        /// </remarks>
        [ExoFileItem(ExoFileItemNameOfIsClipping, Order = 6)]
        public bool IsClipping { get; set; } = false;

        /// <summary>
        /// カメラ制御の対象とするか否かを取得または設定する。
        /// </summary>
        /// <remarks>
        /// オーディオフラグが有効な場合は無視される。
        /// </remarks>
        [ExoFileItem(ExoFileItemNameOfIsCameraTarget, Order = 7)]
        public bool IsCameraTarget { get; set; } = false;

        /// <summary>
        /// 中間点を挟んで先行するアイテムのインデックスを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 負数ならば先行アイテムなし。
        /// </remarks>
        [ExoFileItem(ExoFileItemNameOfChainIndex, Order = 8)]
        public int ChainIndex { get; set; } = -1;

        /// <summary>
        /// コンポーネントコレクションを取得する。
        /// </summary>
        public ComponentCollection Components { get; } = new ComponentCollection();

        /// <summary>
        /// 指定した型のコンポーネントを取得する。
        /// </summary>
        /// <typeparam name="T">コンポーネント型。</typeparam>
        /// <returns>コンポーネント。見つからない場合は default(T) 。</returns>
        public T GetComponent<T>()
            where T : IComponent
            =>
            this.GetComponents<T>().FirstOrDefault();

        /// <summary>
        /// 指定した型のコンポーネント列挙を取得する。
        /// </summary>
        /// <typeparam name="T">コンポーネント型。</typeparam>
        /// <returns>コンポーネント列挙。見つからない場合は空の列挙。</returns>
        public IEnumerable<T> GetComponents<T>()
            where T : IComponent
            =>
            this.Components.OfType<T>();
    }
}
