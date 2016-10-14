using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 合成モード列挙。
    /// </summary>
    [DataContract(Namespace = "")]
    public enum BlendMode
    {
        /// <summary>
        /// 通常。
        /// </summary>
        [Display(Name = @"通常")]
        [EnumMember]
        Normal = 0,

        /// <summary>
        /// 加算。
        /// </summary>
        [Display(Name = @"加算")]
        [EnumMember]
        Add = 1,

        /// <summary>
        /// 減算。
        /// </summary>
        [Display(Name = @"減算")]
        [EnumMember]
        Subtract = 2,

        /// <summary>
        /// 乗算。
        /// </summary>
        [Display(Name = @"乗算")]
        [EnumMember]
        Multiply = 3,

        /// <summary>
        /// スクリーン。
        /// </summary>
        [Display(Name = @"スクリーン")]
        [EnumMember]
        Screen = 4,

        /// <summary>
        /// オーバーレイ。
        /// </summary>
        [Display(Name = @"オーバーレイ")]
        [EnumMember]
        Overlay = 5,

        /// <summary>
        /// 比較(明)。
        /// </summary>
        [Display(Name = @"比較(明)")]
        [EnumMember]
        Lighten = 6,

        /// <summary>
        /// 比較(暗)。
        /// </summary>
        [Display(Name = @"比較(暗)")]
        [EnumMember]
        Darken = 7,

        /// <summary>
        /// 輝度。
        /// </summary>
        [Display(Name = @"輝度")]
        [EnumMember]
        Luminosity = 8,

        /// <summary>
        /// 色差。
        /// </summary>
        [Display(Name = @"色差")]
        [EnumMember]
        ColorDifference = 9,

        /// <summary>
        /// 陰影。
        /// </summary>
        [Display(Name = @"陰影")]
        [EnumMember]
        LinearBurn = 10,

        /// <summary>
        /// 明暗。
        /// </summary>
        [Display(Name = @"明暗")]
        [EnumMember]
        LinearLight = 11,

        /// <summary>
        /// 差分。
        /// </summary>
        [Display(Name = @"差分")]
        [EnumMember]
        Difference = 12,
    }
}
