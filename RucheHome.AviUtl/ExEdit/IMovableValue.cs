using System;
using System.ComponentModel;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 移動可能な数値を表すインタフェース。
    /// </summary>
    public interface IMovableValue : INotifyPropertyChanged
    {
        /// <summary>
        /// 定数情報を取得する。
        /// </summary>
        IMovableValueConstants Constants { get; }

        /// <summary>
        /// 開始値を取得または設定する。
        /// </summary>
        decimal Begin { get; set; }

        /// <summary>
        /// 終端値を取得または設定する。
        /// </summary>
        /// <remarks>
        /// 移動モードが MoveMode.None の場合は無視される。
        /// </remarks>
        decimal End { get; set; }

        /// <summary>
        /// 移動モードを取得または設定する。
        /// </summary>
        MoveMode MoveMode { get; set; }

        /// <summary>
        /// 加速を行うか否かを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 移動モードが加減速指定不可ならば無視される。
        /// </remarks>
        bool IsAccelerating { get; set; }

        /// <summary>
        /// 減速を行うか否かを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 移動モードが加減速指定不可ならば無視される。
        /// </remarks>
        bool IsDecelerating { get; set; }

        /// <summary>
        /// 移動フレーム間隔を取得または設定する。
        /// </summary>
        /// <remarks>
        /// 移動モードが移動フレーム間隔設定を持たないならば無視される。
        /// </remarks>
        int Interval { get; set; }
    }
}
