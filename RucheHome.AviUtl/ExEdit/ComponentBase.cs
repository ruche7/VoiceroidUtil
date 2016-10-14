using System;
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
        [ExoFileItem(ExoFileItemNameOfComponentName, Order = 0)]
        public abstract string ComponentName { get; }

        /// <summary>
        /// このコンポーネントを
        /// 拡張編集オブジェクトファイルのアイテムコレクションに変換する。
        /// </summary>
        /// <returns>アイテムコレクション。</returns>
        public IniFileItemCollection ToExoFileItems() =>
            ExoFileItemsConverter.ToItems(this);

        /// <summary>
        /// 拡張編集オブジェクトファイルのアイテムコレクションに
        /// コンポーネント名が含まれているか否かを取得する。
        /// </summary>
        /// <param name="items">アイテムコレクション。</param>
        /// <param name="componentName">コンポーネント名。</param>
        /// <returns>含まれているならば true 。そうでなければ false 。</returns>
        /// <remarks>
        /// 継承先での HasComponentName 静的メソッドの実装に用いることができる。
        /// </remarks>
        protected static bool HasComponentNameCore(
            IniFileItemCollection items,
            string componentName)
        {
            return
                items != null &&
                componentName != null &&
                componentName ==
                    items
                        .FirstOrDefault(i => i.Name == ExoFileItemNameOfComponentName)?
                        .Value;
        }

        /// <summary>
        /// 拡張編集オブジェクトファイルのアイテムコレクションから
        /// コンポーネントを作成する。
        /// </summary>
        /// <typeparam name="T">コンポーネント型。</typeparam>
        /// <param name="items">アイテムコレクション。</param>
        /// <param name="creater">コンポーネント生成デリゲート。</param>
        /// <returns>コンポーネント。</returns>
        /// <remarks>
        /// 継承先での FromExoFileItems 静的メソッドの実装に用いることができる。
        /// </remarks>
        protected static T FromExoFileItemsCore<T>(
            IniFileItemCollection items,
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

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            // コンポーネント名をチェック
            if (!HasComponentNameCore(items, result.ComponentName))
            {
                throw new ArgumentException(
                    @"The component name is not found.",
                    nameof(items));
            }

            // プロパティ群設定
            ExoFileItemsConverter.ToProperties(items, ref result);

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
                            p.IsDefined(typeof(ExoFileItemAttribute), true));

            foreach (var prop in props)
            {
                prop.SetMethod.Invoke(
                    target,
                    new[] { prop.GetMethod.Invoke(this, null) });
            }
        }
    }
}
