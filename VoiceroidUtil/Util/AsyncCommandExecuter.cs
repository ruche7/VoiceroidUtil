using System;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;

namespace VoiceroidUtil.Util
{
    /// <summary>
    /// ReactiveCommand で非同期実行を行うためのジェネリッククラス。
    /// </summary>
    /// <typeparam name="T">コマンドパラメータ型。</typeparam>
    public class AsyncCommandExecuter<T> : IDisposable
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        public AsyncCommandExecuter(Func<T, Task> asyncFunc)
        {
            if (asyncFunc == null)
            {
                throw new ArgumentNullException(nameof(asyncFunc));
            }

            this.AsyncFunc = asyncFunc;
            this.IsExecutable = this.ExecutableNotifier.ToReadOnlyReactiveProperty(true);
        }

        /// <summary>
        /// 非同期実行可能な状態であるか否かを提供するオブジェクトを取得する。
        /// </summary>
        /// <remarks>
        /// ReactiveCommand の実行可能条件として利用するとよい。
        /// </remarks>
        public ReadOnlyReactiveProperty<bool> IsExecutable { get; }

        /// <summary>
        /// 非同期実行を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <remarks>
        /// 現在実行中の場合は何も行わない。
        /// ReactiveCommand.Subscribe に渡すとよい。
        /// </remarks>
        public async void Execute(T parameter)
        {
            if (this.ExecutableNotifier.Value)
            {
                this.ExecutableNotifier.TurnOff();
                try
                {
                    await this.AsyncFunc(parameter);
                }
                finally
                {
                    this.ExecutableNotifier.TurnOn();
                }
            }
        }

        /// <summary>
        /// リソースを破棄する。
        /// </summary>
        public void Dispose()
        {
            this.IsExecutable?.Dispose();
        }

        /// <summary>
        /// 非同期処理デリゲートを取得する。
        /// </summary>
        private Func<T, Task> AsyncFunc { get; }

        /// <summary>
        /// 非同期実行可能か否かの通知オブジェクトを取得する。
        /// </summary>
        private BooleanNotifier ExecutableNotifier { get; } = new BooleanNotifier(true);
    }

    /// <summary>
    /// ReactiveCommand で非同期実行を行うためのクラス。
    /// </summary>
    public class AsyncCommandExecuter : AsyncCommandExecuter<object>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        public AsyncCommandExecuter(Func<object, Task> asyncFunc) : base(asyncFunc)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        public AsyncCommandExecuter(Func<Task> asyncFunc) : this(_ => asyncFunc())
        {
        }
    }
}
