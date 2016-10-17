using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;

namespace VoiceroidUtil.Extensions
{
    /// <summary>
    /// IObservable{T} ジェネリックインタフェースの拡張メソッドを提供する静的クラス。
    /// </summary>
    public static class ObservableExtension
    {
        /// <summary>
        /// IObservable{T} オブジェクトのプッシュ通知対象オブジェクトに対して
        /// ObserveProperty 拡張メソッドを呼び出す。
        /// </summary>
        /// <typeparam name="T">
        /// プッシュ通知対象オブジェクト。
        /// INotifyPropertyChanged を実装している必要がある。
        /// </typeparam>
        /// <typeparam name="TProperty">
        /// プッシュ通知対象オブジェクト内プロパティの型。
        /// </typeparam>
        /// <param name="self">IObservable{T} オブジェクト。</param>
        /// <param name="selector">
        /// プッシュ通知対象オブジェクト内プロパティセレクタ。
        /// </param>
        /// <returns></returns>
        public static IObservable<TProperty> ObserveInnerProperty<T, TProperty>(
            this IObservable<T> self,
            Expression<Func<T, TProperty>> selector)
            where T : INotifyPropertyChanged
            =>
            self.Select(o => o.ObserveProperty(selector)).Switch();
    }
}
