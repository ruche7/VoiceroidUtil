using System;
using RucheHome.Util;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// コンポーネントコレクションクラス。
    /// </summary>
    public class ComponentCollection : NonNullCollection<IComponent>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ComponentCollection() : base()
        {
        }
    }
}
