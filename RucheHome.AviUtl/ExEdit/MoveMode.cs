using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// MovableValueBase 派生クラスオブジェクトの移動モード列挙。
    /// </summary>
    [DataContract(Namespace = "")]
    public enum MoveMode
    {
        /// <summary>
        /// 移動無し。
        /// </summary>
        [Display(Name = @"移動無し")]
        [EnumMember]
        None,

        /// <summary>
        /// 直線移動。
        /// </summary>
        [Display(Name = @"直線移動")]
        [EnumMember]
        Linear,

        /// <summary>
        /// 加減速移動。
        /// </summary>
        [Display(Name = @"加減速移動")]
        [EnumMember]
        Acceleration,

        /// <summary>
        /// 曲線移動。
        /// </summary>
        [Display(Name = @"曲線移動")]
        [EnumMember]
        Curve,

        /// <summary>
        /// 瞬間移動。
        /// </summary>
        [Display(Name = @"瞬間移動")]
        [EnumMember]
        Teleportation,

        /// <summary>
        /// 中間点無視。
        /// </summary>
        [Display(Name = @"中間点無視")]
        [EnumMember]
        IgnoreMidpoint,

        /// <summary>
        /// 移動量指定。
        /// </summary>
        [Display(Name = @"移動量指定")]
        [EnumMember]
        Amount,

        /// <summary>
        /// ランダム移動。
        /// </summary>
        [Display(Name = @"ランダム移動")]
        [EnumMember]
        Random,

        /// <summary>
        /// 反復移動。
        /// </summary>
        [Display(Name = @"反復移動")]
        [EnumMember]
        Repeat,

        /// <summary>
        /// 補間移動。
        /// </summary>
        [Display(Name = @"補間移動")]
        [EnumMember]
        Interpolation,

        /// <summary>
        /// 回転。
        /// </summary>
        [Display(Name = @"回転")]
        [EnumMember]
        Rotation,
    }

    /// <summary>
    /// MoveMode 列挙の拡張メソッドを提供する静的クラス。
    /// </summary>
    public static class MoveModeExtension
    {
        /// <summary>
        /// 移動モードID値を取得する。
        /// </summary>
        /// <param name="moveMode">移動モード。</param>
        /// <returns>移動モードID値。移動モードが不正な場合は -1 。</returns>
        public static int GetId(this MoveMode moveMode)
        {
            switch (moveMode)
            {
            case MoveMode.None: return 0;
            case MoveMode.Linear: return 1;
            case MoveMode.Acceleration: return 7;
            case MoveMode.Curve: return 2;
            case MoveMode.Teleportation: return 3;
            case MoveMode.IgnoreMidpoint: return 4;
            case MoveMode.Amount: return 5;
            case MoveMode.Random: return 6;
            case MoveMode.Repeat: return 8;

            case MoveMode.Interpolation:
            case MoveMode.Rotation:
                // 補間移動と回転のIDは同値
                return 15;
            }

            return -1;
        }

        /// <summary>
        /// 移動モードの追加ID文字列を取得する。
        /// </summary>
        /// <param name="moveMode">移動モード。</param>
        /// <returns>
        /// 追加ID文字列。追加不要ならば空文字列。移動モードが不正な場合は null 。
        /// </returns>
        public static string GetExtraId(this MoveMode moveMode)
        {
            switch (moveMode)
            {
            case MoveMode.Interpolation:
            case MoveMode.Rotation:
                return ('@' + moveMode.GetName());
            }

            return Enum.IsDefined(moveMode.GetType(), moveMode) ? "" : null;
        }

        /// <summary>
        /// 移動モード名を取得する。
        /// </summary>
        /// <param name="moveMode">移動モード。</param>
        /// <returns>移動モード名。移動モードが不正な場合は null 。</returns>
        public static string GetName(this MoveMode moveMode)
        {
            // 列挙値のメタデータ取得
            var info = moveMode.GetType().GetField(moveMode.ToString());
            if (info == null)
            {
                return null;
            }

            // DisplayAttribute 属性が無いなら列挙値名をそのまま返す
            return
                info.GetCustomAttribute<DisplayAttribute>(false)?.GetName() ??
                moveMode.ToString();
        }

        /// <summary>
        /// 移動モードに対して加減速指定が可能であるか否かを取得する。
        /// </summary>
        /// <param name="moveMode">移動モード。</param>
        /// <returns>加減速指定が可能ならば true 。そうでなければ false 。</returns>
        public static bool CanAccelerate(this MoveMode moveMode)
        {
            switch (moveMode)
            {
            case MoveMode.Linear:
            case MoveMode.Acceleration:
            case MoveMode.Curve:
            case MoveMode.IgnoreMidpoint:
            case MoveMode.Repeat:
            case MoveMode.Interpolation:
                return true;
            }

            return false;
        }

        /// <summary>
        /// 加速を行うか否かの既定値を取得する。
        /// </summary>
        /// <param name="moveMode">移動モード。</param>
        /// <returns>既定で加速を行うならば true 。そうでなければ false 。</returns>
        public static bool IsDefaultAccelerating(this MoveMode moveMode) =>
            moveMode == MoveMode.Acceleration;

        /// <summary>
        /// 減速を行うか否かの既定値を取得する。
        /// </summary>
        /// <param name="moveMode">移動モード。</param>
        /// <returns>既定で減速を行うならば true 。そうでなければ false 。</returns>
        public static bool IsDefaultDecelerating(this MoveMode moveMode) =>
            moveMode == MoveMode.Acceleration;

        /// <summary>
        /// 移動モードが移動フレーム間隔設定を持つか否かを取得する。
        /// </summary>
        /// <param name="moveMode">移動モード。</param>
        /// <returns>
        /// 移動フレーム間隔設定を持つならば true 。そうでなければ false 。
        /// </returns>
        public static bool HasInterval(this MoveMode moveMode)
        {
            switch (moveMode)
            {
            case MoveMode.Random:
            case MoveMode.Repeat:
            case MoveMode.Rotation:
                return true;
            }

            return false;
        }

        /// <summary>
        /// 移動フレーム間隔の既定値を取得する。
        /// </summary>
        /// <param name="moveMode">移動モード。</param>
        /// <returns>移動フレーム間隔の既定値。</returns>
        public static int GetDefaultInterval(this MoveMode moveMode) =>
            (moveMode == MoveMode.Rotation) ? 100 : 0;
    }
}
