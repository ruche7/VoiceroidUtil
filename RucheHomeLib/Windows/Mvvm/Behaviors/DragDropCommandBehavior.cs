using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace RucheHome.Windows.Mvvm.Behaviors
{
    /// <summary>
    /// 各種ドラッグ＆ドロップイベントをコマンドで処理するためのビヘイビアクラス。
    /// </summary>
    /// <remarks>
    /// 各コマンドのコマンドパラメータには DragEventArgs オブジェクトが渡される。
    /// </remarks>
    public class DragDropCommandBehavior : Behavior<UIElement>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public DragDropCommandBehavior() : base()
        {
        }

        /// <summary>
        /// DragEnterCommand 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty DragEnterCommandProperty =
            DependencyProperty.Register(
                nameof(DragEnterCommand),
                typeof(ICommand),
                typeof(DragDropCommandBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// DragEnter イベントを処理するコマンドを取得または設定する。
        /// </summary>
        public ICommand DragEnterCommand
        {
            get => (ICommand)this.GetValue(DragEnterCommandProperty);
            set => this.SetValue(DragEnterCommandProperty, value);
        }

        /// <summary>
        /// DragOverCommand 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty DragOverCommandProperty =
            DependencyProperty.Register(
                nameof(DragOverCommand),
                typeof(ICommand),
                typeof(DragDropCommandBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// DragOver イベントを処理するコマンドを取得または設定する。
        /// </summary>
        public ICommand DragOverCommand
        {
            get => (ICommand)this.GetValue(DragOverCommandProperty);
            set => this.SetValue(DragOverCommandProperty, value);
        }

        /// <summary>
        /// DragLeaveCommand 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty DragLeaveCommandProperty =
            DependencyProperty.Register(
                nameof(DragLeaveCommand),
                typeof(ICommand),
                typeof(DragDropCommandBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// DragLeave イベントを処理するコマンドを取得または設定する。
        /// </summary>
        public ICommand DragLeaveCommand
        {
            get => (ICommand)this.GetValue(DragLeaveCommandProperty);
            set => this.SetValue(DragLeaveCommandProperty, value);
        }

        /// <summary>
        /// DropCommand 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.Register(
                nameof(DropCommand),
                typeof(ICommand),
                typeof(DragDropCommandBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// Drop イベントを処理するコマンドを取得または設定する。
        /// </summary>
        public ICommand DropCommand
        {
            get => (ICommand)this.GetValue(DropCommandProperty);
            set => this.SetValue(DropCommandProperty, value);
        }

        /// <summary>
        /// PreviewDragEnterCommand 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty PreviewDragEnterCommandProperty =
            DependencyProperty.Register(
                nameof(PreviewDragEnterCommand),
                typeof(ICommand),
                typeof(DragDropCommandBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// PreviewDragEnter イベントを処理するコマンドを取得または設定する。
        /// </summary>
        public ICommand PreviewDragEnterCommand
        {
            get => (ICommand)this.GetValue(PreviewDragEnterCommandProperty);
            set => this.SetValue(PreviewDragEnterCommandProperty, value);
        }

        /// <summary>
        /// PreviewDragOverCommand 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty PreviewDragOverCommandProperty =
            DependencyProperty.Register(
                nameof(PreviewDragOverCommand),
                typeof(ICommand),
                typeof(DragDropCommandBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// PreviewDragOver イベントを処理するコマンドを取得または設定する。
        /// </summary>
        public ICommand PreviewDragOverCommand
        {
            get => (ICommand)this.GetValue(PreviewDragOverCommandProperty);
            set => this.SetValue(PreviewDragOverCommandProperty, value);
        }

        /// <summary>
        /// PreviewDragLeaveCommand 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty PreviewDragLeaveCommandProperty =
            DependencyProperty.Register(
                nameof(PreviewDragLeaveCommand),
                typeof(ICommand),
                typeof(DragDropCommandBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// PreviewDragLeave イベントを処理するコマンドを取得または設定する。
        /// </summary>
        public ICommand PreviewDragLeaveCommand
        {
            get => (ICommand)this.GetValue(PreviewDragLeaveCommandProperty);
            set => this.SetValue(PreviewDragLeaveCommandProperty, value);
        }

        /// <summary>
        /// PreviewDropCommand 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty PreviewDropCommandProperty =
            DependencyProperty.Register(
                nameof(PreviewDropCommand),
                typeof(ICommand),
                typeof(DragDropCommandBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// PreviewDrop イベントを処理するコマンドを取得または設定する。
        /// </summary>
        public ICommand PreviewDropCommand
        {
            get => (ICommand)this.GetValue(PreviewDropCommandProperty);
            set => this.SetValue(PreviewDropCommandProperty, value);
        }

        /// <summary>
        /// コマンドによってドラッグ＆ドロップイベントを処理する。
        /// </summary>
        /// <param name="command">コマンド。</param>
        /// <param name="e">イベントデータ。</param>
        private void ExecuteCommand(ICommand command, DragEventArgs e)
        {
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }

        #region イベントメソッド群

        private void OnDragEnter(object sender, DragEventArgs e) =>
            this.ExecuteCommand(this.DragEnterCommand, e);

        private void OnDragOver(object sender, DragEventArgs e) =>
            this.ExecuteCommand(this.DragOverCommand, e);

        private void OnDragLeave(object sender, DragEventArgs e) =>
            this.ExecuteCommand(this.DragLeaveCommand, e);

        private void OnDrop(object sender, DragEventArgs e) =>
            this.ExecuteCommand(this.DropCommand, e);

        private void OnPreviewDragEnter(object sender, DragEventArgs e) =>
            this.ExecuteCommand(this.PreviewDragOverCommand, e);

        private void OnPreviewDragOver(object sender, DragEventArgs e) =>
            this.ExecuteCommand(this.PreviewDragOverCommand, e);

        private void OnPreviewDragLeave(object sender, DragEventArgs e) =>
            this.ExecuteCommand(this.PreviewDragLeaveCommand, e);

        private void OnPreviewDrop(object sender, DragEventArgs e) =>
            this.ExecuteCommand(this.PreviewDropCommand, e);

        #endregion

        #region Behavior<UIElement> のオーバライド

        /// <summary>
        /// ビヘイビアをアタッチした時に呼び出される。
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.DragEnter += this.OnDragEnter;
            this.AssociatedObject.DragOver += this.OnDragOver;
            this.AssociatedObject.DragLeave += this.OnDragLeave;
            this.AssociatedObject.Drop += this.OnDrop;
            this.AssociatedObject.PreviewDragEnter += this.OnPreviewDragEnter;
            this.AssociatedObject.PreviewDragOver += this.OnPreviewDragOver;
            this.AssociatedObject.PreviewDragLeave += this.OnPreviewDragLeave;
            this.AssociatedObject.PreviewDrop += this.OnPreviewDrop;
        }

        /// <summary>
        /// ビヘイビアをデタッチする直前に呼び出される。
        /// </summary>
        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewDrop -= this.OnPreviewDrop;
            this.AssociatedObject.PreviewDragLeave -= this.OnPreviewDragLeave;
            this.AssociatedObject.PreviewDragOver -= this.OnPreviewDragOver;
            this.AssociatedObject.PreviewDragEnter -= this.OnPreviewDragEnter;
            this.AssociatedObject.Drop -= this.OnDrop;
            this.AssociatedObject.DragLeave -= this.OnDragLeave;
            this.AssociatedObject.DragOver -= this.OnDragOver;
            this.AssociatedObject.DragEnter -= this.OnDragEnter;

            base.OnDetaching();
        }

        /// <summary>
        /// 自身の型のインスタンスを作成する。
        /// </summary>
        /// <returns>作成されたインスタンス。</returns>
        protected override Freezable CreateInstanceCore() => new DragDropCommandBehavior();

        #endregion
    }
}
