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
        /// ギャラ子Talk
        /// </summary>
        [EnumMember]
        GalacoTalk,

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
        /// 桜乃そら(VOICEROID2専用)
        /// </summary>
        [EnumMember]
        V2Sora,

        /// <summary>
        /// 東北イタコ(VOICEROID2専用)
        /// </summary>
        [EnumMember]
        V2Itako,

        /// <summary>
        /// ついなちゃん 関西弁(VOICEROID2専用)
        /// </summary>
        [EnumMember]
        V2Tsuina,

        /// <summary>
        /// ついなちゃん 標準語(VOICEROID2専用)
        /// </summary>
        [EnumMember]
        V2TsuinaStandard,

        /// <summary>
        /// 伊織弓鶴(VOICEROID2専用)
        /// </summary>
        [EnumMember]
        V2Iori,

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
        /// flower(ガイノイドTalk専用)
        /// </summary>
        [EnumMember]
        GTFlower,

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
        
        /// <summary>
        /// A.I.VOICE
        /// </summary>
        [EnumMember]
        AiVoice,
    }

    /// <summary>
    /// VoiceroidId 列挙の拡張メソッドを提供する静的クラス。
    /// </summary>
    public static class VoiceroidIdExtension
    {
        /// <summary>
        /// VOICEROID識別IDがVOICEROID2ライクなソフトウェアを示しているか否かを取得する。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <returns>
        /// VOICEROID2ライクなソフトウェアを示しているならば true 。
        /// そうでなければ false 。
        /// </returns>
        /// <remarks>
        /// VoiceroidId.V2Akari 等、ソフトウェア内キャラクターを表すIDは含めない。
        /// </remarks>
        public static bool IsVoiceroid2LikeSoftware(this VoiceroidId id) =>
            (id == VoiceroidId.Voiceroid2 || id == VoiceroidId.GynoidTalk || id == VoiceroidId.AiVoice);

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
                    new[]{ @"東北", @"ずん子" },
                    @"VOICEROID",
                    @"VOICEROID＋ 東北ずん子 EX"),
                new VoiceroidInfo(
                    VoiceroidId.KiritanEx,
                    true,
                    @"東北きりたん",
                    new[]{ @"東北", @"きりたん" },
                    @"VOICEROID",
                    @"VOICEROID＋ 東北きりたん",
                    displayProduct: @"VOICEROID＋ 東北きりたん EX"),
                new VoiceroidInfo(
                    VoiceroidId.Akane,
                    true,
                    @"琴葉茜",
                    new[]{ @"琴葉", @"茜", @"関西弁" },
                    @"VOICEROID",
                    @"VOICEROID＋ 琴葉茜"),
                new VoiceroidInfo(
                    VoiceroidId.Aoi,
                    true,
                    @"琴葉葵",
                    new[]{ @"琴葉", @"葵" },
                    @"VOICEROID",
                    @"VOICEROID＋ 琴葉葵"),
                new VoiceroidInfo(
                    VoiceroidId.AiEx,
                    true,
                    @"月読アイ",
                    new[]{ @"月読", @"アイ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 月読アイ EX"),
                new VoiceroidInfo(
                    VoiceroidId.ShoutaEx,
                    true,
                    @"月読ショウタ",
                    new[]{ @"月読", @"ショウタ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 月読ショウタ EX"),
                new VoiceroidInfo(
                    VoiceroidId.SeikaEx,
                    true,
                    @"京町セイカ",
                    new[]{ @"京町", @"セイカ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 京町セイカ",
                    displayProduct: @"VOICEROID＋ 京町セイカ EX"),
                new VoiceroidInfo(
                    VoiceroidId.KouEx,
                    true,
                    @"水奈瀬コウ",
                    new[]{ @"水奈瀬", @"コウ" },
                    @"VOICEROID",
                    @"VOICEROID＋ 水奈瀬コウ EX"),
                new VoiceroidInfo(
                    VoiceroidId.GalacoTalk,
                    true,
                    @"ギャラ子",
                    new[]{ @"ギャラ子" },
                    @"galacoTalk",
                    @"ギャラ子Talk"),
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
                    VoiceroidId.V2Sora,
                    false,
                    @"桜乃そら",
                    new[]{ @"桜乃", @"そら" },
                    @"VOICEROID2 Editor",
                    @"VOICEROID2 桜乃そら"),
                new VoiceroidInfo(
                    VoiceroidId.V2Itako,
                    false,
                    @"東北イタコ",
                    new[]{ @"東北", @"イタコ" },
                    @"VOICEROID2 Editor",
                    @"VOICEROID2 東北イタコ"),
                new VoiceroidInfo(
                    VoiceroidId.V2Tsuina,
                    false,
                    @"ついなちゃん(関西弁)",
                    new[]{ @"ついな", @"追儺", @"関西弁" },
                    @"VOICEROID2 Editor",
                    @"VOICEROID2 ついなちゃん",
                    shortName: @"ついな関西弁"),
                new VoiceroidInfo(
                    VoiceroidId.V2TsuinaStandard,
                    false,
                    @"ついなちゃん(標準語)",
                    new[]{ @"ついな", @"如月", @"標準語" },
                    @"VOICEROID2 Editor",
                    @"VOICEROID2 ついなちゃん",
                    shortName: @"ついな標準語"),
                new VoiceroidInfo(
                    VoiceroidId.V2Iori,
                    false,
                    @"伊織弓鶴",
                    new[]{ @"伊織", @"弓鶴" },
                    @"VOICEROID2 Editor",
                    @"VOICEROID2 伊織弓鶴"),
                new VoiceroidInfo(
                    VoiceroidId.GTHime,
                    false,
                    @"鳴花ヒメ",
                    new[]{ @"鳴花", @"ヒメ" },
                    @"GynoidTalkEditor",
                    @"ガイノイドTalk 鳴花ヒメ"),
                new VoiceroidInfo(
                    VoiceroidId.GTMikoto,
                    false,
                    @"鳴花ミコト",
                    new[]{ @"鳴花", @"ミコト" },
                    @"GynoidTalkEditor",
                    @"ガイノイドTalk 鳴花ミコト"),
                new VoiceroidInfo(
                    VoiceroidId.GTFlower,
                    false,
                    @"flower(フラワ)",
                    new[]{ @"flower", @"フラワ" },
                    @"GynoidTalkEditor",
                    @"ガイノイドTalk flower",
                    shortName: @"flower"),
                new VoiceroidInfo(
                    VoiceroidId.Voiceroid2,
                    true,
                    @"VOICEROID2",
                    null,
                    @"VoiceroidEditor",
                    @"VOICEROID2 Editor",
                    displayProduct: @"VOICEROID2",
                    multiCharacters: true),
                new VoiceroidInfo(
                    VoiceroidId.GynoidTalk,
                    true,
                    @"ガイノイドTalk",
                    null,
                    @"GynoidTalkEditor",
                    @"GynoidTalk Editor",
                    displayProduct: @"ガイノイドTalk",
                    multiCharacters: true),
                new VoiceroidInfo(
                    VoiceroidId.AiVoice,
                    true,
                    @"A.I.VOICE",
                    null,
                    @"AIVoiceEditor",
                    @"A.I.VOICE Editor",
                    displayProduct: @"A.I.VOICE",
                    multiCharacters: true),
            }
            .ToDictionary(info => info.Id);
    }
}
