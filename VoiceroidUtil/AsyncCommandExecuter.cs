﻿using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Reactive.Bindings;

namespace VoiceroidUtil
{
    /// <summary>
    /// ReactiveCommand で非同期実行を行うためのジェネリッククラス。
    /// </summary>
    /// <typeparam name="T">コマンドパラメータ型。</typeparam>
    public class AsyncCommandExecuter<T>
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
        }

        /// <summary>
        /// 非同期実行可能な状態であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsExecutable => this.IsExecutableCore;

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
            if (
                this.IsExecutableCore.Value &&
                Interlocked.Exchange(ref this.executeLock, 1) == 0)
            {
                try
                {
                    this.IsExecutableCore.Value = false;
                    await this.AsyncFunc(parameter);
                }
                finally
                {
                    Interlocked.Exchange(ref this.executeLock, 0);
                    this.IsExecutableCore.Value = true;
                }
            }
        }
        private int executeLock = 0;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <remarks>
        /// 派生クラスで AsyncFunc を設定する場合に利用する。
        /// </remarks>
        protected AsyncCommandExecuter()
        {
        }

        /// <summary>
        /// 非同期処理デリゲートを取得または設定する。
        /// </summary>
        protected Func<T, Task> AsyncFunc { get; set; } = null;

        /// <summary>
        /// 非同期実行可能な状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// 自己完結型なので Dispose は不要。
        /// </remarks>
        private ReactiveProperty<bool> IsExecutableCore { get; } =
            new ReactiveProperty<bool>(true);
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

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <remarks>
        /// 派生クラスで AsyncFunc を設定する場合に利用する。
        /// </remarks>
        protected AsyncCommandExecuter() : base()
        {
        }
    }
}
