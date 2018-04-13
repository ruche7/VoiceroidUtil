using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;
using VoiceroidUtil.Extensions;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// ViewModel のベースクラス。
    /// </summary>
    public abstract class ViewModelBase : IDisposable
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
        /// IReactiveProperty{T} オブジェクトの内包オブジェクトのプロパティを対象とする
        /// ReactiveProperty{TProperty} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">
        /// 内包オブジェクト。 INotifyPropertyChanged を実装している必要がある。
        /// </typeparam>
        /// <typeparam name="TProperty">内包オブジェクト内プロパティの型。</typeparam>
        /// <param name="owner">IReactiveProperty{T} オブジェクト。</param>
        /// <param name="selector">内包オブジェクト内プロパティセレクタ。</param>
        /// <param name="canModifyNotifier">
        /// 値変更可能状態プッシュ通知。 null を指定すると常に可能となる。
        /// </param>
        /// <param name="notifyOnSameValue">
        /// 同値への変更時にも通知を行うならば true 。
        /// </param>
        /// <returns>ReactiveProperty{TProperty} オブジェクト。</returns>
        protected ReactiveProperty<TProperty> MakeInnerPropertyOf<T, TProperty>(
            IReactiveProperty<T> owner,
            Expression<Func<T, TProperty>> selector,
            IReadOnlyReactiveProperty<bool> canModifyNotifier = null,
            bool notifyOnSameValue = false)
            where T : INotifyPropertyChanged
            =>
            owner
                .MakeInnerReactiveProperty(selector, canModifyNotifier, notifyOnSameValue)
                .AddTo(this.CompositeDisposable);

        /// <summary>
        /// IReadOnlyReactiveProperty{T} オブジェクトの内包オブジェクトのプロパティを
        /// 対象とする ReactiveProperty{TProperty} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">
        /// 内包オブジェクト。 INotifyPropertyChanged を実装している必要がある。
        /// </typeparam>
        /// <typeparam name="TProperty">内包オブジェクト内プロパティの型。</typeparam>
        /// <param name="owner">IReadOnlyReactiveProperty{T} オブジェクト。</param>
        /// <param name="selector">内包オブジェクト内プロパティセレクタ。</param>
        /// <param name="canModifyNotifier">
        /// 値変更可能状態プッシュ通知。 null を指定すると常に可能となる。
        /// </param>
        /// <param name="notifyOnSameValue">
        /// 同値への変更時にも通知を行うならば true 。
        /// </param>
        /// <returns>ReactiveProperty{TProperty} オブジェクト。</returns>
        protected ReactiveProperty<TProperty> MakeInnerPropertyOf<T, TProperty>(
            IReadOnlyReactiveProperty<T> owner,
            Expression<Func<T, TProperty>> selector,
            IReadOnlyReactiveProperty<bool> canModifyNotifier = null,
            bool notifyOnSameValue = false)
            where T : INotifyPropertyChanged
            =>
            owner
                .MakeInnerReactiveProperty(selector, canModifyNotifier, notifyOnSameValue)
                .AddTo(this.CompositeDisposable);

        /// <summary>
        /// IObservable{T} オブジェクトの内包オブジェクトのプロパティを対象とする
        /// ReadOnlyReactiveProperty{TProperty} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">
        /// 内包オブジェクト。 INotifyPropertyChanged を実装している必要がある。
        /// </typeparam>
        /// <typeparam name="TProperty">内包オブジェクト内プロパティの型。</typeparam>
        /// <param name="owner">IObservable{T} オブジェクト。</param>
        /// <param name="selector">内包オブジェクト内プロパティセレクタ。</param>
        /// <param name="notifyOnSameValue">
        /// 同値への変更時にも通知を行うならば true 。
        /// </param>
        /// <returns>ReadOnlyReactiveProperty{TProperty} オブジェクト。</returns>
        protected ReadOnlyReactiveProperty<TProperty>
        MakeInnerReadOnlyPropertyOf<T, TProperty>(
            IObservable<T> owner,
            Expression<Func<T, TProperty>> selector,
            bool notifyOnSameValue = false)
            where T : INotifyPropertyChanged
            =>
            owner
                .MakeInnerReadOnlyReactiveProperty(selector, notifyOnSameValue)
                .AddTo(this.CompositeDisposable);

        /// <summary>
        /// コマンドを作成する。
        /// </summary>
        /// <param name="executer">処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>コマンド。</returns>
        protected ICommand MakeCommand(
            Action executer,
            params IObservable<bool>[] executables)
            =>
            this.MakeCommandCore(
                (executer == null) ? (Action<object>)null : (_ => executer()),
                executables,
                e => e.ToReactiveCommand());

        /// <summary>
        /// コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="executer">処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>コマンド。</returns>
        protected ICommand MakeCommand<T>(
            Action<T> executer,
            params IObservable<bool>[] executables)
            =>
            this.MakeCommandCore(
                executer,
                executables,
                e => e.ToReactiveCommand<T>());

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <param name="executer">非同期処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeAsyncCommand(
            Func<Task> executer,
            params IObservable<bool>[] executables)
            =>
            this.MakeAsyncCommandCore(
                (executer == null) ? (Func<object, Task>)null : (_ => executer()),
                executables,
                e => e.ToAsyncReactiveCommand());

        /// <summary>
        /// 非同期実行コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="executer">非同期処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeAsyncCommand<T>(
            Func<T, Task> executer,
            params IObservable<bool>[] executables)
            =>
            this.MakeAsyncCommandCore(
                executer,
                executables,
                e => e.ToAsyncReactiveCommand<T>());

        /// <summary>
        /// 実施可能状態を共有する非同期実行コマンドを作成する。
        /// </summary>
        /// <param name="sharedExecutable">実行可能状態共有オブジェクト。</param>
        /// <param name="executer">非同期処理デリゲート。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeSharedAsyncCommand(
            IReactiveProperty<bool> sharedExecutable,
            Func<Task> executer)
        {
            ValidateArgumentNull(sharedExecutable, nameof(sharedExecutable));

            return
                this.MakeAsyncCommandCore(
                    (executer == null) ? (Func<object, Task>)null : (_ => executer()),
                    null,
                    _ => sharedExecutable.ToAsyncReactiveCommand());
        }

        /// <summary>
        /// 実施可能状態を共有する非同期実行コマンドを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="sharedExecutable">実行可能状態共有オブジェクト。</param>
        /// <param name="executer">非同期処理デリゲート。</param>
        /// <returns>非同期実行コマンド。</returns>
        protected ICommand MakeSharedAsyncCommand<T>(
            IReactiveProperty<bool> sharedExecutable,
            Func<T, Task> executer)
        {
            ValidateArgumentNull(sharedExecutable, nameof(sharedExecutable));

            return
                this.MakeAsyncCommandCore(
                    executer,
                    null,
                    _ => sharedExecutable.ToAsyncReactiveCommand<T>());
        }

        /// <summary>
        /// コマンド作成の実処理を行う。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="executer">処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <param name="commandMaker">コマンド作成デリゲート。</param>
        /// <returns>コマンド。</returns>
        private ICommand MakeCommandCore<T>(
            Action<T> executer,
            IObservable<bool>[] executables,
            Func<IObservable<bool>, ReactiveCommand<T>> commandMaker)
        {
            ValidateArgumentNull(executer, nameof(executer));
            ValidateArgumentNull(commandMaker, nameof(commandMaker));

            var commandExecutable =
                (executables == null || executables.Length <= 0) ?
                    Observable.Return(true) :
                    executables
                        .CombineLatestValuesAreAllTrue()
                        .DistinctUntilChanged()
                        .ObserveOnUIDispatcher();
            var command = commandMaker(commandExecutable).AddTo(this.CompositeDisposable);

            command.Subscribe(executer).AddTo(this.CompositeDisposable);

            return command;
        }

        /// <summary>
        /// 非同期実行コマンド作成の実処理を行う。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="executer">非同期処理デリゲート。</param>
        /// <param name="executables">実行可能状態プッシュ通知配列。</param>
        /// <param name="commandMaker">コマンド作成デリゲート。</param>
        /// <returns>コマンド。</returns>
        private ICommand MakeAsyncCommandCore<T>(
            Func<T, Task> executer,
            IObservable<bool>[] executables,
            Func<IObservable<bool>, AsyncReactiveCommand<T>> commandMaker)
        {
            ValidateArgumentNull(executer, nameof(executer));
            ValidateArgumentNull(commandMaker, nameof(commandMaker));

            var commandExecutable =
                (executables == null || executables.Length <= 0) ?
                    Observable.Return(true) :
                    executables
                        .CombineLatestValuesAreAllTrue()
                        .DistinctUntilChanged()
                        .ObserveOnUIDispatcher();
            var command = commandMaker(commandExecutable).AddTo(this.CompositeDisposable);

            command.Subscribe(executer).AddTo(this.CompositeDisposable);

            return command;
        }

        #region IDisposable の実装

        /// <summary>
        /// リソースを破棄する。
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                this.CompositeDisposable.Dispose();
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
            }
        }

        #endregion
    }
}
