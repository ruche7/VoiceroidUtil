using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RucheHome.Text
{
    /// <summary>
    /// INIファイルアイテムのコレクションクラス。
    /// </summary>
    public class IniFileItemCollection : Collection<IniFileItem>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public IniFileItemCollection() : base()
        {
        }

        /// <summary>
        /// 指定した名前を持つアイテムの値を取得または設定する。
        /// </summary>
        /// <param name="name">
        /// 名前。
        /// 制御文字や改行文字が含まれていてはならない。
        /// また、空文字列であってはならない。
        /// </param>
        /// <returns>値。</returns>
        /// <remarks>
        /// アイテムが見つからなければ、空文字列値を持つアイテムが追加される。
        /// </remarks>
        public string this[string name]
        {
            get
            {
                var index = this.IndexOf(name);
                return ((index < 0) ? this.Add(name) : this[index]).Value;
            }
            set
            {
                var index = this.IndexOf(name);
                if (index < 0)
                {
                    this.Add(name, value);
                }
                else
                {
                    this[index].Value = value;
                }
            }
        }

        /// <summary>
        /// 指定した名前を持つアイテムが含まれているか否かを取得する。
        /// </summary>
        /// <param name="name">名前。</param>
        /// <returns>含まれているならば true 。そうでなければ false 。</returns>
        public bool Contains(string name) => this.Any(i => i.Name == name);

        /// <summary>
        /// 指定した名前を持つアイテムのインデックスを検索する。
        /// </summary>
        /// <param name="name">名前。</param>
        /// <returns>インデックス。アイテムが含まれていないならば -1 。</returns>
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
        /// 指定した名前と値を持つアイテムを末尾に追加する。
        /// </summary>
        /// <param name="name">
        /// 名前。
        /// 制御文字や改行文字が含まれていてはならない。
        /// また、空文字列であってはならない。
        /// </param>
        /// <param name="value">値。</param>
        /// <returns>追加されたアイテム。</returns>
        public IniFileItem Add(string name, string value = "")
        {
            var item = new IniFileItem(name, value);
            this.Add(item);
            return item;
        }

        /// <summary>
        /// 指定した名前と値を持つアイテムを挿入する。
        /// </summary>
        /// <param name="index">挿入先のインデックス。</param>
        /// <param name="name">
        /// 名前。
        /// 制御文字や改行文字が含まれていてはならない。
        /// また、空文字列であってはならない。
        /// </param>
        /// <param name="value">値。</param>
        /// <returns>挿入されたアイテム。</returns>
        public IniFileItem Insert(int index, string name, string value = "")
        {
            var item = new IniFileItem(name, value);
            this.Insert(index, item);
            return item;
        }

        /// <summary>
        /// 指定した名前を持つアイテムを削除する。
        /// </summary>
        /// <param name="name">名前。</param>
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

        /// <summary>
        /// このオブジェクトのクローンを作成する。
        /// </summary>
        /// <returns>このオブジェクトのクローン。</returns>
        public IniFileItemCollection Clone() =>
            new IniFileItemCollection(this.Select(i => i.Clone()).ToList());

        /// <summary>
        /// クローン用のコンストラクタ。
        /// </summary>
        /// <param name="list">ラップ対象のリスト。</param>
        protected IniFileItemCollection(IList<IniFileItem> list) : base(list)
        {
        }

        #region Collection<IniFileItem> のオーバライド

        /// <summary>
        /// アイテムの挿入時に呼び出される。
        /// </summary>
        /// <param name="index">挿入先のインデックス。</param>
        /// <param name="item">挿入するアイテム。</param>
        protected override void InsertItem(int index, IniFileItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (this.Contains(item.Name))
            {
                throw new ArgumentException(
                    "\"" + item.Name + "\" is already contained item name.",
                    nameof(item));
            }

            base.InsertItem(index, item);
        }

        /// <summary>
        /// アイテムの上書き時に呼び出される。
        /// </summary>
        /// <param name="index">上書き先のインデックス。</param>
        /// <param name="item">上書きするアイテム。</param>
        protected override void SetItem(int index, IniFileItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var contained = this.IndexOf(item.Name);
            if (contained >= 0 && contained != index)
            {
                throw new ArgumentException(
                    "\"" + item.Name + "\" is already contained item name.",
                    nameof(item));
            }

            base.SetItem(index, item);
        }

        #endregion

        #region Object のオーバライド

        /// <summary>
        /// "名前=値" 形式のアイテム文字列を改行で区切った文字列値を取得する。
        /// </summary>
        /// <returns>"名前=値" 形式のアイテム文字列を改行で区切った文字列値。</returns>
        public override string ToString() =>
            string.Join(Environment.NewLine, this.Select(i => i.ToString()));

        #endregion
    }
}
