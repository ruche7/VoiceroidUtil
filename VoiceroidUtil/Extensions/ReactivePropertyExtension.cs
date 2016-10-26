using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Reactive.Bindings;

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
        /// <returns>ReactiveProperty{TProperty} オブジェクト。</returns>
        public static ReactiveProperty<TProperty> MakeInnerReactiveProperty<T, TProperty>(
            this IReactiveProperty<T> self,
            Expression<Func<T, TProperty>> selector,
            IObservable<bool> canModifyNotifier = null)
            where T : INotifyPropertyChanged
            =>
            MakeInnerReactivePropertyCore(
                self,
                selector,
                canModifyNotifier,
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
        /// <returns>ReactiveProperty{TProperty} オブジェクト。</returns>
        public static ReactiveProperty<TProperty> MakeInnerReactiveProperty<T, TProperty>(
            this IReadOnlyReactiveProperty<T> self,
            Expression<Func<T, TProperty>> selector,
            IObservable<bool> canModifyNotifier = null)
            where T : INotifyPropertyChanged
            =>
            MakeInnerReactivePropertyCore(
                self,
                selector,
                canModifyNotifier,
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
        /// <param name="setterExecuter">setter 処理実施デリゲート。</param>
        /// <returns>ReactiveProperty{TProperty} オブジェクト。</returns>
        private static ReactiveProperty<TProperty>
        MakeInnerReactivePropertyCore<T, TProperty>(
            IObservable<T> self,
            Expression<Func<T, TProperty>> selector,
            IObservable<bool> canModifyNotifier,
            Action<Action<T, TProperty>, TProperty> setterExecuter)
            where T : INotifyPropertyChanged
        {
            var result = self.ObserveInnerProperty(selector).ToReactiveProperty();

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
    }
}
