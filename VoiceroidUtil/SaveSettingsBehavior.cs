using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace VoiceroidUtil
{
    /// <summary>
    /// 保存設定の表示状態に応じた処理を行うビヘイビアクラス。
    /// </summary>
    public class SaveSettingsBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public SaveSettingsBehavior()
        {
        }

        /// <summary>
        /// ビヘイビアのアタッチ時に呼び出される。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this.LastHeight = this.CalcHeight();

            this.AssociatedObject.SizeChanged += this.OnSizeChanged;
            BindingOperations.SetBinding(
                this,
                VisibilityProperty,
                new Binding
                {
                    Source = this.AssociatedObject,
                    Path = new PropertyPath(nameof(this.AssociatedObject.Visibility)),
                    Mode = BindingMode.OneWay,
                });
        }

        /// <summary>
        /// ビヘイビアのデタッチ時に呼び出される。
        /// </summary>
        protected override void OnDetaching()
        {
            BindingOperations.ClearBinding(this, VisibilityProperty);
            this.AssociatedObject.SizeChanged -= this.OnSizeChanged;

            base.OnDetaching();
        }

        /// <summary>
        /// Visibility プロパティの変更を監視するための依存関係プロパティ。
        /// </summary>
        private static readonly DependencyProperty VisibilityProperty =
            DependencyProperty.Register(
                @"Visibility",
                typeof(object),
                typeof(SaveSettingsBehavior),
                new PropertyMetadata(
                    null,
                    (s, e) => (s as SaveSettingsBehavior)?.ChangeWindowHeight()));

        /// <summary>
        /// 最後にウィンドウの高さを変更した時のアタッチ対象の高さを取得または設定する。
        /// </summary>
        private double LastHeight { get; set; } = 0;

        /// <summary>
        /// アタッチ対象の高さを算出する。
        /// </summary>
        /// <returns>アタッチ対象の高さ。</returns>
        private double CalcHeight()
        {
            if (
                !this.AssociatedObject.IsMeasureValid ||
                this.AssociatedObject.Visibility == Visibility.Collapsed)
            {
                return 0;
            }

            return
                this.AssociatedObject.ActualHeight +
                this.AssociatedObject.Margin.Top +
                this.AssociatedObject.Margin.Bottom;
        }

        /// <summary>
        /// ウィンドウの高さの変更処理を行う。
        /// </summary>
        private void ChangeWindowHeight()
        {
            var window = Window.GetWindow(this.AssociatedObject);
            if (window == null)
            {
                return;
            }

            var height = this.CalcHeight();

            var diff = height - this.LastHeight;
            if (diff > 0)
            {
                window.Height += diff;
                window.MinHeight += diff;
            }
            else if (diff < 0)
            {
                window.MinHeight += diff;
                window.Height += diff;
            }

            this.LastHeight = height;
        }

        /// <summary>
        /// アタッチ対象のサイズ変更時に呼び出される。
        /// </summary>
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.ChangeWindowHeight();
        }
    }
}
