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
        /// VOICEROID2
        /// </summary>
        [EnumMember]
        Voiceroid2,
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
                    @"結月ゆかり",
                    new[]{ @"結月", @"ゆかり", @"yukari" },
                    @"VOICEROID",
                    @"VOICEROID＋ 結月ゆかり EX"),
                new VoiceroidInfo(
                    VoiceroidId.MakiEx,
                    @"弦巻マキ",
                    new[]{ @"弦巻", @"マキ", @"民安", @"ともえ", @"maki" },
                    @"VOICEROID",
                    @"VOICEROID＋ 民安ともえ EX"),
                new VoiceroidInfo(
                    VoiceroidId.ZunkoEx,
                    @"東北ずん子",
                    new[]{ @"ずん子", @"zunko" },
                    @"VOICEROID",
                    @"VOICEROID＋ 東北ずん子 EX"),
                new VoiceroidInfo(
                    VoiceroidId.KiritanEx,
                    @"東北きりたん",
                    new[]{ @"きりたん", @"kiritan" },
                    @"VOICEROID",
                    @"VOICEROID＋ 東北きりたん",
                    @"VOICEROID＋ 東北きりたん EX"),
                new VoiceroidInfo(
                    VoiceroidId.Akane,
                    @"琴葉茜",
                    new[]{ @"茜", @"akane" },
                    @"VOICEROID",
                    @"VOICEROID＋ 琴葉茜"),
                new VoiceroidInfo(
                    VoiceroidId.Aoi,
                    @"琴葉葵",
                    new[]{ @"葵", @"aoi" },
                    @"VOICEROID",
                    @"VOICEROID＋ 琴葉葵"),
                new VoiceroidInfo(
                    VoiceroidId.AiEx,
                    @"月読アイ",
                    new[]{ @"アイ", @"ai" },
                    @"VOICEROID",
                    @"VOICEROID＋ 月読アイ EX"),
                new VoiceroidInfo(
                    VoiceroidId.ShoutaEx,
                    @"月読ショウタ",
                    new[]{ @"ショウタ", @"shouta" },
                    @"VOICEROID",
                    @"VOICEROID＋ 月読ショウタ EX"),
                new VoiceroidInfo(
                    VoiceroidId.SeikaEx,
                    @"京町セイカ",
                    new[]{ @"京町", @"セイカ", @"seika" },
                    @"VOICEROID",
                    @"VOICEROID＋ 京町セイカ",
                    @"VOICEROID＋ 京町セイカ EX"),
                new VoiceroidInfo(
                    VoiceroidId.KouEx,
                    @"水奈瀬コウ",
                    new[]{ @"水奈瀬", @"コウ", @"kou" },
                    @"VOICEROID",
                    @"VOICEROID＋ 水奈瀬コウ EX"),
                new VoiceroidInfo(
                    VoiceroidId.UnaTalkEx,
                    @"音街ウナ",
                    new[]{ @"音街", @"ウナ", @"una" },
                    @"OtomachiUnaTalkEx",
                    @"音街ウナTalk Ex"),
                new VoiceroidInfo(
                    VoiceroidId.Voiceroid2,
                    @"VOICEROID2",
                    null,
                    @"VoiceroidEditor",
                    @"VOICEROID2 Editor",
                    string.Empty),
            }
            .ToDictionary(info => info.Id);
    }
}
