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
        /// このコンポーネントを拡張編集オブジェクトファイルのセクション形式に変換する。
        /// </summary>
        /// <param name="name">セクション名。</param>
        /// <returns>セクションデータ。</returns>
        public IniFileSection ToExoFileSection(string name)
        {
            var items = ExoFileItemsConverter.ToItems(this);
            if (items == null)
            {
                throw new InvalidCastException(@"Cannot convert to the section.");
            }

            return new IniFileSection(name, items);
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

            // プロパティ群設定
            if (ExoFileItemsConverter.ToProperties(section.Items, ref result) < 0)
            {
                return null;
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
