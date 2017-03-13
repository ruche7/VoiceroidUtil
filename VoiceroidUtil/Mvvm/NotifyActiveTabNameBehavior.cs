using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RucheHome.Windows.Mvvm.Behaviors;

namespace VoiceroidUtil.Mvvm
{
    /// <summary>
    /// 現在アクティブな TabItem の名前を通知するビヘイビアクラス。
    /// </summary>
    /// <remarks>
    /// TabItem の中に TabControl がある場合、最初に見つけた TabControl に対して
    /// 再帰的にアクティブな TabItem を探す。
    /// </remarks>
    public class NotifyActiveTabNameBehavior : FrameworkElementBehavior<TabControl>
    {
        /// <summary>
        /// ActiveTabName 依存関係プロパティ。
        /// </summary>
        private static readonly DependencyProperty ActiveTabNameProperty =
            DependencyProperty.Register(
                nameof(ActiveTabName),
                typeof(string),
                typeof(NotifyActiveTabNameBehavior),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnActiveTabNamePropertyChanged));

        /// <summary>
        /// ActiveTabName 依存関係プロパティ値の変更時に呼び出される。
        /// </summary>
        /// <param name="sender">呼び出し元の NotifyActiveTabNameBehavior 。</param>
        /// <param name="e">イベント引数。</param>
        private static void OnActiveTabNamePropertyChanged(
            DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            if (sender is NotifyActiveTabNameBehavior self)
            {
                if ((string)e.NewValue != self.ActiveTabNameOrg)
                {
                    // ActiveTabNameOrg の値で上書き
                    self.SetValue(e.Property, self.ActiveTabNameOrg);
                }
            }
        }

        /// <summary>
        /// 現在アクティブな TabItem の名前を取得する。
        /// </summary>
        public string ActiveTabName
        {
            get => (string)this.GetValue(ActiveTabNameProperty);
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// 現在アクティブな TabItem の名前を取得または設定する。
        /// </summary>
        /// <remarks>
        /// ActiveTabName が外部から変更された時、この値に戻す。
        /// </remarks>
        private string ActiveTabNameOrg { get; set; } = null;

        /// <summary>
        /// 現在アクティブな TabControl のネスト配列を取得または設定する。
        /// </summary>
        private TabControl[] ActiveTabControls { get; set; } = new TabControl[0];

        /// <summary>
        /// 情報の更新を行う。
        /// </summary>
        private void Update()
        {
            var tabControls = this.FindActiveTabControls(this.AssociatedObject);

            // 必ず ActiveTabNameOrg → ActiveTabName の順に設定する
            // そうしないと古い ActiveTabNameOrg の値で上書きされてしまう
            this.ActiveTabNameOrg =
                (tabControls.LastOrDefault()?.SelectedItem as TabItem)?.Name;
            this.SetValue(ActiveTabNameProperty, this.ActiveTabNameOrg);

            if (!tabControls.SequenceEqual(this.ActiveTabControls))
            {
                Array.ForEach(
                    this.ActiveTabControls,
                    tc => tc.SelectionChanged -= this.OnActiveTabControlSelectionChanged);
                Array.ForEach(
                    tabControls,
                    tc => tc.SelectionChanged += this.OnActiveTabControlSelectionChanged);

                this.ActiveTabControls = tabControls;
            }
        }

        /// <summary>
        /// 現在アクティブな TabItem を持つ TabControl のネスト配列を検索する。
        /// </summary>
        /// <param name="tabControl">ルートの TabControl 。</param>
        /// <returns>TabControl のネスト配列。見つからなければ空の配列。</returns>
        private TabControl[] FindActiveTabControls(TabControl tabControl)
        {
            var item = tabControl?.SelectedItem as TabItem;
            if (item == null)
            {
                return new TabControl[0];
            }

            var results = new[] { tabControl };

            // ネストしている TabControl を探す
            var tc = this.FindTabControlFromContent(item.Content);

            return
                (tc == null) ?
                    results :
                    results.Concat(this.FindActiveTabControls(tc)).ToArray();
        }

        /// <summary>
        /// コンテンツからネストしている TabControl を再帰的に検索する。
        /// </summary>
        /// <param name="content">コンテンツ。</param>
        /// <returns>TabControl 。見つからなければ null 。</returns>
        /// <remarks>
        /// 再帰検索対象は ContentControl, Panel のみ。 ItemsControl は対象外。
        /// </remarks>
        private TabControl FindTabControlFromContent(object content)
        {
            // ItemsControl は再帰検索しない
            switch (content)
            {
            case TabControl tc:
                return tc;

            case ContentControl cc:
                return this.FindTabControlFromContent(cc.Content);

            case Panel p:
                return
                    p.Children
                        .Cast<object>()
                        .Select(pc => this.FindTabControlFromContent(pc))
                        .FirstOrDefault(tc => tc != null);
            }

            return null;
        }

        /// <summary>
        /// アクティブな TabControl の選択 TabItem が変更された時に呼び出される。
        /// </summary>
        private void OnActiveTabControlSelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
            =>
            this.Update();

        #region FrameworkElementBehavior<TabControl> の実装

        /// <summary>
        /// TabControl のロード完了時に呼び出される。
        /// </summary>
        protected override void OnAssociatedObjectLoaded() => this.Update();

        #endregion
    }
}
