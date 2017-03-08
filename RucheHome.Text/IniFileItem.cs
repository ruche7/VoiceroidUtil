using System;
using System.Linq;
using System.Runtime.Serialization;

namespace RucheHome.Text
{
    /// <summary>
    /// INIファイルのアイテムを表すクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class IniFileItem : IEquatable<IniFileItem>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">
        /// 名前。
        /// 制御文字や改行文字が含まれていてはならない。
        /// また、空文字列であってはならない。
        /// </param>
        /// <param name="value">値。制御文字や改行文字が含まれていてはならない。</param>
        public IniFileItem(string name, string value)
        {
            ValidateName(name, nameof(name));
            ValidateValue(value, nameof(value));

            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// コンストラクタ。値は空文字列となる。
        /// </summary>
        /// <param name="name">
        /// 名前。
        /// Unicode文字と数字のみで構成されていなければならず、空文字列であってはならない。
        /// </param>
        public IniFileItem(string name) : this(name, "")
        {
        }

        /// <summary>
        /// 名前を取得する。
        /// </summary>
        [DataMember]
        public string Name
        {
            get => this.name;
            private set
            {
                ValidateName(value, nameof(value));

                this.name = value;
            }
        }
        private string name = null;

        /// <summary>
        /// 値を取得または設定する。
        /// </summary>
        [DataMember]
        public string Value
        {
            get => this.value;
            set
            {
                ValidateValue(value, nameof(value));

                this.value = value;
            }
        }
        private string value = null;

        /// <summary>
        /// このオブジェクトのクローンを作成する。
        /// </summary>
        /// <returns>このオブジェクトのクローン。</returns>
        public IniFileItem Clone() => new IniFileItem(this.Name, this.Value);

        /// <summary>
        /// 名前の正当性をチェックする。
        /// </summary>
        /// <param name="name">名前。</param>
        /// <param name="argName">例外送出時に用いる引数名。</param>
        private static void ValidateName(string name, string argName)
        {
            if (name == null)
            {
                throw new ArgumentNullException(argName);
            }
            if (name == "")
            {
                throw new ArgumentException(@"The name is empty.", argName);
            }
            if (
                name.IndexOfAny(new[] { '\r', '\n' }) >= 0 ||
                name.Any(c => char.IsControl(c)))
            {
                throw new ArgumentException(
                    @"Some invalid characters are contained in the name.",
                    argName);
            }
        }

        /// <summary>
        /// 値の正当性をチェックする。
        /// </summary>
        /// <param name="value">値。</param>
        /// <param name="argName">例外送出時に用いる引数名。</param>
        private static void ValidateValue(string value, string argName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argName);
            }
            if (
                value.IndexOfAny(new[] { '\r', '\n' }) >= 0 ||
                value.Any(c => char.IsControl(c)))
            {
                throw new ArgumentException(
                    @"Some invalid characters are contained in the value.",
                    argName);
            }
        }

        #region Object のオーバライド

        /// <summary>
        /// "名前=値" 形式の文字列値を取得する。
        /// </summary>
        /// <returns>"名前=値" 形式の文字列値。</returns>
        public override string ToString() => (this.Name + @"=" + this.Value);

        /// <summary>
        /// 他のオブジェクトと等価であるか否かを取得する。
        /// </summary>
        /// <param name="obj">比較対象。</param>
        /// <returns>等しいならば true 。そうでなければ false 。</returns>
        public override bool Equals(object obj) => this.Equals(obj as IniFileItem);

        /// <summary>
        /// ハッシュコード値を取得する。
        /// </summary>
        /// <returns>ハッシュコード値。</returns>
        public override int GetHashCode() =>
            (this.Name.GetHashCode() ^ this.Value.GetHashCode());

        #endregion

        #region IEquatable<IniFileItem> の実装

        /// <summary>
        /// このアイテムが他のアイテムと等しい名前および値を持つか否かを取得する。
        /// </summary>
        /// <param name="obj">比較対象。</param>
        /// <returns>等しいならば true 。そうでなければ false 。</returns>
        public bool Equals(IniFileItem other) =>
            (this.Name == other?.Name && this.Value == other?.Value);

        #endregion
    }
}
