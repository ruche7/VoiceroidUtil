using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace VoiceroidUtil
{
    /// <summary>
    /// ファイル名フォーマット種別列挙。
    /// </summary>
    [DataContract(Namespace = "")]
    public enum FileNameFormat
    {
        [Display(Name = @"入力文")]
        [EnumMember]
        Text,

        [Display(Name = @"キャラ名_入力文")]
        [EnumMember]
        NameText,

        [Display(Name = @"短縮キャラ名_入力文")]
        [EnumMember]
        ShortNameText,

        [Display(Name = @"日時_入力文")]
        [EnumMember]
        DateTimeText,

        [Display(Name = @"日時_キャラ名_入力文")]
        [EnumMember]
        DateTimeNameText,

        [Display(Name = @"日時_短縮キャラ名_入力文")]
        [EnumMember]
        DateTimeShortNameText,

        [Display(Name = @"キャラ名\入力文")]
        [EnumMember]
        TextInNameDirectory,

        [Display(Name = @"短縮キャラ名\入力文")]
        [EnumMember]
        TextInShortNameDirectory,

        [Display(Name = @"キャラ名\日時_入力文")]
        [EnumMember]
        DateTimeTextInNameDirectory,

        [Display(Name = @"短縮キャラ名\日時_入力文")]
        [EnumMember]
        DateTimeTextInShortNameDirectory,
    }
}
