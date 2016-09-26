using System;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 移動可能な数値の定数情報を保持するインタフェース。
    /// </summary>
    public interface IMovableValueConstants
    {
        /// <summary>
        /// 小数点以下の桁数を取得する。
        /// </summary>
        int Digits { get; }

        /// <summary>
        /// 既定値を取得する。
        /// </summary>
        decimal DefaultValue { get; }

        /// <summary>
        /// 最小値を取得する。
        /// </summary>
        decimal MinValue { get; }

        /// <summary>
        /// 最大値を取得する。
        /// </summary>
        decimal MaxValue { get; }

        /// <summary>
        /// AviUtlのスライダーで編集可能な最小値を取得する。
        /// </summary>
        decimal MinSliderValue { get; }

        /// <summary>
        /// AviUtlのスライダーで編集可能な最大値を取得する。
        /// </summary>
        decimal MaxSliderValue { get; }
    }
}
