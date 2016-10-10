using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// ViewModel のベースクラス。
    /// </summary>
    public abstract class ViewModelBase : Livet.ViewModel
    {
        /// <summary>
        /// プロパティ値を設定し、変更をイベント通知する。
        /// </summary>
        /// <typeparam name="T">プロパティ値の型。</typeparam>
        /// <param name="field">設定先フィールド。</param>
        /// <param name="value">設定値。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                this.RaisePropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// コマンドを作成する。
        /// </summary>
        /// <param name="action">処理デリゲート。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <returns>コマンド。</returns>
        protected ReactiveCommand MakeCommand(
            Action action,
            IObservable<bool> executable = null)
            =>
            (ReactiveCommand)this.MakeCommandCore(
                (action == null) ? (Action<object>)null : (_ => action()),
                executable,
                e => e.ToReactiveCommand());

        /// <summary>
        /// コマンドを作成する。
        /// </summary>
        /// <param name="action">処理デリゲート。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <returns>コマンド。</returns>
        protected ReactiveCommand MakeCommand(
            Action<object> action,
            IObservable<bool> executable = null)
            =>
            (ReactiveCommand)this.MakeCommandCore(
                action,
                executable,
                e => e.ToReactiveCommand());

        /// <summary>
        /// コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="action">処理デリゲート。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <returns>コマンド。</returns>
        protected ReactiveCommand<T> MakeCommand<T>(
            Action<T> action,
            IObservable<bool> executable = null)
            =>
            this.MakeCommandCore(
                action,
                executable,
                e => e.ToReactiveCommand<T>());

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ReactiveCommand MakeAsyncCommand(
            Func<Task> asyncFunc,
            IObservable<bool> executable = null)
            =>
            (ReactiveCommand)this.MakeAsyncCommandCore(
                (new AsyncCommandExecuter(asyncFunc)).AddTo(this.CompositeDisposable),
                executable,
                e => e.ToReactiveCommand(false));

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ReactiveCommand MakeAsyncCommand(
            Func<object, Task> asyncFunc,
            IObservable<bool> executable = null)
            =>
            (ReactiveCommand)this.MakeAsyncCommandCore(
                (new AsyncCommandExecuter(asyncFunc)).AddTo(this.CompositeDisposable),
                executable,
                e => e.ToReactiveCommand(false));

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="asyncFunc">非同期処理デリゲート。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ReactiveCommand<T> MakeAsyncCommand<T>(
            Func<T, Task> asyncFunc,
            IObservable<bool> executable = null)
            =>
            this.MakeAsyncCommandCore(
                (new AsyncCommandExecuter<T>(asyncFunc)).AddTo(this.CompositeDisposable),
                executable,
                e => e.ToReactiveCommand<T>(false));

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <param name="asyncExecuter">非同期処理ヘルパー。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ReactiveCommand MakeAsyncCommand(
            AsyncCommandExecuter asyncExecuter,
            IObservable<bool> executable = null)
            =>
            (ReactiveCommand)this.MakeAsyncCommandCore(
                asyncExecuter,
                executable,
                e => e.ToReactiveCommand(false));

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="asyncExecuter">非同期処理ヘルパー。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ReactiveCommand<T> MakeAsyncCommand<T>(
            AsyncCommandExecuter<T> asyncExecuter,
            IObservable<bool> executable = null)
            =>
            this.MakeAsyncCommandCore(
                asyncExecuter,
                executable,
                e => e.ToReactiveCommand<T>(false));

        /// <summary>
        /// コマンド作成の実処理を行う。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="action">処理デリゲート。</param>
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <param name="commandMaker">コマンド作成デリゲート。</param>
        /// <returns>コマンド。</returns>
        private ReactiveCommand<T> MakeCommandCore<T>(
            Action<T> action,
            IObservable<bool> executable,
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

            var commandExecutable = executable ?? Observable.Return(true);

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
        /// <param name="executable">実行可能状態プッシュ通知。</param>
        /// <param name="commandMaker">コマンド作成デリゲート。</param>
        /// <returns>非同期実行コマンド。</returns>
        private ReactiveCommand<T> MakeAsyncCommandCore<T>(
            AsyncCommandExecuter<T> asyncExecuter,
            IObservable<bool> executable,
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
                (executable == null) ?
                    asyncExecuter.ObserveExecutable() :
                    new[]
                    {
                        asyncExecuter.ObserveExecutable(),
                        executable,
                    }
                    .CombineLatestValuesAreAllTrue();

            var command =
                commandMaker(commandExecutable).AddTo(this.CompositeDisposable);

            command.Subscribe(asyncExecuter.Execute).AddTo(this.CompositeDisposable);

            return command;
        }
    }
}
