using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RucheHome.Voiceroid
{
    /// <summary>
    /// VOICEROID識別ID列挙。
    /// </summary>
    [DataContract(Namespace = "")]
    public enum VoiceroidId
    {
        /// <summary>
        /// 結月ゆかり EX
        /// </summary>
        [EnumMember]
        YukariEx,

        /// <summary>
        /// 民安ともえ(弦巻マキ) EX
        /// </summary>
        [EnumMember]
        MakiEx,

        /// <summary>
        /// 東北ずん子 EX
        /// </summary>
        [EnumMember]
        ZunkoEx,

        /// <summary>
        /// 東北きりたん EX
        /// </summary>
        [EnumMember]
        KiritanEx,

        /// <summary>
        /// 琴葉茜
        /// </summary>
        [EnumMember]
        Akane,

        /// <summary>
        /// 琴葉葵
        /// </summary>
        [EnumMember]
        Aoi,

        /// <summary>
        /// 月読アイ EX
        /// </summary>
        [EnumMember]
        AiEx,

        /// <summary>
        /// 月読ショウタ EX
        /// </summary>
        [EnumMember]
        ShoutaEx,

        /// <summary>
        /// 京町セイカ EX
        /// </summary>
        [EnumMember]
        SeikaEx,

        /// <summary>
        /// 水奈瀬コウ EX
        /// </summary>
        [EnumMember]
        KouEx,

        /// <summary>
        /// 音街ウナTalk Ex
        /// </summary>
        [EnumMember]
        UnaTalkEx,

        /// <summary>
        /// 紲星あかり(VOICEROID2専用)
        /// </summary>
        [EnumMember]
        V2Akari,

        /// <summary>
        /// 鳴花ヒメ(ガイノイドTalk専用)
        /// </summary>
        [EnumMember]
        GTHime,

        /// <summary>
        /// 鳴花ミコト(ガイノイドTalk専用)
        /// </summary>
        [EnumMember]
        GTMikoto,

        /// <summary>
        /// VOICEROID2
        /// </summary>
        [EnumMember]
        Voiceroid2,

        /// <summary>
        /// ガイノイドTalk
        /// </summary>
        [EnumMember]
        GynoidTalk,
    }

    /// <summary>
    /// VoiceroidId 列挙の拡張メソッドを提供する静的クラス。
    /// </summary>
    public static class VoiceroidIdExtension
    {
        /// <summary>
        /// VOICEROID識別IDに紐付く情報を取得する。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <returns>VOICEROID情報。</returns>
        public static VoiceroidInfo GetInfo(this VoiceroidId id) =>
            Infos.TryGetValue(id, out var info) ? info : null;

        /// <summary>
        /// VoiceroidInfo テーブル。
        /// </summary>
        private static readonly Dictionary<VoiceroidId, VoiceroidInfo> Infos =
            new[]
            {
                new VoiceroidInfo(
                    VoiceroidId.YukariEx,
                    true,
                    @"結月ゆかり",
                    new[]{ @"結月", @"ゆかり" },
                    @"VOICEROID",
                    @"VOICEROID＋ 結月ゆかり EX"),
                new VoiceroidInfo(
                    VoiceroidId.MakiEx,
                    true,
                    @"弦巻マキ",
                    new[]{ @"弦巻", @"マキ", @"民安", @"ともえ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 民安ともえ EX"),
                new VoiceroidInfo(
                    VoiceroidId.ZunkoEx,
                    true,
                    @"東北ずん子",
                    new[]{ @"ずん子" },
                    @"VOICEROID",
                    @"VOICEROID＋ 東北ずん子 EX"),
                new VoiceroidInfo(
                    VoiceroidId.KiritanEx,
                    true,
                    @"東北きりたん",
                    new[]{ @"きりたん" },
                    @"VOICEROID",
                    @"VOICEROID＋ 東北きりたん",
                    @"VOICEROID＋ 東北きりたん EX"),
                new VoiceroidInfo(
                    VoiceroidId.Akane,
                    true,
                    @"琴葉茜",
                    new[]{ @"茜" },
                    @"VOICEROID",
                    @"VOICEROID＋ 琴葉茜"),
                new VoiceroidInfo(
                    VoiceroidId.Aoi,
                    true,
                    @"琴葉葵",
                    new[]{ @"葵" },
                    @"VOICEROID",
                    @"VOICEROID＋ 琴葉葵"),
                new VoiceroidInfo(
                    VoiceroidId.AiEx,
                    true,
                    @"月読アイ",
                    new[]{ @"アイ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 月読アイ EX"),
                new VoiceroidInfo(
                    VoiceroidId.ShoutaEx,
                    true,
                    @"月読ショウタ",
                    new[]{ @"ショウタ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 月読ショウタ EX"),
                new VoiceroidInfo(
                    VoiceroidId.SeikaEx,
                    true,
                    @"京町セイカ",
                    new[]{ @"京町", @"セイカ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 京町セイカ",
                    @"VOICEROID＋ 京町セイカ EX"),
                new VoiceroidInfo(
                    VoiceroidId.KouEx,
                    true,
                    @"水奈瀬コウ",
                    new[]{ @"水奈瀬", @"コウ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 水奈瀬コウ EX"),
                new VoiceroidInfo(
                    VoiceroidId.UnaTalkEx,
                    true,
                    @"音街ウナ",
                    new[]{ @"音街", @"ウナ" },
                    @"OtomachiUnaTalkEx",
                    @"音街ウナTalk Ex"),
                new VoiceroidInfo(
                    VoiceroidId.V2Akari,
                    false,
                    @"紲星あかり",
                    new[]{ @"紲星", @"あかり" },
                    @"VOICEROID2 Editor",
                    @"VOICEROID2 紲星あかり"),
                new VoiceroidInfo(
                    VoiceroidId.GTHime,
                    false,
                    @"鳴花ヒメ",
                    new[]{ @"ヒメ" },
                    @"GynoidTalkEditor",
                    @"ガイノイドTalk 鳴花ヒメ"),
                new VoiceroidInfo(
                    VoiceroidId.GTMikoto,
                    false,
                    @"鳴花ミコト",
                    new[]{ @"ミコト" },
                    @"GynoidTalkEditor",
                    @"ガイノイドTalk 鳴花ミコト"),
                new VoiceroidInfo(
                    VoiceroidId.Voiceroid2,
                    true,
                    @"VOICEROID2",
                    null,
                    @"VoiceroidEditor",
                    @"VOICEROID2 Editor",
                    @"VOICEROID2"),
                new VoiceroidInfo(
                    VoiceroidId.GynoidTalk,
                    true,
                    @"ガイノイドTalk",
                    null,
                    @"GynoidTalkEditor",
                    @"GynoidTalk Editor",
                    @"ガイノイドTalk"),
            }
            .ToDictionary(info => info.Id);
    }
}
