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
        /// 拡張編集オブジェクトファイルのセクションデータからコンポーネントを作成する。
        /// </summary>
        /// <param name="section">セクションデータ。</param>
        /// <returns>コンポーネント。作成できないならば null 。</returns>
        public static IComponent FromExoFileSection(IniFileSection section)
        {
            return Parsers.Select(p => p(section)).FirstOrDefault(c => c != null);
        }

        /// <summary>
        /// パースメソッド群。
        /// </summary>
        private static readonly Func<IniFileSection, IComponent>[] Parsers =
            {
                TextComponent.FromExoFileSection,
                AudioFileComponent.FromExoFileSection,
                RenderComponent.FromExoFileSection,
                PlayComponent.FromExoFileSection,
                section => new UnknownComponent { Items = section.Items },
            };
    }
}
