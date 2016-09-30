using System;
using System.Linq;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// コンポーネント生成処理を提供する静的クラス。
    /// </summary>
    public static class ComponentMaker
    {
        /// <summary>
        /// 拡張編集オブジェクトファイルのアイテムコレクションから
        /// コンポーネントを作成する。
        /// </summary>
        /// <param name="items">アイテムコレクション。</param>
        /// <returns>コンポーネント。</returns>
        public static IComponent FromExoFileItems(IniFileItemCollection items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var result =
                Methods
                    .Select(m => m.CanMake(items) ? m.Make(items) : null)
                    .FirstOrDefault(c => c != null);
            if (result == null)
            {
                throw new InvalidCastException(@"Cannot make a component.");
            }

            return result;
        }

        /// <summary>
        /// パース用メソッド群を保持する構造体。
        /// </summary>
        private struct ParseMethods
        {
            /// <summary>
            /// パース可能であるか否かを調べるメソッド。
            /// </summary>
            public Func<IniFileItemCollection, bool> CanMake;

            /// <summary>
            /// パースを行うメソッド。
            /// </summary>
            public Func<IniFileItemCollection, IComponent> Make;
        }

        /// <summary>
        /// パース用メソッド群。
        /// </summary>
        private static readonly ParseMethods[] Methods =
            {
                new ParseMethods
                {
                    CanMake = TextComponent.HasComponentName,
                    Make = TextComponent.FromExoFileItems,
                },
                new ParseMethods
                {
                    CanMake = AudioFileComponent.HasComponentName,
                    Make = AudioFileComponent.FromExoFileItems,
                },
                new ParseMethods
                {
                    CanMake = RenderComponent.HasComponentName,
                    Make = RenderComponent.FromExoFileItems,
                },
                new ParseMethods
                {
                    CanMake = PlayComponent.HasComponentName,
                    Make = PlayComponent.FromExoFileItems,
                },
                new ParseMethods
                {
                    CanMake = _ => true,
                    Make = items => new UnknownComponent { Items = items.Clone() },
                },
            };
    }
}
