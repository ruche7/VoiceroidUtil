using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using Reactive.Bindings;
using VoiceroidUtil.ViewModel;

namespace VoiceroidUtil.View
{
    /// <summary>
    /// トークテキスト置換アイテムコレクション操作ビューを保持するユーザコントロールクラス。
    /// </summary>
    public partial class TalkTextReplaceItemsView : UserControl
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TalkTextReplaceItemsView()
        {
            this.InitializeComponent();
            this.SetupForDesignTime();
        }

        /// <summary>
        /// デザイン時のセットアップを行う。
        /// </summary>
        [Conditional(@"DEBUG")]
        private void SetupForDesignTime()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            var items = new TalkTextReplaceItemCollection();
            items.Add(
                new TalkTextReplaceItem
                {
                    IsEnabled = true,
                    OldValue = @"aaa",
                    NewValue = @"bbbbbb",
                });
            items.Add(
                new TalkTextReplaceItem
                {
                    IsEnabled = false,
                    OldValue = @"あいう",
                    NewValue = @"x",
                });
            items.Add(
                new TalkTextReplaceItem
                {
                    IsEnabled = true,
                    OldValue = @"123456",
                    NewValue = @"789",
                });

            this.DataContext =
                new TalkTextReplaceItemsViewModel(
                    new ReactiveProperty<bool>(true),
                    new ReactiveProperty<TalkTextReplaceItemCollection>(items));
        }
    }
}
