using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// オブジェクト内の ExoFileItemAttribute 属性を持つプロパティ群と
    /// 拡張編集オブジェクトファイルのアイテムコレクションとの相互変換処理を提供する
    /// 静的クラス。
    /// </summary>
    public static class ExoFileItemsConverter
    {
        /// <summary>
        /// オブジェクト内の ExoFileItemAttribute 属性を持つプロパティ群を
        /// 拡張編集オブジェクトファイルのアイテムコレクションに変換する。
        /// </summary>
        /// <typeparam name="T">変換元オブジェクト型。</typeparam>
        /// <param name="source">変換元オブジェクト。</param>
        /// <returns>アイテムコレクション。</returns>
        public static IniFileItemCollection ToItems<T>(T source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var items = new IniFileItemCollection();

            // 対象プロパティ情報列挙を取得
            var props =
                source.GetType()
                    .GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .Select(
                        info =>
                            new
                            {
                                info,
                                attr =
                                    info.GetCustomAttribute<ExoFileItemAttribute>(true),
                            })
                    .Where(v => v.info.CanRead && v.attr != null)
                    .OrderBy(v => v.attr.Order);

            foreach (var p in props)
            {
                // プロパティ値取得
                var value = p.info.GetMethod.Invoke(source, null);

                // コンバータ取得
                var conv = GetItemConverter(p.attr.ConverterType);

                // プロパティ値をアイテム文字列値に変換
                var exoValue = conv.ToExoFileValue(value, p.info.PropertyType);
                if (exoValue == null)
                {
                    throw new InvalidCastException(
                        @"Cannot convert from the property (" +
                        p.info.Name +
                        @") of " +
                        source.GetType().Name +
                        @".");
                }

                // セクションに追加
                items.Add(p.attr.Name, exoValue);
            }

            return items;
        }

        /// <summary>
        /// 拡張編集オブジェクトファイルのアイテムコレクションの内容を変換し、
        /// オブジェクト内の ExoFileItemAttribute 属性を持つプロパティ群に設定する。
        /// </summary>
        /// <typeparam name="T">設定先オブジェクト型。</typeparam>
        /// <param name="items">アイテムコレクション。</param>
        /// <param name="target">設定先オブジェクト。</param>
        /// <returns>設定したアイテム数。</returns>
        public static int ToProperties<T>(IniFileItemCollection items, ref T target)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            // 対象プロパティ情報列挙を取得
            var props =
                target.GetType()
                    .GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .Select(
                        info =>
                            new
                            {
                                info,
                                attr =
                                    info.GetCustomAttribute<ExoFileItemAttribute>(true),
                            })
                    .Where(v => v.info.CanWrite && v.attr != null);

            int count = 0;

            foreach (var p in props)
            {
                // アイテム文字列値取得
                var exoValue = items.FirstOrDefault(i => i.Name == p.attr.Name)?.Value;
                if (exoValue == null)
                {
                    // 存在しないアイテムは無視
                    continue;
                }

                // コンバータ取得
                var conv = GetItemConverter(p.attr.ConverterType);

                // アイテム文字列値をプロパティ値に変換
                var value = conv.FromExoFileValue(exoValue, p.info.PropertyType);
                if (value == null)
                {
                    throw new InvalidCastException(
                        @"Cannot convert to the property (" +
                        p.info.Name +
                        @") of " +
                        target.GetType().Name +
                        @".");
                }

                // プロパティ値設定
                p.info.SetMethod.Invoke(target, new[] { value.Item1 });
                ++count;
            }

            return count;
        }

        /// <summary>
        /// アイテムコンバータディクショナリを取得する。
        /// </summary>
        private static Dictionary<Type, IExoFileValueConverter> ItemConverters { get; } =
            new Dictionary<Type, IExoFileValueConverter>();

        /// <summary>
        /// ItemConverters の排他ロック用オブジェクト。
        /// </summary>
        private static readonly object ItemConvertersLockObject = new object();

        /// <summary>
        /// アイテムコンバータを取得する。
        /// </summary>
        /// <param name="converterType">アイテムコンバータの型情報。</param>
        /// <returns>アイテムコンバータ。</returns>
        private static IExoFileValueConverter GetItemConverter(Type converterType)
        {
            Debug.Assert(converterType != null);

            IExoFileValueConverter result;

            lock (ItemConvertersLockObject)
            {
                if (!ItemConverters.TryGetValue(converterType, out result))
                {
                    // ディクショナリ未登録なら作成
                    result =
                        Activator.CreateInstance(converterType) as IExoFileValueConverter;
                    if (result == null)
                    {
                        throw new InvalidOperationException(@"Invalid converter type.");
                    }
                    ItemConverters.Add(converterType, result);
                }
            }

            return result;
        }
    }
}
