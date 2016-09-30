using System;
using System.Diagnostics;
using System.Linq;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 拡張編集オブジェクトクラス。
    /// </summary>
    public class ExEditObject
    {
        #region 静的定義群

        #region アイテム名定数群

        /// <summary>
        /// 表示領域の幅を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfWidth = @"width";

        /// <summary>
        /// 表示領域の高さを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfHeight = @"height";

        /// <summary>
        /// フレームレートのベース値を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfFpsBase = @"rate";

        /// <summary>
        /// フレームレートのスケール値を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfFpsScale = @"scale";

        /// <summary>
        /// 全体フレーム長を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfLength = @"length";

        /// <summary>
        /// 音声のサンプリングレートを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfAudioSampleRate = @"audio_rate";

        /// <summary>
        /// 音声のチャンネル数を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfAudioChannelCount = @"audio_ch";

        #endregion

        /// <summary>
        /// ファイルセクション名。
        /// </summary>
        public static readonly string SectionNameOfFile = @"exedit";

        /// <summary>
        /// 拡張編集オブジェクトファイルのセクションコレクションから
        /// 拡張編集オブジェクトを作成する。
        /// </summary>
        /// <param name="sections">セクションコレクション。</param>
        /// <returns>拡張編集オブジェクト。</returns>
        public static ExEditObject FromExoFileSections(
            IniFileSectionCollection sections)
        {
            if (sections == null)
            {
                throw new ArgumentNullException(nameof(sections));
            }

            var result = new ExEditObject();

            // [exedit] セクションのプロパティ値設定
            FromSectionsToFileProperties(sections, result);

            // レイヤーアイテムのベースセクション群([0] ～ [N])取得
            var itemBaseSections = GetLayerItemBaseSections(sections);

            // レイヤーアイテム群作成
            var items =
                itemBaseSections
                    .AsParallel()
                    .AsOrdered()
                    .Select(s => FromSectionsToLayerItem(sections, s));

            // レイヤーアイテム群設定
            foreach (var item in items)
            {
                result.LayerItems.Add(item);
            }

            return result;
        }

        /// <summary>
        /// セクションコレクションからファイルセクションを取得、変換し、
        /// 拡張編集オブジェクトのプロパティ値を設定する。
        /// </summary>
        /// <param name="sections">セクションコレクション。</param>
        /// <param name="target">設定先の拡張編集オブジェクト。</param>
        private static void FromSectionsToFileProperties(
            IniFileSectionCollection sections,
            ExEditObject target)
        {
            Debug.Assert(sections != null);
            Debug.Assert(target != null);

            // ファイルセクション取得
            var section = sections.FirstOrDefault(s => s.Name == SectionNameOfFile);
            if (section == null)
            {
                throw new FormatException(
                    @"The [" + SectionNameOfFile + @"] section is not found.");
            }

            // プロパティ値設定
            ExoFileItemsConverter.ToProperties(section.Items, ref target);
        }

        /// <summary>
        /// レイヤーアイテムのベースセクション配列を取得する。
        /// </summary>
        /// <param name="sections">セクションコレクション。</param>
        /// <returns>ベースセクション配列。</returns>
        private static IniFileSection[] GetLayerItemBaseSections(
            IniFileSectionCollection sections)
        {
            Debug.Assert(sections != null);

            return
                Enumerable
                    .Range(0, int.MaxValue)
                    .Select(i => sections.FirstOrDefault(s => s.Name == i.ToString()))
                    .TakeWhile(s => s != null)
                    .ToArray();
        }

        /// <summary>
        /// セクションコレクションからレイヤーアイテムを作成する。
        /// </summary>
        /// <param name="sections">セクションコレクション。</param>
        /// <param name="baseSection">ベースセクション。</param>
        /// <returns>レイヤーアイテム。</returns>
        private static LayerItem FromSectionsToLayerItem(
            IniFileSectionCollection sections,
            IniFileSection baseSection)
        {
            Debug.Assert(sections != null);
            Debug.Assert(baseSection != null);

            var result = new LayerItem();

            // ベースセクションのプロパティ値設定
            ExoFileItemsConverter.ToProperties(baseSection.Items, ref result);

            // コンポーネント群追加
            var componentSections =
                Enumerable
                    .Range(0, int.MaxValue)
                    .Select(
                        i =>
                            sections.FirstOrDefault(
                                s => s.Name == baseSection.Name + @"." + i))
                    .TakeWhile(s => s != null);
            foreach (var cs in componentSections)
            {
                result.Components.Add(ComponentMaker.FromExoFileItems(cs.Items));
            }

            return result;
        }

        /// <summary>
        /// レイヤーアイテムからセクション群を作成して追加する。
        /// </summary>
        /// <param name="layerItem">レイヤーアイテム。</param>
        /// <param name="index">レイヤーアイテムインデックス。</param>
        /// <param name="target">追加先のセクションコレクション。</param>
        private static void FromLayerItemToSections(
            LayerItem layerItem,
            int index,
            IniFileSectionCollection target)
        {
            Debug.Assert(layerItem != null);
            Debug.Assert(index >= 0);
            Debug.Assert(target != null);

            // ベースセクションアイテム群取得
            var items = ExoFileItemsConverter.ToItems(layerItem);

            // ベースセクションアイテム群を整理
            if (layerItem.GroupId <= 0)
            {
                items.Remove(LayerItem.ExoFileItemNameOfGroupId);
            }
            if (layerItem.IsAudio)
            {
                items.Remove(LayerItem.ExoFileItemNameOfIsClipping);
                items.Remove(LayerItem.ExoFileItemNameOfIsCameraTarget);
            }
            else
            {
                items.Remove(LayerItem.ExoFileItemNameOfIsAudio);
                if (!layerItem.IsClipping)
                {
                    items.Remove(LayerItem.ExoFileItemNameOfIsClipping);
                }
            }
            if (layerItem.ChainIndex < 0)
            {
                items.Remove(LayerItem.ExoFileItemNameOfChainIndex);
            }

            // ベースセクション追加
            var baseName = index.ToString();
            target.Add(baseName, items);

            // コンポーネントセクション群追加
            baseName += '.';
            foreach (var v in layerItem.Components.Select((c, i) => new { c, i }))
            {
                target.Add(baseName + v.i, v.c.ToExoFileItems());
            }
        }

        #endregion

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ExEditObject()
        {
        }

        /// <summary>
        /// 表示領域の幅を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfWidth, Order = 0)]
        public int Width { get; set; } = 0;

        /// <summary>
        /// 表示領域の高さを取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfHeight, Order = 1)]
        public int Height { get; set; } = 0;

        /// <summary>
        /// フレームレートのベース値を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfFpsBase, Order = 2)]
        public int FpsBase { get; set; } = 30;

        /// <summary>
        /// フレームレートのスケール値を取得または設定する。
        /// </summary>
        /// <remarks>
        /// FpsBase の値をこの値で割った結果の実数値が実際のフレームレートとなる。
        /// </remarks>
        [ExoFileItem(ExoFileItemNameOfFpsScale, Order = 3)]
        public int FpsScale { get; set; } = 1;

        /// <summary>
        /// 全体フレーム長を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfLength, Order = 4)]
        public int Length { get; set; } = 0;

        /// <summary>
        /// 音声のサンプリングレートを取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfAudioSampleRate, Order = 5)]
        public int AudioSampleRate { get; set; } = 48000;

        /// <summary>
        /// 音声のチャンネル数を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfAudioChannelCount, Order = 6)]
        public int AudioChannelCount { get; set; } = 2;

        /// <summary>
        /// レイヤーアイテムコレクションを取得する。
        /// </summary>
        public LayerItemCollection LayerItems { get; } = new LayerItemCollection();

        /// <summary>
        /// このオブジェクトを拡張編集オブジェクトファイルのセクション形式に変換する。
        /// </summary>
        /// <returns>セクションコレクションデータ。</returns>
        public IniFileSectionCollection ToExoFileSections()
        {
            var sections = new IniFileSectionCollection();

            // ファイルセクション追加
            sections.Add(
                SectionNameOfFile,
                ExoFileItemsConverter.ToItems(this));

            // レイヤーアイテムセクション群追加
            foreach (var v in this.LayerItems.Select((item, i) => new { item, i }))
            {
                FromLayerItemToSections(v.item, v.i, sections);
            }

            return sections;
        }
    }
}
