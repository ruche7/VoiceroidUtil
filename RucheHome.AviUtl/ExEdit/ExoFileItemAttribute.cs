using System;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// プロパティと拡張編集オブジェクトファイルのアイテムとの相互変換情報を提供する
    /// 属性クラス。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ExoFileItemAttribute : Attribute
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">アイテム名。</param>
        /// <remarks>
        /// コンバータ型には typeof(DefaultExoFileValueConverter) が利用される。
        /// </remarks>
        public ExoFileItemAttribute(string name)
            : this(name, typeof(DefaultExoFileValueConverter))
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">アイテム名。</param>
        /// <param name="converterType">
        /// コンバータ型。
        /// IExoFileValueConverter インタフェースを実装しており、
        /// かつ引数なしのパブリックなコンストラクタを持つ必要がある。
        /// </param>
        public ExoFileItemAttribute(string name, Type converterType)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (converterType == null)
            {
                throw new ArgumentNullException(nameof(converterType));
            }
            if (!typeof(IExoFileValueConverter).IsAssignableFrom(converterType))
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
