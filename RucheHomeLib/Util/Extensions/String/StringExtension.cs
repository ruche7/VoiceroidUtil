using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static RucheHome.Util.ArgumentValidater;

namespace RucheHome.Util.Extensions.String
{
    /// <summary>
    /// String クラスおよび StringBuilder クラスに対する拡張メソッドを提供する静的クラス。
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// サロゲートペアを分断しないように部分文字列を削除する。
        /// </summary>
        /// <param name="self">対象文字列。</param>
        /// <param name="startIndex">削除開始位置。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        /// <returns>部分文字列を削除した文字列。</returns>
        public static string RemoveSurrogateSafe(
            this string self,
            int startIndex,
            bool moveAfter = false)
        {
            CorrectRangeSurrogateSafe(
                self.Length,
                self[startIndex],
                ref startIndex,
                moveAfter);

            return self.Remove(startIndex);
        }

        /// <summary>
        /// サロゲートペアを分断しないように部分文字列を削除する。
        /// </summary>
        /// <param name="self">対象文字列。</param>
        /// <param name="startIndex">削除開始位置。</param>
        /// <param name="count">削除文字数。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        /// <returns>部分文字列を削除した文字列。</returns>
        public static string RemoveSurrogateSafe(
            this string self,
            int startIndex,
            int count,
            bool moveAfter = false)
        {
            CorrectRangeSurrogateSafe(
                self.Length,
                i => self[i],
                ref startIndex,
                ref count,
                moveAfter);

            return self.Remove(startIndex, count);
        }

        /// <summary>
        /// サロゲートペアを分断しないように部分文字列を取得する。
        /// </summary>
        /// <param name="self">対象文字列。</param>
        /// <param name="startIndex">取得開始位置。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        /// <returns>取得した部分文字列。</returns>
        public static string SubstringSurrogateSafe(
            this string self,
            int startIndex,
            bool moveAfter = false)
        {
            CorrectRangeSurrogateSafe(
                self.Length,
                self[startIndex],
                ref startIndex,
                moveAfter);

            return self.Substring(startIndex);
        }

        /// <summary>
        /// サロゲートペアを分断しないように部分文字列を取得する。
        /// </summary>
        /// <param name="self">対象文字列。</param>
        /// <param name="startIndex">取得開始位置。</param>
        /// <param name="count">取得文字数。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        /// <returns>取得した部分文字列。</returns>
        public static string SubstringSurrogateSafe(
            this string self,
            int startIndex,
            int count,
            bool moveAfter = false)
        {
            CorrectRangeSurrogateSafe(
                self.Length,
                i => self[i],
                ref startIndex,
                ref count,
                moveAfter);

            return self.Substring(startIndex, count);
        }

        /// <summary>
        /// サロゲートペアを分断しないように部分文字列を削除する。
        /// </summary>
        /// <param name="self">対象文字列。</param>
        /// <param name="startIndex">削除開始位置。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        /// <returns>部分文字列を削除した文字列。</returns>
        public static StringBuilder RemoveSurrogateSafe(
            this StringBuilder self,
            int startIndex,
            bool moveAfter = false)
        {
            CorrectRangeSurrogateSafe(
                self.Length,
                self[startIndex],
                ref startIndex,
                moveAfter);

            var length =
                (self == null || startIndex < 0 || startIndex > self.Length) ?
                    0 : (self.Length - startIndex);

            return self.Remove(startIndex, length);
        }

        /// <summary>
        /// サロゲートペアを分断しないように部分文字列を削除する。
        /// </summary>
        /// <param name="self">対象文字列。</param>
        /// <param name="startIndex">削除開始位置。</param>
        /// <param name="length">削除文字数。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        /// <returns>部分文字列を削除した文字列。</returns>
        public static StringBuilder RemoveSurrogateSafe(
            this StringBuilder self,
            int startIndex,
            int length,
            bool moveAfter = false)
        {
            CorrectRangeSurrogateSafe(
                self.Length,
                i => self[i],
                ref startIndex,
                ref length,
                moveAfter);

            return self.Remove(startIndex, length);
        }

        /// <summary>
        /// サロゲートペアを分断しないように部分文字列を取得する。
        /// </summary>
        /// <param name="self">対象文字列。</param>
        /// <param name="startIndex">取得開始位置。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        /// <returns>取得した部分文字列。</returns>
        public static string ToStringSurrogateSafe(
            this StringBuilder self,
            int startIndex,
            bool moveAfter = false)
        {
            CorrectRangeSurrogateSafe(
                self.Length,
                self[startIndex],
                ref startIndex,
                moveAfter);

            var length =
                (self == null || startIndex < 0 || startIndex > self.Length) ?
                    0 : (self.Length - startIndex);

            return self.ToString(startIndex, length);
        }

        /// <summary>
        /// サロゲートペアを分断しないように部分文字列を取得する。
        /// </summary>
        /// <param name="self">対象文字列。</param>
        /// <param name="startIndex">取得開始位置。</param>
        /// <param name="length">取得文字数。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        /// <returns>取得した部分文字列。</returns>
        public static string ToStringSurrogateSafe(
            this StringBuilder self,
            int startIndex,
            int length,
            bool moveAfter = false)
        {
            CorrectRangeSurrogateSafe(
                self.Length,
                i => self[i],
                ref startIndex,
                ref length,
                moveAfter);

            return self.ToString(startIndex, length);
        }

        /// <summary>
        /// サロゲートペアを分断しないように範囲指定値を補正する。
        /// </summary>
        /// <param name="selfLength">処理対象文字列の長さ。</param>
        /// <param name="selfCharAtStartIndex">
        /// 処理対象文字列の startIndex の位置にある文字。
        /// </param>
        /// <param name="startIndex">補正対象の開始位置値。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        private static void CorrectRangeSurrogateSafe(
            int selfLength,
            char selfCharAtStartIndex,
            ref int startIndex,
            bool moveAfter)
        {
            if (
                startIndex > 0 &&
                startIndex < selfLength &&
                char.IsLowSurrogate(selfCharAtStartIndex))
            {
                // 開始位置が下位サロゲートなら範囲を移動
                startIndex += moveAfter ? +1 : -1;
            }
        }

        /// <summary>
        /// サロゲートペアを分断しないように範囲指定値を補正する。
        /// </summary>
        /// <param name="selfLength">処理対象文字列の長さ。</param>
        /// <param name="selfCharGetter">
        /// 処理対象文字列から特定位置の文字を取得するデリゲート。
        /// </param>
        /// <param name="startIndex">補正対象の開始位置値。</param>
        /// <param name="count">補正対象の文字数値。</param>
        /// <param name="moveAfter">
        /// 指定位置がサロゲートペアを分断する時、位置を後方にずらすならば true 。
        /// 既定では前方にずらす。
        /// </param>
        private static void CorrectRangeSurrogateSafe(
            int selfLength,
            Func<int, char> selfCharGetter,
            ref int startIndex,
            ref int count,
            bool moveAfter)
        {
            if (
                startIndex >= 0 &&
                count >= 0 &&
                startIndex + count <= selfLength)
            {
                if (
                    startIndex > 0 &&
                    startIndex < selfLength &&
                    char.IsLowSurrogate(selfCharGetter(startIndex)))
                {
                    // 開始位置が下位サロゲートなら範囲を移動
                    startIndex += moveAfter ? +1 : -1;

                    // ++startIndex により終端位置が範囲外になるなら補正
                    if (moveAfter && startIndex + count > selfLength)
                    {
                        --count;
                    }
                }

                if (count > 0)
                {
                    var end = startIndex + count;
                    if (end < selfLength && char.IsLowSurrogate(selfCharGetter(end)))
                    {
                        // 終端位置が下位サロゲートなら範囲を移動
                        count += moveAfter ? +1 : -1;
                    }
                }
            }
        }

        /// <summary>
        /// 文字列列挙による文字列の置換処理を行う。
        /// </summary>
        /// <param name="self">置換対象文字列。</param>
        /// <param name="oldValues">
        /// 置換元文字列列挙。 null や空文字列を含んでいてはならない。
        /// </param>
        /// <param name="newValues">
        /// 置換先文字列列挙。 null を含んでいてはならない。
        /// </param>
        /// <returns>置換された文字列。</returns>
        /// <remarks>
        /// <para>引数 oldValues と newValues の各要素がそれぞれ対応する。</para>
        /// <para>
        /// newValues の要素数が oldValues の要素数より少ない場合、
        /// 超過分の置換先文字列には newValues の末尾要素が利用される。
        /// </para>
        /// <para>
        /// newValues の要素数が oldValues の要素数より多い場合、超過分は無視される。
        /// </para>
        /// </remarks>
        public static string Replace(
            this string self,
            IEnumerable<string> oldValues,
            IEnumerable<string> newValues)
        {
            // 置換処理用アイテムリスト作成
            // 引数の正当性チェックも行われる
            var items = MakeReplaceItems(self, oldValues, newValues);
            if (items.Count <= 0)
            {
                return self;
            }

            var dest = new StringBuilder();
            int selfPos = 0;

            do
            {
                // 最も優先度の高いアイテムを取得
                // 優先度の高い順にソートされているため先頭取得でOK
                var item = items[0];

                // 対象アイテムまでの文字列と対象アイテムの置換先文字列を追加
                dest.Append(self.Substring(selfPos, item.SearchResult - selfPos));
                dest.Append(item.NewValue);

                // 文字列検索基準位置を更新
                selfPos = item.SearchResult + item.OldValue.Length;
                if (selfPos >= self.Length)
                {
                    break;
                }

                // 置換処理用アイテムリスト更新
                UpdateReplaceItems(items, self, selfPos);
            }
            while (items.Count > 0);

            // 末尾までの文字列を追加
            if (selfPos < self.Length)
            {
                dest.Append(self.Substring(selfPos));
            }

            return dest.ToString();
        }

        /// <summary>
        /// 置換処理用アイテムクラス。
        /// </summary>
        private class ReplaceItem : IComparable<ReplaceItem>
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="itemIndex">アイテムの優先度を表すインデックス値。</param>
            /// <param name="oldValue">置換元文字列。</param>
            /// <param name="newValue">置換先文字列。</param>
            /// <param name="searchResult">検索結果保存値。</param>
            public ReplaceItem(
                int itemIndex,
                string oldValue,
                string newValue,
                int searchResult = -1)
            {
                this.ItemIndex = itemIndex;
                this.OldValue = oldValue;
                this.NewValue = newValue;
                this.SearchResult = searchResult;
            }

            /// <summary>
            /// アイテムの優先度を表すインデックス値を取得する。
            /// </summary>
            public int ItemIndex { get; }

            /// <summary>
            /// 置換元文字列を取得する。
            /// </summary>
            public string OldValue { get; }

            /// <summary>
            /// 置換先文字列を取得する。
            /// </summary>
            public string NewValue { get; }

            /// <summary>
            /// 検索結果保存値を取得または設定する。
            /// </summary>
            public int SearchResult { get; set; }

            /// <summary>
            /// 優先度の比較処理を行う。
            /// </summary>
            /// <param name="other">比較対象。</param>
            /// <returns>比較対象との優先順位を表す数値。</returns>
            /// <remarks>
            /// SearchResult の値が異なる場合はその値が小さいほど優先する。
            /// そうではなく OldValue.Length の値が異なる場合はその値が大きいほど優先する。
            /// そうでもなければ ItemIndex の値が小さいほど優先する。
            /// 上記すべての値が等しければ優先順位は等価と判断する。
            /// </remarks>
            public int CompareTo(ReplaceItem other) =>
                (this.SearchResult != other.SearchResult) ?
                    this.SearchResult.CompareTo(other.SearchResult) :
                    (this.OldValue.Length != other.OldValue.Length) ?
                        other.OldValue.Length.CompareTo(this.OldValue.Length) :
                        this.ItemIndex.CompareTo(other.ItemIndex);
        }

        /// <summary>
        /// 置換処理用アイテムリストを作成する。
        /// </summary>
        /// <param name="self">置換対象文字列。</param>
        /// <param name="oldValues">
        /// 置換元文字列列挙。 null や空文字列を含んでいてはならない。
        /// </param>
        /// <param name="newValues">
        /// 置換先文字列列挙。 null を含んでいてはならない。
        /// </param>
        /// <returns>
        /// 置換処理用アイテムリスト。優先度の高い順にソートされている。
        /// </returns>
        private static List<ReplaceItem> MakeReplaceItems(
            string self,
            IEnumerable<string> oldValues,
            IEnumerable<string> newValues)
        {
            // null チェック
            ValidateArgumentNull(self, nameof(self));
            ValidateArgumentNull(oldValues, nameof(oldValues));
            ValidateArgumentNull(newValues, nameof(newValues));

            var newVals = newValues.ToArray();
            if (newVals.Contains(null))
            {
                throw new ArgumentException(
                    @"置換先文字列列挙内に null が含まれています。",
                    nameof(newValues));
            }
            if (!oldValues.Any())
            {
                // 置換元が1つもないなら置換不要なので空リストを返す
                return new List<ReplaceItem>();
            }
            if (newVals.Length <= 0)
            {
                throw new ArgumentException(
                    @"置換先文字列列挙の要素数が 0 です。",
                    nameof(newValues));
            }

            // アイテムリスト作成
            var items =
                oldValues
                    .Select(
                        (v, i) =>
                        {
                            if (v == null)
                            {
                                throw new ArgumentException(
                                    @"置換元文字列列挙内に null が含まれています。",
                                    nameof(oldValues));
                            }
                            if (v.Length == 0)
                            {
                                throw new ArgumentException(
                                    @"置換元文字列列挙内に空文字列が含まれています。",
                                    nameof(oldValues));
                            }

                            var searchResult = self.IndexOf(v);
                            return
                                (searchResult < 0) ?
                                    null :
                                    new ReplaceItem(
                                        i,
                                        v,
                                        newVals[Math.Min(i, newVals.Length - 1)],
                                        searchResult);
                        })
                    .Where(item => item != null)
                    .ToList();

            // ソートする
            items.Sort();

            return items;
        }

        /// <summary>
        /// 置換処理用アイテムリストを更新する。
        /// </summary>
        /// <param name="items">置換処理用アイテムリスト。</param>
        /// <param name="self">置換対象文字列。</param>
        /// <param name="searchResultMin">置換元文字列の検索開始位置。</param>
        private static void UpdateReplaceItems(
            List<ReplaceItem> items,
            string self,
            int searchResultMin)
        {
            // 更新したアイテム数設定先
            int updatedCount = 0;

            // アイテム更新
            // 優先度の関係上 SearchResult が小さい順に並んでいる
            while (
                updatedCount < items.Count &&
                items[updatedCount].SearchResult < searchResultMin)
            {
                var item = items[updatedCount];
                item.SearchResult = self.IndexOf(item.OldValue, searchResultMin);
                ++updatedCount;
            }

            // 更新したアイテムを新しい位置に挿入
            for (int ii = 0; ii < updatedCount; ++ii)
            {
                var item = items[ii];

                // 有効なアイテムのみ挿入する
                if (item.SearchResult >= 0)
                {
                    var pos =
                        items.BinarySearch(
                            updatedCount,
                            items.Count - updatedCount,
                            item,
                            null);
                    items.Insert((pos < 0) ? ~pos : pos, item);
                }
            }

            // 挿入し終えた古いアイテムを削除
            items.RemoveRange(0, updatedCount);
        }
    }
}
