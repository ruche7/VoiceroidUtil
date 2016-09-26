using System;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// プロパティがコンポーネントのアイテムであることを示す属性クラス。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ComponentItemAttribute : Attribute
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">アイテム名。</param>
        public ComponentItemAttribute(string name)
            : this(name, typeof(ComponentItemConverter))
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">アイテム名。</param>
        /// <param name="converterType">コンバータ型。</param>
        public ComponentItemAttribute(string name, Type converterType)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (converterType == null)
            {
                throw new ArgumentNullException(nameof(converterType));
            }
            if (!typeof(ComponentItemConverter).IsAssignableFrom(converterType))
            {
                throw new ArgumentException(
                    @"Invalid converter type.",
                    nameof(converterType));
            }
            if (converterType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException(
                    @"The converter type has not default constructor.",
                    nameof(converterType));
            }

            this.Name = name;
            this.ConverterType = converterType;
        }

        /// <summary>
        /// アイテム名を取得する。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// コンバータ型を取得する。
        /// </summary>
        public Type ConverterType { get; }

        /// <summary>
        /// アイテムの順序を取得または設定する。
        /// </summary>
        public int Order { get; set; } = int.MaxValue;
    }
}
