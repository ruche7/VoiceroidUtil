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
        /// 琴葉茜
        /// </summary>
        [EnumMember]
        Akane,

        /// <summary>
        /// 琴葉葵
        /// </summary>
        [EnumMember]
        Aoi,
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
        public static VoiceroidInfo GetInfo(this VoiceroidId id)
        {
            VoiceroidInfo info = null;
            return Infos.TryGetValue(id, out info) ? info : null;
        }

        /// <summary>
        /// VoiceroidInfo テーブル。
        /// </summary>
        private static readonly Dictionary<VoiceroidId, VoiceroidInfo> Infos =
            new[]
            {
                new VoiceroidInfo(
                    VoiceroidId.YukariEx,
                    @"VOICEROID＋ 結月ゆかり EX",
                    @"結月ゆかり"),
                new VoiceroidInfo(
                    VoiceroidId.MakiEx,
                    @"VOICEROID＋ 民安ともえ EX",
                    @"弦巻マキ"),
                new VoiceroidInfo(
                    VoiceroidId.ZunkoEx,
                    @"VOICEROID＋ 東北ずん子 EX",
                    @"東北ずん子"),
                new VoiceroidInfo(
                    VoiceroidId.Akane,
                    @"VOICEROID＋ 琴葉茜",
                    @"琴葉茜"),
                new VoiceroidInfo(
                    VoiceroidId.Aoi,
                    @"VOICEROID＋ 琴葉葵",
                    @"琴葉葵"),
            }
            .ToDictionary(info => info.Id);
    }
}
