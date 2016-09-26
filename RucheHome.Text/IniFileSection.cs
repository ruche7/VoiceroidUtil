using System;
using System.Linq;
using System.Runtime.Serialization;

namespace RucheHome.Text
{
    /// <summary>
    /// INIファイルのセクションを表すクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    [KnownType(typeof(IniFileItem))]
    public class IniFileSection
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">
        /// セクション名。制御文字や改行文字が含まれていてはならない。
        /// </param>
        /// <param name="items">アイテムコレクション。</param>
        public IniFileSection(string name, IniFileItemCollection items)
        {
            ValidateName(name, nameof(name));
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            this.Name = name;
            this.Items = items;
        }

        /// <summary>
        /// コンストラクタ。アイテムコレクションは空となる。
        /// </summary>
        /// <param name="name">
        /// セクション名。制御文字や改行文字が含まれていてはならない。
        /// </param>
        public IniFileSection(string name) : this(name, new IniFileItemCollection())
        {
        }

        /// <summary>
        /// セクション名を取得する。
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return this.name; }
            private set
            {
                ValidateName(value, nameof(value));

                this.name = value;
            }
        }
        private string name = "";

        /// <summary>
        /// アイテムコレクションを取得する。
        /// </summary>
        [DataMember]
        public IniFileItemCollection Items { get; private set; }

        /// <summary>
        /// セクション名の正当性をチェックする。
        /// </summary>
        /// <param name="name">セクション名。</param>
        /// <param name="argName">例外送出時に用いる引数名。</param>
        private static void ValidateName(string name, string argName)
        {
            if (name == null)
            {
                throw new ArgumentNullException(argName);
            }
            if (
                name.IndexOfAny(new[] { '\r', '\n' }) >= 0 ||
                name.Any(c => char.IsControl(c)))
            {
                throw new ArgumentException(
                    @"Some invalid characters are contained in the section name.",
                    argName);
            }
        }

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.Name = "";
            this.Items = new IniFileItemCollection();
        }

        #region Object のオーバライド

        /// <summary>
        /// INIファイルのセクション形式を表す文字列値を取得する。
        /// </summary>
        /// <returns>INIファイルのセクション形式を表す文字列値。</returns>
        public override string ToString() =>
            @"[" + this.Name + @"]" + Environment.NewLine + this.Items;

        #endregion
    }
}
