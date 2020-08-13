using System;
using System.Windows.Input;

namespace RucheHome.Windows.Mvvm.Commands
{
    /// <summary>
    /// 渡されたデリゲートの実行を行うコマンドクラス。
    /// </summary>
    /// <typeparam name="T">コマンドパラメータ型。</typeparam>
    public class RelayCommand<T> : ICommand
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="execute">コマンドの実処理を行うデリゲート。</param>
        /// <param name="canExecute">
        /// コマンドが実行可能であるか否かを返すデリゲート。
        /// 常に実行可能ならば null を渡してもよい。
        /// </param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            this.ExecuteDelegate =
                execute ?? throw new ArgumentNullException(nameof(execute));
            this.CanExecuteDelegate = canExecute;
        }

        /// <summary>
        /// コマンドを実行する。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        public void Execute(T parameter)
        {
            if (this.CanExecute(parameter))
            {
                this.ExecuteDelegate(parameter);
            }
        }

        /// <summary>
        /// コマンドを実行可能であるか否かを取得する。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <returns>実行可能ならば true 。そうでなければ false 。</returns>
        public bool CanExecute(T parameter) =>
            (this.CanExecuteDelegate?.Invoke(parameter) != false);

        /// <summary>
        /// CanExecute メソッドの戻り値が変化する場合に呼び出す。
        /// </summary>
        public void RaiseCanExecuteChanged() =>
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// コマンドの実処理を行うデリゲートを取得する。
        /// </summary>
        private Action<T> ExecuteDelegate { get; }

        /// <summary>
        /// コマンドが実行可能であるか否かを返すデリゲートを取得する。
        /// </summary>
        /// <remarks>
        /// null ならば常に実行可能として扱う。
        /// </remarks>
        private Func<T, bool> CanExecuteDelegate { get; }

        #region ICommand の実装

        /// <summary>
        /// CanExecute メソッドの戻り値が変化した時に発生するイベント。
        /// </summary>
        public event EventHandler CanExecuteChanged;

        #endregion

        #region ICommand の明示的実装

        bool ICommand.CanExecute(object parameter) =>
            (parameter is T p) ?
                this.CanExecute(p) :
                (default(T) == null && parameter == null && this.CanExecute(default));

        void ICommand.Execute(object parameter)
        {
            if (parameter is T p)
            {
                this.Execute(p);
            }
            else if (default(T) == null && parameter == null)
            {
                this.Execute(default);
            }
        }

        #endregion
    }

    /// <summary>
    /// 渡されたデリゲートの実行を行うコマンドクラス。
    /// </summary>
    public class RelayCommand : RelayCommand<object>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="execute">コマンドの実処理を行うデリゲート。</param>
        /// <param name="canExecute">
        /// コマンドが実行可能であるか否かを返すデリゲート。
        /// 常に実行可能ならば null を渡してもよい。
        /// </param>
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
            : base(execute, canExecute)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="execute">コマンドの実処理を行うデリゲート。</param>
        /// <param name="canExecute">
        /// コマンドが実行可能であるか否かを返すデリゲート。
        /// 常に実行可能ならば null を渡してもよい。
        /// </param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
            :
            base(
                (execute == null) ? (Action<object>)null : (_ => execute()),
                (canExecute == null) ? (Func<object, bool>)null : (_ => canExecute()))
        {
        }
    }
}
