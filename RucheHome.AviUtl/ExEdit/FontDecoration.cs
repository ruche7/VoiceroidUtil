using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// フォント装飾種別列挙。
    /// </summary>
    [DataContract(Namespace = "")]
    public enum FontDecoration
    {
        /// <summary>
        /// 装飾なしの標準文字。
        /// </summary>
        [Display(Name = @"標準文字")]
        [EnumMember]
        None = 0,

        /// <summary>
        /// 影付き文字。
        /// </summary>
        [Display(Name = @"影付き文字")]
        [EnumMember]
        Shadow = 1,

        /// <summary>
        /// 薄い影付き文字。
        /// </summary>
        [Display(Name = @"影付き文字(薄)")]
        [EnumMember]
        ThinShadow = 2,

        /// <summary>
        /// 縁取り文字。
        /// </summary>
        [Display(Name = @"縁取り文字")]
        [EnumMember]
        Edge = 3,

        /// <summary>
        /// 細い縁取り文字。
        /// </summary>
        [Display(Name = @"縁取り文字(細)")]
        [EnumMember]
        ThinEdge = 4,
    }
}
