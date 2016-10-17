﻿using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// ViewModel のベースクラス。
    /// </summary>
    public abstract class ViewModelBase : BindableBase, IDisposable
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ViewModelBase()
        {
        }

        /// <summary>
        /// IDisposable.Dispose をまとめて呼び出すためのコンテナを取得する。
        /// </summary>
        protected CompositeDisposable CompositeDisposable { get; } =
            new CompositeDisposable();

        /// <summary>
        /// 引数値の null チェックを行う。
        /// </summary>
        /// <typeparam name="T">引数値の型。</typeparam>
        /// <param name="arg">引数値。</param>
        /// <param name="argName">引数名。</param>
        protected void ValidateArgNull<T>(T arg, string argName)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        /// <summary>
        /// コマンドを作成する。
        /// </summary>
        /// <param name="action">処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>コマンド。</returns>
        protected ICommand MakeCommand(
            Action action,
            params IObservable<bool>[] executables)
            =>
            this.MakeCommandCore(
                (action == null) ? (Action<object>)null : (_ => action()),
                executables,
                e => e.ToReactiveCommand());

        /// <summary>
        /// コマンドを作成する。
        /// </summary>
        /// <param name="action">処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>コマンド。</returns>
        protected ICommand MakeCommand(
            Action<object> action,
            params IObservable<bool>[] executables)
            =>
            this.MakeCommandCore(
                action,
                executables,
                e => e.ToReactiveCommand());

        /// <summary>
        /// コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="action">処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>コマンド。</returns>
        protected ICommand MakeCommand<T>(
            Action<T> action,
            params IObservable<bool>[] executables)
            =>
            this.MakeCommandCore(
                action,
                executables,
                e => e.ToReactiveCommand<T>());

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeAsyncCommand(
            Func<Task> asyncFunc,
            params IObservable<bool>[] executables)
            =>
            this.MakeAsyncCommandCore(
                new AsyncCommandExecuter(asyncFunc),
                executables,
                e => e.ToReactiveCommand(false));

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeAsyncCommand(
            Func<object, Task> asyncFunc,
            params IObservable<bool>[] executables)
            =>
            this.MakeAsyncCommandCore(
                new AsyncCommandExecuter(asyncFunc),
                executables,
                e => e.ToReactiveCommand(false));

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeAsyncCommand<T>(
            Func<T, Task> asyncFunc,
            params IObservable<bool>[] executables)
            =>
            this.MakeAsyncCommandCore(
                new AsyncCommandExecuter<T>(asyncFunc),
                executables,
                e => e.ToReactiveCommand<T>(false));

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <param name="asyncExecuter">非同期処理ヘルパー。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeAsyncCommand(
            AsyncCommandExecuter asyncExecuter,
            params IObservable<bool>[] executables)
            =>
            this.MakeAsyncCommandCore(
                asyncExecuter,
                executables,
                e => e.ToReactiveCommand(false));

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="asyncExecuter">非同期処理ヘルパー。</param>
        /// <param name="executables">実行可能状態プッシュ通知。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeAsyncCommand<T>(
            AsyncCommandExecuter<T> asyncExecuter,
            params IObservable<bool>[] executables)
            =>
            this.MakeAsyncCommandCore(
                asyncExecuter,
                executables,
                e => e.ToReactiveCommand<T>(false));

        /// <summary>
        /// コマンド作成の実処理を行う。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="action">処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <param name="commandMaker">コマンド作成デリゲート。</param>
        /// <returns>コマンド。</returns>
        private ICommand MakeCommandCore<T>(
            Action<T> action,
            IObservable<bool>[] executables,
            Func<IObservable<bool>, ReactiveCommand<T>> commandMaker)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            if (commandMaker == null)
            {
                throw new ArgumentNullException(nameof(commandMaker));
            }

            var commandExecutable =
                (executables == null || executables.Length <= 0) ?
                    Observable.Return(true) :
                    executables.CombineLatestValuesAreAllTrue();

            var command =
                commandMaker(commandExecutable).AddTo(this.CompositeDisposable);

            command.Subscribe(action).AddTo(this.CompositeDisposable);

            return command;
        }

        /// <summary>
        /// 非同期実行コマンド作成の実処理を行う。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="asyncExecuter">非同期実行ヘルパー。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <param name="commandMaker">コマンド作成デリゲート。</param>
        /// <returns>非同期実行コマンド。</returns>
        private ICommand MakeAsyncCommandCore<T>(
            AsyncCommandExecuter<T> asyncExecuter,
            IObservable<bool>[] executables,
            Func<IObservable<bool>, ReactiveCommand<T>> commandMaker)
        {
            if (asyncExecuter == null)
            {
                throw new ArgumentNullException(nameof(asyncExecuter));
            }
            if (commandMaker == null)
            {
                throw new ArgumentNullException(nameof(commandMaker));
            }

            var commandExecutable =
                (executables == null || executables.Length <= 0) ?
                    asyncExecuter.IsExecutable :
                    new[]
                    {
                        asyncExecuter.IsExecutable,
                        executables.CombineLatestValuesAreAllTrue(),
                    }
                    .CombineLatestValuesAreAllTrue();

            var command =
                commandMaker(commandExecutable).AddTo(this.CompositeDisposable);

            command.Subscribe(asyncExecuter.Execute).AddTo(this.CompositeDisposable);

            return command;
        }

        #region IDisposable の実装

        /// <summary>
        /// リソースを破棄する。
        /// </summary>
        public void Dispose()
        {
            this.CompositeDisposable.Dispose();
        }

        #endregion
    }
}
