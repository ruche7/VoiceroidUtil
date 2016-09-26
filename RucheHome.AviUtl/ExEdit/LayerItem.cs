using System;
using System.Collections.Generic;
using System.Linq;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// レイヤーアイテムを表すクラス。
    /// </summary>
    public class LayerItem
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public LayerItem()
        {
        }

        /// <summary>
        /// 開始フレーム位置を取得または設定する。
        /// </summary>
        public int BeginFrame { get; set; } = 0;

        /// <summary>
        /// 終端フレーム位置を取得または設定する。
        /// </summary>
        /// <remarks>
        /// このフレーム自体も範囲に含む。
        /// </remarks>
        public int EndFrame { get; set; } = 0;

        /// <summary>
        /// レイヤーIDを取得または設定する。
        /// </summary>
        public int LayerId { get; set; } = 0;

        /// <summary>
        /// グループIDを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 0 以下ならばグループ化しない。
        /// </remarks>
        public int GroupId { get; set; } = 0;

        /// <summary>
        /// オーバレイフラグを取得または設定する。
        /// </summary>
        /// <remarks>
        /// どの設定値と紐付いているのか現状不明。観測範囲では必ず true 。
        /// </remarks>
        public bool IsOverlay { get; set; } = true;

        /// <summary>
        /// オーディオフラグを取得または設定する。
        /// </summary>
        public bool IsAudio { get; set; } = false;

        /// <summary>
        /// 上のオブジェクトでクリッピングするか否かを取得する。
        /// </summary>
        /// <remarks>
        /// オーディオフラグが有効な場合は無視される。
        /// </remarks>
        public bool IsClipping { get; set; } = false;

        /// <summary>
        /// カメラ制御の対象とするか否かを取得または設定する。
        /// </summary>
        /// <remarks>
        /// オーディオフラグが有効な場合は無視される。
        /// </remarks>
        public bool IsCameraTarget { get; set; } = false;

        /// <summary>
        /// 中間点を挟んで先行するアイテムのインデックスを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 負数ならば先行アイテムなし。
        /// </remarks>
        public int ChainIndex { get; set; } = -1;

        /// <summary>
        /// コンポーネントリストを取得する。
        /// </summary>
        public List<IComponent> Components { get; } = new List<IComponent>();

        /// <summary>
        /// 指定した型のコンポーネントを取得する。
        /// </summary>
        /// <typeparam name="T">コンポーネント型。</typeparam>
        /// <returns>コンポーネント。見つからない場合は default(T) 。</returns>
        public T GetComponent<T>()
            where T : IComponent
        {
            return (T)this.Components.FirstOrDefault(c => c is T);
        }

        /// <summary>
        /// 指定した型のコンポーネント列挙を取得する。
        /// </summary>
        /// <typeparam name="T">コンポーネント型。</typeparam>
        /// <returns>コンポーネント列挙。見つからない場合は空の列挙。</returns>
        public IEnumerable<T> GetComponents<T>()
            where T : IComponent
        {
            return this.Components.Where(c => c is T).Cast<T>();
        }

        /// <summary>
        /// このアイテムを拡張編集オブジェクトファイルのセクション形式に変換する。
        /// </summary>
        /// <param name="index">アイテムインデックス。</param>
        /// <returns>セクションコレクションデータ。</returns>
        public IniFileSectionCollection ToExoFileSections(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(
                    "The section index is less than 0.",
                    nameof(index));
            }

            var sections = new IniFileSectionCollection();

            // ルートセクション追加＆アイテム設定
            var section = sections.Add(index.ToString());
            section.Items.Add(@"start", this.BeginFrame.ToString());
            section.Items.Add(@"end", this.EndFrame.ToString());
            section.Items.Add(@"layer", this.LayerId.ToString());
            if (this.GroupId > 0)
            {
                section.Items.Add(@"group", this.GroupId.ToString());
            }
            section.Items.Add(@"overlay", this.IsOverlay ? @"1" : @"0");
            if (this.IsAudio)
            {
                section.Items.Add(@"audio", @"1");
            }
            else
            {
                if (this.IsClipping)
                {
                    section.Items.Add(@"clipping", @"1");
                }
                if (this.IsCameraTarget)
                {
                    section.Items.Add(@"camera", @"1");
                }
            }
            if (this.ChainIndex >= 0)
            {
                section.Items.Add(@"chain", this.ChainIndex.ToString());
            }

            // コンポーネント群のセクションを追加
            foreach (var v in this.Components.Select((c, i) => new { c, i }))
            {
                sections.Add(v.c.ToExoFileSection(index + @"." + v.i));
            }

            return sections;
        }
    }
}
