using System;
using System.Windows;
using System.Windows.Controls;

namespace RucheHome.Windows.Mvvm.Behaviors
{
    /// <summary>
    /// ListBox クラスに選択項目への自動スクロールを提供するビヘイビア。
    /// </summary>
    public class ListBoxAutoScrollBehavior : FrameworkElementBehavior<ListBox>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ListBoxAutoScrollBehavior() : base()
        {
        }

        /// <summary>
        /// 選択項目が変更された時に呼び出される。
        /// </summary>
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = this.AssociatedObject;
            var index = listBox.SelectedIndex;
            if (index >= 0 && index < listBox.Items.Count)
            {
                listBox.ScrollIntoView(listBox.Items[index]);
            }
        }

        #region FrameworkElementBehavior<ListBox> のオーバライド

        /// <summary>
        /// ItemsControl のロード完了時に呼び出される。
        /// </summary>
        protected override void OnAssociatedObjectLoaded() =>
            this.AssociatedObject.SelectionChanged += this.OnSelectionChanged;

        /// <summary>
        /// ビヘイビアをデタッチする直前に呼び出される。
        /// </summary>
        protected override void OnDetaching()
        {
            this.AssociatedObject.SelectionChanged -= this.OnSelectionChanged;

            base.OnDetaching();
        }

        /// <summary>
        /// 自身の型のインスタンスを作成する。
        /// </summary>
        /// <returns>作成されたインスタンス。</returns>
        protected override Freezable CreateInstanceCore() => new ListBoxAutoScrollBehavior();

        #endregion
    }
}
