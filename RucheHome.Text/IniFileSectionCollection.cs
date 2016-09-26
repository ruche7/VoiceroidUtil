using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace RucheHome.Text
{
    /// <summary>
    /// INIファイルセクションのコレクションクラス。
    /// </summary>
    public class IniFileSectionCollection : Collection<IniFileSection>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public IniFileSectionCollection() : base()
        {
        }

        /// <summary>
        /// 指定した名前を持つセクションのアイテムコレクションを取得する。
        /// </summary>
        /// <param name="name">
        /// セクション名。制御文字や改行文字が含まれていてはならない。
        /// </param>
        /// <returns>アイテムコレクション。</returns>
        /// <remarks>
        /// セクションが見つからなければ、
        /// 空のアイテムコレクションを持つセクションが追加される。
        /// </remarks>
        public IniFileItemCollection this[string name]
        {
            get
            {
                var index = this.IndexOf(name);
                return ((index < 0) ? this.Add(name) : this[index]).Items;
            }
        }

        /// <summary>
        /// 指定した名前を持つセクションが含まれているか否かを取得する。
        /// </summary>
        /// <param name="name">セクション名。</param>
        /// <returns>含まれているならば true 。そうでなければ false 。</returns>
        public bool Contains(string name) => this.Any(s => s.Name == name);

        /// <summary>
        /// 指定した名前を持つセクションのインデックスを検索する。
        /// </summary>
        /// <param name="name">セクション名。</param>
        /// <returns>インデックス。セクションが含まれていないならば -1 。</returns>
        public int IndexOf(string name)
        {
            for (int i = 0; i < this.Count; ++i)
            {
                if (this[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 指定した名前とアイテムコレクションを持つセクションを末尾に追加する。
        /// </summary>
        /// <param name="name">
        /// セクション名。制御文字や改行文字が含まれていてはならない。
        /// </param>
        /// <param name="items">アイテムコレクション。</param>
        /// <returns>追加されたセクション。</returns>
        public IniFileSection Add(string name, IniFileItemCollection items)
        {
            var section = new IniFileSection(name, items);
            this.Add(section);
            return section;
        }

        /// <summary>
        /// 指定した名前と空のアイテムコレクションを持つセクションを末尾に追加する。
        /// </summary>
        /// <param name="name">
        /// セクション名。制御文字や改行文字が含まれていてはならない。
        /// </param>
        /// <returns>追加されたセクション。</returns>
        public IniFileSection Add(string name) =>
            this.Add(name, new IniFileItemCollection());

        /// <summary>
        /// 指定した名前と空のアイテムコレクションを持つセクションを挿入する。
        /// </summary>
        /// <param name="index">挿入先のインデックス。</param>
        /// <param name="name">
        /// セクション名。制御文字や改行文字が含まれていてはならない。
        /// </param>
        /// <param name="items">アイテムコレクション。</param>
        /// <returns>挿入されたセクション。</returns>
        public IniFileSection Insert(int index, string name, IniFileItemCollection items)
        {
            var section = new IniFileSection(name, items);
            this.Insert(index, section);
            return section;
        }

        /// <summary>
        /// 指定した名前と空のアイテムコレクションを持つセクションを挿入する。
        /// </summary>
        /// <param name="index">挿入先のインデックス。</param>
        /// <param name="name">
        /// セクション名。制御文字や改行文字が含まれていてはならない。
        /// </param>
        /// <returns>挿入されたセクション。</returns>
        public IniFileSection Insert(int index, string name) =>
            this.Insert(index, name, new IniFileItemCollection());

        /// <summary>
        /// 指定した名前を持つセクションを削除する。
        /// </summary>
        /// <param name="name">セクション名。</param>
        /// <returns>削除できたならば true 。そうでなければ false 。</returns>
        public bool Remove(string name)
        {
            var index = this.IndexOf(name);
            if (index < 0)
            {
                return false;
            }

            this.RemoveAt(index);
            return true;
        }

        #region Collection<IniFileSection> のオーバライド

        /// <summary>
        /// セクションの挿入時に呼び出される。
        /// </summary>
        /// <param name="index">挿入先のインデックス。</param>
        /// <param name="section">挿入するセクション。</param>
        protected override void InsertItem(int index, IniFileSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }
            if (this.Contains(section.Name))
            {
                throw new ArgumentException(
                    "\"" + section.Name + "\" is already contained section name.",
                    nameof(section));
            }

            base.InsertItem(index, section);
        }

        /// <summary>
        /// セクションの上書き時に呼び出される。
        /// </summary>
        /// <param name="index">上書き先のインデックス。</param>
        /// <param name="section">上書きするセクション。</param>
        protected override void SetItem(int index, IniFileSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            var contained = this.IndexOf(section.Name);
            if (contained >= 0 && contained != index)
            {
                throw new ArgumentException(
                    "\"" + section.Name + "\" is already contained section name.",
                    nameof(section));
            }

            base.SetItem(index, section);
        }

        #endregion

        #region Object のオーバライド

        /// <summary>
        /// INIファイル形式文字列値を取得する。
        /// </summary>
        /// <returns>INIファイル形式文字列値。</returns>
        public override string ToString() =>
            string.Join(Environment.NewLine, this.Select(s => s.ToString()));

        #endregion
    }
}
