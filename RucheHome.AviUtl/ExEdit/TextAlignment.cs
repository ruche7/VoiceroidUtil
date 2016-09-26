using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// テキスト配置種別列挙。
    /// </summary>
    [DataContract(Namespace = "")]
    public enum TextAlignment
    {
        /// <summary>
        /// 左上。
        /// </summary>
        [Display(Name = @"左寄せ[上]")]
        [EnumMember]
        TopLeft = 0,

        /// <summary>
        /// 中央上。
        /// </summary>
        [Display(Name = @"中央揃え[上]")]
        [EnumMember]
        TopCenter = 1,

        /// <summary>
        /// 右上。
        /// </summary>
        [Display(Name = @"右寄せ[上]")]
        [EnumMember]
        TopRight = 2,

        /// <summary>
        /// 左中央。
        /// </summary>
        [Display(Name = @"左寄せ[中]")]
        [EnumMember]
        MiddleLeft = 3,

        /// <summary>
        /// 中央。
        /// </summary>
        [Display(Name = @"中央揃え[中]")]
        [EnumMember]
        MiddleCenter = 4,

        /// <summary>
        /// 右中央。
        /// </summary>
        [Display(Name = @"右寄せ[中]")]
        [EnumMember]
        MiddleRight = 5,

        /// <summary>
        /// 左下。
        /// </summary>
        [Display(Name = @"左寄せ[下]")]
        [EnumMember]
        BottomLeft = 6,

        /// <summary>
        /// 中央下。
        /// </summary>
        [Display(Name = @"中央揃え[下]")]
        [EnumMember]
        BottomCenter = 7,

        /// <summary>
        /// 右下。
        /// </summary>
        [Display(Name = @"右寄せ[下]")]
        [EnumMember]
        BottomRight = 8,

        /// <summary>
        /// 縦書き右上。
        /// </summary>
        [Display(Name = @"縦書 上寄[右]")]
        [EnumMember]
        VerticalRightTop = 9,

        /// <summary>
        /// 縦書き右中央。
        /// </summary>
        [Display(Name = @"縦書 中央[右]")]
        [EnumMember]
        VerticalRightMiddle = 10,

        /// <summary>
        /// 縦書き右下。
        /// </summary>
        [Display(Name = @"縦書 下寄[右]")]
        [EnumMember]
        VerticalRightBottom = 11,

        /// <summary>
        /// 縦書き中央上。
        /// </summary>
        [Display(Name = @"縦書 上寄[中]")]
        [EnumMember]
        VerticalCenterTop = 12,

        /// <summary>
        /// 縦書き中央。
        /// </summary>
        [Display(Name = @"縦書 中央[中]")]
        [EnumMember]
        VerticalCenterMiddle = 13,

        /// <summary>
        /// 縦書き中央下。
        /// </summary>
        [Display(Name = @"縦書 下寄[中]")]
        [EnumMember]
        VerticalCenterBottom = 14,

        /// <summary>
        /// 縦書き左上。
        /// </summary>
        [Display(Name = @"縦書 上寄[左]")]
        [EnumMember]
        VerticalLeftTop = 15,

        /// <summary>
        /// 縦書き左中央。
        /// </summary>
        [Display(Name = @"縦書 中央[左]")]
        [EnumMember]
        VerticalLeftMiddle = 16,

        /// <summary>
        /// 縦書き左下。
        /// </summary>
        [Display(Name = @"縦書 下寄[左]")]
        [EnumMember]
        VerticalLeftBottom = 17,
    }
}
