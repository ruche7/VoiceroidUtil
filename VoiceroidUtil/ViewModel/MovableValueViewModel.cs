using System;
using System.Linq;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.AviUtl.ExEdit;
using ExEdit = RucheHome.AviUtl.ExEdit;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// IMovableValue オブジェクトをラップする ViewModel クラス。
    /// </summary>
    public class MovableValueViewModel : ConfigViewModelBase<IMovableValue>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canModify">
        /// 再生や音声保存に関わる設定値の変更可否状態値。
        /// </param>
        /// <param name="value">ラップ対象値。</param>
        /// <param name="name">名前。</param>
        public MovableValueViewModel(
            IReadOnlyReactiveProperty<bool> canModify,
            IReadOnlyReactiveProperty<IMovableValue> value,
            string name)
            : base(canModify, value)
        {
            this.ValidateArgNull(name, nameof(name));

            this.Name = name;

            // ラップ対象値の各プロパティラッパ群
            this.Constants = this.MakeConfigProperty(v => v.Constants);
            this.Begin = this.MakeConfigProperty(v => v.Begin);
            this.End = this.MakeConfigProperty(v => v.End);
            this.MoveMode = this.MakeConfigProperty(v => v.MoveMode);
            this.IsAccelerating = this.MakeConfigProperty(v => v.IsAccelerating);
            this.IsDecelerating = this.MakeConfigProperty(v => v.IsDecelerating);
            this.Interval = this.MakeConfigProperty(v => v.Interval);

            // Constants 依存プロパティ群
            this.ValueFormatString =
                this.MakeConstantsRelatingProperty(v => @"F" + v.Digits);
            this.ValueIncrement =
                this.MakeConstantsRelatingProperty(
                    v => 1 / (decimal)Math.Pow(10, v.Digits));
            this.ValueLargeIncrement =
                this.ValueIncrement
                    .Select(v => v * 10)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // MoveMode 依存プロパティ群
            this.IsMoving =
                this.MakeMoveModeRelatingProperty(v => v != ExEdit.MoveMode.None);
            this.MoveModeName = this.MakeMoveModeRelatingProperty(v => v.GetName());
            this.CanAccelerate =
                this.MakeMoveModeRelatingProperty(v => v.CanAccelerate());
            this.HasInterval = this.MakeMoveModeRelatingProperty(v => v.HasInterval());

            // MoveMode 変更時処理
            this.MoveMode
                .Subscribe(
                    mode =>
                    {
                        // 既定値で上書き
                        this.IsAccelerating.Value = mode.IsDefaultAccelerating();
                        this.IsDecelerating.Value = mode.IsDefaultDecelerating();
                        this.Interval.Value = mode.GetDefaultInterval();
                    })
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// 名前を取得する。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 定数情報を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<IMovableValueConstants> Constants { get; }

        /// <summary>
        /// 値のフォーマット文字列を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<string> ValueFormatString { get; }

        /// <summary>
        /// 値のインクリメント量を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<decimal> ValueIncrement { get; }

        /// <summary>
        /// 値の大インクリメント量を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<decimal> ValueLargeIncrement { get; }

        /// <summary>
        /// 開始値を取得する。
        /// </summary>
        public IReactiveProperty<decimal> Begin { get; }

        /// <summary>
        /// 終端値を取得する。
        /// </summary>
        public IReactiveProperty<decimal> End { get; }

        /// <summary>
        /// 移動モードを取得する。
        /// </summary>
        public IReactiveProperty<MoveMode> MoveMode { get; }

        /// <summary>
        /// 移動モードが MoveMode.None 以外であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsMoving { get; }

        /// <summary>
        /// 移動モード名を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<string> MoveModeName { get; }

        /// <summary>
        /// 移動モードに対して加減速指定が可能であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> CanAccelerate { get; }

        /// <summary>
        /// 移動モードが移動フレーム間隔設定を持つか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> HasInterval { get; }

        /// <summary>
        /// 加速を行うか否かを取得する。
        /// </summary>
        public IReactiveProperty<bool> IsAccelerating { get; }

        /// <summary>
        /// 減速を行うか否かを取得する。
        /// </summary>
        public IReactiveProperty<bool> IsDecelerating { get; }

        /// <summary>
        /// 移動フレーム間隔を取得する。
        /// </summary>
        public IReactiveProperty<int> Interval { get; }

        /// <summary>
        /// Constants 値の変更に連動する ReadOnlyReactiveProperty{T} 値を作成する。
        /// </summary>
        /// <typeparam name="T">プロパティ型。</typeparam>
        /// <param name="selector">値セレクタ。</param>
        /// <returns>ReadOnlyReactiveProperty{T} 値。</returns>
        private ReadOnlyReactiveProperty<T> MakeConstantsRelatingProperty<T>(
            Func<IMovableValueConstants, T> selector)
            =>
            this.Constants
                .Select(selector)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.CompositeDisposable);

        /// <summary>
        /// MoveMode 値の変更に連動する ReadOnlyReactiveProperty{T} 値を作成する。
        /// </summary>
        /// <typeparam name="T">プロパティ型。</typeparam>
        /// <param name="selector">値セレクタ。</param>
        /// <returns>ReadOnlyReactiveProperty{T} 値。</returns>
        private ReadOnlyReactiveProperty<T> MakeMoveModeRelatingProperty<T>(
            Func<MoveMode, T> selector)
            =>
            this.MoveMode
                .Select(selector)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.CompositeDisposable);
    }
}
