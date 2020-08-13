using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Reactive.Bindings;
using VoiceroidUtil.Extensions;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// 設定値を保持する ViewModel のベースクラス。
    /// </summary>
    /// <typeparam name="TConfig">設定値の型。</typeparam>
    public abstract class ConfigViewModelBase<TConfig> : ViewModelBase
        where TConfig : INotifyPropertyChanged
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canModify">
        /// 再生や音声保存に関わる設定値の変更可否状態値。
        /// </param>
        /// <param name="config">設定値。</param>
        protected ConfigViewModelBase(
            IReadOnlyReactiveProperty<bool> canModify,
            IReadOnlyReactiveProperty<TConfig> config)
        {
            ValidateArgumentNull(canModify, nameof(canModify));
            ValidateArgumentNull(config, nameof(config));

            this.CanModify = canModify;
            this.BaseConfig = config;
        }

        /// <summary>
        /// 再生や音声保存に関わる設定値を変更できる状態か否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> CanModify { get; }

        /// <summary>
        /// 設定値を取得する。
        /// </summary>
        protected IReadOnlyReactiveProperty<TConfig> BaseConfig { get; }

        /// <summary>
        /// 設定値のプロパティのプッシュ通知オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">プロパティ型。</typeparam>
        /// <param name="selector">プロパティセレクタ。</param>
        /// <returns>プッシュ通知オブジェクト。</returns>
        protected IObservable<T> ObserveConfigProperty<T>(
            Expression<Func<TConfig, T>> selector)
            =>
            this.BaseConfig.ObserveInnerProperty(selector);

        /// <summary>
        /// 設定値のプロパティをラップする ReactiveProperty{T} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">プロパティ型。</typeparam>
        /// <param name="selector">プロパティセレクタ。</param>
        /// <param name="alwaysCanModify">
        /// CanModify プロパティ値に依らず値変更可能ならば true 。
        /// </param>
        /// <param name="notifyOnSameValue">
        /// 同値への変更時にも通知を行うならば true 。
        /// </param>
        /// <returns>ReactiveProperty{T} オブジェクト。</returns>
        protected ReactiveProperty<T> MakeConfigProperty<T>(
            Expression<Func<TConfig, T>> selector,
            bool alwaysCanModify = false,
            bool notifyOnSameValue = false)
            =>
            this.MakeInnerPropertyOf(
                this.BaseConfig,
                selector,
                alwaysCanModify ? null : this.CanModify,
                notifyOnSameValue);

        /// <summary>
        /// 設定値のプロパティをラップする
        /// ReadOnlyReactiveProperty{T} オブジェクトを作成する。
        /// </summary>
        /// <typeparam name="T">プロパティ型。</typeparam>
        /// <param name="selector">プロパティセレクタ。</param>
        /// <param name="notifyOnSameValue">
        /// 同値への変更時にも通知を行うならば true 。
        /// </param>
        /// <returns>ReadOnlyReactiveProperty{T} オブジェクト。</returns>
        protected ReadOnlyReactiveProperty<T> MakeReadOnlyConfigProperty<T>(
            Expression<Func<TConfig, T>> selector,
            bool notifyOnSameValue = false)
            =>
            this.MakeInnerReadOnlyPropertyOf(
                this.BaseConfig,
                selector,
                notifyOnSameValue);
    }
}
