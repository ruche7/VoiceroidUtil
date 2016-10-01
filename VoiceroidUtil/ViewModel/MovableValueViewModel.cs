using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
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
    public class MovableValueViewModel : Livet.ViewModel
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">名前。</param>
        /// <param name="value">ラップ対象値。</param>
        public MovableValueViewModel(string name, IMovableValue value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // 名前
            this.Name =
                (new ReactiveProperty<string>(name)).AddTo(this.CompositeDisposable);
            this.Name
                .Subscribe(
                    n =>
                    {
                        if (n == null)
                        {
                            this.Name.Value = "";
                        }
                    })
                .AddTo(this.CompositeDisposable);

            // ラップ対象値
            this.Value = value;

            // ラップ対象値の各プロパティラッパ群
            this.Begin = this.MakeWrappingProperty(v => v.Begin);
            this.End = this.MakeWrappingProperty(v => v.End);
            this.MoveMode = this.MakeWrappingProperty(v => v.MoveMode);
            this.IsAccelerating = this.MakeWrappingProperty(v => v.IsAccelerating);
            this.IsDecelerating = this.MakeWrappingProperty(v => v.IsDecelerating);
            this.Interval = this.MakeWrappingProperty(v => v.Interval);

            // MoveMode 依存プロパティ群
            this.IsMoving =
                this.MakeMoveModeRelatingProperty(v => v != ExEdit.MoveMode.None);
            this.MoveModeName = this.MakeMoveModeRelatingProperty(v => v.GetName());
            this.CanAccelerate = this.MakeMoveModeRelatingProperty(v => v.CanAccelerate());
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
        /// 定数情報を取得する。
        /// </summary>
        public IMovableValueConstants Constants => this.Value.Constants;

        /// <summary>
        /// 値のフォーマット文字列を取得する。
        /// </summary>
        public string ValueFormatString => @"F" + this.Constants.Digits;

        /// <summary>
        /// 値のインクリメント量を取得する。
        /// </summary>
        public decimal ValueIncrement => 1 / (decimal)Math.Pow(10, this.Constants.Digits);

        /// <summary>
        /// 値の大インクリメント量を取得する。
        /// </summary>
        public decimal ValueLargeIncrement => this.ValueIncrement * 10;

        /// <summary>
        /// 移動モードコレクションを取得する。
        /// </summary>
        public ReadOnlyCollection<MoveMode> MoveModes => TheMoveModes;

        /// <summary>
        /// 名前を取得する。
        /// </summary>
        public ReactiveProperty<string> Name { get; }

        /// <summary>
        /// 開始値を取得する。
        /// </summary>
        public ReactiveProperty<decimal> Begin { get; }

        /// <summary>
        /// 終端値を取得する。
        /// </summary>
        public ReactiveProperty<decimal> End { get; }

        /// <summary>
        /// 移動モードを取得する。
        /// </summary>
        public ReactiveProperty<MoveMode> MoveMode { get; }

        /// <summary>
        /// 移動モードが MoveMode.None 以外であるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsMoving { get; }

        /// <summary>
        /// 移動モード名を取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<string> MoveModeName { get; }

        /// <summary>
        /// 移動モードに対して加減速指定が可能であるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> CanAccelerate { get; }

        /// <summary>
        /// 移動モードが移動フレーム間隔設定を持つか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> HasInterval { get; }

        /// <summary>
        /// 加速を行うか否かを取得する。
        /// </summary>
        public ReactiveProperty<bool> IsAccelerating { get; }

        /// <summary>
        /// 減速を行うか否かを取得する。
        /// </summary>
        public ReactiveProperty<bool> IsDecelerating { get; }

        /// <summary>
        /// 移動フレーム間隔を取得する。
        /// </summary>
        public ReactiveProperty<int> Interval { get; }

        /// <summary>
        /// 移動モードコレクション。
        /// </summary>
        private static ReadOnlyCollection<MoveMode> TheMoveModes =
            new ReadOnlyCollection<MoveMode>(
                Enum.GetValues(typeof(MoveMode)) as MoveMode[]);

        /// <summary>
        /// ラップ対象値を取得する。
        /// </summary>
        private IMovableValue Value { get; }

        /// <summary>
        /// ラップ対象値のプロパティをラップする ReactiveProperty{T} 値を作成する。
        /// </summary>
        /// <typeparam name="T">プロパティ型。</typeparam>
        /// <param name="selector">プロパティセレクタ。</param>
        /// <returns>ReactiveProperty{T} 値。</returns>
        private ReactiveProperty<T> MakeWrappingProperty<T>(
            Expression<Func<IMovableValue, T>> selector)
            =>
            this.Value
                .ToReactivePropertyAsSynchronized(selector)
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

        #region デザイン用定義

        /// <summary>
        /// デザイン用の定数情報クラス。
        /// </summary>
        private class ConstantsForDesign : IMovableValueConstants
        {
            public int Digits => 1;
            public decimal DefaultValue => 0;
            public decimal MinValue => -1000;
            public decimal MaxValue => 1000;
            public decimal MinSliderValue => -256;
            public decimal MaxSliderValue => 256;
        }

        /// <summary>
        /// デザイン用のコンストラクタ。
        /// </summary>
        [Obsolete(@"Can use only design time.", true)]
        public MovableValueViewModel()
            : this(@"Name", new MovableValue<ConstantsForDesign>())
        {
        }

        #endregion
    }
}
