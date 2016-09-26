using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using RucheHome.Text;
using RucheHome.Util;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// レイヤーアイテムのコンポーネントの抽象基底クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public abstract class ComponentBase : BindableConfigBase, IComponent
    {
        /// <summary>
        /// コンポーネント名を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfComponentName = @"_name";

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ComponentBase() : base()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        [ComponentItem(ExoFileItemNameOfComponentName, Order = 0)]
        public abstract string ComponentName { get; }

        /// <summary>
        /// このコンポーネントを拡張編集オブジェクトファイルのセクション形式に変換する。
        /// </summary>
        /// <param name="name">セクション名。</param>
        /// <returns>セクションデータ。</returns>
        public IniFileSection ToExoFileSection(string name)
        {
            var section = new IniFileSection(name);

            // 対象プロパティ情報列挙を取得
            var props =
                this.GetType()
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
                                    info.GetCustomAttribute<ComponentItemAttribute>(true),
                            })
                    .Where(v => v.info.CanRead && v.attr != null)
                    .OrderBy(v => v.attr.Order);

            foreach (var p in props)
            {
                // プロパティ値取得
                var value = p.info.GetMethod.Invoke(this, null);

                // コンバータ取得
                var conv = GetItemConverter(p.attr.ConverterType);

                // プロパティ値をアイテム文字列値に変換
                var exoValue = conv.ToExoFileValue(value, p.info.PropertyType);
                if (exoValue == null)
                {
                    throw new InvalidCastException(
                        @"Connot convert property value. (" + p.info.Name + @")");
                }

                // セクションに追加
                section.Items.Add(p.attr.Name, exoValue);
            }

            return section;
        }

        /// <summary>
        /// 拡張編集オブジェクトファイルのセクションデータからコンポーネントを作成する。
        /// </summary>
        /// <typeparam name="T">コンポーネント型。</typeparam>
        /// <param name="section">セクションデータ。</param>
        /// <param name="creater">コンポーネント生成デリゲート。</param>
        /// <returns>コンポーネント。作成できないならば null 。</returns>
        /// <remarks>
        /// 継承先での FromExoFileSection 静的メソッドの実装に用いることができる。
        /// </remarks>
        protected static T FromExoFileSectionCore<T>(
            IniFileSection section,
            Func<T> creater)
            where T : ComponentBase
        {
            if (creater == null)
            {
                throw new ArgumentNullException(nameof(creater));
            }

            // コンポーネント生成
            var result = creater();
            if (result == null)
            {
                throw new ArgumentException(
                    @"Cannot create the component.",
                    nameof(creater));
            }

            if (section == null)
            {
                return null;
            }
            var items = section.Items;

            // コンポーネント名をチェック
            var nameIndex = items.IndexOf(ExoFileItemNameOfComponentName);
            if (nameIndex < 0 || items[nameIndex].Value != result.ComponentName)
            {
                return null;
            }

            // 対象プロパティ情報列挙を取得
            var props =
                typeof(T).GetType()
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
                                    info.GetCustomAttribute<ComponentItemAttribute>(true),
                            })
                    .Where(v => v.info.CanWrite && v.attr != null);

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
                    // 非対応のアイテム文字列値が含まれているのでパース失敗とする
                    return null;
                }

                // プロパティ値設定
                p.info.SetMethod.Invoke(result, new[] { value.Item1 });
            }

            return result;
        }

        /// <summary>
        /// このコンポーネントの内容を同じ型の別のコンポーネントへコピーする。
        /// </summary>
        /// <param name="target">コピー先。</param>
        /// <remarks>
        /// 継承先での CopyTo メソッドの実装に用いることができる。
        /// </remarks>
        protected void CopyToCore(ComponentBase target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var type = this.GetType();
            if (!type.IsInstanceOfType(target))
            {
                throw new ArgumentException(
                    @"The target type is not " + type,
                    nameof(target));
            }

            // 対象プロパティ情報列挙を取得
            var props =
                this.GetType()
                    .GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .Where(
                        p =>
                            p.CanRead &&
                            p.CanWrite &&
                            p.IsDefined(typeof(ComponentItemAttribute), true));

            foreach (var prop in props)
            {
                prop.SetMethod.Invoke(
                    target,
                    new[] { prop.GetMethod.Invoke(this, null) });
            }
        }

        /// <summary>
        /// コンポーネントアイテムコンバータディクショナリを取得する。
        /// </summary>
        private static Dictionary<Type, ComponentItemConverter> ItemConverters { get; } =
            new Dictionary<Type, ComponentItemConverter>();

        /// <summary>
        /// コンポーネントアイテムコンバータを取得する。
        /// </summary>
        /// <param name="converterType">コンポーネントアイテムコンバータの型情報。</param>
        /// <returns>コンポーネントアイテムコンバータ。</returns>
        private static ComponentItemConverter GetItemConverter(Type converterType)
        {
            Debug.Assert(converterType != null);

            ComponentItemConverter result;
            if (!ItemConverters.TryGetValue(converterType, out result))
            {
                // ディクショナリ未登録なら作成
                result = Activator.CreateInstance(converterType) as ComponentItemConverter;
                if (result == null)
                {
                    throw new InvalidOperationException(@"Invalid converter type.");
                }
                ItemConverters.Add(converterType, result);
            }

            return result;
        }
    }
}
