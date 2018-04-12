using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace VoiceroidUtil.Extensions
{
    /// <summary>
    /// ReactiveProperty 関連の拡張メソッドを提供する静的クラス。
    /// </summary>
    public static class ReactivePropertyExtension
    {
        /// <summary>
        /// IReactiveProperty{T} オブジェクトの内包オブジェクトのプロパティを対象とする
        /// ReactiveProperty{TProperty} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">
        /// 内包オブジェクト。 INotifyPropertyChanged を実装している必要がある。
        /// </typeparam>
        /// <typeparam name="TProperty">内包オブジェクト内プロパティの型。</typeparam>
        /// <param name="self">IReactiveProperty{T} オブジェクト。</param>
        /// <param name="selector">内包オブジェクト内プロパティセレクタ。</param>
        /// <param name="canModifyNotifier">
        /// 値変更可能状態プッシュ通知。 null を指定すると常に可能となる。
        /// </param>
        /// <param name="notifyOnSameValue">
        /// 同値への変更時にも通知を行うならば true 。
        /// </param>
        /// <returns>ReactiveProperty{TProperty} オブジェクト。</returns>
        public static ReactiveProperty<TProperty> MakeInnerReactiveProperty<T, TProperty>(
            this IReactiveProperty<T> self,
            Expression<Func<T, TProperty>> selector,
            IObservable<bool> canModifyNotifier = null,
            bool notifyOnSameValue = false)
            where T : INotifyPropertyChanged
            =>
            MakeInnerReactivePropertyCore(
                self,
                selector,
                canModifyNotifier,
                notifyOnSameValue,
                (setter, value) => setter(self.Value, value));

        /// <summary>
        /// IReadOnlyReactiveProperty{T} オブジェクトの内包オブジェクトのプロパティを
        /// 対象とする ReactiveProperty{TProperty} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">
        /// 内包オブジェクト。 INotifyPropertyChanged を実装している必要がある。
        /// </typeparam>
        /// <typeparam name="TProperty">内包オブジェクト内プロパティの型。</typeparam>
        /// <param name="self">IReadOnlyReactiveProperty{T} オブジェクト。</param>
        /// <param name="selector">内包オブジェクト内プロパティセレクタ。</param>
        /// <param name="canModifyNotifier">
        /// 値変更可能状態プッシュ通知。 null を指定すると常に可能となる。
        /// </param>
        /// <param name="notifyOnSameValue">
        /// 同値への変更時にも通知を行うならば true 。
        /// </param>
        /// <returns>ReactiveProperty{TProperty} オブジェクト。</returns>
        public static ReactiveProperty<TProperty> MakeInnerReactiveProperty<T, TProperty>(
            this IReadOnlyReactiveProperty<T> self,
            Expression<Func<T, TProperty>> selector,
            IObservable<bool> canModifyNotifier = null,
            bool notifyOnSameValue = false)
            where T : INotifyPropertyChanged
            =>
            MakeInnerReactivePropertyCore(
                self,
                selector,
                canModifyNotifier,
                notifyOnSameValue,
                (setter, value) => setter(self.Value, value));

        /// <summary>
        /// MakeInnerReactivePropery 拡張メソッドの実処理を行う。
        /// </summary>
        /// <typeparam name="T">
        /// 内包オブジェクト。 INotifyPropertyChanged を実装している必要がある。
        /// </typeparam>
        /// <typeparam name="TProperty">内包オブジェクト内プロパティの型。</typeparam>
        /// <param name="self">IReactiveProperty{T} オブジェクト。</param>
        /// <param name="selector">内包オブジェクト内プロパティセレクタ。</param>
        /// <param name="canModifyNotifier">
        /// 値変更可能状態プッシュ通知。 null を指定すると常に可能となる。
        /// </param>
        /// <param name="notifyOnSameValue">
        /// 同値への変更時にも通知を行うならば true 。
        /// </param>
        /// <param name="setterExecuter">setter 処理実施デリゲート。</param>
        /// <returns>ReactiveProperty{TProperty} オブジェクト。</returns>
        private static ReactiveProperty<TProperty>
        MakeInnerReactivePropertyCore<T, TProperty>(
            IObservable<T> self,
            Expression<Func<T, TProperty>> selector,
            IObservable<bool> canModifyNotifier,
            bool notifyOnSameValue,
            Action<Action<T, TProperty>, TProperty> setterExecuter)
            where T : INotifyPropertyChanged
        {
            var mode = ReactivePropertyMode.RaiseLatestValueOnSubscribe;
            if (!notifyOnSameValue)
            {
                mode |= ReactivePropertyMode.DistinctUntilChanged;
            }

            var result = self.ObserveInnerProperty(selector).ToReactiveProperty(mode: mode);

            // selector から setter を作成
            var valueExp = Expression.Parameter(selector.Body.Type);
            var setterExp =
                Expression.Lambda<Action<T, TProperty>>(
                    Expression.Assign(selector.Body, valueExp),
                    selector.Parameters[0],
                    valueExp);
            var setter = setterExp.Compile();

            if (canModifyNotifier == null)
            {
                result.Subscribe(value => setterExecuter(setter, value));
            }
            else
            {
                // canModifyNotifier が true を通知したタイミングで値を反映する
                Observable
                    .CombineLatest(
                        result,
                        canModifyNotifier,
                        (value, canModify) => new { value, canModify })
                    .Where(v => v.canModify)
                    .Subscribe(v => setterExecuter(setter, v.value));
            }

            return result;
        }

        /// <summary>
        /// ReactiveCommand{T} オブジェクトの CanExecute メソッドの戻り値を通知する
        /// ReadOnlyReactiveProperty{bool} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="self">ReactiveCommand{T} オブジェクト。</param>
        /// <returns>ReadOnlyReactiveProperty{bool} オブジェクト。</returns>
        public static ReadOnlyReactiveProperty<bool> CanExecuteToReadOnlyReactiveProperty<T>(
            this ReactiveCommand<T> self)
            =>
            CanExecuteToReadOnlyReactivePropertyCore(self, self.CanExecute);

        /// <summary>
        /// AsyncReactiveCommand{T} オブジェクトの CanExecute メソッドの戻り値を通知する
        /// ReadOnlyReactiveProperty{bool} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">コマンドパラメータ型。</typeparam>
        /// <param name="self">ReactiveCommand{T} オブジェクト。</param>
        /// <returns>ReadOnlyReactiveProperty{bool} オブジェクト。</returns>
        public static ReadOnlyReactiveProperty<bool> CanExecuteToReadOnlyReactiveProperty<T>(
            this AsyncReactiveCommand<T> self)
            =>
            CanExecuteToReadOnlyReactivePropertyCore(self, self.CanExecute);

        /// <summary>
        /// CanExecuteToReadOnlyReactiveProperty 拡張メソッドの実処理を行う。
        /// </summary>
        /// <param name="self">コマンドオブジェクト。</param>
        /// <param name="canExecuteGetter">
        /// CanExecute メソッドの戻り値を取得するデリゲート。
        /// </param>
        /// <returns>ReadOnlyReactiveProperty{bool} オブジェクト。</returns>
        private static ReadOnlyReactiveProperty<bool>
        CanExecuteToReadOnlyReactivePropertyCore(
            ICommand self,
            Func<bool> canExecuteGetter)
            =>
            self
                .CanExecuteChangedAsObservable()
                .Select(_ => canExecuteGetter())
                .ToReadOnlyReactiveProperty(canExecuteGetter());
    }
}
