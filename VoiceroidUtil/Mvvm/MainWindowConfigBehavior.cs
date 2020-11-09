using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using RucheHome.Util;

namespace VoiceroidUtil.Mvvm
{
    /// <summary>
    /// メインウィンドウ設定のロード、適用、セーブを担うビヘイビアクラス。
    /// </summary>
    public class MainWindowConfigBehavior : Behavior<Window>
    {
        /// <summary>
        /// メインウィンドウ設定の保持と読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<MainWindowConfig> ConfigKeeper { get; } =
            new ConfigKeeper<MainWindowConfig>(nameof(VoiceroidUtil));

        /// <summary>
        /// ウィンドウが最初に表示された時に呼び出される。
        /// </summary>
        private void OnWindowContentRendered(object sender, EventArgs e) =>
            this.ConfigKeeper.Value?.ApplyMaximizedTo(this.AssociatedObject);

        /// <summary>
        /// ウィンドウが閉じようとしている時に呼び出される。
        /// </summary>
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (this.ConfigKeeper.Value == null)
            {
                this.ConfigKeeper.Value = new MainWindowConfig();
            }

            this.ConfigKeeper.Value.CopyFrom(this.AssociatedObject);
            this.ConfigKeeper.Save();
        }

        #region Behavior<Window> のオーバライド

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Closing += this.OnWindowClosing;

            if (this.ConfigKeeper.Value == null && !this.ConfigKeeper.Load())
            {
                return;
            }

            this.ConfigKeeper.Value.ApplyLocationTo(this.AssociatedObject);
            if (this.AssociatedObject.IsMeasureValid)
            {
                this.OnWindowContentRendered(this.AssociatedObject, EventArgs.Empty);
            }
            else
            {
                this.AssociatedObject.ContentRendered += this.OnWindowContentRendered;
            }
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.ContentRendered -= this.OnWindowContentRendered;
            this.AssociatedObject.Closing -= this.OnWindowClosing;

            base.OnDetaching();
        }

        protected override Freezable CreateInstanceCore() => new MainWindowConfigBehavior();

        #endregion
    }
}
