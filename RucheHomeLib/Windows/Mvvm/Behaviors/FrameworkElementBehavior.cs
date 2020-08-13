using System;
using System.Windows;
using System.Windows.Interactivity;

namespace RucheHome.Windows.Mvvm.Behaviors
{
    /// <summary>
    /// UI要素のロード完了時呼び出しをサポートするビヘイビアの基底クラス。
    /// </summary>
    /// <typeparam name="T">FrameworkElement 派生型。</typeparam>
    public abstract class FrameworkElementBehavior<T> : Behavior<T>
        where T : FrameworkElement
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        protected FrameworkElementBehavior() : base()
        {
        }

        /// <summary>
        /// UI要素のロード完了時に呼び出される。
        /// </summary>
        /// <remarks>
        /// アタッチ時点でロード完了済みの場合は即座に呼び出される。
        /// </remarks>
        protected virtual void OnAssociatedObjectLoaded()
        {
        }

        /// <summary>
        /// UI要素のロード完了時に呼び出される。
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.AssociatedObject.Loaded -= this.OnLoaded;

            this.OnAssociatedObjectLoaded();
        }

        #region Behavior<T> のオーバライド

        /// <summary>
        /// ビヘイビアをアタッチした時に呼び出される。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            if (this.AssociatedObject.IsLoaded)
            {
                // ロード済みなら即呼び出し
                this.OnAssociatedObjectLoaded();
            }
            else
            {
                // 未ロードなら呼び出し予約
                this.AssociatedObject.Loaded += this.OnLoaded;
            }
        }

        #endregion
    }
}
